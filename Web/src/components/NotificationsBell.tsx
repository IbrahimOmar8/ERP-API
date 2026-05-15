"use client";

import { useEffect, useState } from "react";
import Link from "next/link";
import { useQuery, useQueryClient, useMutation } from "@tanstack/react-query";
import { Bell, Check, X } from "lucide-react";
import { api } from "@/lib/api";
import { subscribe } from "@/lib/realtime";
import { formatDateTime } from "@/lib/format";

interface NotificationItem {
  id: string;
  title: string;
  message: string;
  type?: string | null;
  link?: string | null;
  severity: "info" | "success" | "warning" | "error";
  isRead: boolean;
  createdAt: string;
}

const severityClasses: Record<string, string> = {
  info: "border-l-blue-400 bg-blue-50/50 dark:bg-blue-950/20",
  success: "border-l-emerald-400 bg-emerald-50/50 dark:bg-emerald-950/20",
  warning: "border-l-amber-400 bg-amber-50/50 dark:bg-amber-950/20",
  error: "border-l-red-400 bg-red-50/50 dark:bg-red-950/20",
};

export default function NotificationsBell() {
  const qc = useQueryClient();
  const [open, setOpen] = useState(false);

  const unread = useQuery({
    queryKey: ["unread-count"],
    queryFn: async () => (await api.get<{ count: number }>("/Notifications/unread-count")).data.count,
    refetchInterval: 60_000, // safety net poll
    refetchOnWindowFocus: true,
  });

  const list = useQuery({
    enabled: open,
    queryKey: ["notifications"],
    queryFn: async () => (await api.get<NotificationItem[]>("/Notifications?take=20")).data,
  });

  const markOne = useMutation({
    mutationFn: async (id: string) => api.post(`/Notifications/${id}/read`, {}),
    onSuccess: () => {
      qc.invalidateQueries({ queryKey: ["unread-count"] });
      qc.invalidateQueries({ queryKey: ["notifications"] });
    },
  });

  const markAll = useMutation({
    mutationFn: async () => api.post(`/Notifications/read-all`, {}),
    onSuccess: () => {
      qc.invalidateQueries({ queryKey: ["unread-count"] });
      qc.invalidateQueries({ queryKey: ["notifications"] });
    },
  });

  // Listen for real-time notification events and bump the badge
  useEffect(() => {
    const off = subscribe("notification.new", () => {
      qc.invalidateQueries({ queryKey: ["unread-count"] });
      qc.invalidateQueries({ queryKey: ["notifications"] });
    });
    return () => off();
  }, [qc]);

  return (
    <div className="relative">
      <button
        onClick={() => setOpen(!open)}
        className="relative p-2 rounded-lg text-slate-300 hover:bg-slate-800"
        aria-label="الإشعارات"
      >
        <Bell size={18} />
        {(unread.data ?? 0) > 0 && (
          <span className="absolute -top-1 -end-1 min-w-[18px] h-[18px] px-1 bg-red-500 text-white text-[10px] font-bold rounded-full flex items-center justify-center">
            {(unread.data ?? 0) > 9 ? "9+" : unread.data}
          </span>
        )}
      </button>

      {open && (
        <>
          <div className="fixed inset-0 z-30" onClick={() => setOpen(false)} />
          <div className="absolute end-0 mt-2 w-80 max-h-[70vh] bg-white dark:bg-slate-900 text-slate-900 dark:text-slate-100 rounded-xl shadow-xl border border-slate-200 dark:border-slate-700 z-40 flex flex-col">
            <div className="flex items-center justify-between px-3 py-2 border-b border-slate-200 dark:border-slate-700">
              <span className="font-semibold">الإشعارات</span>
              <div className="flex gap-1">
                {(unread.data ?? 0) > 0 && (
                  <button onClick={() => markAll.mutate()} className="text-xs text-brand hover:underline">
                    قراءة الكل
                  </button>
                )}
                <button onClick={() => setOpen(false)} className="text-slate-400">
                  <X size={16} />
                </button>
              </div>
            </div>
            <div className="overflow-y-auto flex-1">
              {list.isLoading ? (
                <div className="text-center text-sm text-slate-500 py-8">جاري التحميل...</div>
              ) : list.data?.length === 0 ? (
                <div className="text-center text-sm text-slate-400 py-12">
                  <Bell className="mx-auto mb-2 text-slate-300" size={28} />
                  لا توجد إشعارات
                </div>
              ) : (
                list.data?.map((n) => {
                  const inner = (
                    <div className={`p-3 border-l-4 ${severityClasses[n.severity] ?? severityClasses.info} ${!n.isRead ? "" : "opacity-60"}`}>
                      <div className="flex items-start justify-between gap-2">
                        <div className="font-medium text-sm">{n.title}</div>
                        {!n.isRead && (
                          <button
                            onClick={(e) => { e.preventDefault(); markOne.mutate(n.id); }}
                            className="text-xs text-brand hover:bg-brand/10 p-1 rounded"
                            title="قراءة"
                          >
                            <Check size={14} />
                          </button>
                        )}
                      </div>
                      <div className="text-xs text-slate-600 dark:text-slate-400 mt-1">{n.message}</div>
                      <div className="text-[10px] text-slate-400 mt-1">{formatDateTime(n.createdAt)}</div>
                    </div>
                  );
                  return n.link ? (
                    <Link key={n.id} href={n.link} onClick={() => { setOpen(false); markOne.mutate(n.id); }} className="block hover:bg-slate-50 dark:hover:bg-slate-800">
                      {inner}
                    </Link>
                  ) : (
                    <div key={n.id}>{inner}</div>
                  );
                })
              )}
            </div>
          </div>
        </>
      )}
    </div>
  );
}
