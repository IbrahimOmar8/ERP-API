"use client";

import { useState } from "react";
import { useQuery } from "@tanstack/react-query";
import { Receipt, Wallet, FileText, TrendingUp, BarChart } from "lucide-react";
import { api } from "@/lib/api";
import { formatMoney, formatDate } from "@/lib/format";
import type { SalesReport } from "@/types/api";
import PageHeader from "@/components/PageHeader";
import KpiCard from "@/components/KpiCard";

function toIso(d: string) {
  return new Date(d).toISOString();
}

export default function SalesReportPage() {
  const today = new Date();
  const monthAgo = new Date(today);
  monthAgo.setDate(today.getDate() - 30);

  const [from, setFrom] = useState(monthAgo.toISOString().slice(0, 10));
  const [to, setTo] = useState(today.toISOString().slice(0, 10));

  const { data, isLoading, refetch } = useQuery({
    queryKey: ["sales-report", from, to],
    queryFn: async () =>
      (
        await api.get<SalesReport>("/reports/sales", {
          params: { from: toIso(from), to: toIso(to + "T23:59:59") },
        })
      ).data,
  });

  return (
    <>
      <PageHeader title="تقرير المبيعات" />
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

      {data && (
        <>
          <div className="grid grid-cols-2 md:grid-cols-5 gap-3 mb-4">
            <KpiCard label="عدد الفواتير" value={data.totalInvoices} icon={Receipt} color="blue" />
            <KpiCard label="صافي المبيعات" value={formatMoney(data.totalNetSales)} icon={Wallet} color="green" />
            <KpiCard label="الضريبة" value={formatMoney(data.totalVat)} icon={FileText} color="amber" />
            <KpiCard label="إجمالي المبيعات" value={formatMoney(data.totalGross)} icon={BarChart} color="blue" />
            <KpiCard label="الربح" value={formatMoney(data.totalProfit)} icon={TrendingUp} color="teal" />
          </div>

          <div className="table-wrap">
            <table>
              <thead>
                <tr>
                  <th>التاريخ</th>
                  <th>عدد الفواتير</th>
                  <th>صافي</th>
                  <th>الضريبة</th>
                  <th>الإجمالي</th>
                  <th>الربح</th>
                </tr>
              </thead>
              <tbody>
                {isLoading ? (
                  <tr><td colSpan={6} className="text-center py-6">جاري التحميل...</td></tr>
                ) : data.rows.length === 0 ? (
                  <tr><td colSpan={6} className="text-center py-6 text-slate-400">لا توجد بيانات</td></tr>
                ) : (
                  data.rows.map((r) => (
                    <tr key={r.date}>
                      <td>{formatDate(r.date)}</td>
                      <td>{r.invoiceCount}</td>
                      <td>{formatMoney(r.netSales)}</td>
                      <td>{formatMoney(r.vatAmount)}</td>
                      <td className="font-semibold">{formatMoney(r.totalSales)}</td>
                      <td className="text-emerald-700">{formatMoney(r.profit)}</td>
                    </tr>
                  ))
                )}
              </tbody>
            </table>
          </div>
        </>
      )}
    </>
  );
}
