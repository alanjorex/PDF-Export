using DigiminePdfExport.Models;
using DigiminePdfExport.Services;

// ═══════════════════════════════════════════════════════════════════════
//  Builder
// ═══════════════════════════════════════════════════════════════════════
var builder = WebApplication.CreateBuilder(args);

// Force Kestrel to listen on port 5000 (HTTP only for this POC)
builder.WebHost.UseUrls("http://localhost:5000");

// Swagger / OpenAPI
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen(c =>
{
    c.SwaggerDoc("v1", new Microsoft.OpenApi.Models.OpenApiInfo
    {
        Title = "Digimine PDF Export POC",
        Version = "v1",
        Description = "Headless-Chromium PDF/HTML export + EPPlus Excel generation"
    });
});

// CORS for local React FE and tools
builder.Services.AddCors(options =>
{
    options.AddDefaultPolicy(policy =>
    {
        policy.AllowAnyOrigin()
              .AllowAnyMethod()
              .AllowAnyHeader();
    });
});

// Services
builder.Services.AddSingleton<PlaywrightExportService>();
builder.Services.AddSingleton<ExcelExportService>();

var app = builder.Build();

// ═══════════════════════════════════════════════════════════════════════
//  Middleware
// ═══════════════════════════════════════════════════════════════════════
app.UseCors();
app.UseSwagger();
app.UseSwaggerUI(c =>
{
    c.SwaggerEndpoint("/swagger/v1/swagger.json", "Digimine PDF Export POC v1");
    c.RoutePrefix = string.Empty;  // Swagger UI at root "/"
});

// ═══════════════════════════════════════════════════════════════════════
//  Endpoints
// ═══════════════════════════════════════════════════════════════════════

// 1) GET /mock-digimine-report — serve the simulated report page
app.MapGet("/mock-digimine-report", () =>
    Results.Content(MockReportHtml.Build(), "text/html"))
    .WithName("MockDigimineReport")
    .WithTags("Mock Report")
    .ExcludeFromDescription();    // hide from Swagger (it's a page, not an API)

// 2) POST /api/export — Single unified endpoint for PDF, Excel (.xlsx), and HTML
app.MapPost("/api/export", async (
    ExportRequest req,
    PlaywrightExportService playwrightSvc,
    ExcelExportService excelSvc,
    CancellationToken ct) =>
{
    var format = (req.Format ?? "Pdf").Trim();
    var name = string.IsNullOrWhiteSpace(req.ReportName) ? "Daily_Mining_Report" : req.ReportName;
    var tz   = string.IsNullOrWhiteSpace(req.SiteTimeZoneId) ? "UTC" : req.SiteTimeZoneId;

    if (format.Equals("Excel", StringComparison.OrdinalIgnoreCase) ||
        format.Equals("Xlsx", StringComparison.OrdinalIgnoreCase))
    {
        if (!string.IsNullOrWhiteSpace(req.Url))
        {
            var widgets = await playwrightSvc.ExtractWidgetsForExcelAsync(req.Url, req.WaitForReadyFlag, ct);
            var result = excelSvc.GenerateFromWidgets(widgets, name, tz);
            return Results.File(result.FileBytes, result.ContentType, result.FileName);
        }
        else if (!string.IsNullOrWhiteSpace(req.Html))
        {
            var result = excelSvc.GenerateFromHtml(req.Html, name, tz);
            return Results.File(result.FileBytes, result.ContentType, result.FileName);
        }
        else
        {
            return Results.BadRequest(new { error = "Either 'html' or 'url' must be provided for Excel export." });
        }
    }
    else if (format.Equals("Pdf", StringComparison.OrdinalIgnoreCase) ||
             format.Equals("Html", StringComparison.OrdinalIgnoreCase))
    {
        if (string.IsNullOrWhiteSpace(req.Url) && string.IsNullOrWhiteSpace(req.Html))
            return Results.BadRequest(new { error = "Either 'html' or 'url' must be provided for export." });

        var result = await playwrightSvc.ExportAsync(req, ct);
        return Results.File(result.FileBytes, result.ContentType, result.FileName);
    }
    else
    {
        return Results.BadRequest(new { error = $"Unsupported format '{req.Format}'. Supported formats are: 'Pdf', 'Excel', 'Html'." });
    }
})
.WithName("Export")
.WithTags("Export")
.Produces(200)
.Produces(400);



// ═══════════════════════════════════════════════════════════════════════
//  Graceful shutdown: dispose Playwright
// ═══════════════════════════════════════════════════════════════════════
var lifetime = app.Services.GetRequiredService<IHostApplicationLifetime>();
lifetime.ApplicationStopping.Register(() =>
{
    var pw = app.Services.GetRequiredService<PlaywrightExportService>();
    pw.DisposeAsync().AsTask().GetAwaiter().GetResult();
});

app.Run();
