"use client";

import { useState, Suspense } from "react";
import Link from "next/link";
import { useRouter, useSearchParams } from "next/navigation";
import toast from "react-hot-toast";
import { Lock } from "lucide-react";
import { api, errorMessage } from "@/lib/api";

function ResetForm() {
  const router = useRouter();
  const params = useSearchParams();
  const [token, setToken] = useState(params.get("token") ?? "");
  const [password, setPassword] = useState("");
  const [confirm, setConfirm] = useState("");
  const [busy, setBusy] = useState(false);

  async function submit(e: React.FormEvent) {
    e.preventDefault();
    if (password !== confirm) {
      toast.error("كلمتا المرور غير متطابقتين");
      return;
    }
    setBusy(true);
    try {
      await api.post("/Auth/reset-password", { token, newPassword: password });
      toast.success("تم تغيير كلمة المرور");
      router.replace("/login");
    } catch (err) {
      toast.error(errorMessage(err));
    } finally {
      setBusy(false);
    }
  }

  return (
    <form
      onSubmit={submit}
      className="w-full max-w-sm bg-white rounded-2xl shadow-xl p-6 space-y-4"
    >
      <div className="text-center">
        <div className="inline-flex items-center justify-center w-14 h-14 bg-brand/10 rounded-full mb-2">
          <Lock className="text-brand" />
        </div>
        <h1 className="text-2xl font-bold">تعيين كلمة مرور جديدة</h1>
      </div>
      <div>
        <label>الرمز (من رابط البريد)</label>
        <input value={token} onChange={(e) => setToken(e.target.value)} required />
      </div>
      <div>
        <label>كلمة المرور الجديدة</label>
        <input type="password" minLength={6} required value={password}
          onChange={(e) => setPassword(e.target.value)} />
      </div>
      <div>
        <label>تأكيد كلمة المرور</label>
        <input type="password" minLength={6} required value={confirm}
          onChange={(e) => setConfirm(e.target.value)} />
      </div>
      <button type="submit" disabled={busy} className="btn w-full py-3 text-base">
        {busy ? "جاري الحفظ..." : "تعيين كلمة المرور"}
      </button>
      <Link href="/login" className="text-center text-sm text-brand block">
        العودة لتسجيل الدخول
      </Link>
    </form>
  );
}

export default function ResetPasswordPage() {
  return (
    <div className="min-h-screen flex items-center justify-center bg-slate-900 p-4">
      <Suspense fallback={null}>
        <ResetForm />
      </Suspense>
    </div>
  );
}
