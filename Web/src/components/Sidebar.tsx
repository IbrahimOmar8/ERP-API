"use client";

import Link from "next/link";
import { usePathname } from "next/navigation";
import { useEffect, useState } from "react";
import { useAuth } from "@/lib/auth";
import ThemeToggle from "./ThemeToggle";
import GlobalSearch from "./GlobalSearch";
import NotificationsBell from "./NotificationsBell";
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
  ClipboardCheck,
  Terminal,
  History,
  Menu,
  X,
  TrendingDown,
  Upload,
  Banknote,
  Ticket,
  ShieldCheck,
  Key,
  Webhook,
  Hourglass,
} from "lucide-react";

type NavItem = {
  href: string;
  label: string;
  icon: typeof LayoutDashboard;
  requireRole?: string;
};

const navItems: NavItem[] = [
  { href: "/dashboard", label: "لوحة التحكم", icon: LayoutDashboard },
  { href: "/pos", label: "نقطة البيع", icon: ShoppingCart },
  { href: "/cash-sessions", label: "جلسات الكاش", icon: Wallet },
  { href: "/sales", label: "فواتير البيع", icon: Receipt },
  { href: "/purchases", label: "فواتير الشراء", icon: FileText },
  { href: "/expenses", label: "المصروفات", icon: TrendingDown },
  { href: "/customers", label: "العملاء", icon: Users },
  { href: "/coupons", label: "الكوبونات", icon: Ticket },
  { href: "/suppliers", label: "الموردون", icon: Truck },
  { href: "/products", label: "الأصناف", icon: Package },
  { href: "/stock", label: "الرصيد", icon: Warehouse },
  { href: "/stock-adjustment", label: "جرد المخزون", icon: ClipboardCheck },
  { href: "/transfers", label: "التحويلات", icon: ArrowLeftRight },
  { href: "/sales-report", label: "تقرير المبيعات", icon: TrendingUp },
  { href: "/reports/pnl", label: "الأرباح والخسائر", icon: TrendingUp },
  { href: "/reports/cash-flow", label: "التدفق النقدي", icon: Banknote },
  { href: "/reports/inventory-aging", label: "عمر المخزون", icon: Hourglass },
  { href: "/reports/cashier-performance", label: "أداء الكاشيرين", icon: UserCog },
  { href: "/master-data", label: "البيانات الأساسية", icon: Database },
  { href: "/import", label: "استيراد جماعي", icon: Upload },
  { href: "/cash-registers", label: "ماكينات الكاشير", icon: Terminal },
  { href: "/users", label: "المستخدمون", icon: UserCog, requireRole: "Admin" },
  { href: "/audit-log", label: "سجل العمليات", icon: History, requireRole: "Admin" },
  { href: "/api-keys", label: "مفاتيح API", icon: Key, requireRole: "Admin" },
  { href: "/webhooks", label: "Webhooks", icon: Webhook, requireRole: "Admin" },
  { href: "/security", label: "الأمن", icon: ShieldCheck },
  { href: "/settings", label: "الإعدادات", icon: Settings },
];

export default function Sidebar() {
  const pathname = usePathname();
  const user = useAuth((s) => s.user);
  const clear = useAuth((s) => s.clear);
  const [open, setOpen] = useState(false);

  // Close drawer on route change
  useEffect(() => setOpen(false), [pathname]);

  function logout() {
    clear();
    window.location.href = "/login";
  }

  const visibleItems = navItems.filter(
    (item) => !item.requireRole || (user?.roles ?? []).includes(item.requireRole)
  );

  const SidebarContent = (
    <>
      <div className="px-4 py-4 border-b border-slate-700">
        <div className="flex items-center justify-between mb-3">
          <div>
            <div className="text-lg font-bold">ERP — نقاط البيع</div>
            <div className="text-xs text-slate-400 mt-1">إصدار 1.0</div>
          </div>
          <div className="flex items-center gap-1">
            <NotificationsBell />
            <button
              className="lg:hidden text-slate-300 hover:text-white"
              onClick={() => setOpen(false)}
              aria-label="إغلاق القائمة"
            >
              <X size={22} />
            </button>
          </div>
        </div>
        <GlobalSearch />
      </div>
      <nav className="flex-1 overflow-y-auto py-2 px-2 space-y-1">
        {visibleItems.map((item) => {
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
        <div className="text-sm font-medium truncate">{user?.fullName ?? user?.userName}</div>
        <div className="text-xs text-slate-400 mb-2 truncate">{user?.roles?.join(" · ")}</div>
        <div className="flex items-center justify-between">
          <button
            onClick={logout}
            className="flex items-center gap-2 text-sm text-red-400 hover:text-red-300"
          >
            <LogOut size={16} />
            <span>خروج</span>
          </button>
          <ThemeToggle />
        </div>
      </div>
    </>
  );

  return (
    <>
      {/* Mobile top-bar */}
      <header className="lg:hidden bg-slate-900 text-white px-4 py-3 flex items-center justify-between sticky top-0 z-30">
        <button onClick={() => setOpen(true)} aria-label="القائمة">
          <Menu size={22} />
        </button>
        <div className="text-base font-bold">ERP — نقاط البيع</div>
        <div className="w-6" />
      </header>

      {/* Desktop sidebar */}
      <aside className="hidden lg:flex w-60 bg-slate-900 text-slate-100 flex-col flex-shrink-0">
        {SidebarContent}
      </aside>

      {/* Mobile drawer */}
      {open && (
        <div className="lg:hidden fixed inset-0 z-40 flex flex-row-reverse">
          <button
            className="flex-1 bg-black/50"
            onClick={() => setOpen(false)}
            aria-label="إغلاق"
          />
          <aside className="w-64 bg-slate-900 text-slate-100 flex flex-col animate-in slide-in-from-right">
            {SidebarContent}
          </aside>
        </div>
      )}
    </>
  );
}
