"use client";

import { useState } from "react";
import Link from "next/link";
import { useRouter } from "next/navigation";
import toast from "react-hot-toast";
import { LogIn, ShieldCheck } from "lucide-react";
import axios from "axios";
import { api, errorMessage } from "@/lib/api";
import { useAuth } from "@/lib/auth";

export default function LoginPage() {
  const router = useRouter();
  const [userName, setUserName] = useState("");
  const [password, setPassword] = useState("");
  const [code, setCode] = useState("");
  const [needs2fa, setNeeds2fa] = useState(false);
  const [busy, setBusy] = useState(false);

  function applySession(data: {
    accessToken: string;
    refreshToken: string;
    user: { id: string; userName: string; fullName: string; email?: string | null; roles: string[] };
  }) {
    useAuth.getState().setSession({
      accessToken: data.accessToken,
      refreshToken: data.refreshToken,
      user: data.user,
    });
    router.replace("/dashboard");
  }

  async function submit(e: React.FormEvent) {
    e.preventDefault();
    setBusy(true);
    try {
      if (needs2fa) {
        const { data } = await api.post("/Auth/login-2fa", { userName, password, code });
        applySession(data);
      } else {
        const { data } = await api.post("/Auth/login", { userName, password });
        applySession(data);
      }
    } catch (err) {
      // Server returns "requires-2fa" message when user has 2FA enabled
      if (axios.isAxiosError(err)) {
        const msg = (err.response?.data as { error?: string } | undefined)?.error;
        if (msg === "requires-2fa") {
          setNeeds2fa(true);
          toast("أدخل رمز التحقق من تطبيق المصادقة", { icon: "🔐" });
          setBusy(false);
          return;
        }
      }
      toast.error(errorMessage(err));
    } finally {
      setBusy(false);
    }
  }

  return (
    <div className="min-h-screen flex items-center justify-center bg-slate-900 p-4">
      <form
        onSubmit={submit}
        className="w-full max-w-sm bg-white rounded-2xl shadow-xl p-6 space-y-4"
      >
        <div className="text-center">
          <div className="inline-flex items-center justify-center w-14 h-14 bg-brand/10 rounded-full mb-2">
            {needs2fa ? <ShieldCheck className="text-brand" /> : <LogIn className="text-brand" />}
          </div>
          <h1 className="text-2xl font-bold">
            {needs2fa ? "التحقق الثنائي" : "تسجيل الدخول"}
          </h1>
          <p className="text-slate-500 text-sm">
            {needs2fa ? "أدخل الرمز من تطبيق المصادقة" : "نظام المخازن ونقاط البيع"}
          </p>
        </div>

        {!needs2fa && (
          <>
            <div>
              <label>اسم المستخدم</label>
              <input autoFocus required value={userName} onChange={(e) => setUserName(e.target.value)} />
            </div>
            <div>
              <label>كلمة المرور</label>
              <input type="password" required value={password} onChange={(e) => setPassword(e.target.value)} />
            </div>
          </>
        )}

        {needs2fa && (
          <div>
            <label>رمز التحقق (6 أرقام)</label>
            <input
              autoFocus
              required
              inputMode="numeric"
              pattern="[0-9]{6}"
              maxLength={6}
              value={code}
              onChange={(e) => setCode(e.target.value.replace(/\D/g, ""))}
              className="font-mono text-center text-lg tracking-widest"
            />
          </div>
        )}

        <button type="submit" disabled={busy} className="btn w-full py-3 text-base">
          {busy ? "جاري الدخول..." : needs2fa ? "تأكيد" : "دخول"}
        </button>

        {!needs2fa && (
          <Link href="/forgot-password" className="text-center text-sm text-brand block">
            نسيت كلمة المرور؟
          </Link>
        )}
        {needs2fa && (
          <button
            type="button"
            onClick={() => { setNeeds2fa(false); setCode(""); }}
            className="text-center text-sm text-slate-500 hover:text-slate-700 block w-full"
          >
            العودة
          </button>
        )}
      </form>
    </div>
  );
}
