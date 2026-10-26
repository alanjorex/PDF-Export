using OfficeOpenXml;
using OfficeOpenXml.Style;
using DigiminePdfExport.Models;
using HtmlAgilityPack;
using System.Drawing;
using System.Globalization;
using System.Net;
using System.Text.RegularExpressions;

namespace DigiminePdfExport.Services;

/// <summary>
/// Generates an Excel (.xlsx) workbook directly from HTML.
/// Extracts each widget (marked by [data-widget="SheetName"]) into a separate worksheet.
/// </summary>
public sealed class ExcelExportService
{
    public ExportResult GenerateFromWidgets(List<ExtractedWidget> widgets, string reportName, string siteTimeZoneId)
    {
        ExcelPackage.LicenseContext = LicenseContext.NonCommercial;

        TimeZoneInfo tz;
        try { tz = TimeZoneInfo.FindSystemTimeZoneById(siteTimeZoneId); }
        catch { tz = TimeZoneInfo.Utc; }

        var siteNow = TimeZoneInfo.ConvertTimeFromUtc(DateTime.UtcNow, tz);

        using var package = new ExcelPackage();

        if (widgets == null || widgets.Count == 0)
        {
            var fallbackWs = package.Workbook.Worksheets.Add("Report");
            fallbackWs.Cells[1, 1].Value = "No widgets detected.";
            return BuildResult(package, reportName, siteNow);
        }

        foreach (var widget in widgets)
        {
            var sheetName = SanitizeSheetName(widget.Name, package);
            var ws = package.Workbook.Worksheets.Add(sheetName);
            ws.View.ShowGridLines = true;

            // Widget title header row
            ws.Cells[1, 1].Value = widget.Name;
            ws.Cells[1, 1].Style.Font.Bold = true;
            ws.Cells[1, 1].Style.Font.Size = 14;
            ws.Cells[1, 1].Style.Font.Color.SetColor(Color.FromArgb(30, 41, 59));

            if (widget.Type == WidgetType.Table && !string.IsNullOrWhiteSpace(widget.TableHtml))
            {
                var doc = new HtmlDocument();
                doc.LoadHtml(widget.TableHtml);
                var tableNode = doc.DocumentNode.SelectSingleNode("//table");
                if (tableNode != null)
                {
                    PopulateTable(ws, tableNode, 3);
                }
                if (ws.Dimension != null)
                {
                    ws.Cells[ws.Dimension.Address].AutoFitColumns(10, 50);
                }
            }
            else if (widget.Type == WidgetType.Image && widget.ImageBytes != null && widget.ImageBytes.Length > 0)
            {
                using var ms = new MemoryStream(widget.ImageBytes);
                var pictureName = "Pic_" + Regex.Replace(sheetName, @"[^\w]", "_");
                var picture = ws.Drawings.AddPicture(pictureName, ms);

                // Place picture starting at cell A3 (row index 2 in 0-indexed EPPlus)
                picture.SetPosition(2, 0, 0, 0);

                // Read dimensions from PNG header (bytes 16-23)
                if (widget.ImageBytes.Length > 24)
                {
                    int origW = (widget.ImageBytes[16] << 24) | (widget.ImageBytes[17] << 16) | (widget.ImageBytes[18] << 8) | widget.ImageBytes[19];
                    int origH = (widget.ImageBytes[20] << 24) | (widget.ImageBytes[21] << 16) | (widget.ImageBytes[22] << 8) | widget.ImageBytes[23];

                    int targetW = 750;
                    if (origW > 0 && origH > 0)
                    {
                        if (origW < targetW) targetW = origW;
                        int targetH = (int)((double)origH * targetW / origW);
                        picture.SetSize(targetW, targetH);
                    }
                }
            }
        }

        return BuildResult(package, reportName, siteNow);
    }

