namespace DigiminePdfExport.Services;

/// <summary>
/// Returns the self-contained HTML for the mock Digimine report dashboard.
/// Served at GET /mock-digimine-report so Playwright can navigate to it.
/// </summary>
public static class MockReportHtml
{
    public static string Build() => """
<!DOCTYPE html>
<html lang="en">
<head>
<meta charset="UTF-8" />
<meta name="viewport" content="width=device-width, initial-scale=1" />
<title>Digimine – Daily Mining Report (Mock)</title>
<script src="https://cdn.jsdelivr.net/npm/chart.js@4.4.7/dist/chart.umd.min.js"></script>
<style>
  /* ── Reset & Base ── */
  *, *::before, *::after { box-sizing: border-box; margin: 0; padding: 0; }
  body {
    font-family: 'Segoe UI', system-ui, -apple-system, sans-serif;
    background: #f0f2f5; color: #1e293b; padding: 32px 40px;
    -webkit-print-color-adjust: exact; print-color-adjust: exact;
  }
  h1 { font-size: 22px; margin-bottom: 4px; }
  .subtitle { font-size: 13px; color: #64748b; margin-bottom: 24px; }

  /* ── KPI Cards ── */
  .kpi-grid {
    display: grid; grid-template-columns: repeat(4, 1fr); gap: 16px;
    margin-bottom: 28px;
  }
  .kpi-card {
    background: #fff; border-radius: 10px; padding: 20px 24px;
    box-shadow: 0 1px 3px rgba(0,0,0,.08);
  }
  .kpi-card .label { font-size: 12px; text-transform: uppercase; color: #94a3b8; letter-spacing: .5px; }
  .kpi-card .value { font-size: 28px; font-weight: 700; margin-top: 6px; }
  .kpi-card .plan  { font-size: 12px; color: #64748b; margin-top: 4px; }
  .kpi-card.positive .value { color: #16a34a; }
  .kpi-card.negative .value { color: #dc2626; }
  .kpi-card.neutral  .value { color: #1e293b; }

  /* ── Failure widget ── */
  .widget-error {
    background: #fff; border-radius: 10px; padding: 24px;
    box-shadow: 0 1px 3px rgba(0,0,0,.08); margin-bottom: 28px;
    border-left: 4px solid #f59e0b;
  }
  .widget-error h2 { font-size: 15px; margin-bottom: 8px; }
  .widget-error p  { color: #b45309; font-style: italic; }

  /* ── Chart ── */
  .chart-container {
    background: #fff; border-radius: 10px; padding: 24px;
    box-shadow: 0 1px 3px rgba(0,0,0,.08); margin-bottom: 28px;
  }
  .chart-container h2 { font-size: 15px; margin-bottom: 16px; }
  canvas#haulageChart { width: 100% !important; height: 320px !important; }

  /* ── Page-break marker ── */
  .canvas-page-break {
    break-after: page; page-break-after: always;
    height: 0; overflow: hidden;
  }

  /* ── Data Table ── */
  .table-container {
    background: #fff; border-radius: 10px; padding: 24px;
    box-shadow: 0 1px 3px rgba(0,0,0,.08); margin-bottom: 16px;
    overflow-x: auto;
  }
  .table-container h2 { font-size: 15px; margin-bottom: 16px; }
  table {
    width: 100%; border-collapse: collapse; font-size: 13px;
  }
  thead { display: table-header-group; }   /* repeat on every print page */
  th {
    background: #334155; color: #fff; text-align: left;
    padding: 10px 12px; font-weight: 600; white-space: nowrap;
    position: sticky; top: 0;
  }
  td {
    padding: 8px 12px; border-bottom: 1px solid #e2e8f0;
  }
  tr:nth-child(even) td { background: #f8fafc; }
  .truncation-note {
    font-size: 12px; color: #94a3b8; font-style: italic;
    padding: 12px 0;
  }

  /* ── Loading overlay ── */
  #loading-overlay {
    position: fixed; inset: 0; background: rgba(255,255,255,.92);
    display: flex; align-items: center; justify-content: center;
    z-index: 9999; transition: opacity .3s;
  }
  #loading-overlay.hidden { opacity: 0; pointer-events: none; }
  .spinner {
    width: 40px; height: 40px;
    border: 4px solid #e2e8f0; border-top-color: #3b82f6;
    border-radius: 50%; animation: spin .8s linear infinite;
  }
  @keyframes spin { to { transform: rotate(360deg); } }

  /* ── Print tweaks ── */
  @media print {
    body { padding: 0; background: #fff; }
    .kpi-card, .chart-container, .widget-error, .table-container {
      box-shadow: none; break-inside: avoid;
    }
    #loading-overlay { display: none !important; }
  }
</style>
</head>
<body>

<!-- Loading overlay -->
<div id="loading-overlay"><div class="spinner"></div></div>

<h1>Daily Mining Report — Main Pit</h1>
<p class="subtitle">Period: 1 – 31 Oct 2025 &nbsp;|&nbsp; Loaders: EX01, EX02 &nbsp;|&nbsp; Generated: <span id="gen-ts">…</span></p>

<!-- KPI Cards -->
<div class="kpi-grid">
  <div class="kpi-card positive">
    <div class="label">Total Material Moved</div>
    <div class="value">245,800 t</div>
    <div class="plan">Plan 250,000 t (−1.7 %)</div>
  </div>
  <div class="kpi-card positive">
    <div class="label">Ore Mined</div>
    <div class="value">98,200 t</div>
    <div class="plan">Plan 100,000 t (−1.8 %)</div>
  </div>
  <div class="kpi-card neutral">
    <div class="label">Strip Ratio</div>
    <div class="value">1.50</div>
    <div class="plan">Plan 1.50</div>
  </div>
  <div class="kpi-card negative">
    <div class="label">Avg Grade</div>
    <div class="value">3.20 %</div>
    <div class="plan">Plan 3.50 % (−8.6 %)</div>
  </div>
</div>

<!-- Partial-failure widget -->
<div class="widget-error">
  <h2>Equipment Utilisation</h2>
  <p>Data unavailable — the source API returned HTTP 503. Contact the data engineering team if this persists.</p>
</div>

<!-- Chart -->
<div class="chart-container">
  <h2>Daily Haulage (t)</h2>
  <canvas id="haulageChart"></canvas>
</div>

<!-- Explicit page-break marker -->
<div class="canvas-page-break"></div>

<!-- Data table (populated by JS) -->
<div class="table-container">
  <h2>Haulage Detail</h2>
  <table>
    <thead>
      <tr>
        <th>#</th><th>Date</th><th>Shift</th><th>Loader</th><th>Truck</th>
        <th>Haulage (t)</th><th>Grade (%)</th><th>Dump</th><th>Material</th><th>Cycle Time (min)</th>
      </tr>
    </thead>
    <tbody id="data-body"></tbody>
  </table>
  <p class="truncation-note">Showing 5,000 of 12,430 rows. Narrow the filters to include all rows.</p>
</div>

<script>
(function() {
  // Simulate a 1-second async data fetch, then render everything
  setTimeout(function() {
    // Timestamp
    document.getElementById('gen-ts').textContent = new Date().toLocaleString();

    // ── Chart ──
    var ctx = document.getElementById('haulageChart').getContext('2d');
    var days = [];
    var oreData = [];
    var wasteData = [];
    for (var d = 1; d <= 31; d++) {
      days.push('Oct ' + d);
      oreData.push(Math.round(2500 + Math.random() * 1500));
      wasteData.push(Math.round(3500 + Math.random() * 2500));
    }
    new Chart(ctx, {
      type: 'bar',
      data: {
        labels: days,
        datasets: [
          { label: 'Ore (t)',   data: oreData,   backgroundColor: 'rgba(34,197,94,.75)' },
          { label: 'Waste (t)', data: wasteData,  backgroundColor: 'rgba(100,116,139,.55)' }
        ]
      },
      options: {
        animation: false,
        responsive: true,
        maintainAspectRatio: false,
        plugins: { legend: { position: 'bottom' } },
        scales: {
          x: { stacked: true },
          y: { stacked: true, title: { display: true, text: 'Tonnes' } }
        }
      }
    });

    // ── Table rows (120 rows for the mock – enough to span multiple PDF pages) ──
    var loaders  = ['EX01','EX02','EX03'];
    var trucks   = ['TR01','TR02','TR03','TR04','TR05'];
    var dumps    = ['ROM Pad','Waste Dump A','Waste Dump B','Stockpile'];
    var materials= ['Ore','Waste','Top Soil'];
    var shifts   = ['Day','Night'];
    var tbody = document.getElementById('data-body');
    var html = '';
    for (var i = 1; i <= 120; i++) {
      var dt = new Date(2025, 9, 1 + Math.floor(Math.random()*30));
      html += '<tr>' +
        '<td>' + i + '</td>' +
        '<td>' + dt.toISOString().slice(0,10) + '</td>' +
        '<td>' + shifts[Math.floor(Math.random()*shifts.length)] + '</td>' +
        '<td>' + loaders[Math.floor(Math.random()*loaders.length)] + '</td>' +
        '<td>' + trucks[Math.floor(Math.random()*trucks.length)] + '</td>' +
        '<td>' + (40 + Math.random()*60).toFixed(1) + '</td>' +
        '<td>' + (Math.random()*6).toFixed(2) + '</td>' +
        '<td>' + dumps[Math.floor(Math.random()*dumps.length)] + '</td>' +
        '<td>' + materials[Math.floor(Math.random()*materials.length)] + '</td>' +
        '<td>' + (8 + Math.random()*22).toFixed(1) + '</td>' +
      '</tr>';
    }
    tbody.innerHTML = html;

    // ── Hide loading overlay ──
    document.getElementById('loading-overlay').classList.add('hidden');

    // ── Signal to Playwright that the page is ready ──
    window.__REPORT_EXPORT_READY__ = true;
    console.log('[MockReport] __REPORT_EXPORT_READY__ = true');

  }, 1000); // 1-second simulated API delay
})();
</script>
</body>
</html>
""";
}
