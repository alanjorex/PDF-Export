using Microsoft.Playwright;
using DigiminePdfExport.Models;

namespace DigiminePdfExport.Services;

/// <summary>
/// Singleton service that owns the Playwright browser lifetime and performs
/// PDF / offline-HTML exports via headless Chromium.
/// </summary>
public sealed class PlaywrightExportService : IAsyncDisposable
{
    private IPlaywright? _playwright;
    private IBrowser? _browser;
    private readonly SemaphoreSlim _gate = new(4, 4);           // max 4 concurrent pages
    private readonly SemaphoreSlim _initLock = new(1, 1);       // lazy one-time init
    private readonly ILogger<PlaywrightExportService> _logger;

    public PlaywrightExportService(ILogger<PlaywrightExportService> logger) => _logger = logger;

    // ──────────────────────────────────────────────────────────────────
    //  Lazy browser init (runs once)
    // ──────────────────────────────────────────────────────────────────
    private async Task<IBrowser> GetBrowserAsync()
    {
        if (_browser is not null) return _browser;

        await _initLock.WaitAsync();
        try
        {
            if (_browser is not null) return _browser;

            _logger.LogInformation("Launching Playwright Chromium …");
            _playwright = await Playwright.CreateAsync();
            _browser = await _playwright.Chromium.LaunchAsync(new BrowserTypeLaunchOptions
            {
                Headless = true
            });
            _logger.LogInformation("Chromium started (PID {Pid}).", _browser.Contexts.Count);
        }
        finally { _initLock.Release(); }

        return _browser;
    }

