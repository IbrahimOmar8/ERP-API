"use client";

import { useState } from "react";
import { useQuery } from "@tanstack/react-query";
import { api } from "@/lib/api";
import { formatMoney, formatNumber } from "@/lib/format";
import type { Product } from "@/types/api";
import PageHeader from "@/components/PageHeader";

export default function ProductsPage() {
  const [search, setSearch] = useState("");

  const { data, isLoading } = useQuery({
    queryKey: ["products", search],
    queryFn: async () =>
      (await api.get<Product[]>("/Products", {
        params: { search: search || undefined, pageSize: 300 },
      })).data,
  });

  return (
    <>
      <PageHeader title="الأصناف" description="عرض الأصناف وأرصدتها الحالية" />
      <div className="card mb-3">
        <input
          placeholder="ابحث بالاسم أو SKU أو الباركود..."
          value={search}
          onChange={(e) => setSearch(e.target.value)}
        />
      </div>
      <div className="table-wrap">
        <table>
          <thead>
            <tr>
              <th>الكود</th>
              <th>الباركود</th>
              <th>الاسم</th>
              <th>الفئة</th>
              <th>سعر البيع</th>
              <th>الضريبة</th>
              <th>الرصيد</th>
            </tr>
          </thead>
          <tbody>
            {isLoading ? (
              <tr><td colSpan={7} className="text-center py-6">جاري التحميل...</td></tr>
            ) : (
              data?.map((p) => (
                <tr key={p.id}>
                  <td className="font-mono text-xs">{p.sku}</td>
                  <td className="font-mono text-xs">{p.barcode}</td>
                  <td className="font-medium">{p.nameAr}</td>
                  <td>{p.categoryName}</td>
                  <td>{formatMoney(p.salePrice)}</td>
                  <td>{p.vatRate.toFixed(1)}%</td>
                  <td className={p.currentStock <= p.minStockLevel ? "text-red-600 font-semibold" : ""}>
                    {formatNumber(p.currentStock)}
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
