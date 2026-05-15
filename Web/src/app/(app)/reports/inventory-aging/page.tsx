"use client";

import { useMemo, useState } from "react";
import { useQuery } from "@tanstack/react-query";
import { Download, Hourglass } from "lucide-react";
import { api } from "@/lib/api";
import { downloadCsv } from "@/lib/csv";
import { formatDate, formatMoney, formatNumber } from "@/lib/format";
import type { InventoryAgingRow } from "@/types/api";
import PageHeader from "@/components/PageHeader";
import EmptyState from "@/components/EmptyState";
import { SkeletonRow } from "@/components/Skeleton";

const BUCKETS: { id: number; label: string; color: string }[] = [
  { id: 0, label: "0–30 يوم", color: "bg-emerald-100 text-emerald-800" },
  { id: 1, label: "30–60 يوم", color: "bg-blue-100 text-blue-800" },
  { id: 2, label: "60–90 يوم", color: "bg-amber-100 text-amber-800" },
  { id: 3, label: "90–180 يوم", color: "bg-orange-100 text-orange-800" },
  { id: 4, label: "+180 / لم تُبَع", color: "bg-red-100 text-red-800" },
];

export default function InventoryAgingPage() {
  const [minDays, setMinDays] = useState("");

  const { data, isLoading } = useQuery({
    queryKey: ["inventory-aging", minDays],
    queryFn: async () =>
      (await api.get<InventoryAgingRow[]>("/reports/inventory-aging", {
        params: { bucketDays: minDays || undefined },
      })).data,
  });

  const summary = useMemo(() => {
    if (!data) return null;
    const byBucket = BUCKETS.map((b) => ({
      ...b,
      count: data.filter((r) => r.bucket === b.id).length,
      value: data.filter((r) => r.bucket === b.id).reduce((s, r) => s + r.stockValue, 0),
    }));
    return {
      total: data.length,
      totalValue: data.reduce((s, r) => s + r.stockValue, 0),
      byBucket,
    };
  }, [data]);

  function exportCsv() {
    if (!data) return;
    downloadCsv("inventory-aging", data, [
      { header: "الصنف", accessor: (r) => r.productName },
      { header: "SKU", accessor: (r) => r.sku },
      { header: "الكمية", accessor: (r) => r.quantity.toFixed(2) },
      { header: "متوسط التكلفة", accessor: (r) => r.averageCost.toFixed(2) },
      { header: "قيمة الرصيد", accessor: (r) => r.stockValue.toFixed(2) },
      { header: "آخر بيع", accessor: (r) => r.lastSoldAt ? new Date(r.lastSoldAt).toLocaleDateString() : "لم يُبَع" },
      { header: "أيام منذ آخر بيع", accessor: (r) => r.daysSinceLastSale < 0 ? "—" : r.daysSinceLastSale },
    ]);
  }

  return (
    <>
      <PageHeader title="عمر المخزون" description="الأصناف الراكدة حسب آخر تاريخ بيع">
        <button onClick={exportCsv} disabled={!data || data.length === 0} className="btn-outline">
          <Download size={16} /> تصدير CSV
        </button>
      </PageHeader>

      <div className="card mb-4 flex flex-wrap items-end gap-3">
        <div>
          <label>عرض الأصناف التي لم تُبَع منذ (يوم على الأقل)</label>
          <input
            type="number"
            min={0}
            value={minDays}
            onChange={(e) => setMinDays(e.target.value)}
            placeholder="الكل"
            className="w-32"
          />
        </div>
      </div>

      {summary && (
        <div className="grid grid-cols-2 md:grid-cols-5 gap-3 mb-4">
          {summary.byBucket.map((b) => (
            <div key={b.id} className="card">
              <div className={`inline-block text-xs px-2 py-0.5 rounded-full mb-2 ${b.color}`}>
                {b.label}
              </div>
              <div className="text-xl font-bold">{b.count}</div>
              <div className="text-xs text-slate-500">{formatMoney(b.value)}</div>
            </div>
          ))}
        </div>
      )}

      <div className="table-wrap">
        <table>
          <thead>
            <tr>
              <th>الصنف</th>
              <th>SKU</th>
              <th>الكمية</th>
              <th>القيمة</th>
              <th>آخر بيع</th>
              <th>منذ</th>
              <th>الفئة</th>
            </tr>
          </thead>
          <tbody>
            {isLoading ? (
              Array.from({ length: 6 }).map((_, i) => <SkeletonRow key={i} cols={7} />)
            ) : !data || data.length === 0 ? (
              <tr>
                <td colSpan={7}>
                  <EmptyState icon={Hourglass} title="لا توجد بيانات" description="جرب تقليل عدد الأيام أو راجع المخزون." />
                </td>
              </tr>
            ) : (
              data.map((r) => {
                const bucket = BUCKETS.find((b) => b.id === r.bucket)!;
                return (
                  <tr key={r.productId}>
                    <td className="font-medium">{r.productName}</td>
                    <td className="font-mono text-xs">{r.sku}</td>
                    <td>{formatNumber(r.quantity)}</td>
                    <td className="font-semibold">{formatMoney(r.stockValue)}</td>
                    <td className="text-xs">
                      {r.lastSoldAt ? formatDate(r.lastSoldAt) : <span className="text-slate-400">لم يُبَع</span>}
                    </td>
                    <td className="text-xs">
                      {r.daysSinceLastSale < 0 ? "—" : `${r.daysSinceLastSale} يوم`}
                    </td>
                    <td>
                      <span className={`text-xs px-2 py-0.5 rounded-full ${bucket.color}`}>
                        {bucket.label}
                      </span>
                    </td>
                  </tr>
                );
              })
            )}
          </tbody>
        </table>
      </div>
    </>
  );
}
