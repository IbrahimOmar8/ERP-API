"use client";

import { useState } from "react";
import { useParams } from "next/navigation";
import { useMutation, useQuery, useQueryClient } from "@tanstack/react-query";
import toast from "react-hot-toast";
import { Printer, FileText, Send, RefreshCw, Ban } from "lucide-react";
import { api, errorMessage } from "@/lib/api";
import { formatMoney, formatDateTime } from "@/lib/format";
import {
  EInvoiceStatusLabels,
  PaymentMethodLabels,
  SaleStatus,
  type EInvoiceSubmission,
  type Sale,
} from "@/types/api";
import PageHeader from "@/components/PageHeader";

export default function SaleDetailPage() {
  const params = useParams<{ id: string }>();
  const id = params.id;
  const apiUrl = process.env.NEXT_PUBLIC_API_URL || "http://localhost:5000/api";
  const qc = useQueryClient();
  const [cancelReason, setCancelReason] = useState("");
  const [showCancel, setShowCancel] = useState(false);

  const sale = useQuery({
    queryKey: ["sale", id],
    queryFn: async () => (await api.get<Sale>(`/Sales/${id}`)).data,
  });

  const submission = useQuery({
    queryKey: ["sale-eta", id],
    queryFn: async () =>
      (await api.get<EInvoiceSubmission | null>(`/einvoice/sales/${id}`)).data,
  });

  const submitEta = useMutation({
    mutationFn: async () => (await api.post(`/einvoice/sales/${id}/submit`, {})).data,
    onSuccess: () => {
      toast.success("تم الإرسال للمصلحة");
      qc.invalidateQueries({ queryKey: ["sale", id] });
      qc.invalidateQueries({ queryKey: ["sale-eta", id] });
    },
    onError: (e) => toast.error(errorMessage(e)),
  });

  const refreshEta = useMutation({
    mutationFn: async () => (await api.post(`/einvoice/sales/${id}/refresh`, {})).data,
    onSuccess: () => {
      toast.success("تم تحديث الحالة");
      qc.invalidateQueries({ queryKey: ["sale", id] });
      qc.invalidateQueries({ queryKey: ["sale-eta", id] });
    },
    onError: (e) => toast.error(errorMessage(e)),
  });

  const cancelEta = useMutation({
    mutationFn: async () =>
      (await api.post(`/einvoice/sales/${id}/cancel`, { reason: cancelReason })).data,
    onSuccess: () => {
      toast.success("تم إلغاء الفاتورة من المصلحة");
      setShowCancel(false);
      setCancelReason("");
      qc.invalidateQueries({ queryKey: ["sale", id] });
      qc.invalidateQueries({ queryKey: ["sale-eta", id] });
    },
    onError: (e) => toast.error(errorMessage(e)),
  });

  if (sale.isLoading || !sale.data) return <p>جاري التحميل...</p>;
  const s = sale.data;

  return (
    <>
      <PageHeader title={`فاتورة ${s.invoiceNumber}`} description={SaleStatus[s.status]}>
        <a href={`${apiUrl}/sales/${s.id}/print?format=a4`} target="_blank" rel="noreferrer" className="btn">
          <Printer size={16} /> طباعة A4
        </a>
        <a
          href={`${apiUrl}/sales/${s.id}/print?format=thermal`}
          target="_blank"
          rel="noreferrer"
          className="btn-outline"
        >
          <FileText size={16} /> طباعة حرارية
        </a>
      </PageHeader>

      <div className="grid md:grid-cols-2 gap-4">
        <div className="card">
          <h3 className="font-semibold mb-3">التفاصيل</h3>
          <table className="w-full text-sm">
            <tbody className="divide-y divide-slate-100">
              <Row k="التاريخ" v={formatDateTime(s.saleDate)} />
              <Row k="العميل" v={s.customerName || "عميل نقدي"} />
              <Row k="المخزن" v={s.warehouseName || ""} />
              <Row k="المجموع" v={formatMoney(s.subTotal)} />
              <Row k="الخصم" v={formatMoney(s.discountAmount)} />
              <Row k="الضريبة" v={formatMoney(s.vatAmount)} />
              <Row k="الإجمالي" v={formatMoney(s.total)} bold />
              <Row k="المدفوع" v={formatMoney(s.paidAmount)} />
              <Row k="الباقي" v={formatMoney(s.changeAmount)} />
            </tbody>
          </table>
        </div>

        <div className="card">
          <h3 className="font-semibold mb-3">الفاتورة الإلكترونية (ETA)</h3>
          {!s.eInvoiceUuid ? (
            <>
              <p className="text-slate-600 mb-3">لم تُرسل بعد.</p>
              <button
                onClick={() => submitEta.mutate()}
                disabled={submitEta.isPending}
                className="btn-success"
              >
                <Send size={16} /> إرسال للمصلحة
              </button>
            </>
          ) : (
            <>
              <table className="w-full text-sm mb-3">
                <tbody className="divide-y divide-slate-100">
                  <Row k="UUID" v={<code className="text-xs break-all">{s.eInvoiceUuid}</code>} />
                  {submission.data?.longId && (
                    <Row k="LongId" v={<code className="text-xs">{submission.data.longId}</code>} />
                  )}
                  <Row
                    k="الحالة"
                    v={EInvoiceStatusLabels[s.eInvoiceStatus ?? 0] ?? "—"}
                  />
                  {submission.data?.errorMessage && (
                    <Row k="رسالة الخطأ" v={<span className="text-red-600">{submission.data.errorMessage}</span>} />
                  )}
                </tbody>
              </table>
              <div className="flex gap-2 flex-wrap">
                <button
                  onClick={() => refreshEta.mutate()}
                  disabled={refreshEta.isPending}
                  className="btn-outline"
                >
                  <RefreshCw size={16} /> تحديث الحالة
                </button>
                <button onClick={() => setShowCancel(!showCancel)} className="btn-danger">
                  <Ban size={16} /> إلغاء من المصلحة
                </button>
              </div>
              {showCancel && (
                <div className="mt-3 space-y-2">
                  <label>سبب الإلغاء</label>
                  <input
                    value={cancelReason}
                    onChange={(e) => setCancelReason(e.target.value)}
                    placeholder="اكتب السبب"
                  />
                  <div className="flex gap-2">
                    <button
                      onClick={() => cancelEta.mutate()}
                      disabled={!cancelReason.trim() || cancelEta.isPending}
                      className="btn-danger"
                    >
                      تأكيد الإلغاء
                    </button>
                    <button onClick={() => setShowCancel(false)} className="btn-outline">
                      تراجع
                    </button>
                  </div>
                </div>
              )}
            </>
          )}
        </div>
      </div>

      <div className="card mt-4">
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
              {s.items.map((i) => (
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

      {s.payments.length > 0 && (
        <div className="card mt-4">
          <h3 className="font-semibold mb-3">الدفعات</h3>
          <div className="table-wrap">
            <table>
              <thead>
                <tr>
                  <th>الطريقة</th>
                  <th>المبلغ</th>
                  <th>المرجع</th>
                  <th>التاريخ</th>
                </tr>
              </thead>
              <tbody>
                {s.payments.map((p) => (
                  <tr key={p.id}>
                    <td>{PaymentMethodLabels[p.method] ?? p.method}</td>
                    <td>{formatMoney(p.amount)}</td>
                    <td>{p.reference || "—"}</td>
                    <td>{formatDateTime(p.paidAt)}</td>
                  </tr>
                ))}
              </tbody>
            </table>
          </div>
        </div>
      )}
    </>
  );
}

function Row({ k, v, bold = false }: { k: string; v: React.ReactNode; bold?: boolean }) {
  return (
    <tr>
      <th className="text-start py-2 font-medium text-slate-600 w-1/3">{k}</th>
      <td className={`py-2 ${bold ? "font-bold text-lg" : ""}`}>{v}</td>
    </tr>
  );
}
