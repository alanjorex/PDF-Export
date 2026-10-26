import React, { useState, useEffect } from "react";
import Header from "./components/Header";
import KpiWidget from "./components/KpiWidget";
import BarChartWidget from "./components/BarChartWidget";
import LineChartWidget from "./components/LineChartWidget";
import DataTableWidget from "./components/DataTableWidget";
import { KPI_DATA, HAULAGE_CHART_DATA, TREND_CHART_DATA, HAULAGE_100_ROWS } from "./data/mockData";
import { exportReport } from "./services/exportService";
import "./App.css";

export default function App() {
  const [isExporting, setIsExporting] = useState(false);
  const [statusMessage, setStatusMessage] = useState("");

  // Check if current page is the headless export view (/export-view)
  const isExportView = typeof window !== "undefined" && window.location.pathname === "/export-view";

  // Signal ready flag for Playwright headless export
  useEffect(() => {
    const timer = setTimeout(() => {
      window.__REPORT_EXPORT_READY__ = true;
      console.log("[Digimine] window.__REPORT_EXPORT_READY__ = true");
    }, 600);
    return () => clearTimeout(timer);
  }, []);

  const handleExport = async (format = "Excel") => {
    setIsExporting(true);
    setStatusMessage(`Requesting ${format} export...`);
    try {
      // Pass the headless /export-view URL to Playwright backend
      const exportUrl = `${window.location.origin}/export-view`;

      await exportReport({
        format,
        url: exportUrl,
        reportName: "Daily_Mining_Report",
        siteTimeZoneId: "Asia/Kolkata",
        onStatusChange: (msg) => setStatusMessage(msg)
      });

      setTimeout(() => setStatusMessage(""), 10000);
    } catch (err) {
      alert(`${format} export failed: ` + err.message);
    } finally {
      setIsExporting(false);
    }
  };

  return (
    <div className={`report-container ${isExportView ? "export-view" : ""}`}>
      {/* Header component with heading and Export dropdown - omitted in /export-view */}
      {!isExportView && (
        <Header
          reportTitle="Daily Mining Production & Dispatch Report"
          site="Main Pit Quarry (Zone 4)"
          period="1 – 31 Oct 2025"
          timeZone="Asia/Kolkata"
          onExport={handleExport}
          isExporting={isExporting}
          statusMessage={statusMessage}
        />
      )}

      <main className="report-body">
        {/* Widget 1: KPI Summary */}
        <KpiWidget data={KPI_DATA} isExportView={isExportView} />

        {/* Charts Row: Bar Chart & Line Chart */}
        <div className="charts-grid">
          {/* Widget 2: Bar Chart */}
          <BarChartWidget data={HAULAGE_CHART_DATA} isExportView={isExportView} />

          {/* Widget 3: Line Chart */}
          <LineChartWidget data={TREND_CHART_DATA} isExportView={isExportView} />
        </div>

        {/* Page Break for Print / PDF */}
        <div className="canvas-page-break" />

        {/* Widget 4: Tabular Data (100 Rows, no search/filter in /export-view) */}
        <DataTableWidget data={HAULAGE_100_ROWS} isExportView={isExportView} />
      </main>

      {/* Report Footer */}
      <footer className="report-footer">
        <div className="footer-left">
          <span>Digimine Autonomous Reporting System • Confidential</span>
        </div>
        <div className="footer-right">
          <span>Generated on {new Date().toLocaleDateString()}</span>
        </div>
      </footer>
    </div>
  );
}
