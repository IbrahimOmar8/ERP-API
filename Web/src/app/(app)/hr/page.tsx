"use client";

import Link from "next/link";
import { useQuery } from "@tanstack/react-query";
import {
  Users,
  Briefcase,
  Clock,
  CalendarCheck,
  CalendarOff,
  Wallet,
  HandCoins,
} from "lucide-react";
import { api } from "@/lib/api";
import type { HrEmployee, LeaveRequest, Payroll } from "@/types/api";
import PageHeader from "@/components/PageHeader";

const sections = [
  { href: "/hr/employees", label: "الموظفون", icon: Users, desc: "ملفات الموظفين والرواتب الأساسية", color: "bg-blue-50 text-blue-700" },
  { href: "/hr/positions", label: "الوظائف", icon: Briefcase, desc: "المسميات الوظيفية والرواتب المرجعية", color: "bg-violet-50 text-violet-700" },
  { href: "/hr/shifts", label: "الشيفتات", icon: Clock, desc: "تعريف ساعات العمل والإسناد للموظفين", color: "bg-amber-50 text-amber-700" },
  { href: "/hr/attendance", label: "الحضور والانصراف", icon: CalendarCheck, desc: "تسجيل الحضور وحساب التأخير والإضافي", color: "bg-emerald-50 text-emerald-700" },
  { href: "/hr/leaves", label: "الإجازات", icon: CalendarOff, desc: "طلبات الإجازة والموافقة عليها", color: "bg-rose-50 text-rose-700" },
  { href: "/hr/payroll", label: "الرواتب", icon: Wallet, desc: "احتساب الرواتب الشهرية والاعتماد", color: "bg-teal-50 text-teal-700" },
  { href: "/hr/loans", label: "السلف", icon: HandCoins, desc: "سلف الموظفين والخصم التلقائي من الراتب", color: "bg-orange-50 text-orange-700" },
];

export default function HrHomePage() {
  const employees = useQuery({
    queryKey: ["hr-employees-quick"],
    queryFn: async () => (await api.get<HrEmployee[]>("/hr/employees", { params: { status: 0 } })).data,
  });

  const now = new Date();
  const payroll = useQuery({
    queryKey: ["hr-payroll-quick", now.getFullYear(), now.getMonth() + 1],
    queryFn: async () =>
      (await api.get<Payroll[]>("/hr/payroll", { params: { year: now.getFullYear(), month: now.getMonth() + 1 } })).data,
  });

  const leaves = useQuery({
    queryKey: ["hr-leaves-pending"],
    queryFn: async () => (await api.get<LeaveRequest[]>("/hr/leaves", { params: { status: 0 } })).data,
  });

  return (
    <>
      <PageHeader title="الموارد البشرية" description="إدارة شاملة للموظفين والحضور والرواتب" />

      <div className="grid grid-cols-2 md:grid-cols-3 gap-3 mb-4">
        <Kpi icon={Users} label="موظفون نشطون" value={employees.data?.length ?? 0} color="bg-blue-50 text-blue-700" />
        <Kpi icon={CalendarOff} label="إجازات معلقة" value={leaves.data?.length ?? 0} color="bg-amber-50 text-amber-700" />
        <Kpi icon={Wallet} label="مرتبات هذا الشهر" value={payroll.data?.length ?? 0} color="bg-emerald-50 text-emerald-700" />
      </div>

      <div className="grid sm:grid-cols-2 lg:grid-cols-3 gap-3">
        {sections.map((s) => (
          <Link key={s.href} href={s.href} className="card hover:shadow-md transition">
            <div className="flex items-start gap-3">
              <div className={`rounded-lg p-2 ${s.color} dark:bg-opacity-20`}>
                <s.icon size={22} />
              </div>
              <div className="flex-1 min-w-0">
                <div className="font-semibold mb-0.5">{s.label}</div>
                <div className="text-xs text-slate-500">{s.desc}</div>
              </div>
            </div>
          </Link>
        ))}
      </div>
    </>
  );
}

function Kpi({
  icon: Icon,
  label,
  value,
  color,
}: {
  icon: typeof Users;
  label: string;
  value: number;
  color: string;
}) {
  return (
    <div className="card">
      <div className="flex items-center gap-3">
        <div className={`rounded-lg p-2 ${color} dark:bg-opacity-20`}>
          <Icon size={20} />
        </div>
        <div>
          <div className="text-xs text-slate-500">{label}</div>
          <div className="text-lg font-bold">{value}</div>
        </div>
      </div>
    </div>
  );
}
