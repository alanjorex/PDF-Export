import React, { useState, useRef, useEffect } from "react";
import {
  Download,
  ChevronDown,
  FileSpreadsheet,
  FileText,
  Globe,
  Clock,
  MapPin,
  Calendar,
  CheckCircle2,
  Loader2
} from "lucide-react";

export default function Header({
  reportTitle = "Daily Mining Production & Dispatch Report",
  site = "Main Pit Quarry (Zone 4)",
  period = "1 – 31 Oct 2025",
  timeZone = "Asia/Kolkata",
  onExport = () => {},
  isExporting = false,
  statusMessage = ""
}) {
  const [dropdownOpen, setDropdownOpen] = useState(false);
  const dropdownRef = useRef(null);

  const currentTime = new Date().toLocaleString("en-US", {
    timeZone: timeZone === "Asia/Kolkata" ? "Asia/Kolkata" : undefined,
    dateStyle: "medium",
    timeStyle: "short"
  });

  // Close dropdown on click outside
  useEffect(() => {
    function handleClickOutside(e) {
      if (dropdownRef.current && !dropdownRef.current.contains(e.target)) {
        setDropdownOpen(false);
      }
    }
    document.addEventListener("mousedown", handleClickOutside);
    return () => document.removeEventListener("mousedown", handleClickOutside);
  }, []);

  const handleSelectFormat = (format) => {
    setDropdownOpen(false);
    onExport(format);
  };

  return (
    <header className="report-header">
      <div className="header-top">
        <div className="brand-group">
          <div className="brand-badge">
            <span className="brand-dot"></span>
            DIGIMINE ENTERPRISE
          </div>
          <h1 className="report-title">{reportTitle}</h1>
          <div className="metadata-row">
            <span className="meta-item">
              <Calendar size={14} className="meta-icon" />
              <strong>Period:</strong> {period}
            </span>
            <span className="meta-separator">•</span>
            <span className="meta-item">
              <MapPin size={14} className="meta-icon" />
              <strong>Site:</strong> {site}
            </span>
            <span className="meta-separator">•</span>
            <span className="meta-item">
              <Clock size={14} className="meta-icon" />
              <strong>Site Time:</strong> {currentTime} ({timeZone})
            </span>
          </div>
        </div>

        {/* Single Export Dropdown Button in Header */}
        <div className="header-actions no-print" ref={dropdownRef}>
          <div className="export-dropdown-wrapper">
            <button
              type="button"
              className="export-main-btn"
              onClick={() => setDropdownOpen((prev) => !prev)}
              disabled={isExporting}
              aria-expanded={dropdownOpen}
              aria-haspopup="true"
            >
              {isExporting ? (
                <>
                  <Loader2 size={16} className="btn-spinner" />
                  <span>Exporting...</span>
                </>
              ) : (
                <>
                  <Download size={16} />
                  <span>Export</span>
                  <ChevronDown
                    size={15}
                    className={`dropdown-chevron ${dropdownOpen ? "open" : ""}`}
                  />
                </>
              )}
            </button>

            {dropdownOpen && !isExporting && (
              <div className="export-dropdown-menu">
                <div className="dropdown-header">Export Format</div>

                <button
                  type="button"
                  className="dropdown-item"
                  onClick={() => handleSelectFormat("Pdf")}
                >
                  <FileText size={16} className="dropdown-item-icon pdf-color" />
                  <div className="dropdown-item-text">
                    <span className="dropdown-item-title">PDF Document</span>
                    <span className="dropdown-item-desc">A4 landscape print layout</span>
                  </div>
                </button>

                <button
                  type="button"
                  className="dropdown-item"
                  onClick={() => handleSelectFormat("Excel")}
                >
                  <FileSpreadsheet size={16} className="dropdown-item-icon excel-color" />
                  <div className="dropdown-item-text">
                    <span className="dropdown-item-title">Excel Workbook</span>
                    <span className="dropdown-item-desc">Multi-sheet .xlsx workbook</span>
                  </div>
                </button>

                <button
                  type="button"
                  className="dropdown-item"
                  onClick={() => handleSelectFormat("Html")}
                >
                  <Globe size={16} className="dropdown-item-icon html-color" />
                  <div className="dropdown-item-text">
                    <span className="dropdown-item-title">Offline HTML</span>
                    <span className="dropdown-item-desc">Self-contained webpage</span>
                  </div>
                </button>
              </div>
            )}
          </div>
        </div>
      </div>

      {statusMessage && (
        <div className="header-status-bar no-print">
          <CheckCircle2 size={14} className="status-icon" />
          <span>{statusMessage}</span>
        </div>
      )}
    </header>
  );
}
