"use client";

import { useState } from "react";
import { useMutation, useQuery, useQueryClient } from "@tanstack/react-query";
import toast from "react-hot-toast";
import { Clock, Pencil, Plus, Trash2, UserPlus, X } from "lucide-react";
import { api, errorMessage } from "@/lib/api";
import { formatDate } from "@/lib/format";
import type { HrEmployee, Shift, ShiftAssignment } from "@/types/api";
import PageHeader from "@/components/PageHeader";
import EmptyState from "@/components/EmptyState";
import { SkeletonRow } from "@/components/Skeleton";
import { useConfirm } from "@/components/ConfirmDialog";

interface Form {
  id?: string;
  name: string;
  startTime: string;
  endTime: string;
  daysMask: number;
  graceMinutes: number;
  standardHours: number;
  overtimeMultiplier: number;
  latePenaltyPerMinute: number;
  isActive: boolean;
}

const emptyForm: Form = {
  name: "",
  startTime: "09:00",
  endTime: "17:00",
  daysMask: 31,
  graceMinutes: 10,
  standardHours: 8,
  overtimeMultiplier: 1.5,
  latePenaltyPerMinute: 0,
  isActive: true,
};

const dayLabels = ["أحد", "اثنين", "ثلاثاء", "أربعاء", "خميس", "جمعة", "سبت"];

