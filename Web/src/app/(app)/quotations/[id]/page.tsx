"use client";

import { useState } from "react";
import { useParams, useRouter } from "next/navigation";
import { useMutation, useQuery, useQueryClient } from "@tanstack/react-query";
import toast from "react-hot-toast";
import { CheckCircle, FileText, Send, Trash2, X, ArrowRight } from "lucide-react";
import { api, errorMessage } from "@/lib/api";
import { formatDate, formatMoney } from "@/lib/format";
import {
  QuotationStatusColors,
  QuotationStatusLabels,
  type CashSession,
  type Quotation,
} from "@/types/api";
import PageHeader from "@/components/PageHeader";
import Modal from "@/components/Modal";
import { useConfirm } from "@/components/ConfirmDialog";

export default function QuotationDetailPage() {
  const params = useParams<{ id: string }>();
  const router = useRouter();
  const qc = useQueryClient();
  const { confirm, dialog } = useConfirm();
  const [showConvert, setShowConvert] = useState(false);
  const [paymentMethod, setPaymentMethod] = useState(1);

  const q = useQuery({
    queryKey: ["quotation", params.id],
    queryFn: async () => (await api.get<Quotation>(`/Quotations/${params.id}`)).data,
  });

  const session = useQuery({
    enabled: showConvert,
    queryKey: ["current-session"],
    queryFn: async () => (await api.get<CashSession | null>("/CashSessions/current")).data,
  });

  const setStatus = useMutation({
    mutationFn: async (status: number) =>
      (await api.post(`/Quotations/${params.id}/status`, { status })).data,
    onSuccess: () => {
      toast.success("تم تحديث الحالة");
      qc.invalidateQueries({ queryKey: ["quotation", params.id] });
    },
    onError: (e) => toast.error(errorMessage(e)),
  });

  const del = useMutation({
    mutationFn: async () => api.delete(`/Quotations/${params.id}`),
    onSuccess: () => {
      toast.success("تم الحذف");
      router.push("/quotations");
    },
    onError: (e) => toast.error(errorMessage(e)),
  });

  const convert = useMutation({
    mutationFn: async () => {
      if (!session.data) throw new Error("افتح جلسة كاش أولاً");
      const total = q.data?.total ?? 0;
      return (await api.post<{ saleId: string }>(`/Quotations/${params.id}/convert`, {
        cashSessionId: session.data.id,
        payments: [{ method: paymentMethod, amount: total }],
      })).data;
    },
    onSuccess: (d) => {
      toast.success("تم التحويل لفاتورة");
      router.push(`/sales/${d.saleId}`);
    },
    onError: (e) => toast.error(errorMessage(e)),
  });

  if (q.isLoading || !q.data) return <p>جاري التحميل...</p>;
  const data = q.data;
  const isConverted = data.status === 5;
  const apiUrl = process.env.NEXT_PUBLIC_API_URL || "http://localhost:5000/api";

  return (
    <>
      <PageHeader title={`عرض سعر ${data.quotationNumber}`}>
        <span className={`text-xs px-3 py-1 rounded-full ${QuotationStatusColors[data.status]}`}>
          {QuotationStatusLabels[data.status]}
        </span>
        {!isConverted && (
          <>
            {data.status === 0 && (
              <button onClick={() => setStatus.mutate(1)} className="btn-outline">
                <Send size={16} /> وضع: مُرسل
              </button>
            )}
            {data.status === 1 && (
              <>
                <button onClick={() => setStatus.mutate(2)} className="btn-success">
                  <CheckCircle size={16} /> قبول
                </button>
                <button onClick={() => setStatus.mutate(3)} className="btn-danger">
                  <X size={16} /> رفض
                </button>
              </>
            )}
            {data.status === 2 && (
              <button onClick={() => setShowConvert(true)} className="btn">
                <ArrowRight size={16} /> تحويل لفاتورة
              </button>
            )}
            <button
              onClick={async () => {
                if (await confirm("حذف", `حذف عرض السعر ${data.quotationNumber}؟`)) del.mutate();
              }}
              className="btn-danger"
            >
              <Trash2 size={16} /> حذف
            </button>
          </>
        )}
        {isConverted && data.convertedSaleId && (
          <a href={`/sales/${data.convertedSaleId}`} className="btn-outline">
            <FileText size={16} /> الفاتورة المحوّلة
          </a>
        )}
      </PageHeader>

      <div className="grid md:grid-cols-2 gap-4 mb-4">
        <div className="card">
          <h3 className="font-semibold mb-3">العميل</h3>
          <table className="w-full text-sm">
            <tbody className="divide-y divide-slate-100 dark:divide-slate-800">
              <tr><th className="text-start py-2 w-1/3">الاسم</th><td>{data.customerName || data.customerNameSnapshot || "—"}</td></tr>
              <tr><th className="text-start py-2">الهاتف</th><td>{data.customerPhoneSnapshot || "—"}</td></tr>
              <tr><th className="text-start py-2">تاريخ الإصدار</th><td>{formatDate(data.issueDate)}</td></tr>
              <tr><th className="text-start py-2">سارٍ حتى</th><td>{data.validUntil ? formatDate(data.validUntil) : "—"}</td></tr>
            </tbody>
          </table>
        </div>
        <div className="card">
          <h3 className="font-semibold mb-3">الإجمالي</h3>
          <table className="w-full text-sm">
            <tbody className="divide-y divide-slate-100 dark:divide-slate-800">
              <tr><th className="text-start py-2 w-1/2">المجموع</th><td>{formatMoney(data.subTotal)}</td></tr>
              <tr><th className="text-start py-2">الخصم</th><td className="text-red-600">{formatMoney(data.discountAmount)}</td></tr>
              <tr><th className="text-start py-2">الضريبة</th><td>{formatMoney(data.vatAmount)}</td></tr>
              <tr><th className="text-start py-2 font-bold">الإجمالي</th><td className="font-bold text-lg">{formatMoney(data.total)}</td></tr>
            </tbody>
          </table>
        </div>
      </div>

      <div className="card mb-4">
        <h3 className="font-semibold mb-3">الأصناف</h3>
        <div className="table-wrap">
          <table>
            <thead>
              <tr>
                <th>الصنف</th>
                <th>الكمية</th>
                <th>السعر</th>
                <th>الخصم</th>
                <th>الضريبة</th>
                <th>الإجمالي</th>
              </tr>
            </thead>
            <tbody>
              {data.items.map((i) => (
                <tr key={i.id}>
                  <td>{i.productNameSnapshot}</td>
                  <td>{i.quantity}</td>
                  <td>{formatMoney(i.unitPrice)}</td>
                  <td>{formatMoney(i.discountAmount)}</td>
                  <td>{formatMoney(i.vatAmount)}</td>
                  <td className="font-semibold">{formatMoney(i.lineTotal)}</td>
                </tr>
              ))}
            </tbody>
          </table>
        </div>
      </div>

      {(data.notes || data.terms) && (
        <div className="card">
          {data.notes && <div className="mb-2"><b>ملاحظات:</b> {data.notes}</div>}
          {data.terms && <div className="text-sm text-slate-600 dark:text-slate-400 whitespace-pre-wrap"><b>الشروط:</b> {data.terms}</div>}
        </div>
      )}

      <Modal open={showConvert} title="تحويل لفاتورة بيع" onClose={() => setShowConvert(false)}>
        {!session.data ? (
          <div className="text-amber-700 bg-amber-50 dark:bg-amber-950/30 rounded p-3">
            لا توجد جلسة كاش مفتوحة. افتح جلسة من شاشة جلسات الكاش أولاً.
          </div>
        ) : (
          <div className="space-y-3">
            <p className="text-sm">سيتم إنشاء فاتورة بقيمة <b>{formatMoney(data.total)}</b> ووسم عرض السعر كـ "مُحوّل".</p>
            <div>
              <label>طريقة الدفع</label>
              <select value={paymentMethod} onChange={(e) => setPaymentMethod(Number(e.target.value))}>
                <option value={1}>نقدي</option>
                <option value={2}>بطاقة</option>
                <option value={3}>إنستا باي</option>
                <option value={4}>محفظة</option>
                <option value={6}>آجل</option>
              </select>
            </div>
          </div>
        )}
        <div className="flex justify-end gap-2 mt-4">
          <button onClick={() => setShowConvert(false)} className="btn-outline">إلغاء</button>
          <button
            onClick={() => convert.mutate()}
            disabled={!session.data || convert.isPending}
            className="btn-success"
          >
            {convert.isPending ? "..." : "تأكيد التحويل"}
          </button>
        </div>
      </Modal>
      {dialog}
    </>
  );
}