    public ExportResult GenerateFromHtml(string html, string reportName, string siteTimeZoneId)
    {
        ExcelPackage.LicenseContext = LicenseContext.NonCommercial;

        TimeZoneInfo tz;
        try { tz = TimeZoneInfo.FindSystemTimeZoneById(siteTimeZoneId); }
        catch { tz = TimeZoneInfo.Utc; }

        var siteNow = TimeZoneInfo.ConvertTimeFromUtc(DateTime.UtcNow, tz);

        using var package = new ExcelPackage();

        if (string.IsNullOrWhiteSpace(html))
        {
            var fallbackWs = package.Workbook.Worksheets.Add("Report");
            fallbackWs.Cells[1, 1].Value = "No HTML content provided.";
            return BuildResult(package, reportName, siteNow);
        }

        var doc = new HtmlDocument();
        doc.LoadHtml(html);

        // Find widgets by data-widget attribute
        var widgetNodes = doc.DocumentNode.SelectNodes("//*[@data-widget]");

        // Fallback: search for sections, articles, or tables if no data-widget exists
        if (widgetNodes == null || widgetNodes.Count == 0)
        {
            widgetNodes = doc.DocumentNode.SelectNodes("//section | //article | //div[contains(@class,'widget')]");
        }

        if (widgetNodes == null || widgetNodes.Count == 0)
        {
            // Fallback: find all tables
            var tables = doc.DocumentNode.SelectNodes("//table");
            if (tables != null && tables.Count > 0)
            {
                int tIndex = 1;
                foreach (var table in tables)
                {
                    var sheetName = SanitizeSheetName($"Table {tIndex++}", package);
                    var ws = package.Workbook.Worksheets.Add(sheetName);
                    PopulateTable(ws, table, 1);
                    ws.Cells[ws.Dimension?.Address ?? "A1"].AutoFitColumns(10, 45);
                }
                return BuildResult(package, reportName, siteNow);
            }

            var wsEmpty = package.Workbook.Worksheets.Add("Report");
            wsEmpty.Cells[1, 1].Value = "No widgets or tables detected in the supplied HTML.";
            return BuildResult(package, reportName, siteNow);
        }

        // Process each widget into its own worksheet
        int widgetIndex = 1;
        foreach (var widget in widgetNodes)
        {
            var rawName = widget.GetAttributeValue("data-widget", "")
                .Trim();
            if (string.IsNullOrWhiteSpace(rawName))
            {
                rawName = widget.GetAttributeValue("data-title", "")
                    .Trim();
            }
            if (string.IsNullOrWhiteSpace(rawName))
            {
                var hNode = widget.SelectSingleNode(".//h1 | .//h2 | .//h3 | .//h4");
                rawName = hNode != null ? WebUtility.HtmlDecode(hNode.InnerText).Trim() : $"Widget {widgetIndex}";
            }

            var sheetName = SanitizeSheetName(rawName, package);
            var ws = package.Workbook.Worksheets.Add(sheetName);

            int currentRow = 1;

            // Optional title at top of worksheet
            var headingNode = widget.SelectSingleNode(".//h1 | .//h2 | .//h3 | .//h4 | .//*[contains(@class,'title')]");
            string? title = headingNode != null ? WebUtility.HtmlDecode(headingNode.InnerText).Trim() : null;

            if (!string.IsNullOrWhiteSpace(title))
            {
                ws.Cells[currentRow, 1].Value = title;
                ws.Cells[currentRow, 1].Style.Font.Bold = true;
                ws.Cells[currentRow, 1].Style.Font.Size = 14;
                ws.Cells[currentRow, 1].Style.Font.Color.SetColor(Color.FromArgb(30, 41, 59));
                currentRow += 2;
            }

            // Check for KPI cards inside widget
            var kpiCards = widget.SelectNodes(".//*[contains(@class,'kpi-card')] | .//*[contains(@class,'metric-card')]");
            if (kpiCards != null && kpiCards.Count > 0)
            {
                currentRow = PopulateKpis(ws, kpiCards, currentRow);
                currentRow++;
            }

            // Check for tables inside widget
            var tables = widget.SelectNodes(".//table");
            if (tables != null && tables.Count > 0)
            {
                foreach (var table in tables)
                {
                    currentRow = PopulateTable(ws, table, currentRow);
                    currentRow += 2;
                }
            }
            else if (kpiCards == null || kpiCards.Count == 0)
            {
                // If neither tables nor KPI cards, capture paragraph/error text
                var paragraphs = widget.SelectNodes(".//p | .//span | .//div[contains(@class,'error')]");
                if (paragraphs != null)
                {
                    foreach (var p in paragraphs)
                    {
                        var text = WebUtility.HtmlDecode(p.InnerText).Trim();
                        if (!string.IsNullOrWhiteSpace(text) && text != title)
                        {
                            ws.Cells[currentRow, 1].Value = text;
                            ws.Cells[currentRow, 1].Style.Font.Italic = true;
                            currentRow++;
                        }
                    }
                }
            }

            // Auto-fit columns
            if (ws.Dimension != null)
            {
                ws.Cells[ws.Dimension.Address].AutoFitColumns(10, 50);
            }

            widgetIndex++;
        }

        return BuildResult(package, reportName, siteNow);
    }

