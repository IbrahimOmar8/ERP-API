"use client";

import { useState } from "react";
import { useMutation, useQuery, useQueryClient } from "@tanstack/react-query";
import toast from "react-hot-toast";
import { Briefcase, Pencil, Plus, Trash2, X } from "lucide-react";
import { api, errorMessage } from "@/lib/api";
import { formatMoney } from "@/lib/format";
import type { Position } from "@/types/api";
import PageHeader from "@/components/PageHeader";
import EmptyState from "@/components/EmptyState";
import { SkeletonRow } from "@/components/Skeleton";
import { useConfirm } from "@/components/ConfirmDialog";

interface Form {
  id?: string;
  title: string;
  baseSalary: number;
  departmentId: string;
  description: string;
  isActive: boolean;
}

const emptyForm: Form = { title: "", baseSalary: 0, departmentId: "", description: "", isActive: true };

export default function PositionsPage() {
  const qc = useQueryClient();
  const { confirm, dialog } = useConfirm();
  const [form, setForm] = useState<Form>(emptyForm);
  const [showForm, setShowForm] = useState(false);

  const { data, isLoading } = useQuery({
    queryKey: ["hr-positions"],
    queryFn: async () => (await api.get<Position[]>("/hr/positions")).data,
  });

  const departments = useQuery({
    queryKey: ["departments"],
    queryFn: async () => (await api.get<{ id: string; name: string }[]>("/Departments")).data,
  });

  const save = useMutation({
    mutationFn: async () => {
      const { id, ...payload } = form;
      const body = { ...payload, departmentId: payload.departmentId || null };
      if (id) return (await api.put(`/hr/positions/${id}`, body)).data;
      return (await api.post("/hr/positions", body)).data;
    },
    onSuccess: () => {
      toast.success("تم الحفظ");
      setShowForm(false);
      setForm(emptyForm);
      qc.invalidateQueries({ queryKey: ["hr-positions"] });
    },
    onError: (e) => toast.error(errorMessage(e)),
  });

  const del = useMutation({
    mutationFn: async (id: string) => (await api.delete(`/hr/positions/${id}`)).data,
    onSuccess: () => {
      toast.success("تم الحذف");
      qc.invalidateQueries({ queryKey: ["hr-positions"] });
    },
    onError: (e) => toast.error(errorMessage(e)),
  });

  function edit(p: Position) {
    setForm({
      id: p.id,
      title: p.title,
      baseSalary: p.baseSalary,
      departmentId: p.departmentId ?? "",
      description: p.description ?? "",
      isActive: p.isActive,
    });
    setShowForm(true);
  }

  return (
    <>
      <PageHeader title="الوظائف" description="المسميات الوظيفية والرواتب المرجعية">
        <button onClick={() => { setForm(emptyForm); setShowForm(true); }} className="btn">
          <Plus size={16} /> وظيفة جديدة
        </button>
      </PageHeader>

      {showForm && (
        <div className="card mb-4">
          <div className="flex items-center justify-between mb-3">
            <h3 className="font-semibold">{form.id ? "تعديل وظيفة" : "وظيفة جديدة"}</h3>
            <button onClick={() => setShowForm(false)} className="text-slate-400 hover:text-slate-700">
              <X size={20} />
            </button>
          </div>
          <div className="grid md:grid-cols-3 gap-3">
            <div>
              <label>المسمى *</label>
              <input value={form.title} onChange={(e) => setForm({ ...form, title: e.target.value })} />
            </div>
            <div>
              <label>الراتب الأساسي</label>
              <input type="number" value={form.baseSalary} onChange={(e) => setForm({ ...form, baseSalary: Number(e.target.value) })} />
            </div>
            <div>
              <label>الإدارة</label>
              <select value={form.departmentId} onChange={(e) => setForm({ ...form, departmentId: e.target.value })}>
                <option value="">— غير محدد —</option>
                {departments.data?.map((d) => <option key={d.id} value={d.id}>{d.name}</option>)}
              </select>
            </div>
            <div className="md:col-span-3">
              <label>الوصف</label>
              <input value={form.description} onChange={(e) => setForm({ ...form, description: e.target.value })} />
            </div>
            <label className="flex items-center gap-2 mt-6">
              <input type="checkbox" checked={form.isActive} onChange={(e) => setForm({ ...form, isActive: e.target.checked })} className="!w-auto" />
              <span>نشطة</span>
            </label>
          </div>
          <div className="flex gap-2 mt-3">
            <button onClick={() => save.mutate()} disabled={!form.title.trim() || save.isPending} className="btn-success">
              {save.isPending ? "جاري الحفظ..." : "حفظ"}
            </button>
            <button onClick={() => setShowForm(false)} className="btn-outline">إلغاء</button>
          </div>
        </div>
      )}

      <div className="table-wrap">
        <table>
          <thead>
            <tr>
              <th>المسمى</th>
              <th>الإدارة</th>
              <th>الراتب الأساسي</th>
              <th>عدد الموظفين</th>
              <th>الحالة</th>
              <th></th>
            </tr>
          </thead>
          <tbody>
            {isLoading ? (
              Array.from({ length: 5 }).map((_, i) => <SkeletonRow key={i} cols={6} />)
            ) : data?.length === 0 ? (
              <tr>
                <td colSpan={6}>
                  <EmptyState
                    icon={Briefcase}
                    title="لا توجد وظائف"
                    description="أضف أول مسمى وظيفي."
                    actionLabel="إضافة وظيفة"
                    onAction={() => { setForm(emptyForm); setShowForm(true); }}
                  />
                </td>
              </tr>
            ) : (
              data?.map((p) => (
                <tr key={p.id}>
                  <td className="font-medium">{p.title}</td>
                  <td>{p.departmentName ?? "—"}</td>
                  <td>{formatMoney(p.baseSalary)}</td>
                  <td>{p.employeeCount}</td>
                  <td>
                    <span className={`text-xs px-2 py-0.5 rounded-full ${p.isActive ? "bg-emerald-100 text-emerald-800" : "bg-slate-200 text-slate-600"}`}>
                      {p.isActive ? "نشطة" : "متوقفة"}
                    </span>
                  </td>
                  <td className="flex gap-1">
                    <button onClick={() => edit(p)} className="btn-outline !px-2 !py-1 text-xs">
                      <Pencil size={14} /> تعديل
                    </button>
                    <button
                      onClick={async () => {
                        if (await confirm({ title: "حذف الوظيفة؟", message: `هل تريد حذف "${p.title}"؟` })) del.mutate(p.id);
                      }}
                      className="btn-outline !px-2 !py-1 text-xs text-red-600"
                    >
                      <Trash2 size={14} />
                    </button>
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
