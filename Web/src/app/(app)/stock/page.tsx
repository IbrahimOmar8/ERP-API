"use client";

import { useState } from "react";
import { useQuery } from "@tanstack/react-query";
import { api } from "@/lib/api";
import { formatMoney, formatNumber } from "@/lib/format";
import type { StockReportRow, Warehouse } from "@/types/api";
import PageHeader from "@/components/PageHeader";

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

  return (
    <>
      <PageHeader title="تقرير الرصيد" description={`قيمة المخزون: ${formatMoney(total)}`} />
      <div className="card mb-3 flex flex-wrap gap-3 items-end">
        <div>
          <label>المخزن</label>
          <select value={warehouseId} onChange={(e) => setWarehouseId(e.target.value)} className="w-56">
            <option value="">كل المخازن</option>
            {warehouses.data?.map((w) => (
              <option key={w.id} value={w.id}>{w.nameAr}</option>
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
            {isLoading ? (
              <tr><td colSpan={6} className="text-center py-6">جاري التحميل...</td></tr>
            ) : (
              data?.map((r, idx) => (
                <tr key={`${r.productId}-${r.warehouseId}-${idx}`} className={r.isLow ? "bg-red-50" : ""}>
                  <td>{r.productName}</td>
                  <td className="font-mono text-xs">{r.sku}</td>
                  <td>{r.warehouseName}</td>
                  <td>{formatNumber(r.quantity)}</td>
                  <td>{formatMoney(r.averageCost)}</td>
                  <td className="font-semibold">{formatMoney(r.stockValue)}</td>
                </tr>
              ))
            )}
          </tbody>
        </table>
      </div>
    </>
  );
}