    private static int PopulateKpis(ExcelWorksheet ws, HtmlNodeCollection kpiCards, int startRow)
    {
        int row = startRow;

        // KPI Table Headers
        var headers = new[] { "Metric", "Value", "Target / Plan", "Variance / Status" };
        for (int c = 0; c < headers.Length; c++)
        {
            ws.Cells[row, c + 1].Value = headers[c];
            ws.Cells[row, c + 1].Style.Font.Bold = true;
            ws.Cells[row, c + 1].Style.Fill.PatternType = ExcelFillStyle.Solid;
            ws.Cells[row, c + 1].Style.Fill.BackgroundColor.SetColor(Color.FromArgb(51, 65, 85));
            ws.Cells[row, c + 1].Style.Font.Color.SetColor(Color.White);
            ws.Cells[row, c + 1].Style.HorizontalAlignment = ExcelHorizontalAlignment.Center;
        }
        row++;

        foreach (var card in kpiCards)
        {
            var labelNode = card.SelectSingleNode(".//*[contains(@class,'label')] | .//span | .//small");
            var valueNode = card.SelectSingleNode(".//*[contains(@class,'value')] | .//strong | .//h3");
            var planNode  = card.SelectSingleNode(".//*[contains(@class,'plan')] | .//*[contains(@class,'target')]");
            var diffNode  = card.SelectSingleNode(".//*[contains(@class,'diff')] | .//*[contains(@class,'variance')] | .//*[contains(@class,'badge')]");

            string label = labelNode != null ? WebUtility.HtmlDecode(labelNode.InnerText).Trim() : "Metric";
            string valueStr = valueNode != null ? WebUtility.HtmlDecode(valueNode.InnerText).Trim() : "";
            string plan = planNode != null ? WebUtility.HtmlDecode(planNode.InnerText).Trim() : "-";
            string diff = diffNode != null ? WebUtility.HtmlDecode(diffNode.InnerText).Trim() : "-";

            ws.Cells[row, 1].Value = label;
            ws.Cells[row, 1].Style.Font.Bold = true;

            SetSmartCellValue(ws.Cells[row, 2], valueStr);
            ws.Cells[row, 3].Value = plan;
            ws.Cells[row, 4].Value = diff;

            // Border
            for (int c = 1; c <= 4; c++)
            {
                ws.Cells[row, c].Style.Border.Bottom.Style = ExcelBorderStyle.Thin;
                ws.Cells[row, c].Style.Border.Bottom.Color.SetColor(Color.FromArgb(226, 232, 240));
            }

            row++;
        }

        return row;
    }

    private static int PopulateTable(ExcelWorksheet ws, HtmlNode table, int startRow)
    {
        int row = startRow;

        // Parse <thead> or first <tr> with <th>
        var headerRows = table.SelectNodes(".//thead/tr | .//tr[th]");
        int headerRowIndex = row;

        if (headerRows != null && headerRows.Count > 0)
        {
            foreach (var hRow in headerRows)
            {
                var ths = hRow.SelectNodes("th | td");
                if (ths != null)
                {
                    int col = 1;
                    foreach (var th in ths)
                    {
                        var text = WebUtility.HtmlDecode(th.InnerText).Trim();
                        ws.Cells[row, col].Value = text;
                        ws.Cells[row, col].Style.Font.Bold = true;
                        ws.Cells[row, col].Style.Fill.PatternType = ExcelFillStyle.Solid;
                        ws.Cells[row, col].Style.Fill.BackgroundColor.SetColor(Color.FromArgb(51, 65, 85));
                        ws.Cells[row, col].Style.Font.Color.SetColor(Color.White);
                        ws.Cells[row, col].Style.HorizontalAlignment = ExcelHorizontalAlignment.Center;
                        col++;
                    }
                    row++;
                }
            }
        }

        // Parse <tbody> or subsequent <tr> with <td>
        var bodyRows = table.SelectNodes(".//tbody/tr | .//tr[td and not(th)]");
        if (bodyRows != null)
        {
            int bodyStartRow = row;
            foreach (var bRow in bodyRows)
            {
                var tds = bRow.SelectNodes("td | th");
                if (tds != null)
                {
                    int col = 1;
                    foreach (var td in tds)
                    {
                        var cellText = WebUtility.HtmlDecode(td.InnerText).Trim();
                        SetSmartCellValue(ws.Cells[row, col], cellText);

                        // Subtle border & zebra striping
                        ws.Cells[row, col].Style.Border.Bottom.Style = ExcelBorderStyle.Thin;
                        ws.Cells[row, col].Style.Border.Bottom.Color.SetColor(Color.FromArgb(241, 245, 249));

                        if ((row - bodyStartRow) % 2 == 1)
                        {
                            ws.Cells[row, col].Style.Fill.PatternType = ExcelFillStyle.Solid;
                            ws.Cells[row, col].Style.Fill.BackgroundColor.SetColor(Color.FromArgb(248, 250, 252));
                        }
                        col++;
                    }
                    row++;
                }
            }

            // Freeze panes below header
            if (headerRows != null && headerRows.Count > 0)
            {
                ws.View.FreezePanes(bodyStartRow, 1);
            }
        }

        return row;
    }

