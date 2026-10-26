import React from "react";
import { TrendingUp, TrendingDown, Minus, Layers } from "lucide-react";

function KpiSparkline({ data, status }) {
  if (!data || data.length < 2) return null;
  const width = 110;
  const height = 32;
  const min = Math.min(...data);
  const max = Math.max(...data);
  const range = max - min || 1;

  const points = data.map((val, i) => {
    const x = (i / (data.length - 1)) * (width - 10) + 5;
    const y = height - 5 - ((val - min) / range) * (height - 12);
    return `${x.toFixed(1)},${y.toFixed(1)}`;
  });

  const polylinePoints = points.join(" ");
  const lastPoint = points[points.length - 1].split(",");
  const strokeColor =
    status === "positive" ? "#10b981" : status === "negative" ? "#ef4444" : "#6366f1";
  const fillColor =
    status === "positive"
      ? "rgba(16, 185, 129, 0.15)"
      : status === "negative"
      ? "rgba(239, 68, 68, 0.15)"
      : "rgba(99, 102, 241, 0.15)";

  const areaPoints = `${points[0].split(",")[0]},${height} ${polylinePoints} ${lastPoint[0]},${height}`;

  return (
    <svg
      className="kpi-sparkline-svg"
      width={width}
      height={height}
      viewBox={`0 0 ${width} ${height}`}
      aria-label="Trend sparkline SVG"
    >
      <polygon points={areaPoints} fill={fillColor} />
      <polyline
        fill="none"
        stroke={strokeColor}
        strokeWidth="2"
        strokeLinecap="round"
        strokeLinejoin="round"
        points={polylinePoints}
      />
      <circle
        cx={lastPoint[0]}
        cy={lastPoint[1]}
        r="3"
        fill={strokeColor}
      />
    </svg>
  );
}

export default function KpiWidget({ data, isExportView = false }) {
  return (
    <section className="report-widget kpi-widget" data-widget="KPI Summary">
      <div className="widget-header">
        <div className="widget-title-group">
          <Layers size={18} className="widget-icon" />
          <h2 className="widget-title">Key Performance Indicators</h2>
        </div>
        {!isExportView && <span className="sheet-badge no-print">Sheet: KPI Summary</span>}
      </div>

      {/* Visual KPI Cards Grid with Vector SVGs */}
      <div className="kpi-grid">
        {data.map((item) => {
          const isPos = item.status === "positive";
          const isNeg = item.status === "negative";

          return (
            <div key={item.id} className={`kpi-card ${item.status}`}>
              <div className="kpi-top">
                <span className="kpi-label">{item.metric}</span>
                <span className={`kpi-badge ${item.status}`}>
                  {isPos && <TrendingUp size={12} />}
                  {isNeg && <TrendingDown size={12} />}
                  {!isPos && !isNeg && <Minus size={12} />}
                  {item.variancePct > 0 ? `+${item.variancePct}%` : `${item.variancePct}%`}
                </span>
              </div>

              <div className="kpi-middle-row">
                <span className="kpi-value">
                  {item.unit === "%"
                    ? `${item.actual.toFixed(2)} %`
                    : item.unit === "ratio"
                    ? item.actual.toFixed(2)
                    : `${item.actual.toLocaleString()} t`}
                </span>
                <KpiSparkline data={item.sparkline} status={item.status} />
              </div>

              <div className="kpi-plan-row">
                <span className="kpi-plan">
                  Plan:{" "}
                  {item.unit === "%"
                    ? `${item.plan.toFixed(2)} %`
                    : item.unit === "ratio"
                    ? item.plan.toFixed(2)
                    : `${item.plan.toLocaleString()} t`}
                </span>
                <span className="kpi-diff">
                  Var: {item.variance > 0 ? `+${item.variance.toLocaleString()}` : item.variance.toLocaleString()}{" "}
                  {item.unit !== "ratio" ? item.unit : ""}
                </span>
              </div>
            </div>
          );
        })}
      </div>
    </section>
  );
}
