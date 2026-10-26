const BACKEND_URL = "http://localhost:5000";

/**
 * Single unified export function that calls POST /api/export
 * Supported formats: "Pdf", "Excel" (or "Xlsx"), "Html"
 * Passes the headless /export-view URL to Playwright backend.
 */
export async function exportReport({
  format = "Excel", // "Pdf" | "Excel" | "Html"
  url = "",
  reportName = "Daily_Mining_Report",
  siteTimeZoneId = "Asia/Kolkata",
  onStatusChange = () => {}
} = {}) {
  try {
    const targetUrl = url || `${window.location.origin}/export-view`;

    onStatusChange(`Sending request to backend export service (format: ${format})...`);

    // Unified POST API call
    const response = await fetch(`${BACKEND_URL}/api/export`, {
      method: "POST",
      headers: {
        "Content-Type": "application/json"
      },
      body: JSON.stringify({
        format,
        url: targetUrl,
        reportName,
        siteTimeZoneId,
        waitForReadyFlag: true
      })
    });

    if (!response.ok) {
      const errorText = await response.text();
      throw new Error(`Export failed (${response.status}): ${errorText}`);
    }

    onStatusChange(`Receiving ${format} stream from server...`);
    const blob = await response.blob();
    const downloadUrl = window.URL.createObjectURL(blob);
    const link = document.createElement("a");
    link.href = downloadUrl;

    const ext = format.toLowerCase().includes("excel") || format.toLowerCase().includes("xlsx")
      ? "xlsx"
      : format.toLowerCase().includes("pdf")
      ? "pdf"
      : "html";

    const now = new Date();
    const stamp = `${now.getFullYear()}${String(now.getMonth() + 1).padStart(2, "0")}${String(now.getDate()).padStart(2, "0")}_${String(now.getHours()).padStart(2, "0")}${String(now.getMinutes()).padStart(2, "0")}`;
    let filename = `${reportName}_${stamp}.${ext}`;

    // Read filename from Content-Disposition header if available
    const disposition = response.headers.get("content-disposition");
    if (disposition && disposition.includes("filename=")) {
      const match = disposition.match(/filename[^;=\n]*=((['"]).*?\2|[^;\n]*)/);
      if (match && match[1]) {
        filename = match[1].replace(/['"]/g, "");
      }
    }

    link.setAttribute("download", filename);
    document.body.appendChild(link);
    link.click();

    // Delay cleanup so browser download completes cleanly
    setTimeout(() => {
      link.remove();
      window.URL.revokeObjectURL(downloadUrl);
    }, 60000);

    onStatusChange(`Downloaded: ${filename}`);
    return { success: true, filename, downloadUrl };
  } catch (error) {
    console.error(`Export (${format}) failed:`, error);
    onStatusChange(`Error: ${error.message}`);
    throw error;
  }
}

/** Convenience helper for Excel export */
export function exportToExcel(options = {}) {
  return exportReport({ ...options, format: "Excel" });
}

/** Convenience helper for PDF export */
export function exportToPdf(options = {}) {
  return exportReport({ ...options, format: "Pdf" });
}

/** Convenience helper for Offline HTML export */
export function exportToHtml(options = {}) {
  return exportReport({ ...options, format: "Html" });
}