    // ──────────────────────────────────────────────────────────────────
    //  Main export entry-point
    // ──────────────────────────────────────────────────────────────────
    public async Task<ExportResult> ExportAsync(ExportRequest req, CancellationToken ct = default)
    {
        await _gate.WaitAsync(ct);
        try
        {
            var browser = await GetBrowserAsync();

            // Resolve the site time-zone (fall back to UTC on bad id)
            TimeZoneInfo tz;
            try { tz = TimeZoneInfo.FindSystemTimeZoneById(req.SiteTimeZoneId); }
            catch { tz = TimeZoneInfo.Utc; }

            // Isolated context with viewport + TZ
            await using var context = await browser.NewContextAsync(new BrowserNewContextOptions
            {
                ViewportSize = new ViewportSize { Width = 1440, Height = 900 },
                TimezoneId = req.SiteTimeZoneId
            });

            var page = await context.NewPageAsync();

            if (!string.IsNullOrWhiteSpace(req.Url))
            {
                _logger.LogInformation("Navigating to {Url}", req.Url);
                await page.GotoAsync(req.Url, new PageGotoOptions
                {
                    WaitUntil = WaitUntilState.NetworkIdle,
                    Timeout = 60_000
                });
            }
            else if (!string.IsNullOrWhiteSpace(req.Html))
            {
                _logger.LogInformation("Loading HTML content directly ({Length} chars)", req.Html.Length);
                await page.SetContentAsync(req.Html, new PageSetContentOptions
                {
                    WaitUntil = WaitUntilState.Load,
                    Timeout = 60_000
                });
            }
            else
            {
                throw new ArgumentException("Either 'url' or 'html' must be provided in ExportRequest.");
            }

            // ── Wait strategy ──
            if (req.WaitForReadyFlag && !string.IsNullOrWhiteSpace(req.Url))
            {
                _logger.LogInformation("Waiting for window.__REPORT_EXPORT_READY__ …");
                await page.WaitForFunctionAsync(
                    "() => window.__REPORT_EXPORT_READY__ === true",
                    null,
                    new PageWaitForFunctionOptions { Timeout = 30_000 });
            }
            else
            {
                // Give client-side charts/images time to settle
                await page.WaitForTimeoutAsync(800);

                // Inject @media print CSS so blocks don't split awkwardly
                await page.AddStyleTagAsync(new PageAddStyleTagOptions
                {
                    Content = @"
                        @media print {
                            img, svg, figure, table, pre, blockquote, .chart-container, .report-widget {
                                break-inside: avoid;
                                page-break-inside: avoid;
                            }
                        }"
                });
            }


            // ── Format-specific export ──
            var isPdf = req.Format.Equals("Pdf", StringComparison.OrdinalIgnoreCase);
            var siteNow = TimeZoneInfo.ConvertTimeFromUtc(DateTime.UtcNow, tz);
            var stamp = siteNow.ToString("yyyy-MM-dd_HHmm");
            var ext = isPdf ? "pdf" : "html";
            var fileName = $"{SanitizeFileName(req.ReportName)}_{stamp}.{ext}";

            byte[] fileBytes;

            if (isPdf)
            {
                fileBytes = await page.PdfAsync(new PagePdfOptions
                {
                    Format = "A4",
                    Landscape = true,
                    Scale = 0.8f,
                    PrintBackground = true,
                    DisplayHeaderFooter = true,
                    HeaderTemplate = "<span></span>",
                    FooterTemplate = @"
                        <div style='width:100%; font-size:9px; color:#666;
                                    padding:0 40px; display:flex; justify-content:space-between;'>
                            <span>Generated from Digimine</span>
                            <span>Page <span class='pageNumber'></span> of <span class='totalPages'></span></span>
                        </div>"
                });
            }
            else
            {
                // ── Build a self-contained offline HTML ──
                // 1) Convert every <canvas> to a static <img>
                await page.EvaluateAsync(@"() => {
                    document.querySelectorAll('canvas').forEach(c => {
                        try {
                            const img = document.createElement('img');
                            img.src = c.toDataURL('image/png');
                            img.style.cssText = c.style.cssText;
                            img.width  = c.width;
                            img.height = c.height;
                            c.replaceWith(img);
                        } catch(_) {}
                    });
                }");

                // 2) Inline all accessible CSS rules
                await page.EvaluateAsync(@"() => {
                    let css = '';
                    for (const sheet of document.styleSheets) {
                        try {
                            for (const rule of sheet.cssRules) {
                                css += rule.cssText + '\n';
                            }
                        } catch(_) {}
                    }
                    const style = document.createElement('style');
                    style.textContent = css;
                    document.head.appendChild(style);
                    // Remove original <link rel=stylesheet> to make it self-contained
                    document.querySelectorAll('link[rel=""stylesheet""]').forEach(l => l.remove());
                }");

                // 3) Strip all <script> tags
                await page.EvaluateAsync(@"() => {
                    document.querySelectorAll('script').forEach(s => s.remove());
                }");

                var html = await page.ContentAsync();
                fileBytes = System.Text.Encoding.UTF8.GetBytes(html);
            }

            return new ExportResult
            {
                FileBytes = fileBytes,
                ContentType = isPdf ? "application/pdf" : "text/html",
                FileName = fileName
            };
        }
        finally { _gate.Release(); }
    }

