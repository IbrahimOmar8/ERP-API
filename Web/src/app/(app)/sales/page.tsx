"use client";

import { useQuery } from "@tanstack/react-query";
import Link from "next/link";
import { Download, Eye, Printer, Receipt } from "lucide-react";
import { api } from "@/lib/api";
import { formatMoney, formatDateTime, formatDate } from "@/lib/format";
import { downloadCsv } from "@/lib/csv";
import type { Sale } from "@/types/api";
import { SaleStatus } from "@/types/api";
import PageHeader from "@/components/PageHeader";
import EmptyState from "@/components/EmptyState";
import { SkeletonRow } from "@/components/Skeleton";

export default function SalesPage() {
  const apiUrl = process.env.NEXT_PUBLIC_API_URL || "http://localhost:5000/api";

  const { data, isLoading } = useQuery({
    queryKey: ["sales"],
    queryFn: async () => (await api.get<Sale[]>("/Sales", { params: { pageSize: 100 } })).data,
  });

  function exportCsv() {
    if (!data) return;
    downloadCsv("sales", data, [
      { header: "رقم الفاتورة", accessor: (s) => s.invoiceNumber },
      { header: "التاريخ", accessor: (s) => formatDate(s.saleDate) },
      { header: "العميل", accessor: (s) => s.customerName ?? "عميل نقدي" },
      { header: "المخزن", accessor: (s) => s.warehouseName ?? "" },
      { header: "الإجمالي", accessor: (s) => s.total.toFixed(2) },
      { header: "الحالة", accessor: (s) => SaleStatus[s.status] ?? "" },
      { header: "ETA UUID", accessor: (s) => s.eInvoiceUuid ?? "" },
    ]);
  }

  return (
    <>
      <PageHeader title="الفواتير">
        <button onClick={exportCsv} disabled={!data || data.length === 0} className="btn-outline">
          <Download size={16} /> تصدير CSV
        </button>
      </PageHeader>

      {!isLoading && data?.length === 0 ? (
        <EmptyState
          icon={Receipt}
          title="لا توجد فواتير بعد"
          description="عند تسجيل فواتير من شاشة نقطة البيع ستظهر هنا."
        />
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
              {isLoading
                ? Array.from({ length: 6 }).map((_, i) => <SkeletonRow key={i} cols={7} />)
                : data?.map((s) => (
                    <tr key={s.id}>
                      <td className="font-mono">{s.invoiceNumber}</td>
                      <td>{formatDateTime(s.saleDate)}</td>
                      <td>{s.customerName || "عميل نقدي"}</td>
                      <td>{s.warehouseName}</td>
                      <td className="font-semibold">{formatMoney(s.total)}</td>
                      <td>{s.eInvoiceUuid ? "✓" : "—"}</td>
                      <td className="flex gap-1">
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
            </tbody>
          </table>
        </div>
      )}
    </>
  );
}
