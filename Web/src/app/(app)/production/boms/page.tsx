"use client";

import { useState } from "react";
import { useMutation, useQuery, useQueryClient } from "@tanstack/react-query";
import toast from "react-hot-toast";
import { FlaskConical, Pencil, Plus, Trash2, X } from "lucide-react";
import { api, errorMessage } from "@/lib/api";
import { formatMoney } from "@/lib/format";
import type { Bom, Product } from "@/types/api";
import PageHeader from "@/components/PageHeader";
import EmptyState from "@/components/EmptyState";
import { SkeletonRow } from "@/components/Skeleton";
import { useConfirm } from "@/components/ConfirmDialog";

interface ComponentLine {
  productId: string;
  quantity: number;
  wastePercent: number;
}

interface Form {
  id?: string;
  productId: string;
  name: string;
  outputQuantity: number;
  additionalCostPerUnit: number;
  isActive: boolean;
  notes: string;
  components: ComponentLine[];
}

const emptyForm: Form = {
  productId: "",
  name: "",
  outputQuantity: 1,
  additionalCostPerUnit: 0,
  isActive: true,
  notes: "",
  components: [{ productId: "", quantity: 0, wastePercent: 0 }],
};

export default function BomsPage() {
  const qc = useQueryClient();
  const { confirm, dialog } = useConfirm();
  const [form, setForm] = useState<Form>(emptyForm);
  const [showForm, setShowForm] = useState(false);

  const { data, isLoading } = useQuery({
    queryKey: ["boms"],
    queryFn: async () => (await api.get<Bom[]>("/production/boms")).data,
  });

  const products = useQuery({
    queryKey: ["products"],
    queryFn: async () => (await api.get<Product[]>("/Products")).data,
  });

  const save = useMutation({
    mutationFn: async () => {
      const { id, ...payload } = form;
      const body = {
        ...payload,
        notes: payload.notes || null,
        components: payload.components.filter((c) => c.productId && c.quantity > 0),
      };
      if (id) return (await api.put(`/production/boms/${id}`, body)).data;
      return (await api.post("/production/boms", body)).data;
    },
    onSuccess: () => {
      toast.success("تم الحفظ");
      setShowForm(false);
      setForm(emptyForm);
      qc.invalidateQueries({ queryKey: ["boms"] });
    },
    onError: (e) => toast.error(errorMessage(e)),
  });

  const del = useMutation({
    mutationFn: async (id: string) => (await api.delete(`/production/boms/${id}`)).data,
    onSuccess: () => { toast.success("تم الحذف"); qc.invalidateQueries({ queryKey: ["boms"] }); },
    onError: (e) => toast.error(errorMessage(e)),
  });

  function edit(b: Bom) {
    setForm({
      id: b.id,
      productId: b.productId,
      name: b.name,
      outputQuantity: b.outputQuantity,
      additionalCostPerUnit: b.additionalCostPerUnit,
      isActive: b.isActive,
      notes: b.notes ?? "",
      components: b.components.map((c) => ({
        productId: c.productId,
        quantity: c.quantity,
        wastePercent: c.wastePercent,
      })),
    });
    setShowForm(true);
  }

  function setComponent(i: number, patch: Partial<ComponentLine>) {
    const next = [...form.components];
    next[i] = { ...next[i], ...patch };
    setForm({ ...form, components: next });
  }

  function addComponent() {
    setForm({ ...form, components: [...form.components, { productId: "", quantity: 0, wastePercent: 0 }] });
  }

  function removeComponent(i: number) {
    setForm({ ...form, components: form.components.filter((_, idx) => idx !== i) });
  }

  return (
    <>
      <PageHeader title="الوصفات" description="تعريف مكونات المنتج النهائي ونسب الفاقد">
        <button onClick={() => { setForm(emptyForm); setShowForm(true); }} className="btn">
          <Plus size={16} /> وصفة جديدة
        </button>
      </PageHeader>

      {showForm && (
        <div className="card mb-4">
          <div className="flex items-center justify-between mb-3">
            <h3 className="font-semibold">{form.id ? "تعديل وصفة" : "وصفة جديدة"}</h3>
            <button onClick={() => setShowForm(false)} className="text-slate-400 hover:text-slate-700"><X size={20} /></button>
          </div>
          <div className="grid md:grid-cols-3 gap-3 mb-3">
            <div>
              <label>الاسم *</label>
              <input value={form.name} onChange={(e) => setForm({ ...form, name: e.target.value })} />
            </div>
            <div>
              <label>المنتج النهائي *</label>
              <select value={form.productId} onChange={(e) => setForm({ ...form, productId: e.target.value })}>
                <option value="">— اختر —</option>
                {products.data?.map((p) => <option key={p.id} value={p.id}>{p.nameAr}</option>)}
              </select>
            </div>
            <div>
              <label>الناتج (الكمية لكل وصفة)</label>
              <input type="number" step="0.01" value={form.outputQuantity} onChange={(e) => setForm({ ...form, outputQuantity: Number(e.target.value) })} />
            </div>
            <div>
              <label>تكلفة إضافية لكل وحدة (عمالة/مصاريف)</label>
              <input type="number" step="0.01" value={form.additionalCostPerUnit} onChange={(e) => setForm({ ...form, additionalCostPerUnit: Number(e.target.value) })} />
            </div>
            <label className="flex items-center gap-2 mt-6">
              <input type="checkbox" checked={form.isActive} onChange={(e) => setForm({ ...form, isActive: e.target.checked })} className="!w-auto" />
              <span>نشطة</span>
            </label>
            <div className="md:col-span-3">
              <label>ملاحظات</label>
              <input value={form.notes} onChange={(e) => setForm({ ...form, notes: e.target.value })} />
            </div>
          </div>

          <div className="border-t border-slate-200 pt-3">
            <div className="flex items-center justify-between mb-2">
              <h4 className="font-semibold text-sm">المكونات</h4>
              <button onClick={addComponent} className="btn-outline !px-2 !py-1 text-xs">
                <Plus size={14} /> إضافة مكون
              </button>
            </div>
            <div className="table-wrap">
              <table>
                <thead>
                  <tr><th>المنتج</th><th>الكمية</th><th>الفاقد %</th><th></th></tr>
                </thead>
                <tbody>
                  {form.components.map((c, i) => (
                    <tr key={i}>
                      <td>
                        <select value={c.productId} onChange={(e) => setComponent(i, { productId: e.target.value })}>
                          <option value="">— اختر —</option>
                          {products.data?.filter((p) => p.id !== form.productId).map((p) => <option key={p.id} value={p.id}>{p.nameAr}</option>)}
                        </select>
                      </td>
                      <td><input type="number" step="0.01" value={c.quantity} onChange={(e) => setComponent(i, { quantity: Number(e.target.value) })} /></td>
                      <td><input type="number" step="0.1" value={c.wastePercent} onChange={(e) => setComponent(i, { wastePercent: Number(e.target.value) })} /></td>
                      <td>
                        <button onClick={() => removeComponent(i)} disabled={form.components.length === 1} className="btn-outline !px-2 !py-1 text-xs text-red-600">
                          <X size={14} />
                        </button>
                      </td>
                    </tr>
                  ))}
                </tbody>
              </table>
            </div>
          </div>

          <div className="flex gap-2 mt-3">
            <button
              onClick={() => save.mutate()}
              disabled={!form.name.trim() || !form.productId || form.components.every((c) => !c.productId) || save.isPending}
              className="btn-success"
            >
              {save.isPending ? "جاري الحفظ..." : "حفظ"}
            </button>
            <button onClick={() => setShowForm(false)} className="btn-outline">إلغاء</button>
          </div>
        </div>
      )}

      <div className="table-wrap">
        <table>
          <thead>
            <tr><th>الوصفة</th><th>المنتج النهائي</th><th>الناتج</th><th>مكونات</th><th>تكلفة الوحدة</th><th>الحالة</th><th></th></tr>
          </thead>
          <tbody>
            {isLoading ? (
              Array.from({ length: 4 }).map((_, i) => <SkeletonRow key={i} cols={7} />)
            ) : data?.length === 0 ? (
              <tr><td colSpan={7}>
                <EmptyState icon={FlaskConical} title="لا توجد وصفات" description="أضف أول وصفة إنتاج." actionLabel="إضافة وصفة" onAction={() => { setForm(emptyForm); setShowForm(true); }} />
              </td></tr>
            ) : (
              data?.map((b) => (
                <tr key={b.id}>
                  <td className="font-medium">{b.name}</td>
                  <td>{b.productName ?? "—"}</td>
                  <td>{b.outputQuantity}</td>
                  <td>{b.components.length}</td>
                  <td>{formatMoney(b.estimatedUnitCost)}</td>
                  <td>
                    <span className={`text-xs px-2 py-0.5 rounded-full ${b.isActive ? "bg-emerald-100 text-emerald-800" : "bg-slate-200 text-slate-600"}`}>
                      {b.isActive ? "نشطة" : "متوقفة"}
                    </span>
                  </td>
                  <td className="flex gap-1">
                    <button onClick={() => edit(b)} className="btn-outline !px-2 !py-1 text-xs"><Pencil size={14} /></button>
                    <button onClick={async () => { if (await confirm({ title: "حذف الوصفة؟", message: b.name })) del.mutate(b.id); }} className="btn-outline !px-2 !py-1 text-xs text-red-600"><Trash2 size={14} /></button>
                  </td>
                </tr>
              ))
            )}
          </tbody>
        </table>
      </div>
      {dialog}
    </>
  );
}