    // ──────────────────────────────────────────────────────────────────
    //  Helpers
    // ──────────────────────────────────────────────────────────────────
    public async Task<List<ExtractedWidget>> ExtractWidgetsForExcelAsync(string url, bool waitForReadyFlag, CancellationToken ct = default)
    {
        await _gate.WaitAsync(ct);
        try
        {
            var browser = await GetBrowserAsync();
            await using var context = await browser.NewContextAsync(new BrowserNewContextOptions
            {
                ViewportSize = new ViewportSize { Width = 1440, Height = 900 }
            });
            var page = await context.NewPageAsync();
            await page.GotoAsync(url, new PageGotoOptions
            {
                WaitUntil = WaitUntilState.NetworkIdle,
                Timeout = 60_000
            });

            if (waitForReadyFlag)
            {
                await page.WaitForFunctionAsync(
                    "() => window.__REPORT_EXPORT_READY__ === true",
                    null,
                    new PageWaitForFunctionOptions { Timeout = 30_000 });
            }
            else
            {
                await page.WaitForTimeoutAsync(1200);
            }

            var result = new List<ExtractedWidget>();

            // 1) Query top-level widgets: prioritize [data-widget], fallback to grid/widget-item containers
            var widgetNodes = await page.QuerySelectorAllAsync("[data-widget]");
            if (widgetNodes == null || widgetNodes.Count == 0)
            {
                widgetNodes = await page.QuerySelectorAllAsync(".react-grid-item, .widget-item, section.report-widget, [id^='widget-']");
            }

            if (widgetNodes != null && widgetNodes.Count > 0)
            {
                int index = 1;
                foreach (var node in widgetNodes)
                {
                    var rawName = await node.GetAttributeAsync("data-widget");
                    if (string.IsNullOrWhiteSpace(rawName))
                        rawName = await node.GetAttributeAsync("data-title");

                    if (string.IsNullOrWhiteSpace(rawName))
                    {
                        var titleEl = await node.QuerySelectorAsync("h1, h2, h3, h4, [class*='title'], [id*='popover-trigger'] span");
                        if (titleEl != null)
                        {
                            var text = await titleEl.InnerTextAsync();
                            if (!string.IsNullOrWhiteSpace(text)) rawName = text.Trim();
                        }
                    }

                    if (string.IsNullOrWhiteSpace(rawName))
                    {
                        rawName = $"Widget {index}";
                    }

                    // Check if widget contains a table
                    var tableEl = await node.QuerySelectorAsync("table");
                    if (tableEl != null)
                    {
                        var tableHtml = await tableEl.EvaluateAsync<string>("el => el.outerHTML");
                        result.Add(new ExtractedWidget
                        {
                            Name = rawName,
                            Type = WidgetType.Table,
                            TableHtml = tableHtml
                        });
                    }
                    else
                    {
                        // Non-table / chart widget: capture high-res element screenshot
                        await node.ScrollIntoViewIfNeededAsync();
                        var pngBytes = await node.ScreenshotAsync(new()
                        {
                            Type = ScreenshotType.Png,
                            Scale = ScreenshotScale.Device
                        });

                        result.Add(new ExtractedWidget
                        {
                            Name = rawName,
                            Type = WidgetType.Image,
                            ImageBytes = pngBytes
                        });
                    }

                    index++;
                }
            }

            return result;
        }
        finally { _gate.Release(); }
    }

    public async Task<string> GetRenderedHtmlAsync(string url, bool waitForReadyFlag, CancellationToken ct = default)
    {
        await _gate.WaitAsync(ct);
        try
        {
            var browser = await GetBrowserAsync();
            await using var context = await browser.NewContextAsync(new BrowserNewContextOptions
            {
                ViewportSize = new ViewportSize { Width = 1440, Height = 900 }
            });
            var page = await context.NewPageAsync();
            await page.GotoAsync(url, new PageGotoOptions
            {
                WaitUntil = WaitUntilState.NetworkIdle,
                Timeout = 60_000
            });

            if (waitForReadyFlag)
            {
                await page.WaitForFunctionAsync(
                    "() => window.__REPORT_EXPORT_READY__ === true",
                    null,
                    new PageWaitForFunctionOptions { Timeout = 30_000 });
            }
            else
            {
                await page.WaitForTimeoutAsync(1200);
            }

            return await page.ContentAsync();
        }
        finally { _gate.Release(); }
    }

    private static string SanitizeFileName(string name)
    {
        foreach (var c in Path.GetInvalidFileNameChars())
            name = name.Replace(c, '_');
        return name;
    }

    public async ValueTask DisposeAsync()
    {
        if (_browser is not null) await _browser.DisposeAsync();
        _playwright?.Dispose();
        _gate.Dispose();
        _initLock.Dispose();
    }
}
