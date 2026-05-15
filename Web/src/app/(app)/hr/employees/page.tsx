"use client";

import { useState } from "react";
import { useMutation, useQuery, useQueryClient } from "@tanstack/react-query";
import toast from "react-hot-toast";
import { Pencil, Plus, Trash2, Users, X } from "lucide-react";
import { api, errorMessage } from "@/lib/api";
import { formatDate, formatMoney } from "@/lib/format";
import type { HrEmployee, Position } from "@/types/api";
import { EmpStatusLabel } from "@/types/api";
import PageHeader from "@/components/PageHeader";
import EmptyState from "@/components/EmptyState";
import { SkeletonRow } from "@/components/Skeleton";
import { useConfirm } from "@/components/ConfirmDialog";

interface Form {
  id?: string;
  name: string;
  email: string;
  phone: string;
  nationalId: string;
  address: string;
  hireDate: string;
  status: number;
  departmentId: string;
  positionId: string;
  baseSalary: number;
  allowances: number;
  deductions: number;
  overtimeHourlyRate: number;
  bankName: string;
  bankAccount: string;
  notes: string;
}

const emptyForm: Form = {
  name: "", email: "", phone: "", nationalId: "", address: "",
  hireDate: new Date().toISOString().slice(0, 10),
  status: 0, departmentId: "", positionId: "",
  baseSalary: 0, allowances: 0, deductions: 0, overtimeHourlyRate: 0,
  bankName: "", bankAccount: "", notes: "",
};

