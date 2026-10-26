import React from "react";
import { BarChart3 } from "lucide-react";
import Highcharts from "highcharts";
import HighchartsReactCore from "highcharts-react-official";

const HighchartsReact = HighchartsReactCore.default || HighchartsReactCore;

export default function BarChartWidget({ data, isExportView = false }) {
  const chartOptions = {
    chart: {
      type: "column",
      animation: false,
      height: 310,
      backgroundColor: "transparent",
      style: {
        fontFamily: "-apple-system, BlinkMacSystemFont, 'Segoe UI', Roboto, sans-serif"
      }
    },
    title: {
      text: null
    },
    credits: {
      enabled: false
    },
    legend: {
      align: "center",
      verticalAlign: "top",
      itemStyle: {
        color: "#334155",
        fontWeight: "600",
        fontSize: "12px"
      }
    },
    tooltip: {
      shared: true,
      valueSuffix: " t",
      backgroundColor: "rgba(15, 23, 42, 0.92)",
      style: { color: "#ffffff" },
      borderRadius: 6
    },
    xAxis: {
      categories: data.map((d) => d.date.slice(5)), // MM-DD
      labels: {
        style: { color: "#64748b", fontSize: "10px" }
      },
      lineColor: "#e2e8f0"
    },
    yAxis: {
      min: 0,
      title: {
        text: "Tonnes (t)",
        style: { color: "#64748b", fontSize: "11px", fontWeight: "600" }
      },
      gridLineColor: "rgba(226, 232, 240, 0.8)",
      labels: {
        style: { color: "#64748b", fontSize: "10px" },
        format: "{value:,.0f} t"
      }
    },
    plotOptions: {
      column: {
        stacking: "normal",
        animation: false,
        borderWidth: 0,
        borderRadius: 3
      }
    },
    series: [
      {
        name: "Ore (t)",
        data: data.map((d) => d.ore),
        color: "rgba(16, 185, 129, 0.9)" // Emerald
      },
      {
        name: "Waste (t)",
        data: data.map((d) => d.waste),
        color: "rgba(100, 116, 139, 0.85)" // Slate
      }
    ]
  };

  return (
    <section className="report-widget chart-widget" data-widget="Daily Haulage">
      <div className="widget-header">
        <div className="widget-title-group">
          <BarChart3 size={18} className="widget-icon" />
          <h2 className="widget-title">Daily Haulage Production (SVG Bar Chart)</h2>
        </div>
        {!isExportView && <span className="sheet-badge no-print">Sheet: Daily Haulage</span>}
      </div>

      <div className="chart-wrapper">
        <HighchartsReact highcharts={Highcharts} options={chartOptions} />
      </div>
    </section>
  );
}
