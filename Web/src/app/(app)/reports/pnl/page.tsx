"use client";

import { useState } from "react";
import { useQuery } from "@tanstack/react-query";
import { api } from "@/lib/api";
import { formatMoney } from "@/lib/format";
import { ExpenseCategoryLabels, type ProfitLossReport } from "@/types/api";
import PageHeader from "@/components/PageHeader";

function toIso(d: string): string {
  return new Date(d + "T00:00:00").toISOString();
}

export default function PnLPage() {
  const today = new Date();
  const monthAgo = new Date(today);
  monthAgo.setDate(today.getDate() - 30);
  const [from, setFrom] = useState(monthAgo.toISOString().slice(0, 10));
  const [to, setTo] = useState(today.toISOString().slice(0, 10));

  const { data, isLoading, refetch } = useQuery({
    queryKey: ["pnl", from, to],
    queryFn: async () =>
      (await api.get<ProfitLossReport>("/reports/pnl", {
        params: { from: toIso(from), to: toIso(to + "T23:59:59") },
      })).data,
  });

  return (
    <>
      <PageHeader title="تقرير الأرباح والخسائر (P&L)" description="ملخص محاسبي للفترة" />
      <div className="card mb-4 flex flex-wrap items-end gap-3">
        <div>
          <label>من</label>
          <input type="date" value={from} onChange={(e) => setFrom(e.target.value)} />
        </div>
        <div>
          <label>إلى</label>
          <input type="date" value={to} onChange={(e) => setTo(e.target.value)} />
        </div>
        <button onClick={() => refetch()} className="btn">عرض</button>
      </div>

      {isLoading || !data ? (
        <p>جاري التحميل...</p>
      ) : (
        <div className="card">
          <table className="w-full text-sm">
            <tbody>
              <Section title="الإيرادات" />
              <Row k="إجمالي المبيعات (Gross Sales)" v={formatMoney(data.grossSales)} />
              <Row k="خصومات على الفواتير" v={`(${formatMoney(data.discounts)})`} negative />
              <Row k="مرتجعات" v={`(${formatMoney(data.refunds)})`} negative />
              <RowTotal k="صافي المبيعات" v={formatMoney(data.netSales)} />

              <Section title="تكلفة البضاعة المباعة" />
              <Row k="تكلفة الأصناف المباعة (COGS)" v={`(${formatMoney(data.costOfGoodsSold)})`} negative />
              <RowTotal
                k={`مجمل الربح (هامش ${data.grossMarginPercent.toFixed(1)}%)`}
                v={formatMoney(data.grossProfit)}
                positive={data.grossProfit >= 0}
              />

              <Section title="المصروفات التشغيلية" />
              {data.expensesByCategory.map((e) => (
                <Row
                  key={e.categoryId}
                  k={`${ExpenseCategoryLabels[e.categoryId] ?? e.category} (${e.percentOfTotal.toFixed(1)}%)`}
                  v={`(${formatMoney(e.amount)})`}
                  negative
                />
              ))}
              {data.expensesByCategory.length === 0 && (
                <tr>
                  <td colSpan={2} className="text-center py-2 text-slate-400 text-xs">
                    لم تُسجّل أي مصروفات في الفترة
                  </td>
                </tr>
              )}
              <RowTotal k="إجمالي المصروفات التشغيلية" v={`(${formatMoney(data.operatingExpenses)})`} negative />

              <Section title="النتيجة النهائية" />
              <tr
                className={`text-lg font-bold ${
                  data.netProfit >= 0 ? "bg-emerald-50 dark:bg-emerald-950/30" : "bg-red-50 dark:bg-red-950/30"
                }`}
              >
                <td className="py-3 px-3">
                  صافي الربح (هامش {data.netMarginPercent.toFixed(1)}%)
                </td>
                <td
                  className={`py-3 px-3 text-end ${
                    data.netProfit >= 0 ? "text-emerald-700 dark:text-emerald-400" : "text-red-700 dark:text-red-400"
                  }`}
                >
                  {formatMoney(data.netProfit)}
                </td>
              </tr>
            </tbody>
          </table>
        </div>
      )}
    </>
  );
}

function Section({ title }: { title: string }) {
  return (
    <tr className="bg-slate-50 dark:bg-slate-800">
      <td colSpan={2} className="py-2 px-3 font-bold text-slate-700 dark:text-slate-200">
        {title}
      </td>
    </tr>
  );
}

function Row({ k, v, negative = false }: { k: string; v: string; negative?: boolean }) {
  return (
    <tr className="border-b border-slate-100 dark:border-slate-800">
      <td className="py-2 px-3">{k}</td>
      <td className={`py-2 px-3 text-end ${negative ? "text-red-600 dark:text-red-400" : ""}`}>{v}</td>
    </tr>
  );
}

function RowTotal({ k, v, positive, negative }: { k: string; v: string; positive?: boolean; negative?: boolean }) {
  return (
    <tr className="border-y-2 border-slate-300 dark:border-slate-600 font-semibold">
      <td className="py-2 px-3">{k}</td>
      <td
        className={`py-2 px-3 text-end ${
          positive ? "text-emerald-700 dark:text-emerald-400" : negative ? "text-red-600 dark:text-red-400" : ""
        }`}
      >
        {v}
      </td>
    </tr>
  );
}
