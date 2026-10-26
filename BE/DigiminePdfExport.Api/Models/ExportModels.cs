namespace DigiminePdfExport.Models;

/// <summary>
/// Unified request body for export: supports Format = "Pdf", "Excel" (or "Xlsx"), or "Html".
/// Accepts either raw HTML payload or a URL.
/// </summary>
public sealed class ExportRequest
{
    /// <summary>Export format: "Pdf", "Excel" (or "Xlsx"), or "Html".</summary>
    public string Format { get; set; } = "Pdf";

    /// <summary>Raw HTML string containing report widgets (each marked with data-widget="SheetName").</summary>
    public string? Html { get; set; }

    /// <summary>Optional URL to export via Playwright (fallback or public URL).</summary>
    public string? Url { get; set; }

    /// <summary>Friendly report name used in the output filename.</summary>
    public string ReportName { get; set; } = "Report";

    /// <summary>IANA time-zone id (e.g. "Asia/Kolkata") for timestamps.</summary>
    public string SiteTimeZoneId { get; set; } = "UTC";

    /// <summary>
    /// When true, Playwright waits for window.__REPORT_EXPORT_READY__ === true before exporting.
    /// </summary>
    public bool WaitForReadyFlag { get; set; } = false;
}

/// <summary>
/// Wrapper returned from export services carrying the file bytes, content type, and download name.
/// </summary>
public sealed class ExportResult
{
    public byte[] FileBytes { get; set; } = Array.Empty<byte>();
    public string ContentType { get; set; } = "application/octet-stream";
    public string FileName { get; set; } = "export";
}

public enum WidgetType
{
    Table,
    Image
}

public sealed class ExtractedWidget
{
    public string Name { get; set; } = "Widget";
    public WidgetType Type { get; set; }
    public string? TableHtml { get; set; }
    public byte[]? ImageBytes { get; set; }
}


