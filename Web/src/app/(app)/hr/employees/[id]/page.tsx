"use client";

import { useParams } from "next/navigation";
import { useQuery } from "@tanstack/react-query";
import {
  CalendarOff,
  Clock,
  Mail,
  Phone,
  Printer,
  User,
  Wallet,
} from "lucide-react";
import { api } from "@/lib/api";
import { formatDate, formatDateTime, formatMoney } from "@/lib/format";
import type {
  Attendance,
  HrEmployee,
  LeaveRequest,
  Payroll,
  ShiftAssignment,
} from "@/types/api";
import {
  AttendanceStatusLabel,
  EmpStatusLabel,
  LeaveStatusLabel,
  LeaveTypeLabel,
  PayrollStatusLabel,
} from "@/types/api";
import PageHeader from "@/components/PageHeader";
import { SkeletonTable } from "@/components/Skeleton";

export default function HrEmployeeDetailPage() {
  const params = useParams<{ id: string }>();
  const id = params.id;
  const apiUrl = process.env.NEXT_PUBLIC_API_URL || "http://localhost:5000/api";

  const employee = useQuery({
    queryKey: ["hr-employee", id],
    queryFn: async () => (await api.get<HrEmployee>(`/hr/employees/${id}`)).data,
  });

  const today = new Date();
  const from = new Date(today.getFullYear(), today.getMonth() - 2, 1).toISOString().slice(0, 10);
  const to = today.toISOString().slice(0, 10);

  const attendance = useQuery({
    queryKey: ["hr-employee-attendance", id, from, to],
    queryFn: async () =>
      (await api.get<Attendance[]>("/hr/attendance", { params: { employeeId: id, from, to } })).data,
  });

  const leaves = useQuery({
    queryKey: ["hr-employee-leaves", id],
    queryFn: async () =>
      (await api.get<LeaveRequest[]>("/hr/leaves", { params: { employeeId: id } })).data,
  });

  const assignments = useQuery({
    queryKey: ["hr-employee-shifts", id],
    queryFn: async () =>
      (await api.get<ShiftAssignment[]>("/hr/shifts/assignments", { params: { employeeId: id } })).data,
  });

  // Last 6 months of payroll
  const months = Array.from({ length: 6 }, (_, i) => {
    const d = new Date(today.getFullYear(), today.getMonth() - i, 1);
    return { year: d.getFullYear(), month: d.getMonth() + 1 };
  });

  const payroll = useQuery({
    queryKey: ["hr-employee-payroll", id, months[0]?.year, months[0]?.month],
    queryFn: async () => {
      const results = await Promise.all(
        months.map((m) =>
          api.get<Payroll[]>("/hr/payroll", { params: { year: m.year, month: m.month } })
        )
      );
      return results.flatMap((r) => r.data.filter((p) => p.employeeId === id));
    },
  });

  const e = employee.data;
  const totalNetYtd = (payroll.data ?? [])
    .filter((p) => p.year === today.getFullYear())
    .reduce((s, p) => s + p.netPay, 0);

  return (
    <>
      <PageHeader
        title={e?.name ?? "موظف"}
        description={[e?.positionTitle, e?.departmentName].filter(Boolean).join(" — ")}
      />

      {/* Profile card */}
      <div className="card mb-4">
        <div className="flex items-start gap-4">
          <div className="w-16 h-16 rounded-full bg-brand/10 text-brand flex items-center justify-center flex-shrink-0">
            <User size={28} />
          </div>
          <div className="flex-1 grid md:grid-cols-3 gap-3">
            <Info icon={Phone} label="الهاتف" value={e?.phone ?? "—"} />
            <Info icon={Mail} label="البريد" value={e?.email ?? "—"} />
            <Info label="الرقم القومي" value={e?.nationalId ?? "—"} />
            <Info label="تاريخ التعيين" value={e ? formatDate(e.hireDate) : "—"} />
            <Info label="البنك / الحساب" value={`${e?.bankName ?? "—"} ${e?.bankAccount ? `(${e.bankAccount})` : ""}`} />
            <Info label="الحالة" value={e ? EmpStatusLabel[e.status] : "—"} />
          </div>
        </div>
      </div>

      {/* KPI tiles */}
      <div className="grid grid-cols-2 md:grid-cols-4 gap-3 mb-4">
        <Stat icon={Wallet} label="الراتب الأساسي" value={formatMoney(e?.baseSalary ?? 0)} color="bg-blue-50 text-blue-700" />
        <Stat icon={Wallet} label="إجمالي الصافي للسنة" value={formatMoney(totalNetYtd)} color="bg-emerald-50 text-emerald-700" />
        <Stat icon={Clock} label="ساعة إضافية" value={e?.overtimeHourlyRate ? formatMoney(e.overtimeHourlyRate) : "—"} color="bg-amber-50 text-amber-700" />
        <Stat icon={CalendarOff} label="إجازات معتمدة" value={(leaves.data?.filter((l) => l.status === 1).length ?? 0).toString()} color="bg-violet-50 text-violet-700" />
      </div>

      {/* Shift assignments */}
      <div className="card mb-4">
        <h3 className="font-semibold mb-3">إسنادات الشيفت</h3>
        {assignments.isLoading ? (
          <SkeletonTable cols={4} />
        ) : assignments.data?.length === 0 ? (
          <p className="text-slate-400 text-sm py-3">لم يتم إسناد شيفت لهذا الموظف.</p>
        ) : (
          <div className="table-wrap">
            <table>
              <thead><tr><th>الشيفت</th><th>من</th><th>إلى</th><th>ملاحظات</th></tr></thead>
              <tbody>
                {assignments.data?.map((a) => (
                  <tr key={a.id}>
                    <td className="font-medium">{a.shiftName}</td>
                    <td>{formatDate(a.effectiveFrom)}</td>
                    <td>{a.effectiveTo ? formatDate(a.effectiveTo) : <span className="text-emerald-700">حالي</span>}</td>
                    <td className="text-xs">{a.notes}</td>
                  </tr>
                ))}
              </tbody>
            </table>
          </div>
        )}
      </div>

      {/* Recent attendance */}
      <div className="card mb-4">
        <h3 className="font-semibold mb-3">آخر سجلات الحضور</h3>
        {attendance.isLoading ? (
          <SkeletonTable cols={5} />
        ) : attendance.data?.length === 0 ? (
          <p className="text-slate-400 text-sm py-3">لا توجد سجلات.</p>
        ) : (
          <div className="table-wrap">
            <table>
              <thead><tr><th>التاريخ</th><th>دخول</th><th>خروج</th><th>ساعات</th><th>تأخير</th><th>الحالة</th></tr></thead>
              <tbody>
                {attendance.data?.slice(0, 30).map((r) => (
                  <tr key={r.id}>
                    <td>{formatDate(r.date)}</td>
                    <td className="font-mono text-xs">{r.checkIn ? formatDateTime(r.checkIn).split(" ")[1] : "—"}</td>
                    <td className="font-mono text-xs">{r.checkOut ? formatDateTime(r.checkOut).split(" ")[1] : "—"}</td>
                    <td>{r.workedHours.toFixed(2)}</td>
                    <td>{r.lateMinutes > 0 ? <span className="text-amber-700">{r.lateMinutes}د</span> : "—"}</td>
                    <td className="text-xs">{AttendanceStatusLabel[r.status]}</td>
                  </tr>
                ))}
              </tbody>
            </table>
          </div>
        )}
      </div>

      {/* Leaves */}
      <div className="card mb-4">
        <h3 className="font-semibold mb-3">طلبات الإجازة</h3>
        {leaves.isLoading ? (
          <SkeletonTable cols={5} />
        ) : leaves.data?.length === 0 ? (
          <p className="text-slate-400 text-sm py-3">لا توجد طلبات.</p>
        ) : (
          <div className="table-wrap">
            <table>
              <thead><tr><th>النوع</th><th>من</th><th>إلى</th><th>الأيام</th><th>الحالة</th></tr></thead>
              <tbody>
                {leaves.data?.map((l) => (
                  <tr key={l.id}>
                    <td>{LeaveTypeLabel[l.type]}</td>
                    <td>{formatDate(l.from)}</td>
                    <td>{formatDate(l.to)}</td>
                    <td>{l.days}</td>
                    <td className="text-xs">{LeaveStatusLabel[l.status]}</td>
                  </tr>
                ))}
              </tbody>
            </table>
          </div>
        )}
      </div>

      {/* Payslips */}
      <div className="card">
        <h3 className="font-semibold mb-3">قسائم الرواتب (آخر 6 شهور)</h3>
        {payroll.isLoading ? (
          <SkeletonTable cols={5} />
        ) : payroll.data?.length === 0 ? (
          <p className="text-slate-400 text-sm py-3">لا توجد كشوف رواتب.</p>
        ) : (
          <div className="table-wrap">
            <table>
              <thead><tr><th>الشهر</th><th>أساسي</th><th>إضافي</th><th>صافي</th><th>الحالة</th><th></th></tr></thead>
              <tbody>
                {payroll.data?.map((p) => (
                  <tr key={p.id}>
                    <td>{p.year}/{String(p.month).padStart(2, "0")}</td>
                    <td>{formatMoney(p.baseSalary)}</td>
                    <td className="text-emerald-700">{p.overtimePay > 0 ? formatMoney(p.overtimePay) : "—"}</td>
                    <td className="font-bold">{formatMoney(p.netPay)}</td>
                    <td className="text-xs">{PayrollStatusLabel[p.status]}</td>
                    <td>
                      <a href={`${apiUrl}/hr/payroll/${p.id}/print`} target="_blank" rel="noreferrer" className="btn-outline !px-2 !py-1 text-xs">
                        <Printer size={14} /> قسيمة
                      </a>
                    </td>
                  </tr>
                ))}
              </tbody>
            </table>
          </div>
        )}
      </div>
    </>
  );
}

function Info({ icon: Icon, label, value }: { icon?: typeof Phone; label: string; value: string }) {
  return (
    <div>
      <div className="text-xs text-slate-500 flex items-center gap-1">
        {Icon && <Icon size={12} />} {label}
      </div>
      <div className="text-sm font-medium">{value}</div>
    </div>
  );
}

function Stat({ icon: Icon, label, value, color }: { icon: typeof Wallet; label: string; value: string; color: string }) {
  return (
    <div className="card">
      <div className="flex items-center gap-3">
        <div className={`rounded-lg p-2 ${color} dark:bg-opacity-20`}>
          <Icon size={20} />
        </div>
        <div className="flex-1 min-w-0">
          <div className="text-xs text-slate-500">{label}</div>
          <div className="text-base font-bold truncate">{value}</div>
        </div>
      </div>
    </div>
  );
}
