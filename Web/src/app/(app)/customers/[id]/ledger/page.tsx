"use client";

import { useState } from "react";
import { useParams } from "next/navigation";
import { useMutation, useQuery, useQueryClient } from "@tanstack/react-query";
import toast from "react-hot-toast";
import { Plus, Receipt, Trash2 } from "lucide-react";
import { api, errorMessage } from "@/lib/api";
import { formatDateTime, formatMoney } from "@/lib/format";
import PageHeader from "@/components/PageHeader";
import Modal from "@/components/Modal";
import { useConfirm } from "@/components/ConfirmDialog";

interface LedgerRow {
  date: string;
  type: string;
  reference: string;
  debit: number;
  credit: number;
  balance: number;
  notes?: string | null;
}
interface Ledger {
  customerId: string;
  customerName: string;
  openingBalance: number;
  closingBalance: number;
  totalDebit: number;
  totalCredit: number;
  rows: LedgerRow[];
}

const PAYMENT_METHODS = [
  { value: 1, label: "نقدي" },
  { value: 2, label: "بطاقة" },
  { value: 5, label: "تحويل بنكي" },
  { value: 4, label: "محفظة" },
  { value: 3, label: "إنستا باي" },
];

export default function CustomerLedgerPage() {
  const params = useParams<{ id: string }>();
  const customerId = params.id;
  const qc = useQueryClient();
  const { confirm, dialog } = useConfirm();
  const [open, setOpen] = useState(false);
  const [form, setForm] = useState({ amount: 0, method: 1, reference: "", notes: "" });

  const ledger = useQuery({
    queryKey: ["customer-ledger", customerId],
    queryFn: async () => (await api.get<Ledger>(`/customer-payments/ledger/${customerId}`)).data,
  });

  const record = useMutation({
    mutationFn: async () =>
      (await api.post("/customer-payments", {
        customerId,
        amount: form.amount,
        method: form.method,
        reference: form.reference || null,
        notes: form.notes || null,
      })).data,
    onSuccess: () => {
      toast.success("تم تسجيل الدفعة");
      setOpen(false);
      setForm({ amount: 0, method: 1, reference: "", notes: "" });
      qc.invalidateQueries({ queryKey: ["customer-ledger", customerId] });
      qc.invalidateQueries({ queryKey: ["customers"] });
    },
    onError: (e) => toast.error(errorMessage(e)),
  });

  return (
    <>
      <PageHeader
        title={ledger.data ? `كشف حساب: ${ledger.data.customerName}` : "كشف حساب العميل"}
      >
        <button onClick={() => setOpen(true)} className="btn">
          <Plus size={16} /> تسجيل دفعة
        </button>
      </PageHeader>

      {ledger.data && (
        <div className="grid grid-cols-2 md:grid-cols-4 gap-3 mb-4">
          <Card label="رصيد افتتاحي" value={formatMoney(ledger.data.openingBalance)} />
          <Card label="إجمالي مدين" value={formatMoney(ledger.data.totalDebit)} color="text-red-600" />
          <Card label="إجمالي دائن" value={formatMoney(ledger.data.totalCredit)} color="text-emerald-600" />
          <Card
            label="الرصيد الحالي"
            value={formatMoney(ledger.data.closingBalance)}
            color={ledger.data.closingBalance > 0 ? "text-amber-600" : "text-emerald-600"}
            big
          />
        </div>
      )}

      <div className="table-wrap">
        <table>
          <thead>
            <tr>
              <th>التاريخ</th>
              <th>النوع</th>
              <th>المرجع</th>
              <th>مدين</th>
              <th>دائن</th>
              <th>الرصيد</th>
            </tr>
          </thead>
          <tbody>
            {ledger.data?.rows.length === 0 ? (
              <tr>
                <td colSpan={6} className="text-center py-8 text-slate-400">
                  لا توجد حركات
                </td>
              </tr>
            ) : (
              ledger.data?.rows.map((r, i) => (
                <tr key={i}>
                  <td className="text-xs">{formatDateTime(r.date)}</td>
                  <td>
                    {r.type === "sale" ? (
                      <span className="text-xs px-2 py-0.5 rounded-full bg-amber-100 text-amber-800">فاتورة</span>
                    ) : (
                      <span className="text-xs px-2 py-0.5 rounded-full bg-emerald-100 text-emerald-800">دفعة</span>
                    )}
                  </td>
                  <td className="font-mono text-xs">{r.reference}</td>
                  <td className="text-red-600">{r.debit > 0 ? formatMoney(r.debit) : "—"}</td>
                  <td className="text-emerald-600">{r.credit > 0 ? formatMoney(r.credit) : "—"}</td>
                  <td className="font-semibold">{formatMoney(r.balance)}</td>
                </tr>
              ))
            )}
          </tbody>
        </table>
      </div>

      <Modal open={open} title="تسجيل دفعة من العميل" onClose={() => setOpen(false)}>
        <div className="space-y-3">
          <div>
            <label>المبلغ *</label>
            <input
              type="number"
              min={0.01}
              step="0.01"
              value={form.amount}
              onChange={(e) => setForm({ ...form, amount: Number(e.target.value) })}
            />
          </div>
          <div>
            <label>طريقة الدفع</label>
            <select value={form.method} onChange={(e) => setForm({ ...form, method: Number(e.target.value) })}>
              {PAYMENT_METHODS.map((m) => (
                <option key={m.value} value={m.value}>{m.label}</option>
              ))}
            </select>
          </div>
          <div>
            <label>المرجع</label>
            <input value={form.reference} onChange={(e) => setForm({ ...form, reference: e.target.value })}
              placeholder="رقم إيصال، حوالة، إلخ" />
          </div>
          <div>
            <label>ملاحظات</label>
            <input value={form.notes} onChange={(e) => setForm({ ...form, notes: e.target.value })} />
          </div>
        </div>
        <div className="flex justify-end gap-2 mt-4">
          <button onClick={() => setOpen(false)} className="btn-outline">إلغاء</button>
          <button
            onClick={() => record.mutate()}
            disabled={form.amount <= 0 || record.isPending}
            className="btn-success"
          >
            {record.isPending ? "..." : "تسجيل"}
          </button>
        </div>
      </Modal>
      {dialog}
    </>
  );
}

function Card({ label, value, color = "", big = false }: { label: string; value: string; color?: string; big?: boolean }) {
  return (
    <div className="card">
      <div className="text-xs text-slate-500">{label}</div>
      <div className={`font-bold ${color} ${big ? "text-2xl" : "text-lg"}`}>{value}</div>
    </div>
  );
}
