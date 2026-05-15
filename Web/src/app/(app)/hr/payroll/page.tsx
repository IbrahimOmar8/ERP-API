"use client";

import { useState } from "react";
import { useMutation, useQuery, useQueryClient } from "@tanstack/react-query";
import toast from "react-hot-toast";
import { Check, Download, Play, Trash2, Wallet } from "lucide-react";
import { api, errorMessage } from "@/lib/api";
import { formatMoney } from "@/lib/format";
import { downloadCsv } from "@/lib/csv";
import type { Payroll } from "@/types/api";
import { PayrollStatusLabel } from "@/types/api";
import PageHeader from "@/components/PageHeader";
import EmptyState from "@/components/EmptyState";
import { SkeletonRow } from "@/components/Skeleton";
import { useConfirm } from "@/components/ConfirmDialog";

export default function PayrollPage() {
  const qc = useQueryClient();
  const { confirm, dialog } = useConfirm();
  const now = new Date();
  const [year, setYear] = useState(now.getFullYear());
  const [month, setMonth] = useState(now.getMonth() + 1);
  const [genOpts, setGenOpts] = useState({ bonus: 0, tax: 0, insuranceContribution: 0, overwrite: false });

  const { data, isLoading } = useQuery({
    queryKey: ["hr-payroll", year, month],
    queryFn: async () =>
      (await api.get<Payroll[]>("/hr/payroll", { params: { year, month } })).data,
  });

  const generate = useMutation({
    mutationFn: async () =>
      (await api.post("/hr/payroll/generate", {
        year, month,
        bonus: genOpts.bonus,
        tax: genOpts.tax,
        insuranceContribution: genOpts.insuranceContribution,
        overwrite: genOpts.overwrite,
      })).data,
    onSuccess: () => {
      toast.success("تم احتساب الرواتب");
      qc.invalidateQueries({ queryKey: ["hr-payroll"] });
    },
    onError: (e) => toast.error(errorMessage(e)),
  });

  const setStatus = useMutation({
    mutationFn: async ({ id, action }: { id: string; action: "approve" | "pay" | "cancel" }) =>
      (await api.post(`/hr/payroll/${id}/${action}`)).data,
    onSuccess: () => { toast.success("تم"); qc.invalidateQueries({ queryKey: ["hr-payroll"] }); },
    onError: (e) => toast.error(errorMessage(e)),
  });

  const del = useMutation({
    mutationFn: async (id: string) => (await api.delete(`/hr/payroll/${id}`)).data,
    onSuccess: () => { toast.success("تم الحذف"); qc.invalidateQueries({ queryKey: ["hr-payroll"] }); },
    onError: (e) => toast.error(errorMessage(e)),
  });

  const totalNet = (data ?? []).reduce((s, p) => s + p.netPay, 0);

  return (
    <>
      <PageHeader title="الرواتب" description="احتساب الرواتب الشهرية والاعتماد">
        <button
          onClick={() => data && downloadCsv(`payroll-${year}-${month}`, data, [
            { header: "الموظف", accessor: (p) => p.employeeName ?? "" },
            { header: "الراتب الأساسي", accessor: (p) => p.baseSalary.toFixed(2) },
            { header: "البدلات", accessor: (p) => p.allowances.toFixed(2) },
            { header: "خصم", accessor: (p) => p.deductions.toFixed(2) },
            { header: "إضافي", accessor: (p) => p.overtimePay.toFixed(2) },
            { header: "غرامة تأخير", accessor: (p) => p.latePenalty.toFixed(2) },
            { header: "بدون أجر", accessor: (p) => p.unpaidLeavePenalty.toFixed(2) },
            { header: "مكافأة", accessor: (p) => p.bonus.toFixed(2) },
            { header: "ضريبة", accessor: (p) => p.tax.toFixed(2) },
            { header: "تأمين", accessor: (p) => p.insuranceContribution.toFixed(2) },
            { header: "إجمالي", accessor: (p) => p.grossPay.toFixed(2) },
            { header: "صافي", accessor: (p) => p.netPay.toFixed(2) },
            { header: "الحالة", accessor: (p) => PayrollStatusLabel[p.status] },
          ])}
          disabled={!data || data.length === 0}
          className="btn-outline"
        >
          <Download size={16} /> تصدير CSV
        </button>
      </PageHeader>

      <div className="card mb-4">
        <h3 className="font-semibold mb-3">احتساب رواتب شهرية</h3>
        <div className="grid md:grid-cols-5 gap-3">
          <div>
            <label>السنة</label>
            <input type="number" value={year} onChange={(e) => setYear(Number(e.target.value))} />
          </div>
          <div>
            <label>الشهر</label>
            <select value={month} onChange={(e) => setMonth(Number(e.target.value))}>
              {Array.from({ length: 12 }, (_, i) => i + 1).map((m) => <option key={m} value={m}>{m}</option>)}
            </select>
          </div>
          <div><label>مكافأة موحدة</label><input type="number" value={genOpts.bonus} onChange={(e) => setGenOpts({ ...genOpts, bonus: Number(e.target.value) })} /></div>
          <div><label>ضريبة موحدة</label><input type="number" value={genOpts.tax} onChange={(e) => setGenOpts({ ...genOpts, tax: Number(e.target.value) })} /></div>
          <div><label>تأمين موحد</label><input type="number" value={genOpts.insuranceContribution} onChange={(e) => setGenOpts({ ...genOpts, insuranceContribution: Number(e.target.value) })} /></div>
        </div>
        <div className="flex items-center justify-between mt-3">
          <label className="flex items-center gap-2 text-sm">
            <input type="checkbox" checked={genOpts.overwrite} onChange={(e) => setGenOpts({ ...genOpts, overwrite: e.target.checked })} className="!w-auto" />
            <span>استبدال المسودات الموجودة</span>
          </label>
          <button onClick={() => generate.mutate()} disabled={generate.isPending} className="btn">
            <Play size={16} /> {generate.isPending ? "جاري..." : "احتساب"}
          </button>
        </div>
      </div>

      {data && data.length > 0 && (
        <div className="card mb-3 flex items-center justify-between">
          <div className="text-sm text-slate-500">إجمالي الصافي للفترة</div>
          <div className="text-lg font-bold">{formatMoney(totalNet)}</div>
        </div>
      )}

      <div className="table-wrap">
        <table>
          <thead>
            <tr>
              <th>الموظف</th>
              <th>أساسي</th>
              <th>بدلات</th>
              <th>إضافي</th>
              <th>خصم</th>
              <th>صافي</th>
              <th>الحالة</th>
              <th></th>
            </tr>
          </thead>
          <tbody>
            {isLoading ? (
              Array.from({ length: 5 }).map((_, i) => <SkeletonRow key={i} cols={8} />)
            ) : data?.length === 0 ? (
              <tr><td colSpan={8}>
                <EmptyState icon={Wallet} title="لا توجد كشوف رواتب" description="اضغط 'احتساب' لإنشاء كشوف الشهر." />
              </td></tr>
            ) : (
              data?.map((p) => {
                const totalDeductions = p.deductions + p.latePenalty + p.unpaidLeavePenalty + p.tax + p.insuranceContribution;
                return (
                  <tr key={p.id}>
                    <td className="font-medium">{p.employeeName}</td>
                    <td>{formatMoney(p.baseSalary)}</td>
                    <td>{formatMoney(p.allowances)}</td>
                    <td className="text-emerald-700">{p.overtimePay > 0 ? formatMoney(p.overtimePay) : "—"}</td>
                    <td className="text-red-700">{formatMoney(totalDeductions)}</td>
                    <td className="font-bold">{formatMoney(p.netPay)}</td>
                    <td>
                      <span className={`text-xs px-2 py-0.5 rounded-full ${
                        p.status === 0 ? "bg-slate-100 text-slate-800"
                        : p.status === 1 ? "bg-blue-100 text-blue-800"
                        : p.status === 2 ? "bg-emerald-100 text-emerald-800"
                        : "bg-red-100 text-red-700"
                      }`}>{PayrollStatusLabel[p.status]}</span>
                    </td>
                    <td className="flex gap-1">
                      {p.status === 0 && (
                        <button onClick={() => setStatus.mutate({ id: p.id, action: "approve" })} className="btn-outline !px-2 !py-1 text-xs text-blue-700"><Check size={14} /> اعتماد</button>
                      )}
                      {p.status === 1 && (
                        <button onClick={() => setStatus.mutate({ id: p.id, action: "pay" })} className="btn-outline !px-2 !py-1 text-xs text-emerald-700"><Check size={14} /> صرف</button>
                      )}
                      {p.status !== 3 && p.status !== 2 && (
                        <button onClick={() => setStatus.mutate({ id: p.id, action: "cancel" })} className="btn-outline !px-2 !py-1 text-xs">إلغاء</button>
                      )}
                      {p.status !== 2 && (
                        <button onClick={async () => { if (await confirm({ title: "حذف؟", message: p.employeeName ?? "" })) del.mutate(p.id); }} className="btn-outline !px-2 !py-1 text-xs text-red-600"><Trash2 size={14} /></button>
                      )}
                    </td>
                  </tr>
                );
              })
            )}
          </tbody>
        </table>
      </div>
      {dialog}
    </>
  );
}