    private static void SetSmartCellValue(ExcelRange cell, string text)
    {
        if (string.IsNullOrWhiteSpace(text))
        {
            cell.Value = string.Empty;
            return;
        }

        // Handle Percentage (e.g. "3.20 %" or "85%")
        if (Regex.IsMatch(text, @"^-?\d+(\.\d+)?\s*%$"))
        {
            var numPart = text.Replace("%", "").Trim();
            if (double.TryParse(numPart, NumberStyles.Any, CultureInfo.InvariantCulture, out double pctVal))
            {
                cell.Value = pctVal / 100.0;
                cell.Style.Numberformat.Format = "0.00%";
                cell.Style.HorizontalAlignment = ExcelHorizontalAlignment.Right;
                return;
            }
        }

        // Handle Clean Number with commas or decimals (e.g. "245,800" or "98.2" or "100")
        var cleanNum = text.Replace(",", "").Trim();
        // Remove trailing unit if any (like "t" for tonnes, "min", etc.)
        if (cleanNum.EndsWith(" t", StringComparison.OrdinalIgnoreCase))
            cleanNum = cleanNum[..^2].Trim();
        else if (cleanNum.EndsWith("t", StringComparison.OrdinalIgnoreCase) && cleanNum.Length > 1 && char.IsDigit(cleanNum[^2]))
            cleanNum = cleanNum[..^1].Trim();

        if (double.TryParse(cleanNum, NumberStyles.Any, CultureInfo.InvariantCulture, out double dVal))
        {
            cell.Value = dVal;
            cell.Style.Numberformat.Format = cleanNum.Contains('.') ? "#,##0.00" : "#,##0";
            cell.Style.HorizontalAlignment = ExcelHorizontalAlignment.Right;
            return;
        }

        // Handle Dates (e.g. "2025-10-15")
        if (DateTime.TryParse(text, CultureInfo.InvariantCulture, DateTimeStyles.None, out DateTime dtVal))
        {
            cell.Value = dtVal;
            cell.Style.Numberformat.Format = "yyyy-mm-dd";
            cell.Style.HorizontalAlignment = ExcelHorizontalAlignment.Center;
            return;
        }

        // Default text
        cell.Value = text;
    }

    private static string SanitizeSheetName(string name, ExcelPackage package)
    {
        if (string.IsNullOrWhiteSpace(name))
            name = "Sheet";

        // Remove invalid characters: \ / ? * [ ] :
        foreach (var c in new[] { '\\', '/', '?', '*', '[', ']', ':' })
        {
            name = name.Replace(c.ToString(), " ");
        }

        name = Regex.Replace(name, @"\s+", " ").Trim();

        if (name.Length > 28)
            name = name.Substring(0, 28).Trim();

        var candidate = name;
        int counter = 2;
        while (package.Workbook.Worksheets[candidate] != null)
        {
            candidate = $"{name}_{counter++}";
            if (candidate.Length > 31)
            {
                candidate = candidate.Substring(0, 31);
            }
        }

        return candidate;
    }

    private static ExportResult BuildResult(ExcelPackage package, string reportName, DateTime siteNow)
    {
        var stamp = siteNow.ToString("yyyy-MM-dd_HHmm");
        var bytes = package.GetAsByteArray();

        return new ExportResult
        {
            FileBytes = bytes,
            ContentType = "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet",
            FileName = $"{SanitizeFileName(reportName)}_{stamp}.xlsx"
        };
    }

    private static string SanitizeFileName(string name)
    {
        foreach (var c in Path.GetInvalidFileNameChars())
            name = name.Replace(c, '_');
        return name;
    }
}
