"use client";

import { useState } from "react";
import { useMutation, useQuery, useQueryClient } from "@tanstack/react-query";
import toast from "react-hot-toast";
import { Plus, Trash2, Wallet, X } from "lucide-react";
import { api, errorMessage } from "@/lib/api";
import { formatDate, formatMoney } from "@/lib/format";
import type { EmployeeLoan, HrEmployee } from "@/types/api";
import { EmployeeLoanStatusLabel } from "@/types/api";
import PageHeader from "@/components/PageHeader";
import EmptyState from "@/components/EmptyState";
import { SkeletonRow } from "@/components/Skeleton";
import { useConfirm } from "@/components/ConfirmDialog";

const today = () => new Date().toISOString().slice(0, 10);

export default function EmployeeLoansPage() {
  const qc = useQueryClient();
  const { confirm, dialog } = useConfirm();
  const [statusFilter, setStatusFilter] = useState<number | "">("");
  const [show, setShow] = useState(false);
  const [form, setForm] = useState({ employeeId: "", amount: 0, installments: 1, issueDate: today(), reason: "", notes: "" });

  const loans = useQuery({
    queryKey: ["employee-loans", statusFilter],
    queryFn: async () =>
      (await api.get<EmployeeLoan[]>("/hr/loans", {
        params: { status: statusFilter === "" ? undefined : statusFilter },
      })).data,
  });

  const employees = useQuery({
    queryKey: ["hr-employees", 0],
    queryFn: async () => (await api.get<HrEmployee[]>("/hr/employees", { params: { status: 0 } })).data,
  });

  const create = useMutation({
    mutationFn: async () =>
      (await api.post("/hr/loans", {
        ...form,
        reason: form.reason || null,
        notes: form.notes || null,
        issueDate: new Date(form.issueDate).toISOString(),
      })).data,
    onSuccess: () => {
      toast.success("تم تسجيل السلفة");
      setShow(false);
      setForm({ employeeId: "", amount: 0, installments: 1, issueDate: today(), reason: "", notes: "" });
      qc.invalidateQueries({ queryKey: ["employee-loans"] });
    },
    onError: (e) => toast.error(errorMessage(e)),
  });

  const cancel = useMutation({
    mutationFn: async (id: string) => (await api.post(`/hr/loans/${id}/cancel`)).data,
    onSuccess: () => { toast.success("تم"); qc.invalidateQueries({ queryKey: ["employee-loans"] }); },
    onError: (e) => toast.error(errorMessage(e)),
  });

  const del = useMutation({
    mutationFn: async (id: string) => (await api.delete(`/hr/loans/${id}`)).data,
    onSuccess: () => { toast.success("تم الحذف"); qc.invalidateQueries({ queryKey: ["employee-loans"] }); },
    onError: (e) => toast.error(errorMessage(e)),
  });

  const monthly = form.amount > 0 && form.installments > 0 ? form.amount / form.installments : 0;

  return (
    <>
      <PageHeader title="سلف الموظفين" description="تُخصم القسط الشهري تلقائياً عند احتساب الراتب">
        <button onClick={() => setShow(true)} className="btn">
          <Plus size={16} /> سلفة جديدة
        </button>
      </PageHeader>

      {show && (
        <div className="card mb-4">
          <div className="flex items-center justify-between mb-3">
            <h3 className="font-semibold">سلفة جديدة</h3>
            <button onClick={() => setShow(false)} className="text-slate-400 hover:text-slate-700"><X size={20} /></button>
          </div>
          <div className="grid md:grid-cols-3 gap-3">
            <div>
              <label>الموظف *</label>
              <select value={form.employeeId} onChange={(e) => setForm({ ...form, employeeId: e.target.value })}>
                <option value="">— اختر —</option>
                {employees.data?.map((e) => <option key={e.id} value={e.id}>{e.name}</option>)}
              </select>
            </div>
            <div>
              <label>المبلغ *</label>
              <input type="number" step="0.01" value={form.amount} onChange={(e) => setForm({ ...form, amount: Number(e.target.value) })} />
            </div>
            <div>
              <label>عدد الأقساط</label>
              <input type="number" min="1" max="60" value={form.installments} onChange={(e) => setForm({ ...form, installments: Number(e.target.value) })} />
            </div>
            <div>
              <label>تاريخ الصرف</label>
              <input type="date" value={form.issueDate} onChange={(e) => setForm({ ...form, issueDate: e.target.value })} />
            </div>
            <div className="md:col-span-2">
              <label>السبب</label>
              <input value={form.reason} onChange={(e) => setForm({ ...form, reason: e.target.value })} />
            </div>
            <div className="md:col-span-3">
              <label>ملاحظات</label>
              <input value={form.notes} onChange={(e) => setForm({ ...form, notes: e.target.value })} />
            </div>
          </div>
          {monthly > 0 && (
            <div className="bg-slate-50 dark:bg-slate-800 rounded-lg p-3 mt-3 text-sm flex justify-between">
              <span>القسط الشهري المتوقع</span>
              <span className="font-bold">{formatMoney(monthly)}</span>
            </div>
          )}
          <div className="flex gap-2 mt-3">
            <button onClick={() => create.mutate()} disabled={!form.employeeId || form.amount <= 0 || create.isPending} className="btn-success">
              {create.isPending ? "جاري..." : "تسجيل"}
            </button>
            <button onClick={() => setShow(false)} className="btn-outline">إلغاء</button>
          </div>
        </div>
      )}

      <div className="card mb-3 flex items-center gap-3">
        <label className="text-sm">الحالة:</label>
        <select className="!w-auto" value={statusFilter} onChange={(e) => setStatusFilter(e.target.value === "" ? "" : Number(e.target.value))}>
          <option value="">الكل</option>
          {Object.entries(EmployeeLoanStatusLabel).map(([k, v]) => <option key={k} value={k}>{v}</option>)}
        </select>
      </div>

      <div className="table-wrap">
        <table>
          <thead>
            <tr>
              <th>الموظف</th>
              <th>المبلغ</th>
              <th>عدد الأقساط</th>
              <th>القسط الشهري</th>
              <th>مدفوع</th>
              <th>متبقي</th>
              <th>تاريخ الصرف</th>
              <th>الحالة</th>
              <th></th>
            </tr>
          </thead>
          <tbody>
            {loans.isLoading ? (
              Array.from({ length: 4 }).map((_, i) => <SkeletonRow key={i} cols={9} />)
            ) : loans.data?.length === 0 ? (
              <tr><td colSpan={9}>
                <EmptyState icon={Wallet} title="لا توجد سلف" description="لا توجد سلف مسجّلة." actionLabel="سلفة جديدة" onAction={() => setShow(true)} />
              </td></tr>
            ) : (
              loans.data?.map((l) => (
                <tr key={l.id}>
                  <td className="font-medium">{l.employeeName}</td>
                  <td>{formatMoney(l.amount)}</td>
                  <td>{l.installments}</td>
                  <td>{formatMoney(l.monthlyDeduction)}</td>
                  <td className="text-emerald-700">{formatMoney(l.amountRepaid)}</td>
                  <td className="font-bold">{formatMoney(l.remaining)}</td>
                  <td>{formatDate(l.issueDate)}</td>
                  <td>
                    <span className={`text-xs px-2 py-0.5 rounded-full ${
                      l.status === 0 ? "bg-amber-100 text-amber-800"
                      : l.status === 1 ? "bg-emerald-100 text-emerald-800"
                      : "bg-slate-200 text-slate-600"
                    }`}>{EmployeeLoanStatusLabel[l.status]}</span>
                  </td>
                  <td className="flex gap-1">
                    {l.status === 0 && (
                      <button onClick={() => cancel.mutate(l.id)} className="btn-outline !px-2 !py-1 text-xs">إلغاء</button>
                    )}
                    {l.amountRepaid === 0 && (
                      <button onClick={async () => { if (await confirm({ title: "حذف؟", message: l.employeeName ?? "" })) del.mutate(l.id); }} className="btn-outline !px-2 !py-1 text-xs text-red-600"><Trash2 size={14} /></button>
                    )}
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
