"use client";

import { useState } from "react";
import { useMutation, useQuery, useQueryClient } from "@tanstack/react-query";
import toast from "react-hot-toast";
import {
  AlertTriangle,
  ArrowDownToLine,
  Banknote,
  Check,
  Clock,
  Pencil,
  Plus,
  Trash2,
  X,
} from "lucide-react";
import { api, errorMessage } from "@/lib/api";
import { formatDate, formatMoney } from "@/lib/format";
import type { Cheque, ChequeStats, Customer, Supplier } from "@/types/api";
import { ChequeStatusLabel, ChequeTypeLabel } from "@/types/api";
import PageHeader from "@/components/PageHeader";
import EmptyState from "@/components/EmptyState";
import { SkeletonRow } from "@/components/Skeleton";
import { useConfirm } from "@/components/ConfirmDialog";

interface Form {
  id?: string;
  chequeNumber: string;
  bankName: string;
  branchName: string;
  accountHolderName: string;
  amount: number;
  issueDate: string;
  dueDate: string;
  type: number;
  customerId: string;
  supplierId: string;
  notes: string;
}

const today = () => new Date().toISOString().slice(0, 10);

const emptyForm: Form = {
  chequeNumber: "", bankName: "", branchName: "", accountHolderName: "",
  amount: 0, issueDate: today(), dueDate: today(),
  type: 1, customerId: "", supplierId: "", notes: "",
};

