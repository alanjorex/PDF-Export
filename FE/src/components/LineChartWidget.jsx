import React from "react";
import { LineChart as LineIcon } from "lucide-react";
import Highcharts from "highcharts";
import HighchartsReactCore from "highcharts-react-official";

const HighchartsReact = HighchartsReactCore.default || HighchartsReactCore;

export default function LineChartWidget({ data, isExportView = false }) {
  const chartOptions = {
    chart: {
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
    yAxis: [
      {
        title: {
          text: "Grade (%)",
          style: { color: "#3b82f6", fontWeight: "600", fontSize: "11px" }
        },
        labels: {
          format: "{value:.2f}%",
          style: { color: "#3b82f6", fontSize: "10px" }
        },
        gridLineColor: "rgba(226, 232, 240, 0.8)"
      },
      {
        title: {
          text: "Cycle Time (min)",
          style: { color: "#d97706", fontWeight: "600", fontSize: "11px" }
        },
        labels: {
          format: "{value:.1f} m",
          style: { color: "#d97706", fontSize: "10px" }
        },
        gridLineWidth: 0,
        opposite: true
      }
    ],
    plotOptions: {
      series: {
        animation: false,
        marker: {
          radius: 3
        }
      }
    },
    series: [
      {
        name: "Average Grade (%)",
        type: "areaspline",
        data: data.map((d) => d.grade),
        color: "#3b82f6",
        fillColor: {
          linearGradient: { x1: 0, y1: 0, x2: 0, y2: 1 },
          stops: [
            [0, "rgba(59, 130, 246, 0.25)"],
            [1, "rgba(59, 130, 246, 0.0)"]
          ]
        },
        yAxis: 0
      },
      {
        name: "Cycle Time (min)",
        type: "spline",
        data: data.map((d) => d.cycleTime),
        color: "#f59e0b",
        dashStyle: "ShortDash",
        yAxis: 1
      }
    ]
  };

  return (
    <section className="report-widget chart-widget" data-widget="Trend Graph">
      <div className="widget-header">
        <div className="widget-title-group">
          <LineIcon size={18} className="widget-icon" />
          <h2 className="widget-title">Grade vs Cycle Time Trend (SVG Line Graph)</h2>
        </div>
        {!isExportView && <span className="sheet-badge no-print">Sheet: Trend Graph</span>}
      </div>

      <div className="chart-wrapper">
        <HighchartsReact highcharts={Highcharts} options={chartOptions} />
      </div>
    </section>
  );
}
