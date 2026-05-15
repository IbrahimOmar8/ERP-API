"use client";

import { useState } from "react";
import Link from "next/link";
import { useQuery } from "@tanstack/react-query";
import { Eye, FileText, Plus } from "lucide-react";
import { api } from "@/lib/api";
import { formatDate, formatMoney } from "@/lib/format";
import {
  QuotationStatusColors,
  QuotationStatusLabels,
  type Quotation,
} from "@/types/api";
import PageHeader from "@/components/PageHeader";
import EmptyState from "@/components/EmptyState";
import { SkeletonRow } from "@/components/Skeleton";

export default function QuotationsPage() {
  const [status, setStatus] = useState<string>("");
  const [search, setSearch] = useState("");

  const { data, isLoading } = useQuery({
    queryKey: ["quotations", status, search],
    queryFn: async () =>
      (await api.get<Quotation[]>("/Quotations", {
        params: {
          status: status === "" ? undefined : status,
          search: search || undefined,
        },
      })).data,
  });

  return (
    <>
      <PageHeader title="عروض الأسعار">
        <Link href="/quotations/new" className="btn">
          <Plus size={16} /> عرض سعر جديد
        </Link>
      </PageHeader>

      <div className="card mb-3 flex flex-wrap items-end gap-3">
        <div className="flex-1 min-w-[200px]">
          <label>بحث</label>
          <input
            value={search}
            onChange={(e) => setSearch(e.target.value)}
            placeholder="رقم العرض أو اسم العميل..."
          />
        </div>
        <div>
          <label>الحالة</label>
          <select value={status} onChange={(e) => setStatus(e.target.value)}>
            <option value="">الكل</option>
            {Object.entries(QuotationStatusLabels).map(([v, l]) => (
              <option key={v} value={v}>{l}</option>
            ))}
          </select>
        </div>
      </div>

      <div className="table-wrap">
        <table>
          <thead>
            <tr>
              <th>الرقم</th>
              <th>التاريخ</th>
              <th>العميل</th>
              <th>الصلاحية</th>
              <th>الإجمالي</th>
              <th>الحالة</th>
              <th></th>
            </tr>
          </thead>
          <tbody>
            {isLoading ? (
              Array.from({ length: 5 }).map((_, i) => <SkeletonRow key={i} cols={7} />)
            ) : data?.length === 0 ? (
              <tr>
                <td colSpan={7}>
                  <EmptyState
                    icon={FileText}
                    title="لا توجد عروض أسعار"
                    description="أنشئ عرض سعر للعميل، اطبعه، ثم حوّله لفاتورة عند الموافقة."
                  />
                </td>
              </tr>
            ) : (
              data?.map((q) => (
                <tr key={q.id}>
                  <td className="font-mono text-xs">{q.quotationNumber}</td>
                  <td>{formatDate(q.issueDate)}</td>
                  <td>{q.customerName || q.customerNameSnapshot || "—"}</td>
                  <td className="text-xs">
                    {q.validUntil ? formatDate(q.validUntil) : "—"}
                  </td>
                  <td className="font-semibold">{formatMoney(q.total)}</td>
                  <td>
                    <span className={`text-xs px-2 py-0.5 rounded-full ${QuotationStatusColors[q.status]}`}>
                      {QuotationStatusLabels[q.status]}
                    </span>
                  </td>
                  <td>
                    <Link href={`/quotations/${q.id}`} className="btn-outline !px-2 !py-1 text-xs">
                      <Eye size={14} /> عرض
                    </Link>
                  </td>
                </tr>
              ))
            )}
          </tbody>
        </table>
      </div>
    </>
  );
}
