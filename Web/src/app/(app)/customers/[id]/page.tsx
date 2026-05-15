"use client";

import Link from "next/link";
import { useParams } from "next/navigation";
import { useQuery } from "@tanstack/react-query";
import {
  Award,
  CreditCard,
  Mail,
  Phone,
  Receipt,
  TrendingUp,
  User,
  Building,
} from "lucide-react";
import { api } from "@/lib/api";
import { formatDateTime, formatMoney } from "@/lib/format";
import type { Customer, CustomerLoyaltyStatus, Sale } from "@/types/api";
import PageHeader from "@/components/PageHeader";
import { SkeletonTable } from "@/components/Skeleton";

export default function CustomerDetailPage() {
  const params = useParams<{ id: string }>();
  const customerId = params.id;

  const customer = useQuery({
    queryKey: ["customer", customerId],
    queryFn: async () => (await api.get<Customer>(`/Customers/${customerId}`)).data,
  });

  const sales = useQuery({
    queryKey: ["customer-sales", customerId],
    queryFn: async () =>
      (await api.get<Sale[]>("/Sales", { params: { customerId, pageSize: 100 } })).data,
  });

  const loyalty = useQuery({
    queryKey: ["customer-loyalty-status", customerId],
    queryFn: async () =>
      (await api.get<CustomerLoyaltyStatus>(`/Loyalty/customers/${customerId}`)).data,
  });

  const c = customer.data;
  const totalLifetime = (sales.data ?? [])
    .filter((s) => s.status === 1)
    .reduce((sum, s) => sum + s.total, 0);

  return (
    <>
      <PageHeader title={c?.name ?? "عميل"} description={c?.isCompany ? "شركة (B2B)" : "فرد"}>
        <Link href={`/customers/${customerId}/ledger`} className="btn-outline">
          <CreditCard size={16} /> كشف الحساب
        </Link>
      </PageHeader>

      {/* Profile card */}
      <div className="card mb-4">
        <div className="flex items-start gap-4">
          <div className="w-16 h-16 rounded-full bg-brand/10 text-brand flex items-center justify-center flex-shrink-0">
            {c?.isCompany ? <Building size={28} /> : <User size={28} />}
          </div>
          <div className="flex-1 grid md:grid-cols-3 gap-3">
            <Info icon={Phone} label="الهاتف" value={c?.phone ?? "—"} />
            <Info icon={Mail} label="البريد" value={c?.email ?? "—"} />
            <Info label="الرقم الضريبي" value={c?.taxRegistrationNumber ?? "—"} />
            <Info label="الرقم القومي" value={c?.nationalId ?? "—"} />
            <Info label="العنوان" value={c?.address ?? "—"} />
            <Info label="حد الائتمان" value={formatMoney(c?.creditLimit ?? 0)} />
          </div>
        </div>
      </div>

      {/* KPI tiles */}
      <div className="grid grid-cols-2 md:grid-cols-4 gap-3 mb-4">
        <Stat
          icon={TrendingUp}
          label="إجمالي المشتريات"
          value={formatMoney(totalLifetime)}
          color="bg-emerald-50 text-emerald-700"
        />
        <Stat
          icon={Receipt}
          label="عدد الفواتير"
          value={(sales.data?.filter((s) => s.status === 1).length ?? 0).toString()}
          color="bg-blue-50 text-blue-700"
        />
        <Stat
          icon={CreditCard}
          label="الرصيد المستحق"
          value={formatMoney(c?.balance ?? 0)}
          color={
            (c?.balance ?? 0) > 0
              ? "bg-amber-50 text-amber-700"
              : "bg-slate-50 text-slate-700"
          }
        />
        <Stat
          icon={Award}
          label="نقاط الولاء"
          value={`${loyalty.data?.currentPoints ?? 0} (${formatMoney(loyalty.data?.pointsValue ?? 0)})`}
          color="bg-violet-50 text-violet-700"
        />
      </div>

      {/* Sales history */}
      <div className="card mb-4">
        <h3 className="font-semibold mb-3">سجل الفواتير</h3>
        {sales.isLoading ? (
          <SkeletonTable cols={5} />
        ) : sales.data?.length === 0 ? (
          <p className="text-slate-400 text-sm py-6 text-center">لا توجد فواتير لهذا العميل</p>
        ) : (
          <div className="table-wrap">
            <table>
              <thead>
                <tr>
                  <th>الرقم</th>
                  <th>التاريخ</th>
                  <th>الإجمالي</th>
                  <th>الحالة</th>
                  <th></th>
                </tr>
              </thead>
              <tbody>
                {sales.data?.map((s) => (
                  <tr key={s.id}>
                    <td className="font-mono text-xs">{s.invoiceNumber}</td>
                    <td>{formatDateTime(s.saleDate)}</td>
                    <td className="font-semibold">{formatMoney(s.total)}</td>
                    <td>
                      <SaleStatusChip status={s.status} />
                    </td>
                    <td>
                      <Link href={`/sales/${s.id}`} className="btn-outline !px-2 !py-1 text-xs">
                        عرض
                      </Link>
                    </td>
                  </tr>
                ))}
              </tbody>
            </table>
          </div>
        )}
      </div>

      {/* Loyalty transactions */}
      {loyalty.data && loyalty.data.recentTransactions.length > 0 && (
        <div className="card">
          <h3 className="font-semibold mb-3">حركات نقاط الولاء</h3>
          <div className="table-wrap">
            <table>
              <thead>
                <tr>
                  <th>التاريخ</th>
                  <th>النوع</th>
                  <th>النقاط</th>
                  <th>الرصيد بعدها</th>
                  <th>ملاحظات</th>
                </tr>
              </thead>
              <tbody>
                {loyalty.data.recentTransactions.map((t) => (
                  <tr key={t.id}>
                    <td className="text-xs">{formatDateTime(t.createdAt)}</td>
                    <td>{loyaltyTypeLabel(t.type)}</td>
                    <td className={t.points >= 0 ? "text-emerald-600" : "text-red-600"}>
                      {t.points > 0 ? `+${t.points}` : t.points}
                    </td>
                    <td className="font-semibold">{t.balanceAfter}</td>
                    <td className="text-xs">{t.notes}</td>
                  </tr>
                ))}
              </tbody>
            </table>
          </div>
        </div>
      )}
    </>
  );
}

