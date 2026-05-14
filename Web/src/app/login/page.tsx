"use client";

import { useState } from "react";
import { useRouter } from "next/navigation";
import toast from "react-hot-toast";
import { LogIn } from "lucide-react";
import { api, errorMessage } from "@/lib/api";
import { useAuth } from "@/lib/auth";

export default function LoginPage() {
  const router = useRouter();
  const [userName, setUserName] = useState("");
  const [password, setPassword] = useState("");
  const [busy, setBusy] = useState(false);

  async function submit(e: React.FormEvent) {
    e.preventDefault();
    setBusy(true);
    try {
      const { data } = await api.post("/Auth/login", { userName, password });
      useAuth.getState().setSession({
        accessToken: data.accessToken,
        refreshToken: data.refreshToken,
        user: data.user,
      });
      router.replace("/dashboard");
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
            <LogIn className="text-brand" />
          </div>
          <h1 className="text-2xl font-bold">تسجيل الدخول</h1>
          <p className="text-slate-500 text-sm">نظام المخازن ونقاط البيع</p>
        </div>
        <div>
          <label>اسم المستخدم</label>
          <input
            autoFocus
            required
            value={userName}
            onChange={(e) => setUserName(e.target.value)}
          />
        </div>
        <div>
          <label>كلمة المرور</label>
          <input
            type="password"
            required
            value={password}
            onChange={(e) => setPassword(e.target.value)}
          />
        </div>
        <button type="submit" disabled={busy} className="btn w-full py-3 text-base">
          {busy ? "جاري الدخول..." : "دخول"}
        </button>
      </form>
    </div>
  );
}
