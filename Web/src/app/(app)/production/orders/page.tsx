"use client";

import { Fragment, useState } from "react";
import { useMutation, useQuery, useQueryClient } from "@tanstack/react-query";
import toast from "react-hot-toast";
import { Check, ClipboardList, Plus, Trash2, X } from "lucide-react";
import { api, errorMessage } from "@/lib/api";
import { formatDate, formatMoney } from "@/lib/format";
import type { Bom, ProductionOrder, Warehouse } from "@/types/api";
import { ProductionOrderStatusLabel } from "@/types/api";
import PageHeader from "@/components/PageHeader";
import EmptyState from "@/components/EmptyState";
import { SkeletonRow } from "@/components/Skeleton";
import { useConfirm } from "@/components/ConfirmDialog";

export default function ProductionOrdersPage() {
  const qc = useQueryClient();
  const { confirm, dialog } = useConfirm();
  const today = new Date().toISOString().slice(0, 10);
  const [show, setShow] = useState(false);
  const [form, setForm] = useState({ billOfMaterialsId: "", warehouseId: "", quantity: 1, plannedDate: today, notes: "" });
  const [expanded, setExpanded] = useState<string | null>(null);

  const orders = useQuery({
    queryKey: ["production-orders"],
    queryFn: async () => (await api.get<ProductionOrder[]>("/production/orders")).data,
  });

  const boms = useQuery({
    queryKey: ["boms"],
    queryFn: async () => (await api.get<Bom[]>("/production/boms")).data,
  });

  const warehouses = useQuery({
    queryKey: ["warehouses"],
    queryFn: async () => (await api.get<Warehouse[]>("/Warehouses")).data,
  });

  const create = useMutation({
    mutationFn: async () =>
      (await api.post("/production/orders", {
        ...form,
        notes: form.notes || null,
        plannedDate: new Date(form.plannedDate).toISOString(),
      })).data,
    onSuccess: () => {
      toast.success("تم إنشاء أمر الإنتاج");
      setShow(false);
      setForm({ billOfMaterialsId: "", warehouseId: "", quantity: 1, plannedDate: today, notes: "" });
      qc.invalidateQueries({ queryKey: ["production-orders"] });
    },
    onError: (e) => toast.error(errorMessage(e)),
  });

  const action = useMutation({
    mutationFn: async ({ id, op }: { id: string; op: "complete" | "cancel" }) => (await api.post(`/production/orders/${id}/${op}`)).data,
    onSuccess: () => { toast.success("تم"); qc.invalidateQueries({ queryKey: ["production-orders"] }); },
    onError: (e) => toast.error(errorMessage(e)),
  });

  const del = useMutation({
    mutationFn: async (id: string) => (await api.delete(`/production/orders/${id}`)).data,
    onSuccess: () => { toast.success("تم الحذف"); qc.invalidateQueries({ queryKey: ["production-orders"] }); },
    onError: (e) => toast.error(errorMessage(e)),
  });

  const selectedBom = boms.data?.find((b) => b.id === form.billOfMaterialsId);

  return (
    <>
      <PageHeader title="أوامر الإنتاج" description="إصدار أوامر تصنيع وخصم المكونات">
        <button onClick={() => setShow(true)} className="btn">
          <Plus size={16} /> أمر جديد
        </button>
      </PageHeader>

      {show && (
        <div className="card mb-4">
          <div className="flex items-center justify-between mb-3">
            <h3 className="font-semibold">أمر إنتاج جديد</h3>
            <button onClick={() => setShow(false)} className="text-slate-400 hover:text-slate-700"><X size={20} /></button>
          </div>
          <div className="grid md:grid-cols-3 gap-3">
            <div>
              <label>الوصفة *</label>
              <select value={form.billOfMaterialsId} onChange={(e) => setForm({ ...form, billOfMaterialsId: e.target.value })}>
                <option value="">— اختر —</option>
                {boms.data?.filter((b) => b.isActive).map((b) => <option key={b.id} value={b.id}>{b.name} → {b.productName}</option>)}
              </select>
            </div>
            <div>
              <label>المخزن *</label>
              <select value={form.warehouseId} onChange={(e) => setForm({ ...form, warehouseId: e.target.value })}>
                <option value="">— اختر —</option>
                {warehouses.data?.filter((w) => w.isActive).map((w) => <option key={w.id} value={w.id}>{w.nameAr}</option>)}
              </select>
            </div>
            <div>
              <label>الكمية المطلوبة *</label>
              <input type="number" step="0.01" min="0.01" value={form.quantity} onChange={(e) => setForm({ ...form, quantity: Number(e.target.value) })} />
            </div>
            <div>
              <label>تاريخ التنفيذ</label>
              <input type="date" value={form.plannedDate} onChange={(e) => setForm({ ...form, plannedDate: e.target.value })} />
            </div>
            <div className="md:col-span-3">
              <label>ملاحظات</label>
              <input value={form.notes} onChange={(e) => setForm({ ...form, notes: e.target.value })} />
            </div>
          </div>

          {selectedBom && form.quantity > 0 && (
            <div className="bg-slate-50 dark:bg-slate-800 rounded-lg p-3 mt-3 text-sm">
              <div className="font-semibold mb-2">المكونات المتوقع استهلاكها:</div>
              <div className="grid md:grid-cols-2 gap-2">
                {selectedBom.components.map((c) => {
                  const batches = selectedBom.outputQuantity > 0 ? form.quantity / selectedBom.outputQuantity : 0;
                  const qty = c.quantity * batches * (1 + c.wastePercent / 100);
                  return (
                    <div key={c.id} className="flex justify-between">
                      <span>{c.productName}</span>
                      <span className="font-mono">{qty.toFixed(2)}</span>
                    </div>
                  );
                })}
              </div>
              <div className="border-t border-slate-200 mt-2 pt-2 flex justify-between font-semibold">
                <span>التكلفة التقديرية للوحدة</span>
                <span>{formatMoney(selectedBom.estimatedUnitCost)}</span>
              </div>
            </div>
          )}

          <div className="flex gap-2 mt-3">
            <button onClick={() => create.mutate()} disabled={!form.billOfMaterialsId || !form.warehouseId || form.quantity <= 0 || create.isPending} className="btn-success">
              {create.isPending ? "جاري..." : "إنشاء"}
            </button>
            <button onClick={() => setShow(false)} className="btn-outline">إلغاء</button>
          </div>
        </div>
      )}

      <div className="table-wrap">
        <table>
          <thead>
            <tr><th>الرقم</th><th>المنتج</th><th>الكمية</th><th>المخزن</th><th>التاريخ</th><th>التكلفة الإجمالية</th><th>الحالة</th><th></th></tr>
          </thead>
          <tbody>
            {orders.isLoading ? (
              Array.from({ length: 5 }).map((_, i) => <SkeletonRow key={i} cols={8} />)
            ) : orders.data?.length === 0 ? (
              <tr><td colSpan={8}>
                <EmptyState icon={ClipboardList} title="لا توجد أوامر إنتاج" description="ابدأ بإنشاء أمر جديد." actionLabel="أمر جديد" onAction={() => setShow(true)} />
              </td></tr>
            ) : (
              orders.data?.map((o) => (
                <Fragment key={o.id}>
                  <tr className="cursor-pointer" onClick={() => setExpanded(expanded === o.id ? null : o.id)}>
                    <td className="font-mono text-xs">{o.orderNumber}</td>
                    <td className="font-medium">{o.productName}</td>
                    <td>{o.quantity}</td>
                    <td>{o.warehouseName}</td>
                    <td>{formatDate(o.plannedDate)}</td>
                    <td>{o.totalCost > 0 ? formatMoney(o.totalCost) : "—"}</td>
                    <td>
                      <span className={`text-xs px-2 py-0.5 rounded-full ${
                        o.status === 0 ? "bg-slate-100 text-slate-800"
                        : o.status === 1 ? "bg-amber-100 text-amber-800"
                        : o.status === 2 ? "bg-emerald-100 text-emerald-800"
                        : "bg-red-100 text-red-700"
                      }`}>{ProductionOrderStatusLabel[o.status]}</span>
                    </td>
                    <td className="flex gap-1" onClick={(e) => e.stopPropagation()}>
                      {(o.status === 0 || o.status === 1) && (
                        <>
                          <button onClick={() => action.mutate({ id: o.id, op: "complete" })} className="btn-outline !px-2 !py-1 text-xs text-emerald-700"><Check size={14} /> إكمال</button>
                          <button onClick={() => action.mutate({ id: o.id, op: "cancel" })} className="btn-outline !px-2 !py-1 text-xs">إلغاء</button>
                        </>
                      )}
                      {o.status !== 2 && (
                        <button onClick={async () => { if (await confirm({ title: "حذف؟", message: o.orderNumber })) del.mutate(o.id); }} className="btn-outline !px-2 !py-1 text-xs text-red-600"><Trash2 size={14} /></button>
                      )}
                    </td>
                  </tr>
                  {expanded === o.id && (
                    <tr>
                      <td colSpan={8} className="bg-slate-50 dark:bg-slate-800">
                        <div className="p-3 text-sm">
                          <div className="font-semibold mb-2">المكونات</div>
                          <table className="w-full">
                            <thead><tr><th className="text-right">المنتج</th><th>الكمية</th><th>تكلفة الوحدة</th><th>الإجمالي</th></tr></thead>
                            <tbody>
                              {o.items.map((it) => (
                                <tr key={it.id}>
                                  <td>{it.productName}</td>
                                  <td>{it.quantity.toFixed(2)}</td>
                                  <td>{it.unitCost > 0 ? formatMoney(it.unitCost) : "—"}</td>
                                  <td>{it.totalCost > 0 ? formatMoney(it.totalCost) : "—"}</td>
                                </tr>
                              ))}
                            </tbody>
                          </table>
                          {o.notes && <div className="mt-2 text-xs text-slate-600">ملاحظات: {o.notes}</div>}
                        </div>
                      </td>
                    </tr>
                  )}
                </Fragment>
              ))
            )}
          </tbody>
        </table>
      </div>
      {dialog}
    </>
  );
}
