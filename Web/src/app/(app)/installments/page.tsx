"use client";

import { Fragment, useState } from "react";
import { useMutation, useQuery, useQueryClient } from "@tanstack/react-query";
import toast from "react-hot-toast";
import { CalendarDays, Check, ChevronDown, ChevronUp, Plus, Trash2, X } from "lucide-react";
import { api, errorMessage } from "@/lib/api";
import { formatDate, formatMoney } from "@/lib/format";
import type { Customer, InstallmentPlan } from "@/types/api";
import {
  InstallmentFrequencyLabel,
  InstallmentPlanStatusLabel,
  InstallmentStatusLabel,
  PaymentMethodLabels,
} from "@/types/api";
import PageHeader from "@/components/PageHeader";
import EmptyState from "@/components/EmptyState";
import { SkeletonRow } from "@/components/Skeleton";
import { useConfirm } from "@/components/ConfirmDialog";

const today = () => new Date().toISOString().slice(0, 10);

export default function InstallmentsPage() {
  const qc = useQueryClient();
  const { confirm, dialog } = useConfirm();
  const [statusFilter, setStatusFilter] = useState<number | "">("");
  const [show, setShow] = useState(false);
  const [expanded, setExpanded] = useState<string | null>(null);
  const [payFor, setPayFor] = useState<{ installmentId: string; defaultAmount: number; label: string } | null>(null);
  const [payAmount, setPayAmount] = useState(0);
  const [payMethod, setPayMethod] = useState(1);
  const [payRef, setPayRef] = useState("");
  const [form, setForm] = useState({
    customerId: "", totalAmount: 0, downPayment: 0, installmentCount: 6,
    frequency: 0, startDate: today(), notes: "",
  });

  const plans = useQuery({
    queryKey: ["installment-plans", statusFilter],
    queryFn: async () =>
      (await api.get<InstallmentPlan[]>("/installments", {
        params: { status: statusFilter === "" ? undefined : statusFilter },
      })).data,
  });

  const customers = useQuery({
    queryKey: ["customers"],
    queryFn: async () => (await api.get<Customer[]>("/Customers")).data,
  });

  const create = useMutation({
    mutationFn: async () =>
      (await api.post("/installments", {
        ...form,
        notes: form.notes || null,
        startDate: new Date(form.startDate).toISOString(),
      })).data,
    onSuccess: () => {
      toast.success("تم إنشاء خطة التقسيط");
      setShow(false);
      setForm({ customerId: "", totalAmount: 0, downPayment: 0, installmentCount: 6, frequency: 0, startDate: today(), notes: "" });
      qc.invalidateQueries({ queryKey: ["installment-plans"] });
    },
    onError: (e) => toast.error(errorMessage(e)),
  });

  const pay = useMutation({
    mutationFn: async () =>
      (await api.post(`/installments/installment/${payFor!.installmentId}/pay`, {
        amount: payAmount,
        method: payMethod,
        reference: payRef || null,
      })).data,
    onSuccess: () => {
      toast.success("تم تسجيل السداد");
      setPayFor(null);
      setPayRef("");
      qc.invalidateQueries({ queryKey: ["installment-plans"] });
    },
    onError: (e) => toast.error(errorMessage(e)),
  });

  const cancel = useMutation({
    mutationFn: async (id: string) => (await api.post(`/installments/${id}/cancel`)).data,
    onSuccess: () => { toast.success("تم"); qc.invalidateQueries({ queryKey: ["installment-plans"] }); },
    onError: (e) => toast.error(errorMessage(e)),
  });

  const del = useMutation({
    mutationFn: async (id: string) => (await api.delete(`/installments/${id}`)).data,
    onSuccess: () => { toast.success("تم الحذف"); qc.invalidateQueries({ queryKey: ["installment-plans"] }); },
    onError: (e) => toast.error(errorMessage(e)),
  });

  const financed = Math.max(0, form.totalAmount - form.downPayment);
  const monthly = form.installmentCount > 0 && financed > 0 ? financed / form.installmentCount : 0;

  return (
    <>
      <PageHeader title="البيع بالتقسيط" description="خطط الأقساط مع الجدولة والتذكير بالمتأخر">
        <button onClick={() => setShow(true)} className="btn">
          <Plus size={16} /> خطة تقسيط جديدة
        </button>
      </PageHeader>

      {show && (
        <div className="card mb-4">
          <div className="flex items-center justify-between mb-3">
            <h3 className="font-semibold">خطة تقسيط جديدة</h3>
            <button onClick={() => setShow(false)} className="text-slate-400 hover:text-slate-700"><X size={20} /></button>
          </div>
          <div className="grid md:grid-cols-3 gap-3">
            <div>
              <label>العميل *</label>
              <select value={form.customerId} onChange={(e) => setForm({ ...form, customerId: e.target.value })}>
                <option value="">— اختر —</option>
                {customers.data?.map((c) => <option key={c.id} value={c.id}>{c.name}</option>)}
              </select>
            </div>
            <div>
              <label>إجمالي المبلغ *</label>
              <input type="number" step="0.01" value={form.totalAmount} onChange={(e) => setForm({ ...form, totalAmount: Number(e.target.value) })} />
            </div>
            <div>
              <label>الدفعة المقدمة</label>
              <input type="number" step="0.01" value={form.downPayment} onChange={(e) => setForm({ ...form, downPayment: Number(e.target.value) })} />
            </div>
            <div>
              <label>عدد الأقساط</label>
              <input type="number" min="1" max="60" value={form.installmentCount} onChange={(e) => setForm({ ...form, installmentCount: Number(e.target.value) })} />
            </div>
            <div>
              <label>الدورية</label>
              <select value={form.frequency} onChange={(e) => setForm({ ...form, frequency: Number(e.target.value) })}>
                {Object.entries(InstallmentFrequencyLabel).map(([k, v]) => <option key={k} value={k}>{v}</option>)}
              </select>
            </div>
            <div>
              <label>تاريخ البداية</label>
              <input type="date" value={form.startDate} onChange={(e) => setForm({ ...form, startDate: e.target.value })} />
            </div>
            <div className="md:col-span-3"><label>ملاحظات</label><input value={form.notes} onChange={(e) => setForm({ ...form, notes: e.target.value })} /></div>
          </div>
          {monthly > 0 && (
            <div className="bg-slate-50 dark:bg-slate-800 rounded-lg p-3 mt-3 text-sm grid grid-cols-2 md:grid-cols-3 gap-2">
              <div><span className="text-slate-500">المبلغ المُموَّل:</span> <span className="font-bold">{formatMoney(financed)}</span></div>
              <div><span className="text-slate-500">القسط الواحد:</span> <span className="font-bold">{formatMoney(monthly)}</span></div>
              <div><span className="text-slate-500">عدد الأقساط:</span> <span className="font-bold">{form.installmentCount}</span></div>
            </div>
          )}
          <div className="flex gap-2 mt-3">
            <button
              onClick={() => create.mutate()}
              disabled={!form.customerId || form.totalAmount <= 0 || form.downPayment >= form.totalAmount || create.isPending}
              className="btn-success"
            >
              {create.isPending ? "جاري..." : "إنشاء"}
            </button>
            <button onClick={() => setShow(false)} className="btn-outline">إلغاء</button>
          </div>
        </div>
      )}

      {payFor && (
        <div className="card mb-4 border-emerald-300">
          <div className="flex items-center justify-between mb-3">
            <h3 className="font-semibold text-emerald-700">سداد {payFor.label}</h3>
            <button onClick={() => setPayFor(null)} className="text-slate-400 hover:text-slate-700"><X size={20} /></button>
          </div>
          <div className="grid md:grid-cols-3 gap-3">
            <div><label>المبلغ المستحق</label><input value={formatMoney(payFor.defaultAmount)} disabled /></div>
            <div><label>المبلغ المُسدد</label><input type="number" step="0.01" value={payAmount} onChange={(e) => setPayAmount(Number(e.target.value))} /></div>
            <div>
              <label>وسيلة الدفع</label>
              <select value={payMethod} onChange={(e) => setPayMethod(Number(e.target.value))}>
                {Object.entries(PaymentMethodLabels).map(([k, v]) => <option key={k} value={k}>{v}</option>)}
              </select>
            </div>
            <div className="md:col-span-3"><label>مرجع (اختياري)</label><input value={payRef} onChange={(e) => setPayRef(e.target.value)} /></div>
          </div>
          <div className="flex gap-2 mt-3">
            <button onClick={() => pay.mutate()} disabled={payAmount <= 0 || pay.isPending} className="btn-success">
              {pay.isPending ? "جاري..." : "تسجيل السداد"}
            </button>
            <button onClick={() => setPayFor(null)} className="btn-outline">إلغاء</button>
          </div>
        </div>
      )}

      <div className="card mb-3 flex items-center gap-3">
        <label className="text-sm">الحالة:</label>
        <select className="!w-auto" value={statusFilter} onChange={(e) => setStatusFilter(e.target.value === "" ? "" : Number(e.target.value))}>
          <option value="">الكل</option>
          {Object.entries(InstallmentPlanStatusLabel).map(([k, v]) => <option key={k} value={k}>{v}</option>)}
        </select>
      </div>

      <div className="table-wrap">
        <table>
          <thead>
            <tr>
              <th></th>
              <th>الرقم</th>
              <th>العميل</th>
              <th>الإجمالي</th>
              <th>القسط</th>
              <th>مدفوع / متبقي</th>
              <th>القسط القادم</th>
              <th>الحالة</th>
              <th></th>
            </tr>
          </thead>
          <tbody>
            {plans.isLoading ? (
              Array.from({ length: 4 }).map((_, i) => <SkeletonRow key={i} cols={9} />)
            ) : plans.data?.length === 0 ? (
              <tr><td colSpan={9}>
                <EmptyState icon={CalendarDays} title="لا توجد خطط" description="ابدأ بإنشاء خطة تقسيط جديدة." actionLabel="خطة جديدة" onAction={() => setShow(true)} />
              </td></tr>
            ) : (
              plans.data?.map((p) => (
                <Fragment key={p.id}>
                  <tr>
                    <td>
                      <button onClick={() => setExpanded(expanded === p.id ? null : p.id)} className="text-slate-400 hover:text-slate-700">
                        {expanded === p.id ? <ChevronUp size={16} /> : <ChevronDown size={16} />}
                      </button>
                    </td>
                    <td className="font-mono text-xs">{p.planNumber}</td>
                    <td className="font-medium">{p.customerName}</td>
                    <td>{formatMoney(p.totalAmount)}</td>
                    <td>{formatMoney(p.installmentAmount)} × {p.installmentCount}</td>
                    <td>
                      <div className="text-emerald-700">{formatMoney(p.totalPaid)}</div>
                      <div className="text-xs text-slate-500">متبقي: {formatMoney(p.remaining)}</div>
                    </td>
                    <td>
                      {p.nextDueDate ? (
                        <>
                          <div>{formatDate(p.nextDueDate)}</div>
                          <div className="text-xs text-slate-500">{formatMoney(p.nextDueAmount ?? 0)}</div>
                        </>
                      ) : "—"}
                    </td>
                    <td>
                      <span className={`text-xs px-2 py-0.5 rounded-full ${
                        p.status === 0 ? p.overdueCount > 0 ? "bg-red-100 text-red-700" : "bg-amber-100 text-amber-800"
                        : p.status === 1 ? "bg-emerald-100 text-emerald-800"
                        : "bg-slate-200 text-slate-600"
                      }`}>
                        {InstallmentPlanStatusLabel[p.status]}
                        {p.overdueCount > 0 && p.status === 0 && ` (${p.overdueCount} متأخر)`}
                      </span>
                    </td>
                    <td className="flex gap-1">
                      {p.status === 0 && (
                        <button onClick={() => cancel.mutate(p.id)} className="btn-outline !px-2 !py-1 text-xs">إلغاء</button>
                      )}
                      {p.totalPaid === p.downPayment && (
                        <button onClick={async () => { if (await confirm({ title: "حذف الخطة؟", message: p.planNumber })) del.mutate(p.id); }} className="btn-outline !px-2 !py-1 text-xs text-red-600">
                          <Trash2 size={14} />
                        </button>
                      )}
                    </td>
                  </tr>
                  {expanded === p.id && (
                    <tr>
                      <td colSpan={9} className="bg-slate-50 dark:bg-slate-800">
                        <div className="p-3">
                          <div className="font-semibold mb-2 text-sm">جدول الأقساط</div>
                          <table className="w-full">
                            <thead>
                              <tr>
                                <th className="text-right">#</th>
                                <th>تاريخ الاستحقاق</th>
                                <th>المبلغ</th>
                                <th>المسدد</th>
                                <th>الحالة</th>
                                <th></th>
                              </tr>
                            </thead>
                            <tbody>
                              {p.installments.map((i) => (
                                <tr key={i.id}>
                                  <td>{i.sequence}</td>
                                  <td>{formatDate(i.dueDate)}</td>
                                  <td>{formatMoney(i.amount)}</td>
                                  <td className="text-emerald-700">{formatMoney(i.amountPaid)}</td>
                                  <td>
                                    <span className={`text-xs px-2 py-0.5 rounded-full ${
                                      i.status === 0 ? "bg-slate-100 text-slate-800"
                                      : i.status === 1 ? "bg-emerald-100 text-emerald-800"
                                      : i.status === 2 ? "bg-red-100 text-red-700"
                                      : "bg-slate-200 text-slate-600"
                                    }`}>
                                      {InstallmentStatusLabel[i.status]}
                                      {i.daysOverdue > 0 && ` (${i.daysOverdue} يوم)`}
                                    </span>
                                  </td>
                                  <td>
                                    {(i.status === 0 || i.status === 2) && (
                                      <button
                                        onClick={() => {
                                          setPayFor({ installmentId: i.id, defaultAmount: i.amount - i.amountPaid, label: `قسط ${i.sequence}` });
                                          setPayAmount(i.amount - i.amountPaid);
                                        }}
                                        className="btn-outline !px-2 !py-1 text-xs text-emerald-700"
                                      >
                                        <Check size={14} /> سداد
                                      </button>
                                    )}
                                  </td>
                                </tr>
                              ))}
                            </tbody>
                          </table>
                        </div>
                      </td>
                    </tr>
                  )}
                </Fragment>
              ))
            )}
          </tbody>
        </table>
      </div>
      {dialog}
    </>
  );
}
