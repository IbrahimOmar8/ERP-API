"use client";

import { useEffect, useState } from "react";
import { useMutation, useQuery, useQueryClient } from "@tanstack/react-query";
import toast from "react-hot-toast";
import { Save, ShieldCheck } from "lucide-react";
import { api, errorMessage } from "@/lib/api";
import type { CompanyProfile } from "@/types/api";
import PageHeader from "@/components/PageHeader";

interface Form {
  nameAr: string;
  nameEn: string;
  taxRegistrationNumber: string;
  commercialRegister: string;
  activityCode: string;
  address: string;
  governorate: string;
  city: string;
  phone: string;
  email: string;
  etaClientId: string;
  etaClientSecret: string;
  etaIssuerId: string;
  etaEnabled: boolean;
}

const empty: Form = {
  nameAr: "",
  nameEn: "",
  taxRegistrationNumber: "",
  commercialRegister: "",
  activityCode: "",
  address: "",
  governorate: "",
  city: "",
  phone: "",
  email: "",
  etaClientId: "",
  etaClientSecret: "",
  etaIssuerId: "",
  etaEnabled: false,
};

export default function SettingsPage() {
  const qc = useQueryClient();
  const [form, setForm] = useState<Form>(empty);
  const [hasSecret, setHasSecret] = useState(false);

  const { data, isLoading } = useQuery({
    queryKey: ["company-profile"],
    queryFn: async () => (await api.get<CompanyProfile | null>("/company-profile")).data,
  });

  useEffect(() => {
    if (!data) return;
    setForm({
      nameAr: data.nameAr,
      nameEn: data.nameEn ?? "",
      taxRegistrationNumber: data.taxRegistrationNumber,
      commercialRegister: data.commercialRegister ?? "",
      activityCode: data.activityCode ?? "",
      address: data.address,
      governorate: data.governorate ?? "",
      city: data.city ?? "",
      phone: data.phone ?? "",
      email: data.email ?? "",
      etaClientId: data.etaClientId ?? "",
      etaClientSecret: "",
      etaIssuerId: data.etaIssuerId ?? "",
      etaEnabled: data.etaEnabled,
    });
    setHasSecret(data.hasEtaSecret);
  }, [data]);

  const save = useMutation({
    mutationFn: async () => {
      const payload = { ...form, etaClientSecret: form.etaClientSecret || null };
      return (await api.put<CompanyProfile>("/company-profile", payload)).data;
    },
    onSuccess: (result) => {
      toast.success("تم حفظ الإعدادات");
      setHasSecret(result.hasEtaSecret);
      setForm((f) => ({ ...f, etaClientSecret: "" }));
      qc.invalidateQueries({ queryKey: ["company-profile"] });
    },
    onError: (e) => toast.error(errorMessage(e)),
  });

  type StringKeys = {
    [K in keyof Form]: Form[K] extends string ? K : never;
  }[keyof Form];

  function bind(k: StringKeys) {
    return {
      value: form[k] as string,
      onChange: (e: React.ChangeEvent<HTMLInputElement>) =>
        setForm((f) => ({ ...f, [k]: e.target.value })),
    };
  }

  if (isLoading) return <p>جاري التحميل...</p>;

  return (
    <>
      <PageHeader title="إعدادات الشركة" description="بيانات الشركة وتكامل الفاتورة الإلكترونية" />

      <div className="card mb-4">
        <h3 className="font-semibold mb-3">بيانات الشركة</h3>
        <div className="grid md:grid-cols-3 gap-3">
          <div><label>الاسم بالعربية *</label><input {...bind("nameAr")} /></div>
          <div><label>الاسم بالإنجليزية</label><input {...bind("nameEn")} /></div>
          <div><label>الرقم الضريبي *</label><input {...bind("taxRegistrationNumber")} /></div>
          <div><label>السجل التجاري</label><input {...bind("commercialRegister")} /></div>
          <div><label>كود النشاط</label><input {...bind("activityCode")} /></div>
          <div><label>الهاتف</label><input {...bind("phone")} /></div>
          <div className="md:col-span-2"><label>العنوان *</label><input {...bind("address")} /></div>
          <div><label>البريد الإلكتروني</label><input {...bind("email")} /></div>
          <div><label>المحافظة</label><input {...bind("governorate")} /></div>
          <div><label>المدينة</label><input {...bind("city")} /></div>
        </div>
      </div>

      <div className="card mb-4">
        <div className="flex items-center gap-2 mb-3">
          <ShieldCheck className="text-brand" />
          <h3 className="font-semibold">تكامل ETA (الفاتورة الإلكترونية)</h3>
        </div>
        <label className="flex items-center gap-2 mb-3">
          <input
            type="checkbox"
            checked={form.etaEnabled}
            onChange={(e) => setForm({ ...form, etaEnabled: e.target.checked })}
            className="!w-auto"
          />
          <span>تفعيل الإرسال للمصلحة</span>
        </label>
        <div className="grid md:grid-cols-3 gap-3">
          <div><label>Issuer ID</label><input {...bind("etaIssuerId")} /></div>
          <div><label>Client ID</label><input {...bind("etaClientId")} /></div>
          <div>
            <label>Client Secret</label>
            <input
              type="password"
              value={form.etaClientSecret}
              onChange={(e) => setForm({ ...form, etaClientSecret: e.target.value })}
              placeholder={hasSecret ? "محفوظ — اتركه فارغ للحفاظ على الحالي" : ""}
            />
            {hasSecret && (
              <p className="text-xs text-slate-500 mt-1">السر محفوظ. لتغييره أدخل قيمة جديدة فقط.</p>
            )}
          </div>
        </div>
      </div>

      <button
        onClick={() => save.mutate()}
        disabled={save.isPending}
        className="btn-success !px-6 !py-3 text-base"
      >
        <Save size={18} />
        {save.isPending ? "جاري الحفظ..." : "حفظ الإعدادات"}
      </button>
    </>
  );
}
