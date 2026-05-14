"use client";

import { useState } from "react";
import { useMutation, useQuery, useQueryClient } from "@tanstack/react-query";
import toast from "react-hot-toast";
import { Download, Pencil, Plus, Trash2, Truck } from "lucide-react";
import { api, errorMessage } from "@/lib/api";
import { formatMoney } from "@/lib/format";
import { downloadCsv } from "@/lib/csv";
import type { Supplier } from "@/types/api";
import PageHeader from "@/components/PageHeader";
import Modal from "@/components/Modal";
import EmptyState from "@/components/EmptyState";
import { SkeletonRow } from "@/components/Skeleton";
import { useConfirm } from "@/components/ConfirmDialog";

interface Form {
  id?: string;
  name: string;
  phone: string;
  email: string;
  address: string;
  taxRegistrationNumber: string;
  commercialRegister: string;
}

const emptyForm: Form = {
  name: "",
  phone: "",
  email: "",
  address: "",
  taxRegistrationNumber: "",
  commercialRegister: "",
};

export default function SuppliersPage() {
  const qc = useQueryClient();
  const { confirm, dialog } = useConfirm();
  const [open, setOpen] = useState(false);
  const [form, setForm] = useState<Form>(emptyForm);

  const { data, isLoading } = useQuery({
    queryKey: ["suppliers"],
    queryFn: async () => (await api.get<Supplier[]>("/Suppliers")).data,
  });

  const save = useMutation({
    mutationFn: async () => {
      const { id, ...payload } = form;
      if (id) return (await api.put(`/Suppliers/${id}`, payload)).data;
      return (await api.post("/Suppliers", payload)).data;
    },
    onSuccess: () => {
      toast.success("تم الحفظ");
      setOpen(false);
      setForm(emptyForm);
      qc.invalidateQueries({ queryKey: ["suppliers"] });
    },
    onError: (e) => toast.error(errorMessage(e)),
  });

  const del = useMutation({
    mutationFn: async (id: string) => api.delete(`/Suppliers/${id}`),
    onSuccess: () => {
      toast.success("تم الحذف");
      qc.invalidateQueries({ queryKey: ["suppliers"] });
    },
    onError: (e) => toast.error(errorMessage(e)),
  });

  function edit(s: Supplier) {
    setForm({
      id: s.id,
      name: s.name,
      phone: s.phone ?? "",
      email: s.email ?? "",
      address: s.address ?? "",
      taxRegistrationNumber: s.taxRegistrationNumber ?? "",
      commercialRegister: s.commercialRegister ?? "",
    });
    setOpen(true);
  }

  async function remove(s: Supplier) {
    if (await confirm("حذف المورد", `حذف "${s.name}"؟`)) del.mutate(s.id);
  }

  return (
    <>
      <PageHeader title="الموردون">
        <button
          onClick={() => data && downloadCsv("suppliers", data, [
            { header: "الاسم", accessor: (s) => s.name },
            { header: "الهاتف", accessor: (s) => s.phone ?? "" },
            { header: "البريد", accessor: (s) => s.email ?? "" },
            { header: "الرقم الضريبي", accessor: (s) => s.taxRegistrationNumber ?? "" },
            { header: "الرصيد", accessor: (s) => s.balance.toFixed(2) },
          ])}
          disabled={!data || data.length === 0}
          className="btn-outline"
        >
          <Download size={16} /> تصدير CSV
        </button>
        <button onClick={() => { setForm(emptyForm); setOpen(true); }} className="btn">
          <Plus size={16} /> مورد جديد
        </button>
      </PageHeader>
      <div className="table-wrap">
        <table>
          <thead>
            <tr><th>الاسم</th><th>الهاتف</th><th>الرقم الضريبي</th><th>الرصيد</th><th></th></tr>
          </thead>
          <tbody>
            {isLoading ? (
              Array.from({ length: 5 }).map((_, i) => <SkeletonRow key={i} cols={5} />)
            ) : data?.length === 0 ? (
              <tr>
                <td colSpan={5}>
                  <EmptyState
                    icon={Truck}
                    title="لا يوجد موردون"
                    description="أضف الموردين لتسجيل فواتير الشراء."
                    actionLabel="إضافة مورد"
                    onAction={() => { setForm(emptyForm); setOpen(true); }}
                  />
                </td>
              </tr>
            ) : (
              data?.map((s) => (
                <tr key={s.id}>
                  <td className="font-medium">{s.name}</td>
                  <td>{s.phone}</td>
                  <td>{s.taxRegistrationNumber}</td>
                  <td>{formatMoney(s.balance)}</td>
                  <td className="flex gap-1">
                    <button onClick={() => edit(s)} className="btn-outline !px-2 !py-1 text-xs">
                      <Pencil size={14} /> تعديل
                    </button>
                    <button onClick={() => remove(s)} className="btn-danger !px-2 !py-1 text-xs">
                      <Trash2 size={14} />
                    </button>
                  </td>
                </tr>
              ))
            )}
          </tbody>
        </table>
      </div>

      <Modal open={open} title={form.id ? "تعديل مورد" : "مورد جديد"} onClose={() => setOpen(false)}>
        <div className="grid md:grid-cols-2 gap-3">
          <div className="md:col-span-2"><label>الاسم *</label><input value={form.name} onChange={(e) => setForm({ ...form, name: e.target.value })} /></div>
          <div><label>الهاتف</label><input value={form.phone} onChange={(e) => setForm({ ...form, phone: e.target.value })} /></div>
          <div><label>البريد</label><input value={form.email} onChange={(e) => setForm({ ...form, email: e.target.value })} /></div>
          <div><label>الرقم الضريبي</label><input value={form.taxRegistrationNumber} onChange={(e) => setForm({ ...form, taxRegistrationNumber: e.target.value })} /></div>
          <div><label>السجل التجاري</label><input value={form.commercialRegister} onChange={(e) => setForm({ ...form, commercialRegister: e.target.value })} /></div>
          <div className="md:col-span-2"><label>العنوان</label><input value={form.address} onChange={(e) => setForm({ ...form, address: e.target.value })} /></div>
        </div>
        <div className="flex justify-end gap-2 mt-4">
          <button onClick={() => setOpen(false)} className="btn-outline">إلغاء</button>
          <button onClick={() => save.mutate()} disabled={!form.name.trim() || save.isPending} className="btn-success">
            {save.isPending ? "..." : "حفظ"}
          </button>
        </div>
      </Modal>
      {dialog}
    </>
  );
}
