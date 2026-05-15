"use client";

import { useState } from "react";
import { useMutation, useQuery, useQueryClient } from "@tanstack/react-query";
import toast from "react-hot-toast";
import { Download, Pencil, Plus, Receipt, Trash2 } from "lucide-react";
import { api, errorMessage } from "@/lib/api";
import { downloadCsv } from "@/lib/csv";
import { formatDate, formatMoney } from "@/lib/format";
import {
  ExpenseCategoryLabels,
  PaymentMethodLabels,
  type Expense,
  type ExpenseSummary,
} from "@/types/api";
import PageHeader from "@/components/PageHeader";
import Modal from "@/components/Modal";
import EmptyState from "@/components/EmptyState";
import { SkeletonRow } from "@/components/Skeleton";
import { useConfirm } from "@/components/ConfirmDialog";

interface Form {
  id?: string;
  title: string;
  category: number;
  amount: number;
  expenseDate: string;
  paymentMethod: number;
  reference: string;
  notes: string;
}

const emptyForm: Form = {
  title: "",
  category: 99,
  amount: 0,
  expenseDate: new Date().toISOString().slice(0, 10),
  paymentMethod: 1,
  reference: "",
  notes: "",
};

const CATEGORIES = Object.entries(ExpenseCategoryLabels).map(([k, v]) => ({
  value: Number(k),
  label: v,
}));

const PAYMENT_METHODS = [
  { value: 1, label: "نقدي" },
  { value: 2, label: "بطاقة" },
  { value: 5, label: "تحويل بنكي" },
  { value: 4, label: "محفظة" },
];

function isoFromDate(d: string): string {
  return new Date(d + "T00:00:00").toISOString();
}

