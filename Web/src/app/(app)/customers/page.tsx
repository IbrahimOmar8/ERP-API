"use client";

import { useState } from "react";
import { useMutation, useQuery, useQueryClient } from "@tanstack/react-query";
import toast from "react-hot-toast";
import { Download, Pencil, Plus, Users as UsersIcon, X } from "lucide-react";
import { api, errorMessage } from "@/lib/api";
import { formatMoney } from "@/lib/format";
import { downloadCsv } from "@/lib/csv";
import type { Customer } from "@/types/api";
import PageHeader from "@/components/PageHeader";
import EmptyState from "@/components/EmptyState";
import { SkeletonRow } from "@/components/Skeleton";

interface FormState {
  id?: string;
  name: string;
  phone: string;
  email: string;
  address: string;
  taxRegistrationNumber: string;
  nationalId: string;
  isCompany: boolean;
  creditLimit: number;
}

const emptyForm: FormState = {
  name: "",
  phone: "",
  email: "",
  address: "",
  taxRegistrationNumber: "",
  nationalId: "",
  isCompany: false,
  creditLimit: 0,
};

export default function CustomersPage() {
  const qc = useQueryClient();
  const [search, setSearch] = useState("");
  const [form, setForm] = useState<FormState>(emptyForm);
  const [showForm, setShowForm] = useState(false);

  const { data, isLoading } = useQuery({
    queryKey: ["customers", search],
    queryFn: async () =>
      (await api.get<Customer[]>("/Customers", { params: { search: search || undefined } })).data,
  });

  const save = useMutation({
    mutationFn: async () => {
      const { id, ...payload } = form;
      if (id) return (await api.put(`/Customers/${id}`, payload)).data;
      return (await api.post("/Customers", payload)).data;
    },
    onSuccess: () => {
      toast.success("تم الحفظ");
      setShowForm(false);
      setForm(emptyForm);
      qc.invalidateQueries({ queryKey: ["customers"] });
    },
    onError: (e) => toast.error(errorMessage(e)),
  });

  function edit(c: Customer) {
    setForm({
      id: c.id,
      name: c.name,
      phone: c.phone ?? "",
      email: c.email ?? "",
      address: c.address ?? "",
      taxRegistrationNumber: c.taxRegistrationNumber ?? "",
      nationalId: c.nationalId ?? "",
      isCompany: c.isCompany,
      creditLimit: c.creditLimit,
    });
    setShowForm(true);
  }

  return (
    <>
      <PageHeader title="العملاء">
        <button
          onClick={() => data && downloadCsv("customers", data, [
            { header: "الاسم", accessor: (c) => c.name },
            { header: "الهاتف", accessor: (c) => c.phone ?? "" },
            { header: "البريد", accessor: (c) => c.email ?? "" },
            { header: "الرقم الضريبي", accessor: (c) => c.taxRegistrationNumber ?? "" },
            { header: "النوع", accessor: (c) => (c.isCompany ? "شركة" : "فرد") },
            { header: "الرصيد", accessor: (c) => c.balance.toFixed(2) },
          ])}
          disabled={!data || data.length === 0}
          className="btn-outline"
        >
          <Download size={16} /> تصدير CSV
        </button>
        <button onClick={() => { setForm(emptyForm); setShowForm(true); }} className="btn">
          <Plus size={16} /> عميل جديد
        </button>
      </PageHeader>

      {showForm && (
        <div className="card mb-4">
          <div className="flex items-center justify-between mb-3">
            <h3 className="font-semibold">{form.id ? "تعديل عميل" : "عميل جديد"}</h3>
            <button onClick={() => setShowForm(false)} className="text-slate-400 hover:text-slate-700">
              <X size={20} />
            </button>
          </div>
          <div className="grid md:grid-cols-3 gap-3">
            <div>
              <label>الاسم *</label>
              <input value={form.name} onChange={(e) => setForm({ ...form, name: e.target.value })} />
            </div>
            <div>
              <label>الهاتف</label>
              <input value={form.phone} onChange={(e) => setForm({ ...form, phone: e.target.value })} />
            </div>
            <div>
              <label>البريد</label>
              <input value={form.email} onChange={(e) => setForm({ ...form, email: e.target.value })} />
            </div>
            <div>
              <label>الرقم الضريبي</label>
              <input value={form.taxRegistrationNumber} onChange={(e) => setForm({ ...form, taxRegistrationNumber: e.target.value })} />
            </div>
            <div>
              <label>الرقم القومي</label>
              <input value={form.nationalId} onChange={(e) => setForm({ ...form, nationalId: e.target.value })} />
            </div>
            <div>
              <label>حد الائتمان</label>
              <input
                type="number"
                value={form.creditLimit}
                onChange={(e) => setForm({ ...form, creditLimit: Number(e.target.value) })}
              />
            </div>
            <div className="md:col-span-2">
              <label>العنوان</label>
              <input value={form.address} onChange={(e) => setForm({ ...form, address: e.target.value })} />
            </div>
            <label className="flex items-center gap-2 mt-6">
              <input
                type="checkbox"
                checked={form.isCompany}
                onChange={(e) => setForm({ ...form, isCompany: e.target.checked })}
                className="!w-auto"
              />
              <span>شركة (B2B)</span>
            </label>
          </div>
          <div className="flex gap-2 mt-3">
            <button onClick={() => save.mutate()} disabled={!form.name.trim() || save.isPending} className="btn-success">
              {save.isPending ? "جاري الحفظ..." : "حفظ"}
            </button>
            <button onClick={() => setShowForm(false)} className="btn-outline">إلغاء</button>
          </div>
        </div>
      )}

      <div className="card mb-3">
        <input
          placeholder="ابحث بالاسم أو الهاتف..."
          value={search}
          onChange={(e) => setSearch(e.target.value)}
        />
      </div>

      <div className="table-wrap">
        <table>
          <thead>
            <tr>
              <th>الاسم</th>
              <th>الهاتف</th>
              <th>الرقم الضريبي</th>
              <th>النوع</th>
              <th>الرصيد</th>
              <th></th>
            </tr>
          </thead>
          <tbody>
            {isLoading ? (
              Array.from({ length: 6 }).map((_, i) => <SkeletonRow key={i} cols={6} />)
            ) : data?.length === 0 ? (
              <tr>
                <td colSpan={6}>
                  <EmptyState
                    icon={UsersIcon}
                    title="لا يوجد عملاء"
                    description="أضف أول عميل لتسجيل فواتيره."
                    actionLabel="إضافة عميل"
                    onAction={() => { setForm(emptyForm); setShowForm(true); }}
                  />
                </td>
              </tr>
            ) : (
              data?.map((c) => (
                <tr key={c.id}>
                  <td className="font-medium">{c.name}</td>
                  <td>{c.phone}</td>
                  <td>{c.taxRegistrationNumber}</td>
                  <td>{c.isCompany ? "شركة" : "فرد"}</td>
                  <td>{formatMoney(c.balance)}</td>
                  <td>
                    <button onClick={() => edit(c)} className="btn-outline !px-2 !py-1 text-xs">
                      <Pencil size={14} /> تعديل
                    </button>
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
