"use client";

import { useState } from "react";
import { useMutation, useQuery, useQueryClient } from "@tanstack/react-query";
import toast from "react-hot-toast";
import { Pencil, Plus, Power, Terminal, Trash2 } from "lucide-react";
import { api, errorMessage } from "@/lib/api";
import type { CashRegister, Warehouse } from "@/types/api";
import PageHeader from "@/components/PageHeader";
import Modal from "@/components/Modal";
import EmptyState from "@/components/EmptyState";
import { SkeletonRow } from "@/components/Skeleton";
import { useConfirm } from "@/components/ConfirmDialog";

interface Form {
  id?: string;
  name: string;
  code: string;
  warehouseId: string;
  isActive?: boolean;
}

const emptyForm: Form = { name: "", code: "", warehouseId: "" };

export default function CashRegistersPage() {
  const qc = useQueryClient();
  const { confirm, dialog } = useConfirm();
  const [open, setOpen] = useState(false);
  const [form, setForm] = useState<Form>(emptyForm);

  const list = useQuery({
    queryKey: ["registers"],
    queryFn: async () => (await api.get<CashRegister[]>("/CashRegisters")).data,
  });
  const warehouses = useQuery({
    queryKey: ["warehouses"],
    queryFn: async () => (await api.get<Warehouse[]>("/Warehouses")).data,
  });

  const save = useMutation({
    mutationFn: async () => {
      const { id, isActive: _isActive, ...payload } = form;
      if (id) return (await api.put(`/CashRegisters/${id}`, payload)).data;
      return (await api.post("/CashRegisters", payload)).data;
    },
    onSuccess: () => {
      toast.success("تم الحفظ");
      setOpen(false);
      setForm(emptyForm);
      qc.invalidateQueries({ queryKey: ["registers"] });
    },
    onError: (e) => toast.error(errorMessage(e)),
  });

  const toggleActive = useMutation({
    mutationFn: async ({ id, active }: { id: string; active: boolean }) =>
      api.post(`/CashRegisters/${id}/active/${active}`, {}),
    onSuccess: () => qc.invalidateQueries({ queryKey: ["registers"] }),
    onError: (e) => toast.error(errorMessage(e)),
  });

  const del = useMutation({
    mutationFn: async (id: string) => api.delete(`/CashRegisters/${id}`),
    onSuccess: () => {
      toast.success("تم الحذف");
      qc.invalidateQueries({ queryKey: ["registers"] });
    },
    onError: (e) => toast.error(errorMessage(e)),
  });

  function edit(r: CashRegister) {
    setForm({
      id: r.id,
      name: r.name,
      code: r.code,
      warehouseId: r.warehouseId,
      isActive: r.isActive,
    });
    setOpen(true);
  }

  async function remove(r: CashRegister) {
    if (await confirm("حذف الماكينة", `حذف "${r.name}"؟`)) del.mutate(r.id);
  }

  return (
    <>
      <PageHeader title="ماكينات الكاشير">
        <button onClick={() => { setForm(emptyForm); setOpen(true); }} className="btn">
          <Plus size={16} /> ماكينة جديدة
        </button>
      </PageHeader>
      <div className="table-wrap">
        <table>
          <thead>
            <tr><th>الاسم</th><th>الكود</th><th>المخزن</th><th>الحالة</th><th>جلسة مفتوحة</th><th></th></tr>
          </thead>
          <tbody>
            {list.isLoading ? (
              Array.from({ length: 5 }).map((_, i) => <SkeletonRow key={i} cols={6} />)
            ) : list.data?.length === 0 ? (
              <tr>
                <td colSpan={6}>
                  <EmptyState
                    icon={Terminal}
                    title="لا توجد ماكينات كاشير"
                    description="أضف ماكينة لربطها بمخزن وافتح جلسات للكاشير."
                    actionLabel="إضافة ماكينة"
                    onAction={() => { setForm(emptyForm); setOpen(true); }}
                  />
                </td>
              </tr>
            ) : (
              list.data?.map((r) => (
                <tr key={r.id} className={!r.isActive ? "opacity-50" : ""}>
                  <td className="font-medium">{r.name}</td>
                  <td className="font-mono text-xs">{r.code}</td>
                  <td>{r.warehouseName}</td>
                  <td>{r.isActive ? "🟢 نشط" : "⚫ معطل"}</td>
                  <td>{r.hasOpenSession ? "✅ نعم" : "—"}</td>
                  <td className="flex gap-1">
                    <button onClick={() => edit(r)} className="btn-outline !px-2 !py-1 text-xs"><Pencil size={14} /></button>
                    <button
                      onClick={() => toggleActive.mutate({ id: r.id, active: !r.isActive })}
                      className="btn-outline !px-2 !py-1 text-xs"
                      title={r.isActive ? "تعطيل" : "تفعيل"}
                    ><Power size={14} /></button>
                    <button onClick={() => remove(r)} className="btn-danger !px-2 !py-1 text-xs"><Trash2 size={14} /></button>
                  </td>
                </tr>
              ))
            )}
          </tbody>
        </table>
      </div>

      <Modal open={open} title={form.id ? "تعديل ماكينة" : "ماكينة جديدة"} onClose={() => setOpen(false)}>
        <div className="space-y-3">
          <div><label>الاسم *</label><input value={form.name} onChange={(e) => setForm({ ...form, name: e.target.value })} /></div>
          <div><label>الكود *</label><input value={form.code} onChange={(e) => setForm({ ...form, code: e.target.value })} /></div>
          <div>
            <label>المخزن *</label>
            <select value={form.warehouseId} onChange={(e) => setForm({ ...form, warehouseId: e.target.value })}>
              <option value="">اختر</option>
              {warehouses.data?.map((w) => <option key={w.id} value={w.id}>{w.nameAr}</option>)}
            </select>
          </div>
        </div>
        <div className="flex justify-end gap-2 mt-4">
          <button onClick={() => setOpen(false)} className="btn-outline">إلغاء</button>
          <button
            onClick={() => save.mutate()}
            disabled={!form.name.trim() || !form.code.trim() || !form.warehouseId || save.isPending}
            className="btn-success"
          >
            {save.isPending ? "..." : "حفظ"}
          </button>
        </div>
      </Modal>
      {dialog}
    </>
  );
}
