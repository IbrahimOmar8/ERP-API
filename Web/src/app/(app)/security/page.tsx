"use client";

import { useState } from "react";
import { useMutation } from "@tanstack/react-query";
import toast from "react-hot-toast";
import { Database, Download, ShieldCheck, ShieldOff } from "lucide-react";
import { api, errorMessage } from "@/lib/api";
import { useAuth } from "@/lib/auth";
import PageHeader from "@/components/PageHeader";

interface InitResponse { secret: string; otpAuthUri: string; }

export default function SecurityPage() {
  const apiUrl = process.env.NEXT_PUBLIC_API_URL || "http://localhost:5000/api";
  const user = useAuth((s) => s.user);
  const isAdmin = (user?.roles ?? []).includes("Admin");

  return (
    <>
      <PageHeader title="الأمن" description="المصادقة الثنائية + النسخ الاحتياطي" />
      <div className="space-y-4">
        <TwoFactorPanel />
        <ChangePasswordPanel />
        {isAdmin && <BackupPanel apiUrl={apiUrl} />}
      </div>
    </>
  );
}

function TwoFactorPanel() {
  const [init, setInit] = useState<InitResponse | null>(null);
  const [code, setCode] = useState("");
  const [disablePassword, setDisablePassword] = useState("");
  const [showDisable, setShowDisable] = useState(false);

  const initFa = useMutation({
    mutationFn: async () => (await api.post<InitResponse>("/Auth/2fa/init", {})).data,
    onSuccess: (d) => setInit(d),
    onError: (e) => toast.error(errorMessage(e)),
  });

  const enableFa = useMutation({
    mutationFn: async () => (await api.post("/Auth/2fa/enable", { code })).data,
    onSuccess: () => {
      toast.success("تم تفعيل المصادقة الثنائية");
      setInit(null);
      setCode("");
    },
    onError: (e) => toast.error(errorMessage(e)),
  });

  const disableFa = useMutation({
    mutationFn: async () => (await api.post("/Auth/2fa/disable", { password: disablePassword })).data,
    onSuccess: () => {
      toast.success("تم إلغاء التحقق الثنائي");
      setShowDisable(false);
      setDisablePassword("");
    },
    onError: (e) => toast.error(errorMessage(e)),
  });

  const qrUrl = init
    ? `https://api.qrserver.com/v1/create-qr-code/?size=220x220&data=${encodeURIComponent(init.otpAuthUri)}`
    : null;

  return (
    <div className="card">
      <h3 className="font-semibold mb-3 flex items-center gap-2">
        <ShieldCheck className="text-brand" /> التحقق الثنائي (2FA)
      </h3>

      {!init && !showDisable && (
        <>
          <p className="text-sm text-slate-600 dark:text-slate-400 mb-3">
            استخدم تطبيق مصادقة (Google Authenticator، Authy، Microsoft Authenticator)
            لإضافة طبقة أمان إضافية عند تسجيل الدخول.
          </p>
          <div className="flex gap-2 flex-wrap">
            <button onClick={() => initFa.mutate()} disabled={initFa.isPending} className="btn-success">
              <ShieldCheck size={16} /> {initFa.isPending ? "..." : "تفعيل المصادقة الثنائية"}
            </button>
            <button onClick={() => setShowDisable(true)} className="btn-outline">
              <ShieldOff size={16} /> إيقاف
            </button>
          </div>
        </>
      )}

      {init && (
        <div className="space-y-3">
          <p className="text-sm">
            امسح هذا الـ QR من تطبيق المصادقة، ثم أدخل الرمز ذو الـ 6 أرقام لتأكيد التفعيل.
          </p>
          <div className="flex flex-col md:flex-row gap-4 items-center">
            <img src={qrUrl!} alt="QR" className="bg-white p-2 rounded-lg" />
            <div className="flex-1">
              <label>الرمز السري (يدوي)</label>
              <input value={init.secret} readOnly className="font-mono text-sm" />
              <p className="text-xs text-slate-500 mt-1">احفظه في مكان آمن كنسخة احتياطية.</p>
              <div className="mt-3">
                <label>رمز التحقق</label>
                <input
                  inputMode="numeric"
                  maxLength={6}
                  pattern="[0-9]{6}"
                  value={code}
                  onChange={(e) => setCode(e.target.value.replace(/\D/g, ""))}
                  className="font-mono text-center text-lg tracking-widest"
                />
              </div>
            </div>
          </div>
          <div className="flex gap-2">
            <button
              onClick={() => enableFa.mutate()}
              disabled={code.length !== 6 || enableFa.isPending}
              className="btn-success"
            >
              {enableFa.isPending ? "..." : "تأكيد التفعيل"}
            </button>
            <button onClick={() => setInit(null)} className="btn-outline">إلغاء</button>
          </div>
        </div>
      )}

      {showDisable && (
        <div className="space-y-3 mt-3 border border-red-200 bg-red-50 dark:bg-red-950/30 rounded-lg p-3">
          <p className="text-sm text-red-800 dark:text-red-300">
            لإيقاف المصادقة الثنائية أدخل كلمة المرور الحالية.
          </p>
          <input
            type="password"
            value={disablePassword}
            onChange={(e) => setDisablePassword(e.target.value)}
            placeholder="كلمة المرور"
          />
          <div className="flex gap-2">
            <button
              onClick={() => disableFa.mutate()}
              disabled={!disablePassword || disableFa.isPending}
              className="btn-danger"
            >
              تأكيد الإيقاف
            </button>
            <button onClick={() => setShowDisable(false)} className="btn-outline">إلغاء</button>
          </div>
        </div>
      )}
    </div>
  );
}

