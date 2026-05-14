"use client";

import { useMemo, useState } from "react";
import { useQuery } from "@tanstack/react-query";
import { api } from "@/lib/api";
import { formatDateTime } from "@/lib/format";
import type { LogHistory } from "@/types/api";
import PageHeader from "@/components/PageHeader";

const ACTION_COLORS: Record<string, string> = {
  Create: "bg-emerald-100 text-emerald-800",
  Update: "bg-amber-100 text-amber-800",
  Delete: "bg-red-100 text-red-800",
};

export default function AuditLogPage() {
  const [search, setSearch] = useState("");
  const [entityFilter, setEntityFilter] = useState("");

  const { data, isLoading } = useQuery({
    queryKey: ["audit-log"],
    queryFn: async () => (await api.get<LogHistory[]>("/LogHistories")).data,
  });

  const entities = useMemo(() => {
    const set = new Set(data?.map((l) => l.entityName) ?? []);
    return Array.from(set).sort();
  }, [data]);

  const filtered = useMemo(() => {
    if (!data) return [];
    return data.filter((l) => {
      if (entityFilter && l.entityName !== entityFilter) return false;
      if (search) {
        const q = search.toLowerCase();
        if (
          !l.userName.toLowerCase().includes(q) &&
          !l.entityName.toLowerCase().includes(q) &&
          !l.action.toLowerCase().includes(q) &&
          !(l.notes ?? "").toLowerCase().includes(q)
        )
          return false;
      }
      return true;
    });
  }, [data, search, entityFilter]);

  return (
    <>
      <PageHeader title="سجل العمليات" description="سجل بكل العمليات الحساسة على النظام" />
      <div className="card mb-3 flex flex-wrap items-end gap-3">
        <div className="flex-1 min-w-[200px]">
          <label>بحث</label>
          <input value={search} onChange={(e) => setSearch(e.target.value)} placeholder="مستخدم، كيان، عملية..." />
        </div>
        <div>
          <label>الكيان</label>
          <select value={entityFilter} onChange={(e) => setEntityFilter(e.target.value)}>
            <option value="">كل الكيانات</option>
            {entities.map((e) => <option key={e} value={e}>{e}</option>)}
          </select>
        </div>
      </div>
      <div className="table-wrap">
        <table>
          <thead>
            <tr>
              <th>التاريخ</th>
              <th>المستخدم</th>
              <th>الكيان</th>
              <th>العملية</th>
              <th>الحقول</th>
              <th>ملاحظات</th>
            </tr>
          </thead>
          <tbody>
            {isLoading ? <tr><td colSpan={6} className="text-center py-6">جاري التحميل...</td></tr> :
              filtered.length === 0 ? <tr><td colSpan={6} className="text-center py-6 text-slate-400">لا توجد سجلات</td></tr> :
              filtered.map((l) => (
                <tr key={l.id}>
                  <td className="text-xs">{formatDateTime(l.timestamp)}</td>
                  <td>{l.userName}</td>
                  <td className="font-mono text-xs">{l.entityName}</td>
                  <td>
                    <span className={`text-xs px-2 py-0.5 rounded-full ${ACTION_COLORS[l.action] ?? "bg-slate-100 text-slate-800"}`}>
                      {l.action}
                    </span>
                  </td>
                  <td className="text-xs">{l.changedFields}</td>
                  <td className="text-xs text-slate-500">{l.notes}</td>
                </tr>
              ))}
          </tbody>
        </table>
      </div>
    </>
  );
}