export default function ChequesPage() {
  const qc = useQueryClient();
  const { confirm, dialog } = useConfirm();
  const [tab, setTab] = useState<"all" | "1" | "2">("all");
  const [statusFilter, setStatusFilter] = useState<number | "">("");
  const [form, setForm] = useState<Form>(emptyForm);
  const [showForm, setShowForm] = useState(false);
  const [bouncingId, setBouncingId] = useState<string | null>(null);
  const [bounceReason, setBounceReason] = useState("");

  const stats = useQuery({
    queryKey: ["cheque-stats"],
    queryFn: async () => (await api.get<ChequeStats>("/cheques/stats")).data,
  });

  const cheques = useQuery({
    queryKey: ["cheques", tab, statusFilter],
    queryFn: async () =>
      (await api.get<Cheque[]>("/cheques", {
        params: {
          type: tab === "all" ? undefined : Number(tab),
          status: statusFilter === "" ? undefined : statusFilter,
        },
      })).data,
  });

  const customers = useQuery({
    queryKey: ["customers"],
    queryFn: async () => (await api.get<Customer[]>("/Customers")).data,
  });

  const suppliers = useQuery({
    queryKey: ["suppliers"],
    queryFn: async () => (await api.get<Supplier[]>("/Suppliers")).data,
  });

  const save = useMutation({
    mutationFn: async () => {
      const { id, ...payload } = form;
      const body = {
        ...payload,
        branchName: payload.branchName || null,
        accountHolderName: payload.accountHolderName || null,
        customerId: payload.type === 1 ? payload.customerId || null : null,
        supplierId: payload.type === 2 ? payload.supplierId || null : null,
        notes: payload.notes || null,
        issueDate: new Date(payload.issueDate).toISOString(),
        dueDate: new Date(payload.dueDate).toISOString(),
      };
      if (id) return (await api.put(`/cheques/${id}`, body)).data;
      return (await api.post("/cheques", body)).data;
    },
    onSuccess: () => {
      toast.success("تم الحفظ");
      setShowForm(false); setForm(emptyForm);
      qc.invalidateQueries({ queryKey: ["cheques"] });
      qc.invalidateQueries({ queryKey: ["cheque-stats"] });
    },
    onError: (e) => toast.error(errorMessage(e)),
  });

  const action = useMutation({
    mutationFn: async ({ id, op, body }: { id: string; op: string; body?: object }) =>
      (await api.post(`/cheques/${id}/${op}`, body ?? {})).data,
    onSuccess: () => {
      toast.success("تم");
      qc.invalidateQueries({ queryKey: ["cheques"] });
      qc.invalidateQueries({ queryKey: ["cheque-stats"] });
    },
    onError: (e) => toast.error(errorMessage(e)),
  });

  const del = useMutation({
    mutationFn: async (id: string) => (await api.delete(`/cheques/${id}`)).data,
    onSuccess: () => {
      toast.success("تم الحذف");
      qc.invalidateQueries({ queryKey: ["cheques"] });
      qc.invalidateQueries({ queryKey: ["cheque-stats"] });
    },
    onError: (e) => toast.error(errorMessage(e)),
  });

  function edit(c: Cheque) {
    setForm({
      id: c.id,
      chequeNumber: c.chequeNumber,
      bankName: c.bankName,
      branchName: c.branchName ?? "",
      accountHolderName: c.accountHolderName ?? "",
      amount: c.amount,
      issueDate: c.issueDate.slice(0, 10),
      dueDate: c.dueDate.slice(0, 10),
      type: c.type,
      customerId: c.customerId ?? "",
      supplierId: c.supplierId ?? "",
      notes: c.notes ?? "",
    });
    setShowForm(true);
  }

  return (
    <>
      <PageHeader title="الشيكات" description="متابعة الشيكات الواردة والصادرة">
        <button onClick={() => { setForm(emptyForm); setShowForm(true); }} className="btn">
          <Plus size={16} /> شيك جديد
        </button>
      </PageHeader>

      <div className="grid grid-cols-2 md:grid-cols-4 gap-3 mb-4">
        <Kpi
          icon={ArrowDownToLine}
          label="شيكات واردة بانتظار"
          value={`${stats.data?.incomingPending ?? 0} (${formatMoney(stats.data?.incomingPendingAmount ?? 0)})`}
          color="bg-emerald-50 text-emerald-700"
        />
        <Kpi
          icon={Banknote}
          label="شيكات صادرة بانتظار"
          value={`${stats.data?.outgoingPending ?? 0} (${formatMoney(stats.data?.outgoingPendingAmount ?? 0)})`}
          color="bg-blue-50 text-blue-700"
        />
        <Kpi
          icon={Clock}
          label="مستحقة خلال أسبوع"
          value={`${stats.data?.dueSoon ?? 0}`}
          color="bg-amber-50 text-amber-700"
        />
        <Kpi
          icon={AlertTriangle}
          label="متأخرة / مرتدة هذا الشهر"
          value={`${stats.data?.overdue ?? 0} / ${stats.data?.bouncedThisMonth ?? 0}`}
          color="bg-red-50 text-red-700"
        />
      </div>

      {showForm && (
        <div className="card mb-4">
          <div className="flex items-center justify-between mb-3">
            <h3 className="font-semibold">{form.id ? "تعديل شيك" : "شيك جديد"}</h3>
            <button onClick={() => setShowForm(false)} className="text-slate-400 hover:text-slate-700"><X size={20} /></button>
          </div>
          <div className="grid md:grid-cols-3 gap-3">
            <div>
              <label>النوع *</label>
              <select value={form.type} onChange={(e) => setForm({ ...form, type: Number(e.target.value) })}>
                <option value={1}>وارد (من عميل)</option>
                <option value={2}>صادر (لمورد)</option>
              </select>
            </div>
            {form.type === 1 ? (
              <div>
                <label>العميل *</label>
                <select value={form.customerId} onChange={(e) => setForm({ ...form, customerId: e.target.value })}>
                  <option value="">— اختر —</option>
                  {customers.data?.map((c) => <option key={c.id} value={c.id}>{c.name}</option>)}
                </select>
              </div>
            ) : (
              <div>
                <label>المورد *</label>
                <select value={form.supplierId} onChange={(e) => setForm({ ...form, supplierId: e.target.value })}>
                  <option value="">— اختر —</option>
                  {suppliers.data?.map((s) => <option key={s.id} value={s.id}>{s.name}</option>)}
                </select>
              </div>
            )}
            <div>
              <label>رقم الشيك *</label>
              <input value={form.chequeNumber} onChange={(e) => setForm({ ...form, chequeNumber: e.target.value })} />
            </div>
            <div>
              <label>البنك *</label>
              <input value={form.bankName} onChange={(e) => setForm({ ...form, bankName: e.target.value })} />
            </div>
            <div>
              <label>الفرع</label>
              <input value={form.branchName} onChange={(e) => setForm({ ...form, branchName: e.target.value })} />
            </div>
            <div>
              <label>اسم الساحب</label>
              <input value={form.accountHolderName} onChange={(e) => setForm({ ...form, accountHolderName: e.target.value })} />
            </div>
            <div>
              <label>المبلغ *</label>
              <input type="number" step="0.01" value={form.amount} onChange={(e) => setForm({ ...form, amount: Number(e.target.value) })} />
            </div>
            <div>
              <label>تاريخ التحرير</label>
              <input type="date" value={form.issueDate} onChange={(e) => setForm({ ...form, issueDate: e.target.value })} />
            </div>
            <div>
              <label>تاريخ الاستحقاق *</label>
              <input type="date" value={form.dueDate} onChange={(e) => setForm({ ...form, dueDate: e.target.value })} />
            </div>
            <div className="md:col-span-3">
              <label>ملاحظات</label>
              <input value={form.notes} onChange={(e) => setForm({ ...form, notes: e.target.value })} />
            </div>
          </div>
          <div className="flex gap-2 mt-3">
            <button
              onClick={() => save.mutate()}
              disabled={
                !form.chequeNumber.trim() || !form.bankName.trim() || form.amount <= 0 ||
                (form.type === 1 ? !form.customerId : !form.supplierId) ||
                save.isPending
              }
              className="btn-success"
            >
              {save.isPending ? "جاري الحفظ..." : "حفظ"}
            </button>
            <button onClick={() => setShowForm(false)} className="btn-outline">إلغاء</button>
          </div>
        </div>
      )}

      {bouncingId && (
        <div className="card mb-4 border-red-300">
          <div className="flex items-center justify-between mb-3">
            <h3 className="font-semibold text-red-700">تسجيل ارتداد الشيك</h3>
            <button onClick={() => { setBouncingId(null); setBounceReason(""); }} className="text-slate-400 hover:text-slate-700"><X size={20} /></button>
          </div>
          <label>سبب الارتداد</label>
          <input value={bounceReason} onChange={(e) => setBounceReason(e.target.value)} placeholder="رصيد غير كاف، إغلاق حساب، ..." />
          <div className="flex gap-2 mt-3">
            <button
              onClick={() => {
                action.mutate({ id: bouncingId, op: "bounce", body: { reason: bounceReason } },
                  { onSuccess: () => { setBouncingId(null); setBounceReason(""); } });
              }}
              className="btn"
              style={{ background: "#dc2626" }}
            >
              تأكيد الارتداد
            </button>
            <button onClick={() => { setBouncingId(null); setBounceReason(""); }} className="btn-outline">إلغاء</button>
          </div>
        </div>
      )}

      <div className="flex gap-1 mb-3 border-b border-slate-200">
        <TabBtn active={tab === "all"} onClick={() => setTab("all")}>الكل</TabBtn>
        <TabBtn active={tab === "1"} onClick={() => setTab("1")}>واردة</TabBtn>
        <TabBtn active={tab === "2"} onClick={() => setTab("2")}>صادرة</TabBtn>
      </div>

      <div className="card mb-3 flex items-center gap-3">
        <label className="text-sm">الحالة:</label>
        <select className="!w-auto" value={statusFilter} onChange={(e) => setStatusFilter(e.target.value === "" ? "" : Number(e.target.value))}>
          <option value="">الكل</option>
          {Object.entries(ChequeStatusLabel).map(([k, v]) => <option key={k} value={k}>{v}</option>)}
        </select>
      </div>

      <div className="table-wrap">
        <table>
          <thead>
            <tr>
              <th>الرقم</th>
              <th>النوع</th>
              <th>الطرف</th>
              <th>البنك</th>
              <th>المبلغ</th>
              <th>الاستحقاق</th>
              <th>الحالة</th>
              <th></th>
            </tr>
          </thead>
          <tbody>
            {cheques.isLoading ? (
              Array.from({ length: 6 }).map((_, i) => <SkeletonRow key={i} cols={8} />)
            ) : cheques.data?.length === 0 ? (
              <tr><td colSpan={8}>
                <EmptyState icon={Banknote} title="لا توجد شيكات" description="ابدأ بإضافة شيك جديد." actionLabel="شيك جديد" onAction={() => { setForm(emptyForm); setShowForm(true); }} />
              </td></tr>
            ) : (
              cheques.data?.map((c) => (
                <tr key={c.id}>
                  <td className="font-mono text-xs">{c.chequeNumber}</td>
                  <td>
                    <span className={`text-xs px-2 py-0.5 rounded-full ${c.type === 1 ? "bg-emerald-100 text-emerald-800" : "bg-blue-100 text-blue-800"}`}>
                      {ChequeTypeLabel[c.type]}
                    </span>
                  </td>
                  <td>{c.type === 1 ? c.customerName : c.supplierName}</td>
                  <td>{c.bankName}</td>
                  <td className="font-semibold">{formatMoney(c.amount)}</td>
                  <td>
                    <div>{formatDate(c.dueDate)}</div>
                    {c.daysToDue < 0 && c.status <= 1 && (
                      <div className="text-xs text-red-600">متأخر {Math.abs(c.daysToDue)} يوم</div>
                    )}
                    {c.daysToDue >= 0 && c.daysToDue <= 7 && c.status <= 1 && (
                      <div className="text-xs text-amber-600">خلال {c.daysToDue} يوم</div>
                    )}
                  </td>
                  <td>
                    <span className={`text-xs px-2 py-0.5 rounded-full ${
                      c.status === 0 ? "bg-slate-100 text-slate-800"
                      : c.status === 1 ? "bg-blue-100 text-blue-800"
                      : c.status === 2 ? "bg-emerald-100 text-emerald-800"
                      : c.status === 3 ? "bg-red-100 text-red-700"
                      : "bg-slate-200 text-slate-600"
                    }`}>{ChequeStatusLabel[c.status]}</span>
                  </td>
                  <td className="flex gap-1">
                    {c.status === 0 && (
                      <>
                        <button onClick={() => action.mutate({ id: c.id, op: "deposit" })} className="btn-outline !px-2 !py-1 text-xs text-blue-700"><ArrowDownToLine size={14} /> إيداع</button>
                        <button onClick={() => edit(c)} className="btn-outline !px-2 !py-1 text-xs"><Pencil size={14} /></button>
                      </>
                    )}
                    {c.status === 1 && (
                      <>
                        <button onClick={() => action.mutate({ id: c.id, op: "clear" })} className="btn-outline !px-2 !py-1 text-xs text-emerald-700"><Check size={14} /> تحصيل</button>
                        <button onClick={() => { setBouncingId(c.id); setBounceReason(""); }} className="btn-outline !px-2 !py-1 text-xs text-red-700">ارتداد</button>
                      </>
                    )}
                    {c.status === 3 && (
                      <button onClick={() => action.mutate({ id: c.id, op: "deposit" })} className="btn-outline !px-2 !py-1 text-xs text-blue-700">إعادة إيداع</button>
                    )}
                    {c.status !== 2 && (
                      <>
                        {c.status !== 5 && (
                          <button onClick={() => action.mutate({ id: c.id, op: "cancel" })} className="btn-outline !px-2 !py-1 text-xs">إلغاء</button>
                        )}
                        <button onClick={async () => { if (await confirm({ title: "حذف الشيك؟", message: c.chequeNumber })) del.mutate(c.id); }} className="btn-outline !px-2 !py-1 text-xs text-red-600"><Trash2 size={14} /></button>
                      </>
                    )}
                  </td>
                </tr>
              ))
            )}
          </tbody>
        </table>
      </div>
      {dialog}
    </>
  );
}

function TabBtn({ active, onClick, children }: { active: boolean; onClick: () => void; children: React.ReactNode }) {
  return (
    <button onClick={onClick} className={`px-4 py-2 text-sm border-b-2 ${active ? "border-brand text-brand font-semibold" : "border-transparent text-slate-500"}`}>
      {children}
    </button>
  );
}

function Kpi({ icon: Icon, label, value, color }: { icon: typeof Banknote; label: string; value: string; color: string }) {
  return (
    <div className="card">
      <div className="flex items-center gap-3">
        <div className={`rounded-lg p-2 ${color} dark:bg-opacity-20`}><Icon size={20} /></div>
        <div className="min-w-0">
          <div className="text-xs text-slate-500">{label}</div>
          <div className="text-base font-bold truncate">{value}</div>
        </div>
      </div>
    </div>
  );
}