function Info({
  icon: Icon,
  label,
  value,
}: {
  icon?: typeof Phone;
  label: string;
  value: string;
}) {
  return (
    <div>
      <div className="text-xs text-slate-500 flex items-center gap-1">
        {Icon && <Icon size={12} />} {label}
      </div>
      <div className="text-sm font-medium">{value}</div>
    </div>
  );
}

function Stat({
  icon: Icon,
  label,
  value,
  color,
}: {
  icon: typeof TrendingUp;
  label: string;
  value: string;
  color: string;
}) {
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

function SaleStatusChip({ status }: { status: number }) {
  const map: Record<number, [string, string]> = {
    0: ["مسودة", "bg-slate-100 text-slate-800"],
    1: ["مكتملة", "bg-emerald-100 text-emerald-800"],
    2: ["ملغاة", "bg-slate-200 text-slate-600"],
    3: ["مرتجعة", "bg-red-100 text-red-800"],
    4: ["مرتجع جزئي", "bg-amber-100 text-amber-800"],
  };
  const [label, klass] = map[status] ?? ["—", "bg-slate-100"];
  return <span className={`text-xs px-2 py-0.5 rounded-full ${klass}`}>{label}</span>;
}

function loyaltyTypeLabel(type: number): string {
  switch (type) {
    case 1: return "اكتساب";
    case 2: return "استبدال";
    case 3: return "تعديل يدوي";
    case 4: return "انتهاء صلاحية";
    default: return "—";
  }
}
