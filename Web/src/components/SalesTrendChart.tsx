"use client";

import {
  Area,
  AreaChart,
  CartesianGrid,
  ResponsiveContainer,
  Tooltip,
  XAxis,
  YAxis,
} from "recharts";
import { useQuery } from "@tanstack/react-query";
import { api } from "@/lib/api";
import { formatMoney } from "@/lib/format";
import type { SalesReport } from "@/types/api";

export default function SalesTrendChart() {
  const { data, isLoading } = useQuery({
    queryKey: ["sales-trend"],
    queryFn: async () => {
      const to = new Date();
      const from = new Date();
      from.setDate(from.getDate() - 29);
      return (
        await api.get<SalesReport>("/reports/sales", {
          params: { from: from.toISOString(), to: to.toISOString() },
        })
      ).data;
    },
  });

  if (isLoading) return <div className="text-slate-400 text-center py-10">جاري التحميل...</div>;
  if (!data || data.rows.length === 0)
    return <div className="text-slate-400 text-center py-10">لا توجد بيانات</div>;

  const points = data.rows.map((r) => ({
    date: new Date(r.date).toLocaleDateString("ar-EG", { day: "2-digit", month: "2-digit" }),
    sales: Number(r.totalSales.toFixed(2)),
    profit: Number(r.profit.toFixed(2)),
  }));

  return (
    <div className="w-full h-72" dir="ltr">
      <ResponsiveContainer width="100%" height="100%">
        <AreaChart data={points} margin={{ top: 10, right: 24, left: 24, bottom: 0 }}>
          <defs>
            <linearGradient id="g-sales" x1="0" y1="0" x2="0" y2="1">
              <stop offset="0%" stopColor="#1976d2" stopOpacity={0.4} />
              <stop offset="100%" stopColor="#1976d2" stopOpacity={0} />
            </linearGradient>
            <linearGradient id="g-profit" x1="0" y1="0" x2="0" y2="1">
              <stop offset="0%" stopColor="#26a69a" stopOpacity={0.35} />
              <stop offset="100%" stopColor="#26a69a" stopOpacity={0} />
            </linearGradient>
          </defs>
          <CartesianGrid strokeDasharray="3 3" stroke="#e2e8f0" />
          <XAxis dataKey="date" fontSize={11} tick={{ fill: "#64748b" }} />
          <YAxis fontSize={11} tick={{ fill: "#64748b" }} />
          <Tooltip
            formatter={(v: number) => formatMoney(v)}
            contentStyle={{ direction: "rtl", fontFamily: "Cairo, sans-serif", fontSize: 13 }}
            labelStyle={{ fontWeight: 600 }}
          />
          <Area type="monotone" dataKey="sales" name="المبيعات" stroke="#1976d2" fill="url(#g-sales)" strokeWidth={2} />
          <Area type="monotone" dataKey="profit" name="الربح" stroke="#26a69a" fill="url(#g-profit)" strokeWidth={2} />
        </AreaChart>
      </ResponsiveContainer>
    </div>
  );
}
