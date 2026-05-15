"use client";

import { useState } from "react";
import { useQuery } from "@tanstack/react-query";
import { ArrowDownCircle, ArrowUpCircle, Banknote, CreditCard, Smartphone, TrendingDown, TrendingUp, ShoppingCart, Undo2 } from "lucide-react";
import { api } from "@/lib/api";
import { formatMoney, formatDate } from "@/lib/format";
import type { CashFlowReport } from "@/types/api";
import PageHeader from "@/components/PageHeader";
import KpiCard from "@/components/KpiCard";

function toIso(d: string): string {
  return new Date(d + "T00:00:00").toISOString();
}

export default function CashFlowPage() {
  const today = new Date();
  const monthAgo = new Date(today);
  monthAgo.setDate(today.getDate() - 30);
  const [from, setFrom] = useState(monthAgo.toISOString().slice(0, 10));
  const [to, setTo] = useState(today.toISOString().slice(0, 10));

  const { data, isLoading, refetch } = useQuery({
    queryKey: ["cashflow", from, to],
    queryFn: async () =>
      (await api.get<CashFlowReport>("/reports/cash-flow", {
        params: { from: toIso(from), to: toIso(to + "T23:59:59") },
      })).data,
  });

  return (
    <>
      <PageHeader title="تقرير التدفق النقدي" description="الداخل والخارج من الكاش بالفترة" />
      <div className="card mb-4 flex flex-wrap items-end gap-3">
        <div>
          <label>من</label>
          <input type="date" value={from} onChange={(e) => setFrom(e.target.value)} />
        </div>
        <div>
          <label>إلى</label>
          <input type="date" value={to} onChange={(e) => setTo(e.target.value)} />
        </div>
        <button onClick={() => refetch()} className="btn">عرض</button>
      </div>

      {isLoading || !data ? (
        <p>جاري التحميل...</p>
      ) : (
        <>
          <h2 className="text-lg font-semibold mb-3 flex items-center gap-2">
            <ArrowDownCircle className="text-emerald-600" /> الداخل
          </h2>
          <div className="grid grid-cols-2 md:grid-cols-4 gap-3 mb-4">
            <KpiCard label="نقدي" value={formatMoney(data.cashSalesIn)} icon={Banknote} color="green" />
            <KpiCard label="بطاقة" value={formatMoney(data.cardSalesIn)} icon={CreditCard} color="blue" />
            <KpiCard label="أخرى" value={formatMoney(data.otherSalesIn)} icon={Smartphone} color="violet" />
            <KpiCard label="إجمالي الداخل" value={formatMoney(data.totalIn)} icon={TrendingUp} color="green" />
          </div>

          <h2 className="text-lg font-semibold mb-3 flex items-center gap-2">
            <ArrowUpCircle className="text-red-600" /> الخارج
          </h2>
          <div className="grid grid-cols-2 md:grid-cols-4 gap-3 mb-4">
            <KpiCard label="مشتريات" value={formatMoney(data.purchasesOut)} icon={ShoppingCart} color="amber" />
            <KpiCard label="مصروفات" value={formatMoney(data.expensesOut)} icon={TrendingDown} color="red" />
            <KpiCard label="مرتجعات" value={formatMoney(data.refundsOut)} icon={Undo2} color="red" />
            <KpiCard label="إجمالي الخارج" value={formatMoney(data.totalOut)} icon={TrendingDown} color="red" />
          </div>

          <div
            className={`card mb-4 ${
              data.netCashFlow >= 0 ? "bg-emerald-50 dark:bg-emerald-950/30" : "bg-red-50 dark:bg-red-950/30"
            }`}
          >
            <div className="text-sm text-slate-600 dark:text-slate-400">صافي التدفق النقدي للفترة</div>
            <div
              className={`text-3xl font-bold ${
                data.netCashFlow >= 0 ? "text-emerald-700 dark:text-emerald-400" : "text-red-700 dark:text-red-400"
              }`}
            >
              {formatMoney(data.netCashFlow)}
            </div>
          </div>

          <div className="card">
            <h3 className="font-semibold mb-3">التفصيل اليومي</h3>
            <div className="table-wrap">
              <table>
                <thead>
                  <tr>
                    <th>التاريخ</th>
                    <th>داخل</th>
                    <th>خارج</th>
                    <th>الصافي</th>
                  </tr>
                </thead>
                <tbody>
                  {data.daily.length === 0 ? (
                    <tr>
                      <td colSpan={4} className="text-center py-6 text-slate-400">
                        لا توجد حركات
                      </td>
                    </tr>
                  ) : (
                    data.daily.map((d) => (
                      <tr key={d.date}>
                        <td>{formatDate(d.date)}</td>
                        <td className="text-emerald-700 dark:text-emerald-400">{formatMoney(d.in)}</td>
                        <td className="text-red-700 dark:text-red-400">{formatMoney(d.out)}</td>
                        <td
                          className={`font-semibold ${
                            d.net >= 0 ? "text-emerald-700 dark:text-emerald-400" : "text-red-700 dark:text-red-400"
                          }`}
                        >
                          {formatMoney(d.net)}
                        </td>
                      </tr>
                    ))
                  )}
                </tbody>
              </table>
            </div>
          </div>
        </>
      )}
    </>
  );
}
