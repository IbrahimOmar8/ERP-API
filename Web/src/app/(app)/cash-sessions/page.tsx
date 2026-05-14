"use client";

import { useState } from "react";
import { useMutation, useQuery, useQueryClient } from "@tanstack/react-query";
import toast from "react-hot-toast";
import { api, errorMessage } from "@/lib/api";
import { formatMoney, formatDateTime } from "@/lib/format";
import type { CashRegister, CashSession } from "@/types/api";
import PageHeader from "@/components/PageHeader";

export default function CashSessionsPage() {
  const qc = useQueryClient();
  const [registerId, setRegisterId] = useState("");
  const [openingBalance, setOpeningBalance] = useState(0);
  const [closingBalance, setClosingBalance] = useState(0);
  const [closingNotes, setClosingNotes] = useState("");

  const current = useQuery({
    queryKey: ["current-session"],
    queryFn: async () => (await api.get<CashSession | null>("/CashSessions/current")).data,
  });
  const registers = useQuery({
    queryKey: ["registers"],
    queryFn: async () => (await api.get<CashRegister[]>("/CashRegisters")).data,
  });
  const all = useQuery({
    queryKey: ["sessions"],
    queryFn: async () => (await api.get<CashSession[]>("/CashSessions")).data,
  });

  const open = useMutation({
    mutationFn: async () =>
      (await api.post("/CashSessions/open", { cashRegisterId: registerId, openingBalance })).data,
    onSuccess: () => {
      toast.success("تم فتح الجلسة");
      setOpeningBalance(0);
      qc.invalidateQueries();
    },
    onError: (e) => toast.error(errorMessage(e)),
  });

  const close = useMutation({
    mutationFn: async () =>
      (
        await api.post(`/CashSessions/${current.data!.id}/close`, {
          closingBalance,
          notes: closingNotes || null,
        })
      ).data,
    onSuccess: () => {
      toast.success("تم إغلاق الجلسة");
      setClosingBalance(0);
      setClosingNotes("");
      qc.invalidateQueries();
    },
    onError: (e) => toast.error(errorMessage(e)),
  });

  return (
    <>
      <PageHeader title="جلسات الكاش" />

      <div className="card mb-4">
        {!current.data ? (
          <>
            <h3 className="font-semibold mb-3">فتح جلسة جديدة</h3>
            <div className="grid md:grid-cols-3 gap-3">
              <div>
                <label>الكاشير</label>
                <select value={registerId} onChange={(e) => setRegisterId(e.target.value)}>
                  <option value="">اختر</option>
                  {registers.data?.map((r) => (
                    <option key={r.id} value={r.id}>{r.name} ({r.warehouseName})</option>
                  ))}
                </select>
              </div>
              <div>
                <label>الرصيد الافتتاحي</label>
                <input
                  type="number"
                  min={0}
                  step={0.01}
                  value={openingBalance}
                  onChange={(e) => setOpeningBalance(Number(e.target.value))}
                />
              </div>
              <div className="flex items-end">
                <button onClick={() => open.mutate()} disabled={!registerId || open.isPending} className="btn-success">
                  {open.isPending ? "جاري..." : "فتح الجلسة"}
                </button>
              </div>
            </div>
          </>
        ) : (
          <>
            <h3 className="font-semibold mb-3">الجلسة الحالية — {current.data.cashRegisterName}</h3>
            <div className="grid md:grid-cols-3 gap-3 text-sm">
              <Stat label="فُتحت في" value={formatDateTime(current.data.openedAt)} />
              <Stat label="الرصيد الافتتاحي" value={formatMoney(current.data.openingBalance)} />
              <Stat label="مبيعات نقدي" value={formatMoney(current.data.totalCashSales)} />
              <Stat label="مبيعات بطاقة" value={formatMoney(current.data.totalCardSales)} />
              <Stat label="مبيعات أخرى" value={formatMoney(current.data.totalOtherSales)} />
              <Stat label="الرصيد المتوقع" value={formatMoney(current.data.expectedBalance)} highlight />
            </div>
            <div className="grid md:grid-cols-3 gap-3 mt-4">
              <div>
                <label>الرصيد الفعلي</label>
                <input
                  type="number"
                  step={0.01}
                  value={closingBalance}
                  onChange={(e) => setClosingBalance(Number(e.target.value))}
                />
              </div>
              <div className="md:col-span-2">
                <label>ملاحظات</label>
                <input value={closingNotes} onChange={(e) => setClosingNotes(e.target.value)} />
              </div>
            </div>
            <button onClick={() => close.mutate()} disabled={close.isPending} className="btn-danger mt-3">
              {close.isPending ? "جاري..." : "إغلاق الجلسة"}
            </button>
          </>
        )}
      </div>

      <div className="card">
        <h3 className="font-semibold mb-3">كل الجلسات</h3>
        <div className="table-wrap">
          <table>
            <thead>
              <tr>
                <th>الكاشير</th>
                <th>فُتحت</th>
                <th>أُغلقت</th>
                <th>افتتاحي</th>
                <th>متوقع</th>
                <th>فعلي</th>
                <th>فرق</th>
                <th>الحالة</th>
              </tr>
            </thead>
            <tbody>
              {all.data?.map((s) => (
                <tr key={s.id} className={s.closedAt && s.difference !== 0 ? "bg-amber-50" : ""}>
                  <td>{s.cashRegisterName}</td>
                  <td>{formatDateTime(s.openedAt)}</td>
                  <td>{s.closedAt ? formatDateTime(s.closedAt) : "—"}</td>
                  <td>{formatMoney(s.openingBalance)}</td>
                  <td>{formatMoney(s.expectedBalance)}</td>
                  <td>{s.closedAt ? formatMoney(s.closingBalance) : "—"}</td>
                  <td className={s.difference < 0 ? "text-red-600" : s.difference > 0 ? "text-emerald-600" : ""}>
                    {s.closedAt ? formatMoney(s.difference) : "—"}
                  </td>
                  <td>{s.status === 1 ? "🟢 مفتوحة" : "⚫ مغلقة"}</td>
                </tr>
              ))}
            </tbody>
          </table>
        </div>
      </div>
    </>
  );
}

function Stat({ label, value, highlight = false }: { label: string; value: string; highlight?: boolean }) {
  return (
    <div className={`rounded-lg p-3 ${highlight ? "bg-brand text-white" : "bg-slate-50"}`}>
      <div className={`text-xs ${highlight ? "text-blue-100" : "text-slate-500"}`}>{label}</div>
      <div className="font-bold mt-1">{value}</div>
    </div>
  );
}
