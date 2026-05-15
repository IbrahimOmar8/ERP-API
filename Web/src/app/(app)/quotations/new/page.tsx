"use client";

import { useState } from "react";
import { useRouter } from "next/navigation";
import { useMutation, useQuery } from "@tanstack/react-query";
import toast from "react-hot-toast";
import { Plus, Trash2 } from "lucide-react";
import { api, errorMessage } from "@/lib/api";
import { formatMoney } from "@/lib/format";
import type { Customer, Product, Warehouse } from "@/types/api";
import PageHeader from "@/components/PageHeader";

interface Line {
  productId: string;
  productName: string;
  quantity: number;
  unitPrice: number;
  vatRate: number;
  discountAmount: number;
}

export default function NewQuotationPage() {
  const router = useRouter();
  const [customerId, setCustomerId] = useState("");
  const [customerNameSnapshot, setCustomerNameSnapshot] = useState("");
  const [customerPhoneSnapshot, setCustomerPhoneSnapshot] = useState("");
  const [warehouseId, setWarehouseId] = useState("");
  const [validUntil, setValidUntil] = useState("");
  const [discount, setDiscount] = useState(0);
  const [notes, setNotes] = useState("");
  const [terms, setTerms] = useState("سعر العرض ساري لمدة 7 أيام. الأسعار شاملة الضريبة المضافة.");
  const [lines, setLines] = useState<Line[]>([]);
  const [pickProductId, setPickProductId] = useState("");
  const [pickQty, setPickQty] = useState(1);

  const products = useQuery({
    queryKey: ["products", "all"],
    queryFn: async () => (await api.get<Product[]>("/Products", { params: { pageSize: 500 } })).data,
  });
  const customers = useQuery({
    queryKey: ["customers"],
    queryFn: async () => (await api.get<Customer[]>("/Customers")).data,
  });
  const warehouses = useQuery({
    queryKey: ["warehouses"],
    queryFn: async () => (await api.get<Warehouse[]>("/Warehouses")).data,
  });

  function addLine() {
    const p = products.data?.find((x) => x.id === pickProductId);
    if (!p || pickQty <= 0) return;
    setLines((ls) => [
      ...ls,
      {
        productId: p.id,
        productName: p.nameAr,
        quantity: pickQty,
        unitPrice: p.salePrice,
        vatRate: p.vatRate,
        discountAmount: 0,
      },
    ]);
    setPickProductId("");
    setPickQty(1);
  }

  const subTotal = lines.reduce((s, l) => s + l.quantity * l.unitPrice, 0);
  const lineDiscounts = lines.reduce((s, l) => s + l.discountAmount, 0);
  const vat = lines.reduce(
    (s, l) => s + (l.quantity * l.unitPrice - l.discountAmount) * (l.vatRate / 100),
    0
  );
  const total = Math.max(0, subTotal - lineDiscounts - discount + vat);

  const create = useMutation({
    mutationFn: async () =>
      (await api.post<{ id: string }>("/Quotations", {
        customerId: customerId || null,
        customerNameSnapshot: customerNameSnapshot || null,
        customerPhoneSnapshot: customerPhoneSnapshot || null,
        warehouseId: warehouseId || null,
        validUntil: validUntil ? new Date(validUntil).toISOString() : null,
        discountAmount: discount,
        notes: notes || null,
        terms: terms || null,
        items: lines.map((l) => ({
          productId: l.productId,
          quantity: l.quantity,
          unitPrice: l.unitPrice,
          discountAmount: l.discountAmount,
        })),
      })).data,
    onSuccess: (d) => {
      toast.success("تم إنشاء عرض السعر");
      router.push(`/quotations/${d.id}`);
    },
    onError: (e) => toast.error(errorMessage(e)),
  });

  return (
    <>
      <PageHeader title="عرض سعر جديد" />

      <div className="card mb-3">
        <h3 className="font-semibold mb-3">بيانات العميل</h3>
        <div className="grid md:grid-cols-3 gap-3">
          <div>
            <label>عميل مسجل</label>
            <select value={customerId} onChange={(e) => setCustomerId(e.target.value)}>
              <option value="">— عميل خارجي —</option>
              {customers.data?.map((c) => (
                <option key={c.id} value={c.id}>{c.name}</option>
              ))}
            </select>
          </div>
          <div>
            <label>اسم العميل (إن لم يكن مسجلاً)</label>
            <input value={customerNameSnapshot} onChange={(e) => setCustomerNameSnapshot(e.target.value)} disabled={!!customerId} />
          </div>
          <div>
            <label>هاتف العميل</label>
            <input value={customerPhoneSnapshot} onChange={(e) => setCustomerPhoneSnapshot(e.target.value)} disabled={!!customerId} />
          </div>
          <div>
            <label>المخزن</label>
            <select value={warehouseId} onChange={(e) => setWarehouseId(e.target.value)}>
              <option value="">—</option>
              {warehouses.data?.map((w) => <option key={w.id} value={w.id}>{w.nameAr}</option>)}
            </select>
          </div>
          <div>
            <label>سارٍ حتى</label>
            <input type="date" value={validUntil} onChange={(e) => setValidUntil(e.target.value)} />
          </div>
        </div>
      </div>

      <div className="card mb-3">
        <h3 className="font-semibold mb-3">الأصناف</h3>
        <div className="flex flex-wrap gap-2 items-end mb-3">
          <div className="flex-1 min-w-[200px]">
            <label>صنف</label>
            <select value={pickProductId} onChange={(e) => setPickProductId(e.target.value)}>
              <option value="">اختر</option>
              {products.data?.map((p) => <option key={p.id} value={p.id}>{p.nameAr} ({p.sku})</option>)}
            </select>
          </div>
          <div className="w-24">
            <label>الكمية</label>
            <input type="number" min={0.01} step="0.01" value={pickQty} onChange={(e) => setPickQty(Number(e.target.value))} />
          </div>
          <button onClick={addLine} className="btn-outline">إضافة</button>
        </div>

        {lines.length > 0 && (
          <div className="table-wrap">
            <table>
              <thead>
                <tr>
                  <th>الصنف</th>
                  <th>الكمية</th>
                  <th>السعر</th>
                  <th>خصم</th>
                  <th>الإجمالي</th>
                  <th></th>
                </tr>
              </thead>
              <tbody>
                {lines.map((l, i) => (
                  <tr key={i}>
                    <td>{l.productName}</td>
                    <td>
                      <input
                        type="number" step="0.01" min={0.01}
                        value={l.quantity}
                        onChange={(e) => setLines(lines.map((x, j) => j === i ? { ...x, quantity: Number(e.target.value) } : x))}
                        className="!py-1 w-20"
                      />
                    </td>
                    <td>
                      <input
                        type="number" step="0.01" min={0}
                        value={l.unitPrice}
                        onChange={(e) => setLines(lines.map((x, j) => j === i ? { ...x, unitPrice: Number(e.target.value) } : x))}
                        className="!py-1 w-24"
                      />
                    </td>
                    <td>
                      <input
                        type="number" step="0.01" min={0}
                        value={l.discountAmount}
                        onChange={(e) => setLines(lines.map((x, j) => j === i ? { ...x, discountAmount: Number(e.target.value) } : x))}
                        className="!py-1 w-20"
                      />
                    </td>
                    <td className="font-semibold">
                      {formatMoney(l.quantity * l.unitPrice - l.discountAmount)}
                    </td>
                    <td>
                      <button onClick={() => setLines(lines.filter((_, j) => j !== i))} className="text-red-500">
                        <Trash2 size={14} />
                      </button>
                    </td>
                  </tr>
                ))}
              </tbody>
            </table>
          </div>
        )}
      </div>

      <div className="card mb-3">
        <h3 className="font-semibold mb-3">الملخص والشروط</h3>
        <div className="grid md:grid-cols-3 gap-3">
          <div>
            <label>خصم على الفاتورة</label>
            <input type="number" step="0.01" min={0} value={discount} onChange={(e) => setDiscount(Number(e.target.value))} />
          </div>
          <div className="md:col-span-2">
            <label>ملاحظات</label>
            <input value={notes} onChange={(e) => setNotes(e.target.value)} />
          </div>
          <div className="md:col-span-3">
            <label>الشروط</label>
            <textarea rows={2} value={terms} onChange={(e) => setTerms(e.target.value)} />
          </div>
        </div>
        <div className="bg-slate-50 dark:bg-slate-800 rounded-lg p-3 mt-3 grid grid-cols-2 md:grid-cols-4 gap-2 text-sm">
          <div><span className="text-slate-500">المجموع:</span> <b>{formatMoney(subTotal)}</b></div>
          <div><span className="text-slate-500">خصم:</span> <b className="text-red-600">{formatMoney(lineDiscounts + discount)}</b></div>
          <div><span className="text-slate-500">ضريبة:</span> <b>{formatMoney(vat)}</b></div>
          <div><span className="text-slate-500">الإجمالي:</span> <b className="text-lg">{formatMoney(total)}</b></div>
        </div>
      </div>

      <button
        onClick={() => create.mutate()}
        disabled={lines.length === 0 || create.isPending}
        className="btn-success !px-6 !py-3 text-base"
      >
        {create.isPending ? "جاري الحفظ..." : "حفظ كمسودة"}
      </button>
    </>
  );
}