export default function HrEmployeesPage() {
  const qc = useQueryClient();
  const { confirm, dialog } = useConfirm();
  const [statusFilter, setStatusFilter] = useState<number | "">("");
  const [form, setForm] = useState<Form>(emptyForm);
  const [showForm, setShowForm] = useState(false);

  const { data, isLoading } = useQuery({
    queryKey: ["hr-employees", statusFilter],
    queryFn: async () =>
      (await api.get<HrEmployee[]>("/hr/employees", {
        params: { status: statusFilter === "" ? undefined : statusFilter },
      })).data,
  });

  const departments = useQuery({
    queryKey: ["departments"],
    queryFn: async () => (await api.get<{ id: string; name: string }[]>("/Departments")).data,
  });

  const positions = useQuery({
    queryKey: ["hr-positions"],
    queryFn: async () => (await api.get<Position[]>("/hr/positions")).data,
  });

  const save = useMutation({
    mutationFn: async () => {
      const { id, ...payload } = form;
      const body = {
        ...payload,
        positionId: payload.positionId || null,
        email: payload.email || null,
        phone: payload.phone || null,
        nationalId: payload.nationalId || null,
        address: payload.address || null,
        bankName: payload.bankName || null,
        bankAccount: payload.bankAccount || null,
        notes: payload.notes || null,
      };
      if (id) return (await api.put(`/hr/employees/${id}`, body)).data;
      return (await api.post("/hr/employees", body)).data;
    },
    onSuccess: () => {
      toast.success("تم الحفظ");
      setShowForm(false);
      setForm(emptyForm);
      qc.invalidateQueries({ queryKey: ["hr-employees"] });
    },
    onError: (e) => toast.error(errorMessage(e)),
  });

  const del = useMutation({
    mutationFn: async (id: string) => (await api.delete(`/hr/employees/${id}`)).data,
    onSuccess: () => {
      toast.success("تم الحذف");
      qc.invalidateQueries({ queryKey: ["hr-employees"] });
    },
    onError: (e) => toast.error(errorMessage(e)),
  });

  function edit(e: HrEmployee) {
    setForm({
      id: e.id,
      name: e.name,
      email: e.email ?? "",
      phone: e.phone ?? "",
      nationalId: e.nationalId ?? "",
      address: e.address ?? "",
      hireDate: e.hireDate.slice(0, 10),
      status: e.status,
      departmentId: e.departmentId,
      positionId: e.positionId ?? "",
      baseSalary: e.baseSalary,
      allowances: e.allowances,
      deductions: e.deductions,
      overtimeHourlyRate: e.overtimeHourlyRate,
      bankName: e.bankName ?? "",
      bankAccount: e.bankAccount ?? "",
      notes: e.notes ?? "",
    });
    setShowForm(true);
  }

  return (
    <>
      <PageHeader title="الموظفون" description="الملفات الكاملة للموظفين">
        <button onClick={() => { setForm(emptyForm); setShowForm(true); }} className="btn">
          <Plus size={16} /> موظف جديد
        </button>
      </PageHeader>

      {showForm && (
        <div className="card mb-4">
          <div className="flex items-center justify-between mb-3">
            <h3 className="font-semibold">{form.id ? "تعديل موظف" : "موظف جديد"}</h3>
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
              <label>البريد</label>
              <input value={form.email} onChange={(e) => setForm({ ...form, email: e.target.value })} />
            </div>
            <div>
              <label>الهاتف</label>
              <input value={form.phone} onChange={(e) => setForm({ ...form, phone: e.target.value })} />
            </div>
            <div>
              <label>الرقم القومي</label>
              <input value={form.nationalId} onChange={(e) => setForm({ ...form, nationalId: e.target.value })} />
            </div>
            <div>
              <label>تاريخ التعيين</label>
              <input type="date" value={form.hireDate} onChange={(e) => setForm({ ...form, hireDate: e.target.value })} />
            </div>
            <div>
              <label>الحالة</label>
              <select value={form.status} onChange={(e) => setForm({ ...form, status: Number(e.target.value) })}>
                {Object.entries(EmpStatusLabel).map(([k, v]) => <option key={k} value={k}>{v}</option>)}
              </select>
            </div>
            <div>
              <label>الإدارة *</label>
              <select value={form.departmentId} onChange={(e) => setForm({ ...form, departmentId: e.target.value })}>
                <option value="">— اختر —</option>
                {departments.data?.map((d) => <option key={d.id} value={d.id}>{d.name}</option>)}
              </select>
            </div>
            <div>
              <label>الوظيفة</label>
              <select
                value={form.positionId}
                onChange={(e) => {
                  const pid = e.target.value;
                  const p = positions.data?.find((x) => x.id === pid);
                  setForm({ ...form, positionId: pid, baseSalary: p?.baseSalary || form.baseSalary });
                }}
              >
                <option value="">— غير محدد —</option>
                {positions.data?.map((p) => <option key={p.id} value={p.id}>{p.title}</option>)}
              </select>
            </div>
            <div>
              <label>الراتب الأساسي</label>
              <input type="number" value={form.baseSalary} onChange={(e) => setForm({ ...form, baseSalary: Number(e.target.value) })} />
            </div>
            <div>
              <label>البدلات</label>
              <input type="number" value={form.allowances} onChange={(e) => setForm({ ...form, allowances: Number(e.target.value) })} />
            </div>
            <div>
              <label>الاستقطاعات الثابتة</label>
              <input type="number" value={form.deductions} onChange={(e) => setForm({ ...form, deductions: Number(e.target.value) })} />
            </div>
            <div>
              <label>أجر الساعة الإضافية</label>
              <input type="number" value={form.overtimeHourlyRate} onChange={(e) => setForm({ ...form, overtimeHourlyRate: Number(e.target.value) })} />
            </div>
            <div>
              <label>البنك</label>
              <input value={form.bankName} onChange={(e) => setForm({ ...form, bankName: e.target.value })} />
            </div>
            <div>
              <label>رقم الحساب</label>
              <input value={form.bankAccount} onChange={(e) => setForm({ ...form, bankAccount: e.target.value })} />
            </div>
            <div className="md:col-span-3">
              <label>العنوان</label>
              <input value={form.address} onChange={(e) => setForm({ ...form, address: e.target.value })} />
            </div>
            <div className="md:col-span-3">
              <label>ملاحظات</label>
              <input value={form.notes} onChange={(e) => setForm({ ...form, notes: e.target.value })} />
            </div>
          </div>
          <div className="flex gap-2 mt-3">
            <button onClick={() => save.mutate()} disabled={!form.name.trim() || !form.departmentId || save.isPending} className="btn-success">
              {save.isPending ? "جاري الحفظ..." : "حفظ"}
            </button>
            <button onClick={() => setShowForm(false)} className="btn-outline">إلغاء</button>
          </div>
        </div>
      )}

      <div className="card mb-3 flex items-center gap-3">
        <label className="text-sm">الحالة:</label>
        <select
          className="!w-auto"
          value={statusFilter}
          onChange={(e) => setStatusFilter(e.target.value === "" ? "" : Number(e.target.value))}
        >
          <option value="">الكل</option>
          {Object.entries(EmpStatusLabel).map(([k, v]) => <option key={k} value={k}>{v}</option>)}
        </select>
      </div>

      <div className="table-wrap">
        <table>
          <thead>
            <tr>
              <th>الاسم</th>
              <th>الإدارة</th>
              <th>الوظيفة</th>
              <th>تاريخ التعيين</th>
              <th>الراتب الأساسي</th>
              <th>الحالة</th>
              <th></th>
            </tr>
          </thead>
          <tbody>
            {isLoading ? (
              Array.from({ length: 5 }).map((_, i) => <SkeletonRow key={i} cols={7} />)
            ) : data?.length === 0 ? (
              <tr>
                <td colSpan={7}>
                  <EmptyState
                    icon={Users}
                    title="لا يوجد موظفون"
                    description="أضف أول موظف."
                    actionLabel="إضافة موظف"
                    onAction={() => { setForm(emptyForm); setShowForm(true); }}
                  />
                </td>
              </tr>
            ) : (
              data?.map((e) => (
                <tr key={e.id}>
                  <td className="font-medium">
                    <a href={`/hr/employees/${e.id}`} className="hover:text-brand hover:underline">
                      {e.name}
                    </a>
                  </td>
                  <td>{e.departmentName ?? "—"}</td>
                  <td>{e.positionTitle ?? "—"}</td>
                  <td>{formatDate(e.hireDate)}</td>
                  <td>{formatMoney(e.baseSalary)}</td>
                  <td>
                    <span className={`text-xs px-2 py-0.5 rounded-full ${
                      e.status === 0 ? "bg-emerald-100 text-emerald-800"
                      : e.status === 1 ? "bg-amber-100 text-amber-800"
                      : "bg-slate-200 text-slate-600"
                    }`}>
                      {EmpStatusLabel[e.status]}
                    </span>
                  </td>
                  <td className="flex gap-1">
                    <button onClick={() => edit(e)} className="btn-outline !px-2 !py-1 text-xs">
                      <Pencil size={14} /> تعديل
                    </button>
                    <button
                      onClick={async () => {
                        if (await confirm({ title: "حذف الموظف؟", message: `حذف "${e.name}"؟ سيتم تحويله إلى غير نشط إذا كان لديه سجل رواتب.` })) del.mutate(e.id);
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
