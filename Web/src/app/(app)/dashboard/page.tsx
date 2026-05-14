"use client";

import { useQuery } from "@tanstack/react-query";
import {
  Wallet,
  Receipt,
  TrendingUp,
  ShoppingCart,
  Users,
  Package,
  AlertTriangle,
  Warehouse,
} from "lucide-react";
import { api } from "@/lib/api";
import { formatMoney } from "@/lib/format";
import type { DashboardKpi, TopProduct } from "@/types/api";
import PageHeader from "@/components/PageHeader";
import KpiCard from "@/components/KpiCard";

export default function DashboardPage() {
  const kpi = useQuery({
    queryKey: ["dashboard"],
    queryFn: async () => (await api.get<DashboardKpi>("/reports/dashboard")).data,
  });
  const top = useQuery({
    queryKey: ["top-products"],
    queryFn: async () => (await api.get<TopProduct[]>("/reports/top-products?take=10")).data,
  });

  return (
    <>
      <PageHeader title="لوحة التحكم" description="نظرة سريعة على المؤشرات الرئيسية" />
      {kpi.isLoading ? (
        <p>جاري التحميل...</p>
      ) : kpi.data ? (
        <>
          <h2 className="text-lg font-semibold mb-3">اليوم</h2>
          <div className="grid grid-cols-2 md:grid-cols-4 gap-3 mb-6">
            <KpiCard label="مبيعات اليوم" value={formatMoney(kpi.data.todaySales)} icon={Wallet} color="green" />
            <KpiCard label="فواتير اليوم" value={kpi.data.todayInvoiceCount} icon={Receipt} color="blue" />
            <KpiCard label="ربح اليوم" value={formatMoney(kpi.data.todayProfit)} icon={TrendingUp} color="teal" />
            <KpiCard label="جلسات مفتوحة" value={kpi.data.openSessionCount} icon={ShoppingCart} color="amber" />
          </div>

          <h2 className="text-lg font-semibold mb-3">الشهر</h2>
          <div className="grid grid-cols-2 md:grid-cols-4 gap-3 mb-6">
            <KpiCard label="مبيعات الشهر" value={formatMoney(kpi.data.monthSales)} icon={Wallet} color="green" />
            <KpiCard label="فواتير الشهر" value={kpi.data.monthInvoiceCount} icon={Receipt} color="blue" />
            <KpiCard label="ربح الشهر" value={formatMoney(kpi.data.monthProfit)} icon={TrendingUp} color="teal" />
            <KpiCard label="قيمة المخزون" value={formatMoney(kpi.data.totalStockValue)} icon={Warehouse} color="violet" />
          </div>

          <h2 className="text-lg font-semibold mb-3">عام</h2>
          <div className="grid grid-cols-2 md:grid-cols-3 gap-3 mb-6">
            <KpiCard label="العملاء" value={kpi.data.customerCount} icon={Users} color="violet" />
            <KpiCard label="الأصناف" value={kpi.data.productCount} icon={Package} color="amber" />
            <KpiCard label="أصناف بحد أدنى" value={kpi.data.lowStockCount} icon={AlertTriangle} color="red" />
          </div>
        </>
      ) : null}

      <div className="card">
        <h3 className="font-semibold mb-3">الأعلى مبيعاً (آخر 30 يوم)</h3>
        {top.isLoading ? (
          <p className="text-slate-500">جاري التحميل...</p>
        ) : top.data && top.data.length > 0 ? (
          <div className="table-wrap">
            <table>
              <thead>
                <tr>
                  <th>الصنف</th>
                  <th>الكمية المباعة</th>
                  <th>الإيرادات</th>
                  <th>الربح</th>
                </tr>
              </thead>
              <tbody>
                {top.data.map((p) => (
                  <tr key={p.productId}>
                    <td>{p.productName}</td>
                    <td>{p.quantitySold}</td>
                    <td>{formatMoney(p.revenue)}</td>
                    <td className="text-emerald-700 font-medium">{formatMoney(p.profit)}</td>
                  </tr>
                ))}
              </tbody>
            </table>
          </div>
        ) : (
          <p className="text-slate-500">لا توجد بيانات.</p>
        )}
      </div>
    </>
  );
}
