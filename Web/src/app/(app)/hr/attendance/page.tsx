"use client";

import { useState } from "react";
import { useMutation, useQuery, useQueryClient } from "@tanstack/react-query";
import toast from "react-hot-toast";
import { CalendarCheck, LogIn, LogOut, Pencil, Plus, X } from "lucide-react";
import { api, errorMessage } from "@/lib/api";
import { formatDate, formatDateTime } from "@/lib/format";
import type { Attendance, AttendanceSummary, HrEmployee } from "@/types/api";
import { AttendanceStatusLabel } from "@/types/api";
import PageHeader from "@/components/PageHeader";
import EmptyState from "@/components/EmptyState";
import { SkeletonRow } from "@/components/Skeleton";

export default function AttendancePage() {
  const qc = useQueryClient();
  const today = new Date().toISOString().slice(0, 10);
  const monthStart = today.slice(0, 8) + "01";
  const [tab, setTab] = useState<"records" | "summary">("records");
  const [from, setFrom] = useState(monthStart);
  const [to, setTo] = useState(today);
  const [employeeId, setEmployeeId] = useState("");
  const [showManual, setShowManual] = useState(false);
  const [manual, setManual] = useState({ employeeId: "", date: today, checkIn: "", checkOut: "", status: 0, notes: "" });

  const employees = useQuery({
    queryKey: ["hr-employees", 0],
    queryFn: async () => (await api.get<HrEmployee[]>("/hr/employees", { params: { status: 0 } })).data,
  });

  const records = useQuery({
    queryKey: ["hr-attendance", from, to, employeeId],
    queryFn: async () =>
      (await api.get<Attendance[]>("/hr/attendance", {
        params: { from, to, employeeId: employeeId || undefined },
      })).data,
  });

  const summary = useQuery({
    queryKey: ["hr-attendance-summary", from, to],
    queryFn: async () =>
      (await api.get<AttendanceSummary[]>("/hr/attendance/summary", { params: { from, to } })).data,
    enabled: tab === "summary",
  });

  const checkIn = useMutation({
    mutationFn: async (id: string) => (await api.post("/hr/attendance/check-in", { employeeId: id })).data,
    onSuccess: () => {
      toast.success("تم تسجيل الدخول");
      qc.invalidateQueries({ queryKey: ["hr-attendance"] });
    },
    onError: (e) => toast.error(errorMessage(e)),
  });

  const checkOut = useMutation({
    mutationFn: async (id: string) => (await api.post("/hr/attendance/check-out", { employeeId: id })).data,
    onSuccess: () => {
      toast.success("تم تسجيل الخروج");
      qc.invalidateQueries({ queryKey: ["hr-attendance"] });
    },
    onError: (e) => toast.error(errorMessage(e)),
  });

  const upsert = useMutation({
    mutationFn: async () => {
      const body = {
        employeeId: manual.employeeId,
        date: manual.date,
        checkIn: manual.checkIn ? `${manual.date}T${manual.checkIn}:00` : null,
        checkOut: manual.checkOut ? `${manual.date}T${manual.checkOut}:00` : null,
        status: manual.status,
        notes: manual.notes || null,
      };
      return (await api.post("/hr/attendance/manual", body)).data;
    },
    onSuccess: () => {
      toast.success("تم الحفظ");
      setShowManual(false);
      setManual({ employeeId: "", date: today, checkIn: "", checkOut: "", status: 0, notes: "" });
      qc.invalidateQueries({ queryKey: ["hr-attendance"] });
    },
    onError: (e) => toast.error(errorMessage(e)),
  });

  return (
    <>
      <PageHeader title="الحضور والانصراف" description="تسجيل ومتابعة حضور الموظفين">
        <button onClick={() => setShowManual(true)} className="btn">
          <Plus size={16} /> تسجيل يدوي
        </button>
      </PageHeader>

      {/* Quick check-in panel */}
      <div className="card mb-4">
        <h3 className="font-semibold mb-3">تسجيل سريع</h3>
        <div className="grid md:grid-cols-3 gap-3">
          {employees.data?.slice(0, 12).map((e) => (
            <div key={e.id} className="flex items-center justify-between p-2 rounded border border-slate-200">
              <span className="text-sm font-medium truncate">{e.name}</span>
              <div className="flex gap-1">
                <button onClick={() => checkIn.mutate(e.id)} className="btn-outline !px-2 !py-1 text-xs text-emerald-700">
                  <LogIn size={14} /> دخول
                </button>
                <button onClick={() => checkOut.mutate(e.id)} className="btn-outline !px-2 !py-1 text-xs text-amber-700">
                  <LogOut size={14} /> خروج
                </button>
              </div>
            </div>
          ))}
        </div>
      </div>

      <div className="flex gap-1 mb-3 border-b border-slate-200">
        <button onClick={() => setTab("records")} className={`px-4 py-2 text-sm border-b-2 ${tab === "records" ? "border-brand text-brand font-semibold" : "border-transparent text-slate-500"}`}>السجلات</button>
        <button onClick={() => setTab("summary")} className={`px-4 py-2 text-sm border-b-2 ${tab === "summary" ? "border-brand text-brand font-semibold" : "border-transparent text-slate-500"}`}>الملخص</button>
      </div>

      <div className="card mb-3 grid md:grid-cols-3 gap-3">
        <div>
          <label>من</label>
          <input type="date" value={from} onChange={(e) => setFrom(e.target.value)} />
        </div>
        <div>
          <label>إلى</label>
          <input type="date" value={to} onChange={(e) => setTo(e.target.value)} />
        </div>
        {tab === "records" && (
          <div>
            <label>الموظف</label>
            <select value={employeeId} onChange={(e) => setEmployeeId(e.target.value)}>
              <option value="">الكل</option>
              {employees.data?.map((e) => <option key={e.id} value={e.id}>{e.name}</option>)}
            </select>
          </div>
        )}
      </div>

      {showManual && (
        <div className="card mb-4">
          <div className="flex items-center justify-between mb-3">
            <h3 className="font-semibold">تسجيل حضور يدوي</h3>
            <button onClick={() => setShowManual(false)} className="text-slate-400 hover:text-slate-700"><X size={20} /></button>
          </div>
          <div className="grid md:grid-cols-3 gap-3">
            <div>
              <label>الموظف *</label>
              <select value={manual.employeeId} onChange={(e) => setManual({ ...manual, employeeId: e.target.value })}>
                <option value="">— اختر —</option>
                {employees.data?.map((e) => <option key={e.id} value={e.id}>{e.name}</option>)}
              </select>
            </div>
            <div>
              <label>التاريخ *</label>
              <input type="date" value={manual.date} onChange={(e) => setManual({ ...manual, date: e.target.value })} />
            </div>
            <div>
              <label>الحالة</label>
              <select value={manual.status} onChange={(e) => setManual({ ...manual, status: Number(e.target.value) })}>
                {Object.entries(AttendanceStatusLabel).map(([k, v]) => <option key={k} value={k}>{v}</option>)}
              </select>
            </div>
            <div><label>وقت الدخول</label><input type="time" value={manual.checkIn} onChange={(e) => setManual({ ...manual, checkIn: e.target.value })} /></div>
            <div><label>وقت الخروج</label><input type="time" value={manual.checkOut} onChange={(e) => setManual({ ...manual, checkOut: e.target.value })} /></div>
            <div className="md:col-span-3"><label>ملاحظات</label><input value={manual.notes} onChange={(e) => setManual({ ...manual, notes: e.target.value })} /></div>
          </div>
          <div className="flex gap-2 mt-3">
            <button onClick={() => upsert.mutate()} disabled={!manual.employeeId || !manual.date || upsert.isPending} className="btn-success">
              {upsert.isPending ? "جاري..." : "حفظ"}
            </button>
            <button onClick={() => setShowManual(false)} className="btn-outline">إلغاء</button>
          </div>
        </div>
      )}

      {tab === "records" ? (
        <div className="table-wrap">
          <table>
            <thead>
              <tr><th>الموظف</th><th>التاريخ</th><th>دخول</th><th>خروج</th><th>الساعات</th><th>إضافي</th><th>تأخير</th><th>الحالة</th></tr>
            </thead>
            <tbody>
              {records.isLoading ? (
                Array.from({ length: 6 }).map((_, i) => <SkeletonRow key={i} cols={8} />)
              ) : records.data?.length === 0 ? (
                <tr><td colSpan={8}><EmptyState icon={CalendarCheck} title="لا توجد سجلات" description="اختر فترة مختلفة أو سجّل حضوراً." /></td></tr>
              ) : (
                records.data?.map((r) => (
                  <tr key={r.id}>
                    <td className="font-medium">{r.employeeName}</td>
                    <td>{formatDate(r.date)}</td>
                    <td className="font-mono text-xs">{r.checkIn ? formatDateTime(r.checkIn).split(" ")[1] : "—"}</td>
                    <td className="font-mono text-xs">{r.checkOut ? formatDateTime(r.checkOut).split(" ")[1] : "—"}</td>
                    <td>{r.workedHours.toFixed(2)}</td>
                    <td>{r.overtimeHours > 0 ? <span className="text-emerald-700">+{r.overtimeHours.toFixed(2)}</span> : "—"}</td>
                    <td>{r.lateMinutes > 0 ? <span className="text-amber-700">{r.lateMinutes}د</span> : "—"}</td>
                    <td>
                      <span className={`text-xs px-2 py-0.5 rounded-full ${
                        r.status === 0 ? "bg-emerald-100 text-emerald-800"
                        : r.status === 1 ? "bg-red-100 text-red-800"
                        : r.status === 2 ? "bg-amber-100 text-amber-800"
                        : r.status === 3 ? "bg-blue-100 text-blue-800"
                        : "bg-slate-100 text-slate-700"
                      }`}>{AttendanceStatusLabel[r.status]}</span>
                    </td>
                  </tr>
                ))
              )}
            </tbody>
          </table>
        </div>
      ) : (
        <div className="table-wrap">
          <table>
            <thead>
              <tr><th>الموظف</th><th>أيام الحضور</th><th>غياب</th><th>تأخير</th><th>إجمالي ساعات</th><th>إضافي</th><th>إجمالي دقائق التأخير</th></tr>
            </thead>
            <tbody>
              {summary.isLoading ? (
                Array.from({ length: 4 }).map((_, i) => <SkeletonRow key={i} cols={7} />)
              ) : summary.data?.length === 0 ? (
                <tr><td colSpan={7}><EmptyState icon={CalendarCheck} title="لا توجد بيانات" description="اختر فترة مختلفة." /></td></tr>
              ) : (
                summary.data?.map((s) => (
                  <tr key={s.employeeId}>
                    <td className="font-medium">{s.employeeName}</td>
                    <td>{s.presentDays}</td>
                    <td className="text-red-700">{s.absentDays}</td>
                    <td className="text-amber-700">{s.lateDays}</td>
                    <td>{s.totalWorkedHours.toFixed(2)}</td>
                    <td className="text-emerald-700">{s.totalOvertimeHours.toFixed(2)}</td>
                    <td>{s.totalLateMinutes}</td>
                  </tr>
                ))
              )}
            </tbody>
          </table>
        </div>
      )}
    </>
  );
}