export default function ShiftsPage() {
  const qc = useQueryClient();
  const { confirm, dialog } = useConfirm();
  const [tab, setTab] = useState<"shifts" | "assignments">("shifts");
  const [form, setForm] = useState<Form>(emptyForm);
  const [showForm, setShowForm] = useState(false);
  const [assignForm, setAssignForm] = useState({ employeeId: "", shiftId: "", effectiveFrom: new Date().toISOString().slice(0, 10), notes: "" });
  const [showAssign, setShowAssign] = useState(false);

  const shifts = useQuery({
    queryKey: ["hr-shifts"],
    queryFn: async () => (await api.get<Shift[]>("/hr/shifts")).data,
  });

  const assignments = useQuery({
    queryKey: ["hr-shift-assignments"],
    queryFn: async () => (await api.get<ShiftAssignment[]>("/hr/shifts/assignments")).data,
  });

  const employees = useQuery({
    queryKey: ["hr-employees", 0],
    queryFn: async () => (await api.get<HrEmployee[]>("/hr/employees", { params: { status: 0 } })).data,
  });

  const save = useMutation({
    mutationFn: async () => {
      const { id, ...payload } = form;
      if (id) return (await api.put(`/hr/shifts/${id}`, payload)).data;
      return (await api.post("/hr/shifts", payload)).data;
    },
    onSuccess: () => {
      toast.success("تم الحفظ");
      setShowForm(false); setForm(emptyForm);
      qc.invalidateQueries({ queryKey: ["hr-shifts"] });
    },
    onError: (e) => toast.error(errorMessage(e)),
  });

  const del = useMutation({
    mutationFn: async (id: string) => (await api.delete(`/hr/shifts/${id}`)).data,
    onSuccess: () => { toast.success("تم الحذف"); qc.invalidateQueries({ queryKey: ["hr-shifts"] }); },
    onError: (e) => toast.error(errorMessage(e)),
  });

  const assign = useMutation({
    mutationFn: async () =>
      (await api.post("/hr/shifts/assignments", {
        ...assignForm,
        effectiveFrom: assignForm.effectiveFrom ? new Date(assignForm.effectiveFrom).toISOString() : null,
      })).data,
    onSuccess: () => {
      toast.success("تم الإسناد");
      setShowAssign(false);
      setAssignForm({ employeeId: "", shiftId: "", effectiveFrom: new Date().toISOString().slice(0, 10), notes: "" });
      qc.invalidateQueries({ queryKey: ["hr-shift-assignments"] });
    },
    onError: (e) => toast.error(errorMessage(e)),
  });

  const removeAssign = useMutation({
    mutationFn: async (id: string) => (await api.delete(`/hr/shifts/assignments/${id}`)).data,
    onSuccess: () => { toast.success("تم الحذف"); qc.invalidateQueries({ queryKey: ["hr-shift-assignments"] }); },
    onError: (e) => toast.error(errorMessage(e)),
  });

  function edit(s: Shift) {
    setForm({
      id: s.id,
      name: s.name,
      startTime: s.startTime,
      endTime: s.endTime,
      daysMask: s.daysMask,
      graceMinutes: s.graceMinutes,
      standardHours: s.standardHours,
      overtimeMultiplier: s.overtimeMultiplier,
      latePenaltyPerMinute: s.latePenaltyPerMinute,
      isActive: s.isActive,
    });
    setShowForm(true);
  }

  function toggleDay(idx: number) {
    const bit = 1 << idx;
    setForm({ ...form, daysMask: form.daysMask ^ bit });
  }

  return (
    <>
      <PageHeader title="الشيفتات" description="تعريف ساعات العمل والإسناد للموظفين">
        {tab === "shifts" ? (
          <button onClick={() => { setForm(emptyForm); setShowForm(true); }} className="btn">
            <Plus size={16} /> شيفت جديد
          </button>
        ) : (
          <button onClick={() => setShowAssign(true)} className="btn">
            <UserPlus size={16} /> إسناد شيفت
          </button>
        )}
      </PageHeader>

      <div className="flex gap-1 mb-3 border-b border-slate-200">
        <button
          onClick={() => setTab("shifts")}
          className={`px-4 py-2 text-sm border-b-2 ${tab === "shifts" ? "border-brand text-brand font-semibold" : "border-transparent text-slate-500"}`}
        >
          الشيفتات
        </button>
        <button
          onClick={() => setTab("assignments")}
          className={`px-4 py-2 text-sm border-b-2 ${tab === "assignments" ? "border-brand text-brand font-semibold" : "border-transparent text-slate-500"}`}
        >
          إسنادات الموظفين
        </button>
      </div>

      {tab === "shifts" && showForm && (
        <div className="card mb-4">
          <div className="flex items-center justify-between mb-3">
            <h3 className="font-semibold">{form.id ? "تعديل شيفت" : "شيفت جديد"}</h3>
            <button onClick={() => setShowForm(false)} className="text-slate-400 hover:text-slate-700"><X size={20} /></button>
          </div>
          <div className="grid md:grid-cols-3 gap-3">
            <div><label>الاسم *</label><input value={form.name} onChange={(e) => setForm({ ...form, name: e.target.value })} /></div>
            <div><label>وقت البداية</label><input type="time" value={form.startTime} onChange={(e) => setForm({ ...form, startTime: e.target.value })} /></div>
            <div><label>وقت النهاية</label><input type="time" value={form.endTime} onChange={(e) => setForm({ ...form, endTime: e.target.value })} /></div>
            <div><label>مهلة التأخير (دقيقة)</label><input type="number" value={form.graceMinutes} onChange={(e) => setForm({ ...form, graceMinutes: Number(e.target.value) })} /></div>
            <div><label>ساعات الشيفت القياسية</label><input type="number" step="0.5" value={form.standardHours} onChange={(e) => setForm({ ...form, standardHours: Number(e.target.value) })} /></div>
            <div><label>مضاعف الساعة الإضافية</label><input type="number" step="0.1" value={form.overtimeMultiplier} onChange={(e) => setForm({ ...form, overtimeMultiplier: Number(e.target.value) })} /></div>
            <div><label>غرامة التأخير لكل دقيقة</label><input type="number" step="0.01" value={form.latePenaltyPerMinute} onChange={(e) => setForm({ ...form, latePenaltyPerMinute: Number(e.target.value) })} /></div>
            <div className="md:col-span-3">
              <label>أيام العمل</label>
              <div className="flex gap-2 flex-wrap">
                {dayLabels.map((d, i) => (
                  <label key={i} className="flex items-center gap-1 text-sm">
                    <input type="checkbox" checked={(form.daysMask & (1 << i)) !== 0} onChange={() => toggleDay(i)} className="!w-auto" />
                    <span>{d}</span>
                  </label>
                ))}
              </div>
            </div>
            <label className="flex items-center gap-2 mt-2">
              <input type="checkbox" checked={form.isActive} onChange={(e) => setForm({ ...form, isActive: e.target.checked })} className="!w-auto" />
              <span>نشط</span>
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

      {tab === "shifts" && (
        <div className="table-wrap">
          <table>
            <thead>
              <tr><th>الاسم</th><th>الفترة</th><th>ساعات</th><th>مهلة</th><th>إضافي×</th><th>الحالة</th><th></th></tr>
            </thead>
            <tbody>
              {shifts.isLoading ? (
                Array.from({ length: 4 }).map((_, i) => <SkeletonRow key={i} cols={7} />)
              ) : shifts.data?.length === 0 ? (
                <tr><td colSpan={7}>
                  <EmptyState icon={Clock} title="لا توجد شيفتات" description="أضف أول شيفت." actionLabel="إضافة شيفت" onAction={() => { setForm(emptyForm); setShowForm(true); }} />
                </td></tr>
              ) : (
                shifts.data?.map((s) => (
                  <tr key={s.id}>
                    <td className="font-medium">{s.name}</td>
                    <td className="font-mono text-xs">{s.startTime} – {s.endTime}</td>
                    <td>{s.standardHours}</td>
                    <td>{s.graceMinutes}د</td>
                    <td>×{s.overtimeMultiplier}</td>
                    <td>
                      <span className={`text-xs px-2 py-0.5 rounded-full ${s.isActive ? "bg-emerald-100 text-emerald-800" : "bg-slate-200 text-slate-600"}`}>
                        {s.isActive ? "نشط" : "متوقف"}
                      </span>
                    </td>
                    <td className="flex gap-1">
                      <button onClick={() => edit(s)} className="btn-outline !px-2 !py-1 text-xs"><Pencil size={14} /></button>
                      <button onClick={async () => { if (await confirm({ title: "حذف الشيفت؟", message: s.name })) del.mutate(s.id); }} className="btn-outline !px-2 !py-1 text-xs text-red-600"><Trash2 size={14} /></button>
                    </td>
                  </tr>
                ))
              )}
            </tbody>
          </table>
        </div>
      )}

      {tab === "assignments" && showAssign && (
        <div className="card mb-4">
          <div className="flex items-center justify-between mb-3">
            <h3 className="font-semibold">إسناد شيفت لموظف</h3>
            <button onClick={() => setShowAssign(false)} className="text-slate-400 hover:text-slate-700"><X size={20} /></button>
          </div>
          <div className="grid md:grid-cols-3 gap-3">
            <div>
              <label>الموظف *</label>
              <select value={assignForm.employeeId} onChange={(e) => setAssignForm({ ...assignForm, employeeId: e.target.value })}>
                <option value="">— اختر —</option>
                {employees.data?.map((e) => <option key={e.id} value={e.id}>{e.name}</option>)}
              </select>
            </div>
            <div>
              <label>الشيفت *</label>
              <select value={assignForm.shiftId} onChange={(e) => setAssignForm({ ...assignForm, shiftId: e.target.value })}>
                <option value="">— اختر —</option>
                {shifts.data?.filter((s) => s.isActive).map((s) => <option key={s.id} value={s.id}>{s.name}</option>)}
              </select>
            </div>
            <div>
              <label>يبدأ من</label>
              <input type="date" value={assignForm.effectiveFrom} onChange={(e) => setAssignForm({ ...assignForm, effectiveFrom: e.target.value })} />
            </div>
            <div className="md:col-span-3">
              <label>ملاحظات</label>
              <input value={assignForm.notes} onChange={(e) => setAssignForm({ ...assignForm, notes: e.target.value })} />
            </div>
          </div>
          <div className="flex gap-2 mt-3">
            <button onClick={() => assign.mutate()} disabled={!assignForm.employeeId || !assignForm.shiftId || assign.isPending} className="btn-success">
              {assign.isPending ? "جاري..." : "إسناد"}
            </button>
            <button onClick={() => setShowAssign(false)} className="btn-outline">إلغاء</button>
          </div>
        </div>
      )}

      {tab === "assignments" && (
        <div className="table-wrap">
          <table>
            <thead>
              <tr><th>الموظف</th><th>الشيفت</th><th>من</th><th>إلى</th><th>ملاحظات</th><th></th></tr>
            </thead>
            <tbody>
              {assignments.isLoading ? (
                Array.from({ length: 4 }).map((_, i) => <SkeletonRow key={i} cols={6} />)
              ) : assignments.data?.length === 0 ? (
                <tr><td colSpan={6}><EmptyState icon={Clock} title="لا توجد إسنادات" description="أسند شيفتاً لموظف." /></td></tr>
              ) : (
                assignments.data?.map((a) => (
                  <tr key={a.id}>
                    <td className="font-medium">{a.employeeName}</td>
                    <td>{a.shiftName}</td>
                    <td>{formatDate(a.effectiveFrom)}</td>
                    <td>{a.effectiveTo ? formatDate(a.effectiveTo) : "—"}</td>
                    <td className="text-xs">{a.notes}</td>
                    <td>
                      <button onClick={async () => { if (await confirm({ title: "حذف الإسناد؟", message: a.employeeName ?? "" })) removeAssign.mutate(a.id); }} className="btn-outline !px-2 !py-1 text-xs text-red-600"><Trash2 size={14} /></button>
                    </td>
                  </tr>
                ))
              )}
            </tbody>
          </table>
        </div>
      )}
      {dialog}
    </>
  );
}
