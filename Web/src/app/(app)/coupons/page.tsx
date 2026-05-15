"use client";

import { useState } from "react";
import { useMutation, useQuery, useQueryClient } from "@tanstack/react-query";
import toast from "react-hot-toast";
import { Pencil, Plus, Ticket, Trash2 } from "lucide-react";
import { api, errorMessage } from "@/lib/api";
import { formatMoney, formatDate } from "@/lib/format";
import type { Coupon } from "@/types/api";
import PageHeader from "@/components/PageHeader";
import Modal from "@/components/Modal";
import EmptyState from "@/components/EmptyState";
import { SkeletonRow } from "@/components/Skeleton";
import { useConfirm } from "@/components/ConfirmDialog";

interface Form {
  id?: string;
  code: string;
  description: string;
  type: number;
  value: number;
  minSubtotal: number;
  maxDiscountAmount?: number | null;
  validFrom: string;
  validTo: string;
  maxUses?: number | null;
  maxUsesPerCustomer?: number | null;
  isActive: boolean;
}

const emptyForm: Form = {
  code: "",
  description: "",
  type: 1,
  value: 0,
  minSubtotal: 0,
  validFrom: "",
  validTo: "",
  isActive: true,
};

export default function CouponsPage() {
  const qc = useQueryClient();
  const { confirm, dialog } = useConfirm();
  const [open, setOpen] = useState(false);
  const [form, setForm] = useState<Form>(emptyForm);

  const list = useQuery({
    queryKey: ["coupons"],
    queryFn: async () => (await api.get<Coupon[]>("/Coupons")).data,
  });

  const save = useMutation({
    mutationFn: async () => {
      const { id, ...rest } = form;
      const payload = {
        ...rest,
        validFrom: form.validFrom ? new Date(form.validFrom).toISOString() : null,
        validTo: form.validTo ? new Date(form.validTo).toISOString() : null,
        maxDiscountAmount: form.maxDiscountAmount || null,
        maxUses: form.maxUses || null,
        maxUsesPerCustomer: form.maxUsesPerCustomer || null,
      };
      if (id) return (await api.put(`/Coupons/${id}`, payload)).data;
      return (await api.post("/Coupons", payload)).data;
    },
    onSuccess: () => {
      toast.success("تم الحفظ");
      setOpen(false);
      setForm(emptyForm);
      qc.invalidateQueries({ queryKey: ["coupons"] });
    },
    onError: (e) => toast.error(errorMessage(e)),
  });

  const del = useMutation({
    mutationFn: async (id: string) => api.delete(`/Coupons/${id}`),
    onSuccess: () => {
      toast.success("تم الحذف");
      qc.invalidateQueries({ queryKey: ["coupons"] });
    },
    onError: (e) => toast.error(errorMessage(e)),
  });

  function edit(c: Coupon) {
    setForm({
      id: c.id,
      code: c.code,
      description: c.description ?? "",
      type: c.type,
      value: c.value,
      minSubtotal: c.minSubtotal,
      maxDiscountAmount: c.maxDiscountAmount,
      validFrom: c.validFrom?.slice(0, 10) ?? "",
      validTo: c.validTo?.slice(0, 10) ?? "",
      maxUses: c.maxUses,
      maxUsesPerCustomer: c.maxUsesPerCustomer,
      isActive: c.isActive,
    });
    setOpen(true);
  }

  async function remove(c: Coupon) {
    if (await confirm("حذف الكوبون", `حذف ${c.code}؟`)) del.mutate(c.id);
  }

  return (
    <>
      <PageHeader title="الكوبونات" description="أكواد خصم نسبية أو ثابتة">
        <button onClick={() => { setForm(emptyForm); setOpen(true); }} className="btn">
          <Plus size={16} /> كوبون جديد
        </button>
      </PageHeader>

      <div className="table-wrap">
        <table>
          <thead>
            <tr>
              <th>الكود</th>
              <th>الوصف</th>
              <th>النوع</th>
              <th>القيمة</th>
              <th>أقل فاتورة</th>
              <th>صلاحية</th>
              <th>استخدامات</th>
              <th>الحالة</th>
              <th></th>
            </tr>
          </thead>
          <tbody>
            {list.isLoading ? (
              Array.from({ length: 5 }).map((_, i) => <SkeletonRow key={i} cols={9} />)
            ) : list.data?.length === 0 ? (
              <tr>
                <td colSpan={9}>
                  <EmptyState
                    icon={Ticket}
                    title="لا توجد كوبونات"
                    description="أنشئ كوبون خصم لعروضك الترويجية"
                    actionLabel="إضافة كوبون"
                    onAction={() => { setForm(emptyForm); setOpen(true); }}
                  />
                </td>
              </tr>
            ) : (
              list.data?.map((c) => (
                <tr key={c.id} className={!c.isActive ? "opacity-50" : ""}>
                  <td className="font-mono font-bold">{c.code}</td>
                  <td className="text-xs">{c.description}</td>
                  <td>{c.type === 1 ? "نسبة" : "مبلغ ثابت"}</td>
                  <td className="font-semibold">
                    {c.type === 1 ? `${c.value}%` : formatMoney(c.value)}
                  </td>
                  <td>{formatMoney(c.minSubtotal)}</td>
                  <td className="text-xs">
                    {c.validFrom ? formatDate(c.validFrom) : "—"} /
                    {c.validTo ? formatDate(c.validTo) : "—"}
                  </td>
                  <td>
                    {c.usageCount}
                    {c.maxUses ? ` / ${c.maxUses}` : ""}
                  </td>
                  <td>{c.isActive ? "🟢 نشط" : "⚫ معطل"}</td>
                  <td className="flex gap-1">
                    <button onClick={() => edit(c)} className="btn-outline !px-2 !py-1 text-xs">
                      <Pencil size={14} />
                    </button>
                    <button onClick={() => remove(c)} className="btn-danger !px-2 !py-1 text-xs">
                      <Trash2 size={14} />
                    </button>
                  </td>
                </tr>
              ))
            )}
          </tbody>
        </table>
      </div>

      <Modal open={open} title={form.id ? "تعديل كوبون" : "كوبون جديد"} onClose={() => setOpen(false)} size="lg">
        <div className="grid md:grid-cols-2 gap-3">
          <div>
            <label>الكود *</label>
            <input
              value={form.code}
              onChange={(e) => setForm({ ...form, code: e.target.value })}
              className="font-mono uppercase"
              placeholder="SUMMER25"
            />
          </div>
          <div>
            <label>الوصف</label>
            <input
              value={form.description}
              onChange={(e) => setForm({ ...form, description: e.target.value })}
            />
          </div>
          <div>
            <label>نوع الخصم *</label>
            <select value={form.type} onChange={(e) => setForm({ ...form, type: Number(e.target.value) })}>
              <option value={1}>نسبة مئوية %</option>
              <option value={2}>مبلغ ثابت</option>
            </select>
          </div>
          <div>
            <label>القيمة *</label>
            <input
              type="number"
              step="0.01"
              value={form.value}
              onChange={(e) => setForm({ ...form, value: Number(e.target.value) })}
            />
          </div>
          <div>
            <label>الحد الأدنى للفاتورة</label>
            <input
              type="number"
              step="0.01"
              value={form.minSubtotal}
              onChange={(e) => setForm({ ...form, minSubtotal: Number(e.target.value) })}
            />
          </div>
          <div>
            <label>سقف الخصم (للنسبة فقط)</label>
            <input
              type="number"
              step="0.01"
              value={form.maxDiscountAmount ?? ""}
              onChange={(e) => setForm({ ...form, maxDiscountAmount: e.target.value ? Number(e.target.value) : null })}
            />
          </div>
          <div>
            <label>صالح من</label>
            <input type="date" value={form.validFrom} onChange={(e) => setForm({ ...form, validFrom: e.target.value })} />
          </div>
          <div>
            <label>صالح إلى</label>
            <input type="date" value={form.validTo} onChange={(e) => setForm({ ...form, validTo: e.target.value })} />
          </div>
          <div>
            <label>أقصى عدد استخدامات (إجمالي)</label>
            <input
              type="number"
              min={0}
              value={form.maxUses ?? ""}
              onChange={(e) => setForm({ ...form, maxUses: e.target.value ? Number(e.target.value) : null })}
            />
          </div>
          <div>
            <label>أقصى استخدامات لكل عميل</label>
            <input
              type="number"
              min={0}
              value={form.maxUsesPerCustomer ?? ""}
              onChange={(e) => setForm({ ...form, maxUsesPerCustomer: e.target.value ? Number(e.target.value) : null })}
            />
          </div>
          <label className="flex items-center gap-2 mt-2 md:col-span-2">
            <input
              type="checkbox"
              checked={form.isActive}
              onChange={(e) => setForm({ ...form, isActive: e.target.checked })}
              className="!w-auto"
            />
            <span>الكوبون مفعّل</span>
          </label>
        </div>
        <div className="flex justify-end gap-2 mt-4 pt-4 border-t border-slate-200 dark:border-slate-700">
          <button onClick={() => setOpen(false)} className="btn-outline">إلغاء</button>
          <button
            onClick={() => save.mutate()}
            disabled={!form.code.trim() || form.value <= 0 || save.isPending}
            className="btn-success"
          >
            {save.isPending ? "جاري الحفظ..." : "حفظ"}
          </button>
        </div>
      </Modal>
      {dialog}
    </>
  );
}
