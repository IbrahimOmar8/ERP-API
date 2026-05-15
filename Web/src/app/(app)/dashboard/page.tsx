"use client";

import { useQuery } from "@tanstack/react-query";
import {
  Wallet,
  Receipt,
  TrendingUp,
  TrendingDown,
  ShoppingCart,
  Users,
  Package,
  AlertTriangle,
  Warehouse,
  PiggyBank,
} from "lucide-react";
import { api } from "@/lib/api";
import { formatMoney, formatDate } from "@/lib/format";
import type { DashboardKpi, TopCustomer, TopProduct } from "@/types/api";
import PageHeader from "@/components/PageHeader";
import KpiCard from "@/components/KpiCard";
import SalesTrendChart from "@/components/SalesTrendChart";
import { SkeletonCards } from "@/components/Skeleton";

export default function DashboardPage() {
  const kpi = useQuery({
    queryKey: ["dashboard"],
    queryFn: async () => (await api.get<DashboardKpi>("/reports/dashboard")).data,
  });
  const topProducts = useQuery({
    queryKey: ["top-products"],
    queryFn: async () => (await api.get<TopProduct[]>("/reports/top-products?take=10")).data,
  });
  const topCustomers = useQuery({
    queryKey: ["top-customers"],
    queryFn: async () => (await api.get<TopCustomer[]>("/reports/top-customers?take=10")).data,
  });

  return (
    <>
      <PageHeader title="لوحة التحكم" description="نظرة سريعة على المؤشرات الرئيسية" />
      {kpi.isLoading ? (
        <SkeletonCards count={8} />
      ) : kpi.data ? (
        <>
          <h2 className="text-lg font-semibold mb-3">اليوم</h2>
          <div className="grid grid-cols-2 md:grid-cols-4 gap-3 mb-3">
            <KpiCard label="المبيعات" value={formatMoney(kpi.data.todaySales)} icon={Wallet} color="green" />
            <KpiCard label="الفواتير" value={kpi.data.todayInvoiceCount} icon={Receipt} color="blue" />
            <KpiCard label="الربح" value={formatMoney(kpi.data.todayProfit)} icon={TrendingUp} color="teal" />
            <KpiCard label="جلسات مفتوحة" value={kpi.data.openSessionCount} icon={ShoppingCart} color="amber" />
          </div>
          <div className="grid grid-cols-2 md:grid-cols-2 gap-3 mb-6">
            <KpiCard label="المصروفات اليوم" value={formatMoney(kpi.data.todayExpenses)} icon={TrendingDown} color="red" />
            <KpiCard
              label="صافي الربح اليوم"
              value={formatMoney(kpi.data.todayNetProfit)}
              icon={PiggyBank}
              color={kpi.data.todayNetProfit >= 0 ? "green" : "red"}
            />
          </div>

          <h2 className="text-lg font-semibold mb-3">الشهر</h2>
          <div className="grid grid-cols-2 md:grid-cols-4 gap-3 mb-3">
            <KpiCard label="المبيعات" value={formatMoney(kpi.data.monthSales)} icon={Wallet} color="green" />
            <KpiCard label="الفواتير" value={kpi.data.monthInvoiceCount} icon={Receipt} color="blue" />
            <KpiCard label="الربح الإجمالي" value={formatMoney(kpi.data.monthProfit)} icon={TrendingUp} color="teal" />
            <KpiCard label="قيمة المخزون" value={formatMoney(kpi.data.totalStockValue)} icon={Warehouse} color="violet" />
          </div>
          <div className="grid grid-cols-2 md:grid-cols-2 gap-3 mb-6">
            <KpiCard label="المصروفات" value={formatMoney(kpi.data.monthExpenses)} icon={TrendingDown} color="red" />
            <KpiCard
              label="صافي الربح"
              value={formatMoney(kpi.data.monthNetProfit)}
              icon={PiggyBank}
              color={kpi.data.monthNetProfit >= 0 ? "green" : "red"}
            />
          </div>

          <h2 className="text-lg font-semibold mb-3">عام</h2>
          <div className="grid grid-cols-2 md:grid-cols-3 gap-3 mb-6">
            <KpiCard label="العملاء" value={kpi.data.customerCount} icon={Users} color="violet" />
            <KpiCard label="الأصناف" value={kpi.data.productCount} icon={Package} color="amber" />
            <KpiCard label="أصناف بحد أدنى" value={kpi.data.lowStockCount} icon={AlertTriangle} color="red" />
          </div>
        </>
      ) : null}

      <div className="card mb-4">
        <h3 className="font-semibold mb-3">اتجاه المبيعات (آخر 30 يوم)</h3>
        <SalesTrendChart />
      </div>

      <div className="grid md:grid-cols-2 gap-4">
        <div className="card">
          <h3 className="font-semibold mb-3">الأعلى مبيعاً (آخر 30 يوم)</h3>
          {topProducts.isLoading ? (
            <p className="text-slate-500">جاري التحميل...</p>
          ) : topProducts.data && topProducts.data.length > 0 ? (
            <div className="table-wrap">
              <table>
                <thead>
                  <tr>
                    <th>الصنف</th>
                    <th>الكمية</th>
                    <th>الإيرادات</th>
                  </tr>
                </thead>
                <tbody>
                  {topProducts.data.slice(0, 5).map((p) => (
                    <tr key={p.productId}>
                      <td>{p.productName}</td>
                      <td>{p.quantitySold}</td>
                      <td className="text-emerald-700 font-medium">{formatMoney(p.revenue)}</td>
                    </tr>
                  ))}
                </tbody>
              </table>
            </div>
          ) : (
            <p className="text-slate-500">لا توجد بيانات.</p>
          )}
        </div>

        <div className="card">
          <h3 className="font-semibold mb-3">أهم العملاء (آخر 30 يوم)</h3>
          {topCustomers.isLoading ? (
            <p className="text-slate-500">جاري التحميل...</p>
          ) : topCustomers.data && topCustomers.data.length > 0 ? (
            <div className="table-wrap">
              <table>
                <thead>
                  <tr>
                    <th>العميل</th>
                    <th>فواتير</th>
                    <th>الإجمالي</th>
                    <th>آخر شراء</th>
                  </tr>
                </thead>
                <tbody>
                  {topCustomers.data.slice(0, 5).map((c) => (
                    <tr key={c.customerId}>
                      <td className="font-medium">{c.customerName}</td>
                      <td>{c.invoiceCount}</td>
                      <td className="text-emerald-700 font-medium">{formatMoney(c.totalSpent)}</td>
                      <td className="text-xs text-slate-500">{formatDate(c.lastPurchase)}</td>
                    </tr>
                  ))}
                </tbody>
              </table>
            </div>
          ) : (
            <p className="text-slate-500">لا يوجد عملاء مسجلون في فواتير.</p>
          )}
        </div>
      </div>
    </>
  );
}
