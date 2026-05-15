"use client";

import { useState } from "react";
import { useMutation, useQuery, useQueryClient } from "@tanstack/react-query";
import toast from "react-hot-toast";
import { Bell, Pencil, Plus, Send, Trash2, Webhook } from "lucide-react";
import { api, errorMessage } from "@/lib/api";
import { formatDateTime } from "@/lib/format";
import PageHeader from "@/components/PageHeader";
import Modal from "@/components/Modal";
import EmptyState from "@/components/EmptyState";
import { SkeletonRow } from "@/components/Skeleton";
import { useConfirm } from "@/components/ConfirmDialog";

interface WebhookSubscription {
  id: string;
  name: string;
  url: string;
  events: string;
  isActive: boolean;
  createdAt: string;
  hasSecret: boolean;
}
interface WebhookDelivery {
  id: string;
  subscriptionId: string;
  event: string;
  responseStatus?: number | null;
  error?: string | null;
  attempts: number;
  createdAt: string;
  deliveredAt?: string | null;
}

export default function WebhooksPage() {
  const qc = useQueryClient();
  const { confirm, dialog } = useConfirm();
  const [open, setOpen] = useState(false);
  const [form, setForm] = useState({
    id: "",
    name: "",
    url: "",
    events: "",
    secret: "",
    isActive: true,
  });
  const [deliveriesFor, setDeliveriesFor] = useState<string | null>(null);

  const list = useQuery({
    queryKey: ["webhooks"],
    queryFn: async () => (await api.get<WebhookSubscription[]>("/Webhooks")).data,
  });

  const events = useQuery({
    queryKey: ["webhook-events"],
    queryFn: async () => (await api.get<string[]>("/Webhooks/events")).data,
  });

  const deliveries = useQuery({
    enabled: !!deliveriesFor,
    queryKey: ["webhook-deliveries", deliveriesFor],
    queryFn: async () =>
      (await api.get<WebhookDelivery[]>(`/Webhooks/${deliveriesFor}/deliveries`)).data,
  });

  const save = useMutation({
    mutationFn: async () => {
      const { id, ...payload } = form;
      const body = {
        ...payload,
        secret: payload.secret || null,
      };
      if (id) return (await api.put(`/Webhooks/${id}`, body)).data;
      return (await api.post("/Webhooks", body)).data;
    },
    onSuccess: () => {
      toast.success("تم الحفظ");
      setOpen(false);
      setForm({ id: "", name: "", url: "", events: "", secret: "", isActive: true });
      qc.invalidateQueries({ queryKey: ["webhooks"] });
    },
    onError: (e) => toast.error(errorMessage(e)),
  });

  const del = useMutation({
    mutationFn: async (id: string) => api.delete(`/Webhooks/${id}`),
    onSuccess: () => {
      toast.success("تم الحذف");
      qc.invalidateQueries({ queryKey: ["webhooks"] });
    },
    onError: (e) => toast.error(errorMessage(e)),
  });

  const test = useMutation({
    mutationFn: async (id: string) => api.post("/Webhooks/test", { subscriptionId: id }),
    onSuccess: () => {
      toast.success("تم إرسال test.ping — راجع السجل");
    },
    onError: (e) => toast.error(errorMessage(e)),
  });

  function edit(w: WebhookSubscription) {
    setForm({
      id: w.id,
      name: w.name,
      url: w.url,
      events: w.events,
      secret: "",
      isActive: w.isActive,
    });
    setOpen(true);
  }

  return (
    <>
      <PageHeader title="Webhooks" description="إخبار خدمات خارجية بالأحداث (مبيعات، مرتجعات، ETA، ...)">
        <button onClick={() => { setForm({ id: "", name: "", url: "", events: "", secret: "", isActive: true }); setOpen(true); }} className="btn">
          <Plus size={16} /> Webhook جديد
        </button>
      </PageHeader>

      <div className="table-wrap mb-4">
        <table>
          <thead>
            <tr>
              <th>الاسم</th>
              <th>URL</th>
              <th>الأحداث</th>
              <th>الحالة</th>
              <th>منذ</th>
              <th></th>
            </tr>
          </thead>
          <tbody>
            {list.isLoading ? (
              Array.from({ length: 4 }).map((_, i) => <SkeletonRow key={i} cols={6} />)
            ) : list.data?.length === 0 ? (
              <tr>
                <td colSpan={6}>
                  <EmptyState
                    icon={Webhook}
                    title="لا توجد Webhooks"
                    description="سجّل URL لخدمة خارجية لإرسال الأحداث إليها تلقائياً عند حدوثها."
                    actionLabel="إضافة Webhook"
                    onAction={() => setOpen(true)}
                  />
                </td>
              </tr>
            ) : (
              list.data?.map((w) => (
                <tr key={w.id} className={!w.isActive ? "opacity-50" : ""}>
                  <td className="font-medium">{w.name}</td>
                  <td className="font-mono text-xs break-all max-w-xs">{w.url}</td>
                  <td className="font-mono text-xs">{w.events}</td>
                  <td>{w.isActive ? "🟢 نشط" : "⚫ معطل"}</td>
                  <td className="text-xs">{formatDateTime(w.createdAt)}</td>
                  <td className="flex gap-1 flex-wrap">
                    <button onClick={() => test.mutate(w.id)} className="btn-outline !px-2 !py-1 text-xs" title="إرسال test.ping">
                      <Send size={14} />
                    </button>
                    <button onClick={() => setDeliveriesFor(w.id)} className="btn-outline !px-2 !py-1 text-xs" title="السجل">
                      <Bell size={14} />
                    </button>
                    <button onClick={() => edit(w)} className="btn-outline !px-2 !py-1 text-xs">
                      <Pencil size={14} />
                    </button>
                    <button
                      onClick={async () => {
                        if (await confirm("حذف Webhook", `حذف "${w.name}"؟`)) del.mutate(w.id);
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

      <Modal open={open} title={form.id ? "تعديل Webhook" : "Webhook جديد"} onClose={() => setOpen(false)} size="lg">
        <div className="space-y-3">
          <div>
            <label>الاسم *</label>
            <input value={form.name} onChange={(e) => setForm({ ...form, name: e.target.value })} />
          </div>
          <div>
            <label>URL *</label>
            <input
              value={form.url}
              onChange={(e) => setForm({ ...form, url: e.target.value })}
              type="url"
              placeholder="https://example.com/webhook"
              className="font-mono text-sm"
            />
          </div>
          <div>
            <label>الأحداث (مفصولة بفاصلة، أو * لكل الأحداث، أو sale.*)</label>
            <input
              value={form.events}
              onChange={(e) => setForm({ ...form, events: e.target.value })}
              placeholder="sale.created,sale.refunded,stock.low"
              className="font-mono text-sm"
            />
            {events.data && events.data.length > 0 && (
              <p className="text-xs text-slate-500 mt-2">
                الأحداث المعروفة: {events.data.join("، ")}
              </p>
            )}
          </div>
          <div>
            <label>السر (اختياري — يُولّد تلقائياً)</label>
            <input
              value={form.secret}
              onChange={(e) => setForm({ ...form, secret: e.target.value })}
              placeholder={form.id ? "اتركه فارغاً للحفاظ على الحالي" : ""}
              className="font-mono text-sm"
            />
            <p className="text-xs text-slate-500 mt-1">
              يُستخدم لتوقيع الطلبات عبر هيدر <code>X-Webhook-Signature: sha256=...</code>
            </p>
          </div>
          <label className="flex items-center gap-2">
            <input type="checkbox" checked={form.isActive} onChange={(e) => setForm({ ...form, isActive: e.target.checked })} className="!w-auto" />
            <span>مفعّل</span>
          </label>
        </div>
        <div className="flex justify-end gap-2 mt-4">
          <button onClick={() => setOpen(false)} className="btn-outline">إلغاء</button>
          <button
            onClick={() => save.mutate()}
            disabled={!form.name.trim() || !form.url.trim() || !form.events.trim() || save.isPending}
            className="btn-success"
          >
            {save.isPending ? "..." : "حفظ"}
          </button>
        </div>
      </Modal>

      <Modal open={!!deliveriesFor} title="سجل التسليم" onClose={() => setDeliveriesFor(null)} size="xl">
        {deliveries.isLoading ? (
          <p>جاري التحميل...</p>
        ) : !deliveries.data || deliveries.data.length === 0 ? (
          <EmptyState icon={Bell} title="لا توجد محاولات بعد" />
        ) : (
          <div className="table-wrap">
            <table>
              <thead>
                <tr>
                  <th>الحدث</th>
                  <th>الحالة</th>
                  <th>المحاولات</th>
                  <th>أنشئ</th>
                  <th>سُلّم</th>
                  <th>الخطأ</th>
                </tr>
              </thead>
              <tbody>
                {deliveries.data.map((d) => (
                  <tr key={d.id}>
                    <td className="font-mono text-xs">{d.event}</td>
                    <td>
                      {d.responseStatus ? (
                        <span className={d.responseStatus < 300 ? "text-emerald-600" : "text-red-600"}>
                          {d.responseStatus}
                        </span>
                      ) : (
                        <span className="text-red-600">فشل</span>
                      )}
                    </td>
                    <td>{d.attempts}</td>
                    <td className="text-xs">{formatDateTime(d.createdAt)}</td>
                    <td className="text-xs">{d.deliveredAt ? formatDateTime(d.deliveredAt) : "—"}</td>
                    <td className="text-xs text-red-600 max-w-xs truncate">{d.error}</td>
                  </tr>
                ))}
              </tbody>
            </table>
          </div>
        )}
      </Modal>
      {dialog}
    </>
  );
}
