"use client";

import {
  BarChart, Bar, LineChart, Line,
  XAxis, YAxis, CartesianGrid, Tooltip, Legend, ResponsiveContainer,
} from "recharts";
import type { MonthlyTrend, FinancialRatios } from "@accounting/types";

function fmtNum(n: number) {
  return n.toLocaleString("es-GT", { minimumFractionDigits: 0, maximumFractionDigits: 0 });
}

export function IncomeExpenseChart({ data }: { data: MonthlyTrend[] }) {
  return (
    <div className="rounded-xl border border-slate-200 bg-white p-5 shadow-sm dark:border-slate-700 dark:bg-slate-800">
      <h3 className="mb-4 text-sm font-semibold text-slate-700 dark:text-slate-300">Ingresos vs Gastos</h3>
      <ResponsiveContainer width="100%" height={220}>
        <BarChart data={data} barGap={3} barCategoryGap="30%">
          <CartesianGrid strokeDasharray="3 3" stroke="#e2e8f0" />
          <XAxis dataKey="label" tick={{ fontSize: 11 }} stroke="#94a3b8" />
          <YAxis tickFormatter={fmtNum} tick={{ fontSize: 11 }} stroke="#94a3b8" width={60} />
          <Tooltip
            formatter={(v: unknown, name: unknown) => [fmtNum(Number(v)), name === "income" ? "Ingresos" : "Gastos"]}
            labelStyle={{ fontWeight: 600, fontSize: 12 }}
            contentStyle={{ borderRadius: 8, border: "1px solid #e2e8f0", fontSize: 12 }}
          />
          <Legend formatter={(v) => v === "income" ? "Ingresos" : "Gastos"} iconType="circle" iconSize={8} />
          <Bar dataKey="income"  fill="#6366f1" radius={[4, 4, 0, 0]} />
          <Bar dataKey="expense" fill="#f87171" radius={[4, 4, 0, 0]} />
        </BarChart>
      </ResponsiveContainer>
    </div>
  );
}

export function EquityTrendChart({ data }: { data: MonthlyTrend[] }) {
  const chartData = data.map((d) => ({
    label: d.label,
    netIncome: d.income - d.expense,
  }));

  return (
    <div className="rounded-xl border border-slate-200 bg-white p-5 shadow-sm dark:border-slate-700 dark:bg-slate-800">
      <h3 className="mb-4 text-sm font-semibold text-slate-700 dark:text-slate-300">Resultado mensual</h3>
      <ResponsiveContainer width="100%" height={220}>
        <LineChart data={chartData}>
          <CartesianGrid strokeDasharray="3 3" stroke="#e2e8f0" />
          <XAxis dataKey="label" tick={{ fontSize: 11 }} stroke="#94a3b8" />
          <YAxis tickFormatter={fmtNum} tick={{ fontSize: 11 }} stroke="#94a3b8" width={60} />
          <Tooltip
            formatter={(v: unknown) => [fmtNum(Number(v)), "Utilidad / Pérdida"]}
            contentStyle={{ borderRadius: 8, border: "1px solid #e2e8f0", fontSize: 12 }}
          />
          <Line
            type="monotone"
            dataKey="netIncome"
            stroke="#10b981"
            strokeWidth={2}
            dot={{ r: 4, fill: "#10b981" }}
            activeDot={{ r: 6 }}
          />
        </LineChart>
      </ResponsiveContainer>
    </div>
  );
}

export function RatioCards({ ratios }: { ratios: FinancialRatios }) {
  const cards = [
    {
      label: "Índice de liquidez",
      value: ratios.liquidityRatio !== null ? ratios.liquidityRatio.toFixed(2) : "N/A",
      tip: "Activos líquidos / Pasivos corrientes. > 1 es favorable.",
      good: ratios.liquidityRatio === null ? null : ratios.liquidityRatio >= 1,
    },
    {
      label: "Ratio de endeudamiento",
      value: `${(ratios.debtRatio * 100).toFixed(1)} %`,
      tip: "Pasivos / Activos. < 50 % es favorable.",
      good: ratios.debtRatio < 0.5,
    },
    {
      label: "Margen de utilidad neta",
      value: ratios.netProfitMargin !== null ? `${ratios.netProfitMargin.toFixed(1)} %` : "N/A",
      tip: "Utilidad neta / Ingresos totales.",
      good: ratios.netProfitMargin === null ? null : ratios.netProfitMargin > 0,
    },
  ];

  return (
    <div className="grid gap-4 sm:grid-cols-3">
      {cards.map((c) => (
        <div key={c.label} className="rounded-xl border border-slate-200 bg-white p-5 shadow-sm dark:border-slate-700 dark:bg-slate-800">
          <p className="text-xs font-medium uppercase tracking-wide text-slate-500 dark:text-slate-400">{c.label}</p>
          <p className={`mt-2 text-2xl font-bold tabular-nums ${
            c.good === null ? "text-slate-500 dark:text-slate-400" :
            c.good ? "text-emerald-600 dark:text-emerald-400" : "text-red-600 dark:text-red-400"
          }`}>
            {c.value}
          </p>
          <p className="mt-1 text-xs text-slate-400 dark:text-slate-500">{c.tip}</p>
        </div>
      ))}
    </div>
  );
}