function ChangePasswordPanel() {
  const [current, setCurrent] = useState("");
  const [newPwd, setNewPwd] = useState("");
  const [confirm, setConfirm] = useState("");

  const change = useMutation({
    mutationFn: async () => {
      if (newPwd !== confirm) throw new Error("كلمتا المرور غير متطابقتين");
      return (await api.post("/Auth/change-password", { currentPassword: current, newPassword: newPwd })).data;
    },
    onSuccess: () => {
      toast.success("تم تغيير كلمة المرور");
      setCurrent("");
      setNewPwd("");
      setConfirm("");
    },
    onError: (e) => toast.error(errorMessage(e)),
  });

  return (
    <div className="card">
      <h3 className="font-semibold mb-3">تغيير كلمة المرور</h3>
      <div className="grid md:grid-cols-3 gap-3">
        <div>
          <label>الحالية</label>
          <input type="password" value={current} onChange={(e) => setCurrent(e.target.value)} />
        </div>
        <div>
          <label>الجديدة</label>
          <input type="password" minLength={6} value={newPwd} onChange={(e) => setNewPwd(e.target.value)} />
        </div>
        <div>
          <label>تأكيد الجديدة</label>
          <input type="password" minLength={6} value={confirm} onChange={(e) => setConfirm(e.target.value)} />
        </div>
      </div>
      <button
        onClick={() => change.mutate()}
        disabled={!current || newPwd.length < 6 || change.isPending}
        className="btn mt-3"
      >
        {change.isPending ? "..." : "تغيير"}
      </button>
    </div>
  );
}

function BackupPanel({ apiUrl }: { apiUrl: string }) {
  const token = useAuth((s) => s.accessToken);
  const [downloading, setDownloading] = useState(false);

  async function download() {
    setDownloading(true);
    try {
      const res = await fetch(`${apiUrl}/Backup/download`, {
        headers: { Authorization: `Bearer ${token}` },
      });
      if (!res.ok) throw new Error(`HTTP ${res.status}`);
      const blob = await res.blob();
      const url = URL.createObjectURL(blob);
      const a = document.createElement("a");
      a.href = url;
      const cd = res.headers.get("content-disposition") ?? "";
      const match = cd.match(/filename="?([^";]+)"?/i);
      a.download = match?.[1] ?? `erp-backup-${Date.now()}.db`;
      document.body.appendChild(a);
      a.click();
      document.body.removeChild(a);
      URL.revokeObjectURL(url);
      toast.success("تم تنزيل النسخة الاحتياطية");
    } catch (e) {
      toast.error(errorMessage(e));
    } finally {
      setDownloading(false);
    }
  }

  return (
    <div className="card">
      <h3 className="font-semibold mb-3 flex items-center gap-2">
        <Database className="text-brand" /> النسخ الاحتياطي
      </h3>
      <p className="text-sm text-slate-600 dark:text-slate-400 mb-3">
        تنزيل نسخة من قاعدة البيانات (SQLite). آمن أثناء التشغيل لأنه يستخدم
        SQLite native backup. احفظ الملف في مكان آمن (سحابة/قرص خارجي).
      </p>
      <button onClick={download} disabled={downloading} className="btn">
        <Download size={16} /> {downloading ? "جاري التنزيل..." : "تنزيل نسخة احتياطية"}
      </button>
    </div>
  );
}
