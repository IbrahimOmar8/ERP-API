"use client";

import { useMemo, useState } from "react";
import { useMutation, useQuery, useQueryClient } from "@tanstack/react-query";
import toast from "react-hot-toast";
import { Save, Search } from "lucide-react";
import { api, errorMessage } from "@/lib/api";
import { formatMoney, formatNumber } from "@/lib/format";
import type { StockReportRow, Warehouse } from "@/types/api";
import PageHeader from "@/components/PageHeader";

interface AdjustRow {
  productId: string;
  productName: string;
  sku: string;
  systemQty: number;
  newQty: number;
  diff: number;
}

export default function StockAdjustmentPage() {
  const qc = useQueryClient();
  const [warehouseId, setWarehouseId] = useState("");
  const [search, setSearch] = useState("");
  const [adjustments, setAdjustments] = useState<Record<string, number>>({});
  const [reason, setReason] = useState("جرد دوري");

  const warehouses = useQuery({
    queryKey: ["warehouses"],
    queryFn: async () => (await api.get<Warehouse[]>("/Warehouses")).data,
  });

  const stock = useQuery({
    enabled: !!warehouseId,
    queryKey: ["stock", warehouseId],
    queryFn: async () =>
      (await api.get<StockReportRow[]>("/reports/stock", { params: { warehouseId } })).data,
  });

  const filtered = useMemo(() => {
    if (!stock.data) return [];
    const q = search.trim().toLowerCase();
    return q
      ? stock.data.filter((r) => r.productName.toLowerCase().includes(q) || r.sku.toLowerCase().includes(q))
      : stock.data;
  }, [stock.data, search]);

  const adjust = useMutation({
    mutationFn: async () => {
      const promises = Object.entries(adjustments).map(([productId, newQty]) =>
        api.post("/Stock/adjust", { productId, warehouseId, newQuantity: newQty, reason })
      );
      await Promise.all(promises);
    },
    onSuccess: () => {
      toast.success(`تم تسوية ${Object.keys(adjustments).length} صنف`);
      setAdjustments({});
      qc.invalidateQueries({ queryKey: ["stock"] });
    },
    onError: (e) => toast.error(errorMessage(e)),
  });

  const pendingRows: AdjustRow[] = useMemo(() => {
    if (!stock.data) return [];
    return Object.entries(adjustments).map(([pid, newQty]) => {
      const r = stock.data!.find((x) => x.productId === pid);
      return {
        productId: pid,
        productName: r?.productName ?? "",
        sku: r?.sku ?? "",
        systemQty: r?.quantity ?? 0,
        newQty,
        diff: newQty - (r?.quantity ?? 0),
      };
    });
  }, [adjustments, stock.data]);

  return (
    <>
      <PageHeader title="جرد المخزون" description="تسوية كميات المخزون الفعلية مع كميات النظام" />

      <div className="card mb-4 flex flex-wrap items-end gap-3">
        <div>
          <label>المخزن *</label>
          <select value={warehouseId} onChange={(e) => { setWarehouseId(e.target.value); setAdjustments({}); }}>
            <option value="">اختر</option>
            {warehouses.data?.map((w) => <option key={w.id} value={w.id}>{w.nameAr}</option>)}
          </select>
        </div>
        <div className="flex-1 min-w-[200px]">
          <label>سبب الجرد</label>
          <input value={reason} onChange={(e) => setReason(e.target.value)} />
        </div>
      </div>

      {warehouseId && (
        <>
          <div className="card mb-3">
            <div className="relative">
              <Search className="absolute right-3 top-2.5 text-slate-400" size={18} />
              <input
                placeholder="ابحث عن صنف..."
                value={search}
                onChange={(e) => setSearch(e.target.value)}
                className="pe-10"
              />
            </div>
          </div>

          <div className="table-wrap mb-4">
            <table>
              <thead>
                <tr>
                  <th>الصنف</th>
                  <th>SKU</th>
                  <th>كمية النظام</th>
                  <th>التكلفة</th>
                  <th>الكمية الفعلية</th>
                  <th>الفرق</th>
                </tr>
              </thead>
              <tbody>
                {stock.isLoading ? <tr><td colSpan={6} className="text-center py-6">جاري التحميل...</td></tr> :
                  filtered.map((r) => {
                    const newQty = adjustments[r.productId];
                    const diff = newQty != null ? newQty - r.quantity : 0;
                    return (
                      <tr key={r.productId} className={newQty != null ? "bg-amber-50" : ""}>
                        <td>{r.productName}</td>
                        <td className="font-mono text-xs">{r.sku}</td>
                        <td>{formatNumber(r.quantity)}</td>
                        <td>{formatMoney(r.averageCost)}</td>
                        <td>
                          <input
                            type="number"
                            step="0.01"
                            min={0}
                            value={newQty ?? ""}
                            placeholder={r.quantity.toString()}
                            onChange={(e) => {
                              const v = e.target.value;
                              setAdjustments((a) => {
                                const copy = { ...a };
                                if (v === "") delete copy[r.productId];
                                else copy[r.productId] = Number(v);
                                return copy;
                              });
                            }}
                            className="w-28 !py-1"
                          />
                        </td>
                        <td className={diff > 0 ? "text-emerald-600" : diff < 0 ? "text-red-600" : ""}>
                          {newQty != null ? formatNumber(diff) : "—"}
                        </td>
                      </tr>
                    );
                  })}
              </tbody>
            </table>
          </div>

          {pendingRows.length > 0 && (
            <div className="card sticky bottom-4 border-2 border-amber-400 bg-amber-50">
              <h3 className="font-bold mb-2">تسويات معلّقة ({pendingRows.length})</h3>
              <div className="text-sm space-y-1 mb-3 max-h-32 overflow-y-auto">
                {pendingRows.map((r) => (
                  <div key={r.productId} className="flex justify-between">
                    <span>{r.productName}</span>
                    <span>
                      <span className="text-slate-500">{formatNumber(r.systemQty)} →</span>
                      <strong className="mx-1">{formatNumber(r.newQty)}</strong>
                      <span className={r.diff > 0 ? "text-emerald-600" : "text-red-600"}>
                        ({r.diff > 0 ? "+" : ""}{formatNumber(r.diff)})
                      </span>
                    </span>
                  </div>
                ))}
              </div>
              <button onClick={() => adjust.mutate()} disabled={adjust.isPending} className="btn-success">
                <Save size={16} /> {adjust.isPending ? "جاري التسوية..." : "حفظ التسويات"}
              </button>
            </div>
          )}
        </>
      )}
    </>
  );
}
