"use client";

import { useState } from "react";
import { useMutation, useQuery, useQueryClient } from "@tanstack/react-query";
import toast from "react-hot-toast";
import { ArrowLeftRight, CheckCircle2, Plus, Trash2, X } from "lucide-react";
import EmptyState from "@/components/EmptyState";
import { SkeletonRow } from "@/components/Skeleton";
import { api, errorMessage } from "@/lib/api";
import { formatMoney, formatDateTime } from "@/lib/format";
import type { Product, StockTransfer, Warehouse } from "@/types/api";
import PageHeader from "@/components/PageHeader";

interface Line {
  productId: string;
  productName: string;
  quantity: number;
}

export default function TransfersPage() {
  const qc = useQueryClient();
  const [showForm, setShowForm] = useState(false);
  const [fromId, setFromId] = useState("");
  const [toId, setToId] = useState("");
  const [notes, setNotes] = useState("");
  const [lines, setLines] = useState<Line[]>([]);
  const [pickProductId, setPickProductId] = useState("");
  const [pickQty, setPickQty] = useState(1);

  const list = useQuery({
    queryKey: ["transfers"],
    queryFn: async () => (await api.get<StockTransfer[]>("/StockTransfers")).data,
  });
  const warehouses = useQuery({
    queryKey: ["warehouses"],
    queryFn: async () => (await api.get<Warehouse[]>("/Warehouses")).data,
  });
  const products = useQuery({
    queryKey: ["products", "all"],
    queryFn: async () => (await api.get<Product[]>("/Products", { params: { pageSize: 500 } })).data,
  });

  function addLine() {
    if (!pickProductId || pickQty <= 0) return;
    const p = products.data?.find((x) => x.id === pickProductId);
    if (!p) return;
    setLines((l) => [...l, { productId: p.id, productName: p.nameAr, quantity: pickQty }]);
    setPickProductId("");
    setPickQty(1);
  }

  const create = useMutation({
    mutationFn: async () =>
      (
        await api.post<StockTransfer>("/StockTransfers", {
          fromWarehouseId: fromId,
          toWarehouseId: toId,
          notes: notes || null,
          items: lines.map((l) => ({ productId: l.productId, quantity: l.quantity })),
        })
      ).data,
    onSuccess: () => {
      toast.success("تم إنشاء التحويل");
      setLines([]);
      setNotes("");
      setShowForm(false);
      qc.invalidateQueries({ queryKey: ["transfers"] });
    },
    onError: (e) => toast.error(errorMessage(e)),
  });

  const complete = useMutation({
    mutationFn: async (id: string) => (await api.post(`/StockTransfers/${id}/complete`, {})).data,
    onSuccess: () => {
      toast.success("تم تنفيذ التحويل");
      qc.invalidateQueries({ queryKey: ["transfers"] });
    },
    onError: (e) => toast.error(errorMessage(e)),
  });

  const cancel = useMutation({
    mutationFn: async (id: string) => (await api.delete(`/StockTransfers/${id}`)).data,
    onSuccess: () => {
      toast.success("تم الإلغاء");
      qc.invalidateQueries({ queryKey: ["transfers"] });
    },
    onError: (e) => toast.error(errorMessage(e)),
  });

  return (
    <>
      <PageHeader title="تحويلات المخازن">
        <button onClick={() => setShowForm(!showForm)} className="btn">
          <Plus size={16} /> تحويل جديد
        </button>
      </PageHeader>

      {showForm && (
        <div className="card mb-4">
          <div className="flex items-center justify-between mb-3">
            <h3 className="font-semibold">تحويل جديد</h3>
            <button onClick={() => setShowForm(false)} className="text-slate-400">
              <X size={20} />
            </button>
          </div>
          <div className="grid md:grid-cols-3 gap-3">
            <div>
              <label>من مخزن</label>
              <select value={fromId} onChange={(e) => setFromId(e.target.value)}>
                <option value="">اختر</option>
                {warehouses.data?.map((w) => <option key={w.id} value={w.id}>{w.nameAr}</option>)}
              </select>
            </div>
            <div>
              <label>إلى مخزن</label>
              <select value={toId} onChange={(e) => setToId(e.target.value)}>
                <option value="">اختر</option>
                {warehouses.data?.map((w) => <option key={w.id} value={w.id}>{w.nameAr}</option>)}
              </select>
            </div>
            <div>
              <label>ملاحظات</label>
              <input value={notes} onChange={(e) => setNotes(e.target.value)} />
            </div>
          </div>
          <div className="mt-3 flex gap-2 items-end flex-wrap">
            <div className="flex-1 min-w-[200px]">
              <label>الصنف</label>
              <select value={pickProductId} onChange={(e) => setPickProductId(e.target.value)}>
                <option value="">اختر صنف</option>
                {products.data?.map((p) => <option key={p.id} value={p.id}>{p.nameAr} ({p.sku})</option>)}
              </select>
            </div>
            <div className="w-32">
              <label>الكمية</label>
              <input type="number" min={0.01} step={0.01} value={pickQty} onChange={(e) => setPickQty(Number(e.target.value))} />
            </div>
            <button onClick={addLine} className="btn-outline">إضافة</button>
          </div>

          {lines.length > 0 && (
            <div className="table-wrap mt-3">
              <table>
                <thead>
                  <tr><th>الصنف</th><th>الكمية</th><th></th></tr>
                </thead>
                <tbody>
                  {lines.map((l, i) => (
                    <tr key={i}>
                      <td>{l.productName}</td>
                      <td>{l.quantity}</td>
                      <td>
                        <button onClick={() => setLines(lines.filter((_, j) => j !== i))} className="text-red-500">
                          <Trash2 size={16} />
                        </button>
                      </td>
                    </tr>
                  ))}
                </tbody>
              </table>
            </div>
          )}
          <button
            onClick={() => create.mutate()}
            disabled={!fromId || !toId || lines.length === 0 || create.isPending}
            className="btn-success mt-3"
          >
            {create.isPending ? "جاري الحفظ..." : "حفظ التحويل"}
          </button>
        </div>
      )}

      <div className="table-wrap">
        <table>
          <thead>
            <tr>
              <th>الرقم</th>
              <th>من</th>
              <th>إلى</th>
              <th>التاريخ</th>
              <th>الأصناف</th>
              <th>القيمة</th>
              <th>الحالة</th>
              <th></th>
            </tr>
          </thead>
          <tbody>
            {list.isLoading ? (
              Array.from({ length: 5 }).map((_, i) => <SkeletonRow key={i} cols={8} />)
            ) : list.data?.length === 0 ? (
              <tr>
                <td colSpan={8}>
                  <EmptyState
                    icon={ArrowLeftRight}
                    title="لا توجد تحويلات"
                    description="استخدم نموذج التحويل أعلاه لنقل أصناف بين المخازن."
                  />
                </td>
              </tr>
            ) : (
              list.data?.map((t) => (
              <tr key={t.id}>
                <td className="font-mono text-xs">{t.transferNumber}</td>
                <td>{t.fromWarehouseName}</td>
                <td>{t.toWarehouseName}</td>
                <td>{formatDateTime(t.transferDate)}</td>
                <td>{t.items.length}</td>
                <td>{formatMoney(t.totalValue)}</td>
                <td>
                  {t.isCompleted ? (
                    <span className="text-emerald-600 font-medium">✅ منفذ</span>
                  ) : (
                    <span className="text-amber-600 font-medium">⏳ قيد التنفيذ</span>
                  )}
                </td>
                <td>
                  {!t.isCompleted && (
                    <div className="flex gap-1">
                      <button
                        onClick={() => complete.mutate(t.id)}
                        className="btn-success !px-2 !py-1 text-xs"
                      >
                        <CheckCircle2 size={14} /> تنفيذ
                      </button>
                      <button
                        onClick={() => cancel.mutate(t.id)}
                        className="btn-danger !px-2 !py-1 text-xs"
                      >
                        <Trash2 size={14} /> إلغاء
                      </button>
                    </div>
                  )}
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
