"use client";

import { useState } from "react";
import Link from "next/link";
import toast from "react-hot-toast";
import { KeyRound } from "lucide-react";
import { api, errorMessage } from "@/lib/api";

export default function ForgotPasswordPage() {
  const [email, setEmail] = useState("");
  const [busy, setBusy] = useState(false);
  const [sent, setSent] = useState(false);

  async function submit(e: React.FormEvent) {
    e.preventDefault();
    setBusy(true);
    try {
      await api.post("/Auth/forgot-password", { email });
      setSent(true);
    } catch (err) {
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
            <KeyRound className="text-brand" />
          </div>
          <h1 className="text-2xl font-bold">استعادة كلمة المرور</h1>
          <p className="text-slate-500 text-sm">سنرسل لك رابط استعادة على بريدك</p>
        </div>
        {sent ? (
          <>
            <div className="bg-emerald-50 text-emerald-800 rounded-lg p-3 text-sm">
              إذا كان البريد مسجلاً عندنا، ستجد رابط الاستعادة في الإيميل خلال دقائق.
            </div>
            <Link href="/login" className="btn-outline w-full inline-flex justify-center">
              العودة لتسجيل الدخول
            </Link>
          </>
        ) : (
          <>
            <div>
              <label>البريد الإلكتروني</label>
              <input
                type="email"
                autoFocus
                required
                value={email}
                onChange={(e) => setEmail(e.target.value)}
              />
            </div>
            <button type="submit" disabled={busy} className="btn w-full py-3 text-base">
              {busy ? "جاري الإرسال..." : "إرسال رابط الاستعادة"}
            </button>
            <Link href="/login" className="text-center text-sm text-brand block">
              العودة لتسجيل الدخول
            </Link>
          </>
        )}
      </form>
    </div>
  );
}
