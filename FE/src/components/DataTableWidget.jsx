import React, { useState } from "react";
import { Table, Search, Filter } from "lucide-react";

export default function DataTableWidget({ data, isExportView = false }) {
  const [searchTerm, setSearchTerm] = useState("");
  const [materialFilter, setMaterialFilter] = useState("ALL");

  // In interactive view allow filtering; in export view always show full 100 rows without toolbar
  const rowsToRender = isExportView
    ? data
    : data.filter((row) => {
        const matchesSearch =
          searchTerm === "" ||
          row.truck.toLowerCase().includes(searchTerm.toLowerCase()) ||
          row.loader.toLowerCase().includes(searchTerm.toLowerCase()) ||
          row.dump.toLowerCase().includes(searchTerm.toLowerCase());

        const matchesMaterial =
          materialFilter === "ALL" || row.material === materialFilter;

        return matchesSearch && matchesMaterial;
      });

  return (
    <section className="report-widget table-widget" data-widget="Haulage Detail">
      <div className="widget-header">
        <div className="widget-title-group">
          <Table size={18} className="widget-icon" />
          <h2 className="widget-title">Haulage Detail Log ({data.length} Records)</h2>
        </div>
        {!isExportView && <span className="sheet-badge no-print">Sheet: Haulage Detail</span>}
      </div>

      {/* Interactive Toolbar for screen viewing (omitted in export view) */}
      {!isExportView && (
        <div className="table-toolbar no-print">
          <div className="search-box">
            <Search size={14} className="search-icon" />
            <input
              type="text"
              placeholder="Search by truck, loader, dump..."
              value={searchTerm}
              onChange={(e) => setSearchTerm(e.target.value)}
            />
          </div>

          <div className="filter-group">
            <Filter size={14} className="filter-icon" />
            <select
              value={materialFilter}
              onChange={(e) => setMaterialFilter(e.target.value)}
              className="filter-select"
            >
              <option value="ALL">All Materials (100 Rows)</option>
              <option value="Ore">Ore Only</option>
              <option value="Waste">Waste Only</option>
              <option value="Top Soil">Top Soil Only</option>
            </select>
          </div>

          <span className="table-count">
            Showing <strong>{rowsToRender.length}</strong> of {data.length} records
          </span>
        </div>
      )}

      {/* Main 100-row table */}
      <div className="table-scroll-container">
        <table className="report-table haulage-table">
          <thead>
            <tr>
              <th style={{ width: "50px" }}>#</th>
              <th>Date</th>
              <th>Shift</th>
              <th>Loader</th>
              <th>Truck</th>
              <th>Haulage (t)</th>
              <th>Grade (%)</th>
              <th>Dump Location</th>
              <th>Material</th>
              <th>Cycle Time (min)</th>
            </tr>
          </thead>
          <tbody>
            {rowsToRender.map((row) => (
              <tr key={row.rowNum}>
                <td className="center-cell">{row.rowNum}</td>
                <td className="center-cell">{row.date}</td>
                <td className="center-cell">
                  <span className={`shift-tag ${row.shift.toLowerCase()}`}>
                    {row.shift}
                  </span>
                </td>
                <td className="center-cell font-mono">{row.loader}</td>
                <td className="center-cell font-mono">{row.truck}</td>
                <td className="right-cell font-mono">{row.haulage.toFixed(1)} t</td>
                <td className="right-cell font-mono">
                  {row.material === "Ore" ? `${row.grade.toFixed(2)} %` : "—"}
                </td>
                <td>{row.dump}</td>
                <td>
                  <span className={`material-pill ${row.material.toLowerCase().replace(" ", "-")}`}>
                    {row.material}
                  </span>
                </td>
                <td className="right-cell font-mono">{row.cycleTime.toFixed(1)} min</td>
              </tr>
            ))}
          </tbody>
        </table>
      </div>

      <div className="table-footer-note">
        <span>Displaying full verified operational dataset for dispatch analysis and shift handover.</span>
      </div>
    </section>
  );
}
