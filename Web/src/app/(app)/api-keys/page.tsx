"use client";

import { useState } from "react";
import { useMutation, useQuery, useQueryClient } from "@tanstack/react-query";
import toast from "react-hot-toast";
import { Ban, Copy, Eye, EyeOff, Key, Plus, Trash2 } from "lucide-react";
import { api, errorMessage } from "@/lib/api";
import { formatDateTime } from "@/lib/format";
import PageHeader from "@/components/PageHeader";
import Modal from "@/components/Modal";
import EmptyState from "@/components/EmptyState";
import { SkeletonRow } from "@/components/Skeleton";
import { useConfirm } from "@/components/ConfirmDialog";

interface ApiKey {
  id: string;
  name: string;
  prefix: string;
  scopes?: string | null;
  createdAt: string;
  expiresAt?: string | null;
  lastUsedAt?: string | null;
  isActive: boolean;
}
interface CreatedApiKey extends ApiKey { rawKey: string; }

export default function ApiKeysPage() {
  const qc = useQueryClient();
  const { confirm, dialog } = useConfirm();
  const [open, setOpen] = useState(false);
  const [createdKey, setCreatedKey] = useState<CreatedApiKey | null>(null);
  const [reveal, setReveal] = useState(false);
  const [form, setForm] = useState({ name: "", scopes: "", expiresAt: "" });

  const list = useQuery({
    queryKey: ["api-keys"],
    queryFn: async () => (await api.get<ApiKey[]>("/api-keys")).data,
  });

  const create = useMutation({
    mutationFn: async () =>
      (await api.post<CreatedApiKey>("/api-keys", {
        name: form.name,
        scopes: form.scopes || null,
        expiresAt: form.expiresAt ? new Date(form.expiresAt).toISOString() : null,
      })).data,
    onSuccess: (d) => {
      setCreatedKey(d);
      setForm({ name: "", scopes: "", expiresAt: "" });
      setOpen(false);
      qc.invalidateQueries({ queryKey: ["api-keys"] });
    },
    onError: (e) => toast.error(errorMessage(e)),
  });

  const revoke = useMutation({
    mutationFn: async (id: string) => api.post(`/api-keys/${id}/revoke`, {}),
    onSuccess: () => {
      toast.success("تم إلغاء المفتاح");
      qc.invalidateQueries({ queryKey: ["api-keys"] });
    },
    onError: (e) => toast.error(errorMessage(e)),
  });

  const del = useMutation({
    mutationFn: async (id: string) => api.delete(`/api-keys/${id}`),
    onSuccess: () => {
      toast.success("تم الحذف");
      qc.invalidateQueries({ queryKey: ["api-keys"] });
    },
    onError: (e) => toast.error(errorMessage(e)),
  });

  async function copy(text: string) {
    await navigator.clipboard.writeText(text);
    toast.success("تم النسخ");
  }

  return (
    <>
      <PageHeader title="مفاتيح API" description="مفاتيح للتطبيقات الخارجية للاتصال بـ API">
        <button onClick={() => setOpen(true)} className="btn">
          <Plus size={16} /> مفتاح جديد
        </button>
      </PageHeader>

      {/* One-time-display panel after creation */}
      {createdKey && (
        <div className="card mb-4 border-2 border-amber-400 bg-amber-50 dark:bg-amber-950/30">
          <h3 className="font-bold text-amber-900 dark:text-amber-200 mb-2 flex items-center gap-2">
            <Key /> المفتاح الجديد — انسخه الآن
          </h3>
          <p className="text-sm text-amber-800 dark:text-amber-300 mb-3">
            هذا المفتاح يظهر مرة واحدة فقط. خزّنه في مكان آمن قبل إغلاق هذه النافذة.
          </p>
          <div className="flex items-center gap-2 mb-3">
            <input
              readOnly
              value={reveal ? createdKey.rawKey : createdKey.rawKey.replace(/./g, "•")}
              className="font-mono"
            />
            <button onClick={() => setReveal(!reveal)} className="btn-outline !px-3" title={reveal ? "إخفاء" : "إظهار"}>
              {reveal ? <EyeOff size={16} /> : <Eye size={16} />}
            </button>
            <button onClick={() => copy(createdKey.rawKey)} className="btn !px-3">
              <Copy size={16} />
            </button>
          </div>
          <div className="text-xs text-slate-600 dark:text-slate-400">
            استخدمه عبر هيدر: <code className="bg-white dark:bg-slate-800 px-2 py-1 rounded">X-API-Key</code>
          </div>
          <button onClick={() => setCreatedKey(null)} className="btn-outline mt-3">
            فهمت، أخفِ المفتاح
          </button>
        </div>
      )}

      <div className="table-wrap">
        <table>
          <thead>
            <tr>
              <th>الاسم</th>
              <th>المعرّف</th>
              <th>الصلاحيات</th>
              <th>أُنشئ</th>
              <th>آخر استخدام</th>
              <th>الحالة</th>
              <th></th>
            </tr>
          </thead>
          <tbody>
            {list.isLoading ? (
              Array.from({ length: 4 }).map((_, i) => <SkeletonRow key={i} cols={7} />)
            ) : list.data?.length === 0 ? (
              <tr>
                <td colSpan={7}>
                  <EmptyState
                    icon={Key}
                    title="لا توجد مفاتيح API بعد"
                    description="أنشئ مفتاحاً لتطبيق خارجي ليتصل بالـ API دون استخدام بيانات مستخدم"
                    actionLabel="مفتاح جديد"
                    onAction={() => setOpen(true)}
                  />
                </td>
              </tr>
            ) : (
              list.data?.map((k) => (
                <tr key={k.id} className={!k.isActive ? "opacity-50" : ""}>
                  <td className="font-medium">{k.name}</td>
                  <td className="font-mono text-xs">{k.prefix}…</td>
                  <td className="text-xs">{k.scopes || "—"}</td>
                  <td className="text-xs">{formatDateTime(k.createdAt)}</td>
                  <td className="text-xs">{k.lastUsedAt ? formatDateTime(k.lastUsedAt) : "—"}</td>
                  <td>{k.isActive ? "🟢 نشط" : "⚫ ملغي"}</td>
                  <td className="flex gap-1">
                    {k.isActive && (
                      <button
                        onClick={async () => {
                          if (await confirm("إلغاء المفتاح", `إلغاء "${k.name}"؟`)) revoke.mutate(k.id);
                        }}
                        className="btn-outline !px-2 !py-1 text-xs"
                        title="إلغاء"
                      >
                        <Ban size={14} />
                      </button>
                    )}
                    <button
                      onClick={async () => {
                        if (await confirm("حذف المفتاح", `حذف "${k.name}" نهائياً؟`)) del.mutate(k.id);
                      }}
                      className="btn-danger !px-2 !py-1 text-xs"
                    >
                      <Trash2 size={14} />
                    </button>
                  </td>
                </tr>
              ))
            )}
          </tbody>
        </table>
      </div>

      <Modal open={open} title="مفتاح API جديد" onClose={() => setOpen(false)}>
        <div className="space-y-3">
          <div>
            <label>الاسم *</label>
            <input
              value={form.name}
              onChange={(e) => setForm({ ...form, name: e.target.value })}
              placeholder="متجر إلكتروني، تطبيق توصيل، ..."
            />
          </div>
          <div>
            <label>الصلاحيات (Scopes) — اختياري</label>
            <input
              value={form.scopes}
              onChange={(e) => setForm({ ...form, scopes: e.target.value })}
              placeholder="read:products,read:sales"
              className="font-mono text-sm"
            />
          </div>
          <div>
            <label>تاريخ الانتهاء — اختياري</label>
            <input
              type="date"
              value={form.expiresAt}
              onChange={(e) => setForm({ ...form, expiresAt: e.target.value })}
            />
          </div>
        </div>
        <div className="flex justify-end gap-2 mt-4">
          <button onClick={() => setOpen(false)} className="btn-outline">إلغاء</button>
          <button
            onClick={() => create.mutate()}
            disabled={!form.name.trim() || create.isPending}
            className="btn-success"
          >
            {create.isPending ? "..." : "إنشاء"}
          </button>
        </div>
      </Modal>
      {dialog}
    </>
  );
}
