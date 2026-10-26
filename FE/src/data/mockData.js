// Seeded / deterministic dataset for Mining Dashboard

export const KPI_DATA = [
  {
    id: "material_moved",
    metric: "Total Material Moved",
    actual: 248500,
    plan: 250000,
    unit: "t",
    variance: -1500,
    variancePct: -0.6,
    status: "positive",
    sparkline: [238000, 241000, 244000, 242000, 246500, 247200, 248500]
  },
  {
    id: "ore_mined",
    metric: "Ore Mined",
    actual: 98400,
    plan: 100000,
    unit: "t",
    variance: -1600,
    variancePct: -1.6,
    status: "positive",
    sparkline: [93000, 94200, 95800, 95100, 97000, 96800, 98400]
  },
  {
    id: "strip_ratio",
    metric: "Strip Ratio",
    actual: 1.52,
    plan: 1.50,
    unit: "ratio",
    variance: 0.02,
    variancePct: 1.33,
    status: "neutral",
    sparkline: [1.46, 1.48, 1.50, 1.55, 1.49, 1.54, 1.52]
  },
  {
    id: "avg_grade",
    metric: "Average Grade",
    actual: 3.24,
    plan: 3.50,
    unit: "%",
    variance: -0.26,
    variancePct: -7.43,
    status: "negative",
    sparkline: [3.45, 3.40, 3.32, 3.28, 3.35, 3.21, 3.24]
  }
];

// 30 days of haulage chart data
export const HAULAGE_CHART_DATA = Array.from({ length: 30 }, (_, i) => {
  const day = i + 1;
  const date = `2025-10-${String(day).padStart(2, "0")}`;
  // Pseudo-random but deterministic values
  const ore = Math.round(2800 + Math.sin(day * 0.7) * 450 + (day % 3) * 120);
  const waste = Math.round(4400 + Math.cos(day * 0.5) * 600 + (day % 4) * 150);
  return {
    day: `Day ${day}`,
    date,
    ore,
    waste,
    total: ore + waste
  };
});

// 30 days of trend graph data
export const TREND_CHART_DATA = Array.from({ length: 30 }, (_, i) => {
  const day = i + 1;
  const date = `2025-10-${String(day).padStart(2, "0")}`;
  const grade = Number((3.1 + Math.sin(day * 0.4) * 0.35 + (day % 5) * 0.04).toFixed(2));
  const cycleTime = Number((18.5 + Math.cos(day * 0.6) * 3.2 + (day % 2) * 0.8).toFixed(1));
  const efficiency = Number((88 + Math.sin(day * 0.3) * 6).toFixed(1));
  return {
    day: `Day ${day}`,
    date,
    grade,
    cycleTime,
    efficiency,
    status: grade >= 3.2 ? "Optimal" : "Sub-target"
  };
});

// 100 rows of haulage detail operational records
const LOADERS = ["EX01", "EX02", "EX03"];
const TRUCKS = ["TR01", "TR02", "TR03", "TR04", "TR05", "TR06"];
const DUMPS = ["ROM Pad", "Waste Dump A", "Waste Dump B", "Stockpile"];
const MATERIALS = ["Ore", "Waste", "Top Soil"];
const SHIFTS = ["Day", "Night"];

export const HAULAGE_100_ROWS = Array.from({ length: 100 }, (_, i) => {
  const id = i + 1;
  const day = 1 + (i % 30);
  const date = `2025-10-${String(day).padStart(2, "0")}`;
  const shift = SHIFTS[i % 2];
  const loader = LOADERS[i % LOADERS.length];
  const truck = TRUCKS[(i * 2 + 1) % TRUCKS.length];
  const material = MATERIALS[i % 3 === 0 ? 0 : (i % 3 === 1 ? 1 : 2)];
  const haulageTons = Number((48 + ((i * 17) % 46) + ((i % 5) * 1.3)).toFixed(1));
  const gradePct = material === "Ore" 
    ? Number((2.8 + ((i * 7) % 25) / 10).toFixed(2)) 
    : 0.0;
  const dump = material === "Ore" 
    ? (i % 2 === 0 ? "ROM Pad" : "Stockpile") 
    : (i % 2 === 0 ? "Waste Dump A" : "Waste Dump B");
  const cycleTimeMin = Number((12.5 + ((i * 13) % 15) + ((i % 3) * 1.5)).toFixed(1));

  return {
    rowNum: id,
    date,
    shift,
    loader,
    truck,
    haulage: haulageTons,
    grade: gradePct,
    dump,
    material,
    cycleTime: cycleTimeMin
  };
});