export default function ExpensesPage() {
  const qc = useQueryClient();
  const { confirm, dialog } = useConfirm();
  const today = new Date();
  const monthAgo = new Date(today);
  monthAgo.setDate(today.getDate() - 30);

  const [from, setFrom] = useState(monthAgo.toISOString().slice(0, 10));
  const [to, setTo] = useState(today.toISOString().slice(0, 10));
  const [category, setCategory] = useState("");
  const [open, setOpen] = useState(false);
  const [form, setForm] = useState<Form>(emptyForm);

  const list = useQuery({
    queryKey: ["expenses", from, to, category],
    queryFn: async () =>
      (await api.get<Expense[]>("/Expenses", {
        params: {
          from: isoFromDate(from),
          to: isoFromDate(to + "T23:59:59"),
          category: category || undefined,
        },
      })).data,
  });

  const summary = useQuery({
    queryKey: ["expenses-summary", from, to],
    queryFn: async () =>
      (await api.get<ExpenseSummary>("/Expenses/summary", {
        params: { from: isoFromDate(from), to: isoFromDate(to + "T23:59:59") },
      })).data,
  });

  const save = useMutation({
    mutationFn: async () => {
      const payload = {
        ...form,
        amount: Number(form.amount),
        expenseDate: isoFromDate(form.expenseDate),
        reference: form.reference || null,
        notes: form.notes || null,
      };
      if (form.id) return (await api.put(`/Expenses/${form.id}`, payload)).data;
      return (await api.post("/Expenses", payload)).data;
    },
    onSuccess: () => {
      toast.success("تم الحفظ");
      setOpen(false);
      setForm(emptyForm);
      qc.invalidateQueries({ queryKey: ["expenses"] });
      qc.invalidateQueries({ queryKey: ["expenses-summary"] });
    },
    onError: (e) => toast.error(errorMessage(e)),
  });

  const del = useMutation({
    mutationFn: async (id: string) => api.delete(`/Expenses/${id}`),
    onSuccess: () => {
      toast.success("تم الحذف");
      qc.invalidateQueries({ queryKey: ["expenses"] });
      qc.invalidateQueries({ queryKey: ["expenses-summary"] });
    },
    onError: (e) => toast.error(errorMessage(e)),
  });

  function edit(e: Expense) {
    setForm({
      id: e.id,
      title: e.title,
      category: e.category,
      amount: e.amount,
      expenseDate: e.expenseDate.slice(0, 10),
      paymentMethod: e.paymentMethod,
      reference: e.reference ?? "",
      notes: e.notes ?? "",
    });
    setOpen(true);
  }

  async function remove(e: Expense) {
    if (await confirm("حذف المصروف", `حذف "${e.title}"؟`)) del.mutate(e.id);
  }

  function exportCsv() {
    if (!list.data) return;
    downloadCsv("expenses", list.data, [
      { header: "العنوان", accessor: (e) => e.title },
      { header: "الفئة", accessor: (e) => ExpenseCategoryLabels[e.category] ?? "" },
      { header: "المبلغ", accessor: (e) => e.amount.toFixed(2) },
      { header: "التاريخ", accessor: (e) => formatDate(e.expenseDate) },
      { header: "طريقة الدفع", accessor: (e) => PaymentMethodLabels[e.paymentMethod] ?? "" },
      { header: "المرجع", accessor: (e) => e.reference ?? "" },
      { header: "ملاحظات", accessor: (e) => e.notes ?? "" },
    ]);
  }

  return (
    <>
      <PageHeader title="المصروفات" description="إيجار، رواتب، مرافق، صيانة، ...">
        <button
          onClick={exportCsv}
          disabled={!list.data || list.data.length === 0}
          className="btn-outline"
        >
          <Download size={16} /> تصدير CSV
        </button>
        <button onClick={() => { setForm(emptyForm); setOpen(true); }} className="btn">
          <Plus size={16} /> مصروف جديد
        </button>
      </PageHeader>

      {/* Summary */}
      {summary.data && (
        <div className="grid grid-cols-2 md:grid-cols-4 gap-3 mb-4">
          <div className="card">
            <div className="text-xs text-slate-500">إجمالي المصروفات</div>
            <div className="text-2xl font-bold text-red-600">
              {formatMoney(summary.data.total)}
            </div>
            <div className="text-xs text-slate-400 mt-1">
              {summary.data.count} عملية
            </div>
          </div>
          {summary.data.byCategory.slice(0, 3).map((c) => (
            <div key={c.category} className="card">
              <div className="text-xs text-slate-500">
                {ExpenseCategoryLabels[c.category]}
              </div>
              <div className="text-lg font-bold">{formatMoney(c.total)}</div>
              <div className="text-xs text-slate-400 mt-1">
                {c.count} عملية
              </div>
            </div>
          ))}
        </div>
      )}

      {/* Filters */}
      <div className="card mb-3 flex flex-wrap items-end gap-3">
        <div>
          <label>من</label>
          <input type="date" value={from} onChange={(e) => setFrom(e.target.value)} />
        </div>
        <div>
          <label>إلى</label>
          <input type="date" value={to} onChange={(e) => setTo(e.target.value)} />
        </div>
        <div>
          <label>الفئة</label>
          <select value={category} onChange={(e) => setCategory(e.target.value)}>
            <option value="">كل الفئات</option>
            {CATEGORIES.map((c) => (
              <option key={c.value} value={c.value}>{c.label}</option>
            ))}
          </select>
        </div>
      </div>

      <div className="table-wrap">
        <table>
          <thead>
            <tr>
              <th>التاريخ</th>
              <th>العنوان</th>
              <th>الفئة</th>
              <th>طريقة الدفع</th>
              <th>المبلغ</th>
              <th>المرجع</th>
              <th></th>
            </tr>
          </thead>
          <tbody>
            {list.isLoading ? (
              Array.from({ length: 6 }).map((_, i) => <SkeletonRow key={i} cols={7} />)
            ) : list.data?.length === 0 ? (
              <tr>
                <td colSpan={7}>
                  <EmptyState
                    icon={Receipt}
                    title="لا توجد مصروفات"
                    description="سجل أول مصروف لتتبع المصاريف التشغيلية وحساب صافي الربح."
                    actionLabel="إضافة مصروف"
                    onAction={() => { setForm(emptyForm); setOpen(true); }}
                  />
                </td>
              </tr>
            ) : (
              list.data?.map((e) => (
                <tr key={e.id}>
                  <td>{formatDate(e.expenseDate)}</td>
                  <td className="font-medium">{e.title}</td>
                  <td>
                    <span className="text-xs px-2 py-0.5 rounded-full bg-slate-100 dark:bg-slate-800">
                      {ExpenseCategoryLabels[e.category] ?? "—"}
                    </span>
                  </td>
                  <td>{PaymentMethodLabels[e.paymentMethod] ?? "—"}</td>
                  <td className="font-semibold text-red-600">{formatMoney(e.amount)}</td>
                  <td className="font-mono text-xs">{e.reference}</td>
                  <td className="flex gap-1">
                    <button onClick={() => edit(e)} className="btn-outline !px-2 !py-1 text-xs">
                      <Pencil size={14} />
                    </button>
                    <button onClick={() => remove(e)} className="btn-danger !px-2 !py-1 text-xs">
                      <Trash2 size={14} />
                    </button>
                  </td>
                </tr>
              ))
            )}
          </tbody>
        </table>
      </div>

      <Modal open={open} title={form.id ? "تعديل مصروف" : "مصروف جديد"} onClose={() => setOpen(false)}>
        <div className="grid md:grid-cols-2 gap-3">
          <div className="md:col-span-2">
            <label>العنوان *</label>
            <input value={form.title} onChange={(e) => setForm({ ...form, title: e.target.value })} />
          </div>
          <div>
            <label>الفئة *</label>
            <select
              value={form.category}
              onChange={(e) => setForm({ ...form, category: Number(e.target.value) })}
            >
              {CATEGORIES.map((c) => (
                <option key={c.value} value={c.value}>{c.label}</option>
              ))}
            </select>
          </div>
          <div>
            <label>المبلغ *</label>
            <input
              type="number"
              step="0.01"
              min={0}
              value={form.amount}
              onChange={(e) => setForm({ ...form, amount: Number(e.target.value) })}
            />
          </div>
          <div>
            <label>التاريخ *</label>
            <input
              type="date"
              value={form.expenseDate}
              onChange={(e) => setForm({ ...form, expenseDate: e.target.value })}
            />
          </div>
          <div>
            <label>طريقة الدفع</label>
            <select
              value={form.paymentMethod}
              onChange={(e) => setForm({ ...form, paymentMethod: Number(e.target.value) })}
            >
              {PAYMENT_METHODS.map((m) => (
                <option key={m.value} value={m.value}>{m.label}</option>
              ))}
            </select>
          </div>
          <div>
            <label>المرجع (إيصال/مستند)</label>
            <input value={form.reference} onChange={(e) => setForm({ ...form, reference: e.target.value })} />
          </div>
          <div className="md:col-span-2">
            <label>ملاحظات</label>
            <textarea
              rows={2}
              value={form.notes}
              onChange={(e) => setForm({ ...form, notes: e.target.value })}
            />
          </div>
        </div>
        <div className="flex justify-end gap-2 mt-4 pt-4 border-t border-slate-200 dark:border-slate-700">
          <button onClick={() => setOpen(false)} className="btn-outline">إلغاء</button>
          <button
            onClick={() => save.mutate()}
            disabled={!form.title.trim() || form.amount <= 0 || save.isPending}
            className="btn-success"
          >
            {save.isPending ? "جاري الحفظ..." : "حفظ"}
          </button>
        </div>
      </Modal>
      {dialog}
    </>
  );
}
