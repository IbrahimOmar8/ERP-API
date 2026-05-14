"use client";

import { useState } from "react";
import { useQuery } from "@tanstack/react-query";
import { Download, Warehouse as WarehouseIcon } from "lucide-react";
import { api } from "@/lib/api";
import { formatMoney, formatNumber } from "@/lib/format";
import { downloadCsv } from "@/lib/csv";
import type { StockReportRow, Warehouse } from "@/types/api";
import PageHeader from "@/components/PageHeader";
import EmptyState from "@/components/EmptyState";
import { SkeletonRow } from "@/components/Skeleton";

export default function StockPage() {
  const [warehouseId, setWarehouseId] = useState("");
  const [onlyLow, setOnlyLow] = useState(false);

  const warehouses = useQuery({
    queryKey: ["warehouses"],
    queryFn: async () => (await api.get<Warehouse[]>("/Warehouses")).data,
  });

  const { data, isLoading } = useQuery({
    queryKey: ["stock", warehouseId, onlyLow],
    queryFn: async () =>
      (
        await api.get<StockReportRow[]>("/reports/stock", {
          params: { warehouseId: warehouseId || undefined, onlyLow },
        })
      ).data,
  });

  const total = (data ?? []).reduce((s, r) => s + r.stockValue, 0);

  function exportCsv() {
    if (!data) return;
    downloadCsv("stock", data, [
      { header: "الصنف", accessor: (r) => r.productName },
      { header: "SKU", accessor: (r) => r.sku },
      { header: "المخزن", accessor: (r) => r.warehouseName },
      { header: "الكمية", accessor: (r) => r.quantity.toFixed(2) },
      { header: "متوسط التكلفة", accessor: (r) => r.averageCost.toFixed(2) },
      { header: "قيمة الرصيد", accessor: (r) => r.stockValue.toFixed(2) },
      { header: "حد أدنى", accessor: (r) => r.minQuantity.toFixed(2) },
      { header: "بحد أدنى؟", accessor: (r) => (r.isLow ? "نعم" : "لا") },
    ]);
  }

  return (
    <>
      <PageHeader title="تقرير الرصيد" description={`قيمة المخزون: ${formatMoney(total)}`}>
        <button onClick={exportCsv} disabled={!data || data.length === 0} className="btn-outline">
          <Download size={16} /> تصدير CSV
        </button>
      </PageHeader>

      <div className="card mb-3 flex flex-wrap gap-3 items-end">
        <div>
          <label>المخزن</label>
          <select value={warehouseId} onChange={(e) => setWarehouseId(e.target.value)} className="w-56">
            <option value="">كل المخازن</option>
            {warehouses.data?.map((w) => (
              <option key={w.id} value={w.id}>
                {w.nameAr}
              </option>
            ))}
          </select>
        </div>
        <label className="flex items-center gap-2 mb-2">
          <input
            type="checkbox"
            checked={onlyLow}
            onChange={(e) => setOnlyLow(e.target.checked)}
            className="!w-auto"
          />
          <span>أصناف بحد أدنى فقط</span>
        </label>
      </div>

      {!isLoading && data?.length === 0 ? (
        <EmptyState
          icon={WarehouseIcon}
          title="لا توجد بيانات مخزون"
          description={onlyLow ? "لا توجد أصناف وصلت للحد الأدنى." : "لم يتم إضافة أصناف أو تسجيل حركات بعد."}
        />
      ) : (
        <div className="table-wrap">
          <table>
            <thead>
              <tr>
                <th>الصنف</th>
                <th>SKU</th>
                <th>المخزن</th>
                <th>الكمية</th>
                <th>تكلفة الوحدة</th>
                <th>القيمة</th>
              </tr>
            </thead>
            <tbody>
              {isLoading
                ? Array.from({ length: 8 }).map((_, i) => <SkeletonRow key={i} cols={6} />)
                : data?.map((r, idx) => (
                    <tr
                      key={`${r.productId}-${r.warehouseId}-${idx}`}
                      className={r.isLow ? "bg-red-50" : ""}
                    >
                      <td>{r.productName}</td>
                      <td className="font-mono text-xs">{r.sku}</td>
                      <td>{r.warehouseName}</td>
                      <td>{formatNumber(r.quantity)}</td>
                      <td>{formatMoney(r.averageCost)}</td>
                      <td className="font-semibold">{formatMoney(r.stockValue)}</td>
                    </tr>
                  ))}
            </tbody>
          </table>
        </div>
      )}
    </>
  );
}
