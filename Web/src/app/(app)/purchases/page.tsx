"use client";

import { useState } from "react";
import { useMutation, useQuery, useQueryClient } from "@tanstack/react-query";
import toast from "react-hot-toast";
import { FileText, Plus, Trash2 } from "lucide-react";
import EmptyState from "@/components/EmptyState";
import { SkeletonRow } from "@/components/Skeleton";
import { api, errorMessage } from "@/lib/api";
import { formatMoney, formatDate } from "@/lib/format";
import type { Product, PurchaseInvoice, Supplier, Warehouse } from "@/types/api";
import PageHeader from "@/components/PageHeader";
import Modal from "@/components/Modal";

interface Line {
  productId: string;
  productName: string;
  quantity: number;
  unitCost: number;
  vatRate: number;
}

export default function PurchasesPage() {
  const qc = useQueryClient();
  const [open, setOpen] = useState(false);

  const [supplierId, setSupplierId] = useState("");
  const [warehouseId, setWarehouseId] = useState("");
  const [paid, setPaid] = useState(0);
  const [notes, setNotes] = useState("");
  const [lines, setLines] = useState<Line[]>([]);
  const [pickProductId, setPickProductId] = useState("");
  const [pickQty, setPickQty] = useState(1);
  const [pickCost, setPickCost] = useState(0);

  const list = useQuery({
    queryKey: ["purchases"],
    queryFn: async () => (await api.get<PurchaseInvoice[]>("/PurchaseInvoices")).data,
  });
  const suppliers = useQuery({
    queryKey: ["suppliers"],
    queryFn: async () => (await api.get<Supplier[]>("/Suppliers")).data,
  });
  const warehouses = useQuery({
    queryKey: ["warehouses"],
    queryFn: async () => (await api.get<Warehouse[]>("/Warehouses")).data,
  });
  const products = useQuery({
    queryKey: ["products", "all-purch"],
    queryFn: async () => (await api.get<Product[]>("/Products", { params: { pageSize: 500 } })).data,
  });

  function reset() {
    setSupplierId("");
    setWarehouseId("");
    setPaid(0);
    setNotes("");
    setLines([]);
    setPickProductId("");
    setPickQty(1);
    setPickCost(0);
  }

  function pickProduct(id: string) {
    setPickProductId(id);
    const p = products.data?.find((x) => x.id === id);
    if (p) setPickCost(p.purchasePrice);
  }

  function addLine() {
    if (!pickProductId || pickQty <= 0) return;
    const p = products.data?.find((x) => x.id === pickProductId);
    if (!p) return;
    setLines((ls) => [
      ...ls,
      {
        productId: p.id,
        productName: p.nameAr,
        quantity: pickQty,
        unitCost: pickCost,
        vatRate: p.vatRate,
      },
    ]);
    setPickProductId("");
    setPickQty(1);
    setPickCost(0);
  }

  const subTotal = lines.reduce((s, l) => s + l.quantity * l.unitCost, 0);
  const vatTotal = lines.reduce((s, l) => s + l.quantity * l.unitCost * (l.vatRate / 100), 0);
  const total = subTotal + vatTotal;

  const create = useMutation({
    mutationFn: async () =>
      (
        await api.post<PurchaseInvoice>("/PurchaseInvoices", {
          supplierId,
          warehouseId,
          paid,
          notes: notes || null,
          items: lines.map((l) => ({
            productId: l.productId,
            quantity: l.quantity,
            unitCost: l.unitCost,
            discountAmount: 0,
            vatRate: l.vatRate,
          })),
        })
      ).data,
    onSuccess: () => {
      toast.success("تم تسجيل فاتورة الشراء");
      reset();
      setOpen(false);
      qc.invalidateQueries({ queryKey: ["purchases"] });
    },
    onError: (e) => toast.error(errorMessage(e)),
  });

  return (
    <>
      <PageHeader title="فواتير المشتريات">
        <button onClick={() => setOpen(true)} className="btn">
          <Plus size={16} /> فاتورة جديدة
        </button>
      </PageHeader>

      <div className="table-wrap">
        <table>
          <thead>
            <tr>
              <th>الرقم</th>
              <th>المورد</th>
              <th>المخزن</th>
              <th>التاريخ</th>
              <th>الإجمالي</th>
              <th>المدفوع</th>
              <th>الباقي</th>
            </tr>
          </thead>
          <tbody>
            {list.isLoading ? (
              Array.from({ length: 5 }).map((_, i) => <SkeletonRow key={i} cols={7} />)
            ) : list.data?.length === 0 ? (
              <tr>
                <td colSpan={7}>
                  <EmptyState
                    icon={FileText}
                    title="لا توجد فواتير شراء"
                    description="ابدأ بتسجيل فاتورة شراء لاستلام أصناف من الموردين."
                    actionLabel="فاتورة جديدة"
                    onAction={() => setOpen(true)}
                  />
                </td>
              </tr>
            ) : (
              list.data?.map((p) => (
                <tr key={p.id}>
                  <td className="font-mono text-xs">{p.invoiceNumber}</td>
                  <td>{p.supplierName}</td>
                  <td>{p.warehouseName}</td>
                  <td>{formatDate(p.invoiceDate)}</td>
                  <td className="font-semibold">{formatMoney(p.total)}</td>
                  <td>{formatMoney(p.paid)}</td>
                  <td className={p.remaining > 0 ? "text-amber-700 font-medium" : ""}>{formatMoney(p.remaining)}</td>
                </tr>
              ))
            )}
          </tbody>
        </table>
      </div>

      <Modal open={open} title="فاتورة شراء جديدة" onClose={() => setOpen(false)} size="xl">
        <div className="grid md:grid-cols-3 gap-3 mb-3">
          <div>
            <label>المورد *</label>
            <select value={supplierId} onChange={(e) => setSupplierId(e.target.value)}>
              <option value="">اختر</option>
              {suppliers.data?.map((s) => <option key={s.id} value={s.id}>{s.name}</option>)}
            </select>
          </div>
          <div>
            <label>المخزن *</label>
            <select value={warehouseId} onChange={(e) => setWarehouseId(e.target.value)}>
              <option value="">اختر</option>
              {warehouses.data?.map((w) => <option key={w.id} value={w.id}>{w.nameAr}</option>)}
            </select>
          </div>
          <div>
            <label>المدفوع</label>
            <input type="number" min={0} step="0.01" value={paid} onChange={(e) => setPaid(Number(e.target.value))} />
          </div>
          <div className="md:col-span-3">
            <label>ملاحظات</label>
            <input value={notes} onChange={(e) => setNotes(e.target.value)} />
          </div>
        </div>

        <div className="flex flex-wrap gap-2 items-end mb-3">
          <div className="flex-1 min-w-[200px]">
            <label>الصنف</label>
            <select value={pickProductId} onChange={(e) => pickProduct(e.target.value)}>
              <option value="">اختر صنف</option>
              {products.data?.map((p) => <option key={p.id} value={p.id}>{p.nameAr} ({p.sku})</option>)}
            </select>
          </div>
          <div className="w-28">
            <label>الكمية</label>
            <input type="number" min={0.01} step="0.01" value={pickQty} onChange={(e) => setPickQty(Number(e.target.value))} />
          </div>
          <div className="w-32">
            <label>التكلفة</label>
            <input type="number" min={0} step="0.01" value={pickCost} onChange={(e) => setPickCost(Number(e.target.value))} />
          </div>
          <button onClick={addLine} className="btn-outline">إضافة</button>
        </div>

        <div className="table-wrap mb-3">
          <table>
            <thead><tr><th>الصنف</th><th>الكمية</th><th>التكلفة</th><th>ضريبة %</th><th>الإجمالي</th><th></th></tr></thead>
            <tbody>
              {lines.map((l, i) => (
                <tr key={i}>
                  <td>{l.productName}</td>
                  <td>{l.quantity}</td>
                  <td>{formatMoney(l.unitCost)}</td>
                  <td>{l.vatRate}%</td>
                  <td className="font-semibold">{formatMoney(l.quantity * l.unitCost * (1 + l.vatRate / 100))}</td>
                  <td>
                    <button onClick={() => setLines(lines.filter((_, j) => j !== i))} className="text-red-500">
                      <Trash2 size={14} />
                    </button>
                  </td>
                </tr>
              ))}
              {lines.length === 0 && <tr><td colSpan={6} className="text-center py-4 text-slate-400">لا توجد أصناف</td></tr>}
            </tbody>
          </table>
        </div>

        <div className="bg-slate-50 rounded-lg p-3 grid grid-cols-3 gap-3 mb-3 text-sm">
          <div><span className="text-slate-500">صافي:</span> <span className="font-semibold">{formatMoney(subTotal)}</span></div>
          <div><span className="text-slate-500">ضريبة:</span> <span className="font-semibold">{formatMoney(vatTotal)}</span></div>
          <div><span className="text-slate-500">إجمالي:</span> <span className="font-bold text-lg">{formatMoney(total)}</span></div>
        </div>

        <div className="flex justify-end gap-2 pt-3 border-t border-slate-200">
          <button onClick={() => setOpen(false)} className="btn-outline">إلغاء</button>
          <button
            onClick={() => create.mutate()}
            disabled={!supplierId || !warehouseId || lines.length === 0 || create.isPending}
            className="btn-success"
          >
            {create.isPending ? "جاري الحفظ..." : "حفظ الفاتورة"}
          </button>
        </div>
      </Modal>
    </>
  );
}
