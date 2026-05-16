"use client";

import { useState } from "react";
import { useQuery } from "@tanstack/react-query";
import { Download, Users } from "lucide-react";
import { api } from "@/lib/api";
import { formatMoney } from "@/lib/format";
import { downloadCsv } from "@/lib/csv";
import type { SalesmanCommissionRow } from "@/types/api";
import PageHeader from "@/components/PageHeader";
import EmptyState from "@/components/EmptyState";
import { SkeletonRow } from "@/components/Skeleton";

const today = () => new Date().toISOString().slice(0, 10);
const monthStart = () => {
  const d = new Date();
  return new Date(d.getFullYear(), d.getMonth(), 1).toISOString().slice(0, 10);
};

export default function SalesmanCommissionsReportPage() {
  const [from, setFrom] = useState(monthStart());
  const [to, setTo] = useState(today());

  const { data, isLoading } = useQuery({
    queryKey: ["salesman-commissions", from, to],
    queryFn: async () =>
      (await api.get<SalesmanCommissionRow[]>("/Reports/salesman-commissions", { params: { from, to } })).data,
  });

  const totalSales = (data ?? []).reduce((s, r) => s + r.totalSales, 0);
  const totalCommission = (data ?? []).reduce((s, r) => s + r.commissionAmount, 0);

  return (
    <>
      <PageHeader title="عمولات المندوبين" description="حصة كل مندوب من المبيعات والعمولة المستحقة">
        <button
          onClick={() => data && downloadCsv(`commissions-${from}-to-${to}`, data, [
            { header: "المندوب", accessor: (r) => r.salesmanName },
            { header: "عدد الفواتير", accessor: (r) => r.invoiceCount.toString() },
            { header: "إجمالي المبيعات", accessor: (r) => r.totalSales.toFixed(2) },
            { header: "نسبة العمولة %", accessor: (r) => r.commissionPercent.toFixed(2) },
            { header: "العمولة", accessor: (r) => r.commissionAmount.toFixed(2) },
          ])}
          disabled={!data || data.length === 0}
          className="btn-outline"
        >
          <Download size={16} /> تصدير CSV
        </button>
      </PageHeader>

      <div className="card mb-3 grid md:grid-cols-2 gap-3">
        <div><label>من</label><input type="date" value={from} onChange={(e) => setFrom(e.target.value)} /></div>
        <div><label>إلى</label><input type="date" value={to} onChange={(e) => setTo(e.target.value)} /></div>
      </div>

      {data && data.length > 0 && (
        <div className="grid grid-cols-2 gap-3 mb-3">
          <div className="card">
            <div className="text-xs text-slate-500">إجمالي المبيعات للفترة</div>
            <div className="text-lg font-bold">{formatMoney(totalSales)}</div>
          </div>
          <div className="card">
            <div className="text-xs text-slate-500">إجمالي العمولات المستحقة</div>
            <div className="text-lg font-bold text-emerald-700">{formatMoney(totalCommission)}</div>
          </div>
        </div>
      )}

      <div className="table-wrap">
        <table>
          <thead>
            <tr><th>المندوب</th><th>عدد الفواتير</th><th>إجمالي المبيعات</th><th>نسبة العمولة %</th><th>العمولة المستحقة</th></tr>
          </thead>
          <tbody>
            {isLoading ? (
              Array.from({ length: 4 }).map((_, i) => <SkeletonRow key={i} cols={5} />)
            ) : data?.length === 0 ? (
              <tr><td colSpan={5}>
                <EmptyState icon={Users} title="لا توجد عمولات" description="فعّل خاصية المندوب لموظفين وحدد نسبة العمولة، ثم اربط المبيعات بمندوب." />
              </td></tr>
            ) : (
              data?.map((r) => (
                <tr key={r.salesmanId}>
                  <td className="font-medium">{r.salesmanName}</td>
                  <td>{r.invoiceCount}</td>
                  <td>{formatMoney(r.totalSales)}</td>
                  <td>{r.commissionPercent.toFixed(2)}%</td>
                  <td className="font-bold text-emerald-700">{formatMoney(r.commissionAmount)}</td>
                </tr>
              ))
            )}
          </tbody>
        </table>
      </div>
    </>
  );
}
