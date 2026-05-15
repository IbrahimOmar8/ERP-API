"use client";

import { useState } from "react";
import { useMutation, useQuery, useQueryClient } from "@tanstack/react-query";
import toast from "react-hot-toast";
import { CalendarOff, Check, Plus, Trash2, X } from "lucide-react";
import { api, errorMessage } from "@/lib/api";
import { formatDate } from "@/lib/format";
import type { HrEmployee, LeaveRequest } from "@/types/api";
import { LeaveStatusLabel, LeaveTypeLabel } from "@/types/api";
import PageHeader from "@/components/PageHeader";
import EmptyState from "@/components/EmptyState";
import { SkeletonRow } from "@/components/Skeleton";
import { useConfirm } from "@/components/ConfirmDialog";

export default function LeavesPage() {
  const qc = useQueryClient();
  const { confirm, dialog } = useConfirm();
  const [statusFilter, setStatusFilter] = useState<number | "">("");
  const [show, setShow] = useState(false);
  const today = new Date().toISOString().slice(0, 10);
  const [form, setForm] = useState({ employeeId: "", type: 0, from: today, to: today, reason: "" });

  const employees = useQuery({
    queryKey: ["hr-employees", 0],
    queryFn: async () => (await api.get<HrEmployee[]>("/hr/employees", { params: { status: 0 } })).data,
  });

  const leaves = useQuery({
    queryKey: ["hr-leaves", statusFilter],
    queryFn: async () =>
      (await api.get<LeaveRequest[]>("/hr/leaves", {
        params: { status: statusFilter === "" ? undefined : statusFilter },
      })).data,
  });

  const create = useMutation({
    mutationFn: async () =>
      (await api.post("/hr/leaves", { ...form, from: new Date(form.from).toISOString(), to: new Date(form.to).toISOString() })).data,
    onSuccess: () => {
      toast.success("تم إرسال الطلب");
      setShow(false);
      setForm({ employeeId: "", type: 0, from: today, to: today, reason: "" });
      qc.invalidateQueries({ queryKey: ["hr-leaves"] });
    },
    onError: (e) => toast.error(errorMessage(e)),
  });

  const setStatus = useMutation({
    mutationFn: async ({ id, action }: { id: string; action: "approve" | "reject" | "cancel" }) =>
      (await api.post(`/hr/leaves/${id}/${action}`)).data,
    onSuccess: () => {
      toast.success("تم");
      qc.invalidateQueries({ queryKey: ["hr-leaves"] });
    },
    onError: (e) => toast.error(errorMessage(e)),
  });

  const del = useMutation({
    mutationFn: async (id: string) => (await api.delete(`/hr/leaves/${id}`)).data,
    onSuccess: () => { toast.success("تم الحذف"); qc.invalidateQueries({ queryKey: ["hr-leaves"] }); },
    onError: (e) => toast.error(errorMessage(e)),
  });

  return (
    <>
      <PageHeader title="الإجازات" description="طلبات الإجازة والموافقة عليها">
        <button onClick={() => setShow(true)} className="btn">
          <Plus size={16} /> طلب إجازة
        </button>
      </PageHeader>

      {show && (
        <div className="card mb-4">
          <div className="flex items-center justify-between mb-3">
            <h3 className="font-semibold">طلب إجازة</h3>
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
              <label>النوع</label>
              <select value={form.type} onChange={(e) => setForm({ ...form, type: Number(e.target.value) })}>
                {Object.entries(LeaveTypeLabel).map(([k, v]) => <option key={k} value={k}>{v}</option>)}
              </select>
            </div>
            <div></div>
            <div><label>من *</label><input type="date" value={form.from} onChange={(e) => setForm({ ...form, from: e.target.value })} /></div>
            <div><label>إلى *</label><input type="date" value={form.to} onChange={(e) => setForm({ ...form, to: e.target.value })} /></div>
            <div className="md:col-span-3"><label>السبب</label><input value={form.reason} onChange={(e) => setForm({ ...form, reason: e.target.value })} /></div>
          </div>
          <div className="flex gap-2 mt-3">
            <button onClick={() => create.mutate()} disabled={!form.employeeId || create.isPending} className="btn-success">
              {create.isPending ? "جاري..." : "إرسال"}
            </button>
            <button onClick={() => setShow(false)} className="btn-outline">إلغاء</button>
          </div>
        </div>
      )}

      <div className="card mb-3 flex items-center gap-3">
        <label className="text-sm">الحالة:</label>
        <select className="!w-auto" value={statusFilter} onChange={(e) => setStatusFilter(e.target.value === "" ? "" : Number(e.target.value))}>
          <option value="">الكل</option>
          {Object.entries(LeaveStatusLabel).map(([k, v]) => <option key={k} value={k}>{v}</option>)}
        </select>
      </div>

      <div className="table-wrap">
        <table>
          <thead>
            <tr><th>الموظف</th><th>النوع</th><th>من</th><th>إلى</th><th>الأيام</th><th>السبب</th><th>الحالة</th><th></th></tr>
          </thead>
          <tbody>
            {leaves.isLoading ? (
              Array.from({ length: 5 }).map((_, i) => <SkeletonRow key={i} cols={8} />)
            ) : leaves.data?.length === 0 ? (
              <tr><td colSpan={8}>
                <EmptyState icon={CalendarOff} title="لا توجد طلبات" description="لم يتم تقديم أي طلبات إجازة." actionLabel="طلب إجازة" onAction={() => setShow(true)} />
              </td></tr>
            ) : (
              leaves.data?.map((l) => (
                <tr key={l.id}>
                  <td className="font-medium">{l.employeeName}</td>
                  <td>{LeaveTypeLabel[l.type]}</td>
                  <td>{formatDate(l.from)}</td>
                  <td>{formatDate(l.to)}</td>
                  <td>{l.days}</td>
                  <td className="text-xs">{l.reason}</td>
                  <td>
                    <span className={`text-xs px-2 py-0.5 rounded-full ${
                      l.status === 0 ? "bg-amber-100 text-amber-800"
                      : l.status === 1 ? "bg-emerald-100 text-emerald-800"
                      : l.status === 2 ? "bg-red-100 text-red-800"
                      : "bg-slate-200 text-slate-600"
                    }`}>{LeaveStatusLabel[l.status]}</span>
                  </td>
                  <td className="flex gap-1">
                    {l.status === 0 && (
                      <>
                        <button onClick={() => setStatus.mutate({ id: l.id, action: "approve" })} className="btn-outline !px-2 !py-1 text-xs text-emerald-700"><Check size={14} /> اعتماد</button>
                        <button onClick={() => setStatus.mutate({ id: l.id, action: "reject" })} className="btn-outline !px-2 !py-1 text-xs text-red-700"><X size={14} /> رفض</button>
                      </>
                    )}
                    {l.status !== 0 && l.status !== 3 && (
                      <button onClick={() => setStatus.mutate({ id: l.id, action: "cancel" })} className="btn-outline !px-2 !py-1 text-xs">إلغاء</button>
                    )}
                    <button onClick={async () => { if (await confirm({ title: "حذف الطلب؟", message: l.employeeName ?? "" })) del.mutate(l.id); }} className="btn-outline !px-2 !py-1 text-xs text-red-600"><Trash2 size={14} /></button>
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
