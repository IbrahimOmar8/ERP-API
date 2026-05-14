"use client";

import { useQuery } from "@tanstack/react-query";
import Link from "next/link";
import { Eye, Printer } from "lucide-react";
import { api } from "@/lib/api";
import { formatMoney, formatDateTime } from "@/lib/format";
import type { Sale } from "@/types/api";
import PageHeader from "@/components/PageHeader";

export default function SalesPage() {
  const apiUrl = process.env.NEXT_PUBLIC_API_URL || "http://localhost:5000/api";

  const { data, isLoading } = useQuery({
    queryKey: ["sales"],
    queryFn: async () => (await api.get<Sale[]>("/Sales", { params: { pageSize: 100 } })).data,
  });

  return (
    <>
      <PageHeader title="الفواتير" />
      {isLoading ? (
        <p>جاري التحميل...</p>
      ) : (
        <div className="table-wrap">
          <table>
            <thead>
              <tr>
                <th>الرقم</th>
                <th>التاريخ</th>
                <th>العميل</th>
                <th>المخزن</th>
                <th>الإجمالي</th>
                <th>ETA</th>
                <th></th>
              </tr>
            </thead>
            <tbody>
              {data?.map((s) => (
                <tr key={s.id}>
                  <td className="font-mono">{s.invoiceNumber}</td>
                  <td>{formatDateTime(s.saleDate)}</td>
                  <td>{s.customerName || "عميل نقدي"}</td>
                  <td>{s.warehouseName}</td>
                  <td className="font-semibold">{formatMoney(s.total)}</td>
                  <td>{s.eInvoiceUuid ? "✓" : "—"}</td>
                  <td className="space-x-1 space-x-reverse">
                    <Link href={`/sales/${s.id}`} className="btn-outline !px-2 !py-1 text-xs">
                      <Eye size={14} /> عرض
                    </Link>
                    <a
                      href={`${apiUrl}/sales/${s.id}/print?format=a4`}
                      target="_blank"
                      rel="noreferrer"
                      className="btn !px-2 !py-1 text-xs"
                    >
                      <Printer size={14} /> طباعة
                    </a>
                  </td>
                </tr>
              ))}
              {data?.length === 0 && (
                <tr>
                  <td colSpan={7} className="text-center text-slate-400 py-8">
                    لا توجد فواتير
                  </td>
                </tr>
              )}
            </tbody>
          </table>
        </div>
      )}
    </>
  );
}
