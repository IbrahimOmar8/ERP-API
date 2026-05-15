"use client";

import { useState } from "react";
import { useQuery } from "@tanstack/react-query";
import { Download, UserCog } from "lucide-react";
import { api } from "@/lib/api";
import { downloadCsv } from "@/lib/csv";
import { formatMoney } from "@/lib/format";
import type { CashierPerformanceRow } from "@/types/api";
import PageHeader from "@/components/PageHeader";
import EmptyState from "@/components/EmptyState";
import { SkeletonRow } from "@/components/Skeleton";

function toIso(d: string): string {
  return new Date(d + "T00:00:00").toISOString();
}

export default function CashierPerformancePage() {
  const today = new Date();
  const monthAgo = new Date(today);
  monthAgo.setDate(today.getDate() - 30);
  const [from, setFrom] = useState(monthAgo.toISOString().slice(0, 10));
  const [to, setTo] = useState(today.toISOString().slice(0, 10));

  const { data, isLoading, refetch } = useQuery({
    queryKey: ["cashier-performance", from, to],
    queryFn: async () =>
      (await api.get<CashierPerformanceRow[]>("/reports/cashier-performance", {
        params: { from: toIso(from), to: toIso(to + "T23:59:59") },
      })).data,
  });

  function exportCsv() {
    if (!data) return;
    downloadCsv("cashier-performance", data, [
      { header: "الكاشير", accessor: (r) => r.cashierName },
      { header: "عدد الفواتير", accessor: (r) => r.invoiceCount },
      { header: "إجمالي المبيعات", accessor: (r) => r.totalSales.toFixed(2) },
      { header: "متوسط الفاتورة", accessor: (r) => r.averageTicket.toFixed(2) },
      { header: "مرتجعات", accessor: (r) => r.refundCount },
      { header: "قيمة المرتجعات", accessor: (r) => r.refundsAmount.toFixed(2) },
      { header: "صافي المبيعات", accessor: (r) => r.netSales.toFixed(2) },
    ]);
  }

  return (
    <>
      <PageHeader title="أداء الكاشيرين">
        <button onClick={exportCsv} disabled={!data || data.length === 0} className="btn-outline">
          <Download size={16} /> تصدير CSV
        </button>
      </PageHeader>

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

      <div className="table-wrap">
        <table>
          <thead>
            <tr>
              <th>الكاشير</th>
              <th>الفواتير</th>
              <th>إجمالي المبيعات</th>
              <th>متوسط الفاتورة</th>
              <th>المرتجعات</th>
              <th>قيمة المرتجعات</th>
              <th>صافي المبيعات</th>
            </tr>
          </thead>
          <tbody>
            {isLoading ? (
              Array.from({ length: 4 }).map((_, i) => <SkeletonRow key={i} cols={7} />)
            ) : !data || data.length === 0 ? (
              <tr>
                <td colSpan={7}>
                  <EmptyState icon={UserCog} title="لا توجد بيانات" description="لا توجد فواتير في الفترة المحددة." />
                </td>
              </tr>
            ) : (
              data.map((r, i) => (
                <tr key={r.cashierUserId}>
                  <td className="font-medium">
                    {i === 0 && <span className="me-1">🥇</span>}
                    {i === 1 && <span className="me-1">🥈</span>}
                    {i === 2 && <span className="me-1">🥉</span>}
                    {r.cashierName}
                  </td>
                  <td>{r.invoiceCount}</td>
                  <td className="font-semibold">{formatMoney(r.totalSales)}</td>
                  <td>{formatMoney(r.averageTicket)}</td>
                  <td>{r.refundCount}</td>
                  <td className="text-red-600">{formatMoney(r.refundsAmount)}</td>
                  <td className="font-bold text-emerald-700 dark:text-emerald-400">
                    {formatMoney(r.netSales)}
                  </td>
                </tr>
              ))
            )}
          </tbody>
        </table>
      </div>
    </>
  );
}
