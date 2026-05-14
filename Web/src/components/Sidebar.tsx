"use client";

import Link from "next/link";
import { usePathname } from "next/navigation";
import { useAuth } from "@/lib/auth";
import {
  LayoutDashboard,
  ShoppingCart,
  Wallet,
  Receipt,
  Users,
  Package,
  Warehouse,
  ArrowLeftRight,
  TrendingUp,
  Settings,
  LogOut,
  Truck,
  FileText,
  Database,
  UserCog,
} from "lucide-react";

const navItems = [
  { href: "/dashboard", label: "لوحة التحكم", icon: LayoutDashboard },
  { href: "/pos", label: "نقطة البيع", icon: ShoppingCart },
  { href: "/cash-sessions", label: "جلسات الكاش", icon: Wallet },
  { href: "/sales", label: "فواتير البيع", icon: Receipt },
  { href: "/purchases", label: "فواتير الشراء", icon: FileText },
  { href: "/customers", label: "العملاء", icon: Users },
  { href: "/suppliers", label: "الموردون", icon: Truck },
  { href: "/products", label: "الأصناف", icon: Package },
  { href: "/stock", label: "الرصيد", icon: Warehouse },
  { href: "/transfers", label: "التحويلات", icon: ArrowLeftRight },
  { href: "/sales-report", label: "تقرير المبيعات", icon: TrendingUp },
  { href: "/master-data", label: "البيانات الأساسية", icon: Database },
  { href: "/users", label: "المستخدمون", icon: UserCog, requireRole: "Admin" as const },
  { href: "/settings", label: "الإعدادات", icon: Settings },
] as const;

export default function Sidebar() {
  const pathname = usePathname();
  const user = useAuth((s) => s.user);
  const clear = useAuth((s) => s.clear);

  function logout() {
    clear();
    window.location.href = "/login";
  }

  return (
    <aside className="w-60 bg-slate-900 text-slate-100 flex flex-col flex-shrink-0">
      <div className="px-4 py-4 border-b border-slate-700">
        <div className="text-lg font-bold">ERP — نقاط البيع</div>
        <div className="text-xs text-slate-400 mt-1">إصدار 1.0</div>
      </div>
      <nav className="flex-1 overflow-y-auto py-2 px-2 space-y-1">
        {navItems
          .filter((item) => !("requireRole" in item) || (user?.roles ?? []).includes(item.requireRole))
          .map((item) => {
            const active = pathname === item.href || pathname.startsWith(item.href + "/");
            const Icon = item.icon;
            return (
              <Link
                key={item.href}
                href={item.href}
                className={`flex items-center gap-3 rounded-lg px-3 py-2 text-sm transition ${
                  active ? "bg-brand text-white" : "text-slate-300 hover:bg-slate-800"
                }`}
              >
                <Icon size={18} />
                <span>{item.label}</span>
              </Link>
            );
          })}
      </nav>
      <div className="border-t border-slate-700 p-3">
        <div className="text-sm font-medium">{user?.fullName ?? user?.userName}</div>
        <div className="text-xs text-slate-400 mb-2">{user?.roles?.join(" · ")}</div>
        <button onClick={logout} className="flex items-center gap-2 text-sm text-red-400 hover:text-red-300">
          <LogOut size={16} />
          <span>خروج</span>
        </button>
      </div>
    </aside>
  );
}
