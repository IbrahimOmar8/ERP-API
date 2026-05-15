"use client";

import { useState } from "react";
import { useMutation, useQueryClient } from "@tanstack/react-query";
import toast from "react-hot-toast";
import { Download, Upload, FileSpreadsheet, CheckCircle2, AlertTriangle } from "lucide-react";
import { api, errorMessage } from "@/lib/api";
import PageHeader from "@/components/PageHeader";

type Kind = "products" | "customers";

interface ImportResult {
  total: number;
  created: number;
  updated: number;
  skipped: number;
  errors: { row: number; field?: string; message: string }[];
  dryRun: boolean;
}

export default function ImportPage() {
  const apiUrl = process.env.NEXT_PUBLIC_API_URL || "http://localhost:5000/api";
  const [tab, setTab] = useState<Kind>("products");

  return (
    <>
      <PageHeader title="استيراد جماعي" description="رفع أصناف أو عملاء من ملف Excel/CSV" />

      <div className="flex gap-2 mb-4 border-b border-slate-200 dark:border-slate-700">
        <TabButton active={tab === "products"} onClick={() => setTab("products")}>الأصناف</TabButton>
        <TabButton active={tab === "customers"} onClick={() => setTab("customers")}>العملاء</TabButton>
      </div>

      <ImportPanel
        kind={tab}
        templateUrl={`${apiUrl}/Import/template/${tab}`}
      />
    </>
  );
}

function TabButton({ active, onClick, children }: { active: boolean; onClick: () => void; children: React.ReactNode }) {
  return (
    <button
      onClick={onClick}
      className={`px-4 py-2 text-sm font-medium border-b-2 -mb-px transition ${
        active ? "border-brand text-brand" : "border-transparent text-slate-500 hover:text-slate-700 dark:hover:text-slate-300"
      }`}
    >
      {children}
    </button>
  );
}

function ImportPanel({ kind, templateUrl }: { kind: Kind; templateUrl: string }) {
  const qc = useQueryClient();
  const [file, setFile] = useState<File | null>(null);
  const [result, setResult] = useState<ImportResult | null>(null);

  const send = useMutation({
    mutationFn: async ({ commit }: { commit: boolean }) => {
      if (!file) throw new Error("اختر ملفاً أولاً");
      const fd = new FormData();
      fd.append("file", file);
      fd.append("dryRun", commit ? "false" : "true");
      const { data } = await api.post<ImportResult>(`/Import/${kind}`, fd, {
        headers: { "Content-Type": "multipart/form-data" },
      });
      return data;
    },
    onSuccess: (data, vars) => {
      setResult(data);
      if (vars.commit) {
        toast.success("تم الاستيراد بنجاح");
        qc.invalidateQueries({ queryKey: kind === "products" ? ["products"] : ["customers"] });
      } else {
        toast.success("تمت معاينة الملف — راجع النتيجة قبل التأكيد");
      }
    },
    onError: (e) => toast.error(errorMessage(e)),
  });

  const headers = kind === "products"
    ? "sku, barcode, nameAr, nameEn, category, unit, purchasePrice, salePrice, vatRate, minStockLevel"
    : "name, phone, email, address, taxRegistrationNumber, nationalId, isCompany, creditLimit";

  return (
    <div className="space-y-4">
      <div className="card">
        <h3 className="font-semibold mb-2 flex items-center gap-2">
          <FileSpreadsheet size={18} /> تنسيق الملف
        </h3>
        <p className="text-sm text-slate-600 dark:text-slate-400 mb-3">
          الملف CSV بترميز UTF-8، السطر الأول هو العناوين. الأعمدة المتوقعة:
        </p>
        <div className="bg-slate-50 dark:bg-slate-800 rounded p-3 font-mono text-xs overflow-x-auto whitespace-nowrap">
          {headers}
        </div>
        <a href={templateUrl} className="btn-outline mt-3 inline-flex">
          <Download size={16} /> تنزيل قالب جاهز
        </a>
      </div>

      <div className="card">
        <h3 className="font-semibold mb-3">رفع الملف</h3>
        <input
          type="file"
          accept=".csv,text/csv"
          onChange={(e) => { setFile(e.target.files?.[0] ?? null); setResult(null); }}
          className="!p-2"
        />
        <div className="flex gap-2 mt-3 flex-wrap">
          <button
            onClick={() => send.mutate({ commit: false })}
            disabled={!file || send.isPending}
            className="btn-outline"
          >
            <Upload size={16} /> معاينة (Dry-run)
          </button>
          <button
            onClick={() => send.mutate({ commit: true })}
            disabled={!file || send.isPending}
            className="btn"
          >
            <CheckCircle2 size={16} /> تأكيد الاستيراد
          </button>
        </div>
        <p className="text-xs text-slate-500 mt-3">
          المعاينة لا تحفظ شيئاً في قاعدة البيانات — فقط ترجع تقريراً بما سيحدث.
          استخدمها أولاً للتأكد من خلو الملف من الأخطاء.
        </p>
      </div>

      {result && (
        <div className="card">
          <h3 className="font-semibold mb-3 flex items-center gap-2">
            {result.dryRun ? "نتيجة المعاينة" : "نتيجة الاستيراد"}
            {result.errors.length === 0 ? (
              <CheckCircle2 className="text-emerald-600" size={18} />
            ) : (
              <AlertTriangle className="text-amber-600" size={18} />
            )}
          </h3>
          <div className="grid grid-cols-2 md:grid-cols-4 gap-3 mb-4">
            <Stat label="إجمالي" value={result.total} color="bg-blue-50 text-blue-700" />
            <Stat label="جديدة" value={result.created} color="bg-emerald-50 text-emerald-700" />
            <Stat label="محدّثة" value={result.updated} color="bg-amber-50 text-amber-700" />
            <Stat label="متخطاة" value={result.skipped} color="bg-red-50 text-red-700" />
          </div>
          {result.errors.length > 0 && (
            <div>
              <h4 className="font-medium mb-2">الأخطاء ({result.errors.length})</h4>
              <div className="table-wrap">
                <table>
                  <thead>
                    <tr><th>السطر</th><th>الحقل</th><th>الرسالة</th></tr>
                  </thead>
                  <tbody>
                    {result.errors.map((e, i) => (
                      <tr key={i}>
                        <td className="font-mono text-xs">{e.row}</td>
                        <td className="font-mono text-xs">{e.field ?? "—"}</td>
                        <td className="text-red-700 dark:text-red-400">{e.message}</td>
                      </tr>
                    ))}
                  </tbody>
                </table>
              </div>
            </div>
          )}
        </div>
      )}
    </div>
  );
}

function Stat({ label, value, color }: { label: string; value: number; color: string }) {
  return (
    <div className={`rounded-lg p-3 ${color} dark:bg-opacity-20`}>
      <div className="text-xs">{label}</div>
      <div className="text-2xl font-bold">{value}</div>
    </div>
  );
}
