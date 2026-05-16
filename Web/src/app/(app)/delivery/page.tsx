"use client";

import { useState } from "react";
import { useMutation, useQuery, useQueryClient } from "@tanstack/react-query";
import toast from "react-hot-toast";
import {
  Bike,
  Check,
  MapPin,
  Pencil,
  Plus,
  Trash2,
  Truck,
  X,
} from "lucide-react";
import { api, errorMessage } from "@/lib/api";
import { formatMoney } from "@/lib/format";
import type { Customer, DeliveryOrder, DeliveryZone, Driver } from "@/types/api";
import { DeliveryStatusLabel, VehicleTypeLabel } from "@/types/api";
import PageHeader from "@/components/PageHeader";
import EmptyState from "@/components/EmptyState";
import { SkeletonRow } from "@/components/Skeleton";
import { useConfirm } from "@/components/ConfirmDialog";

type Tab = "orders" | "drivers" | "zones";

export default function DeliveryPage() {
  const [tab, setTab] = useState<Tab>("orders");
  return (
    <>
      <PageHeader title="الديليفري" description="إدارة الطلبات والمناديب ومناطق التوصيل" />

      <div className="flex gap-1 mb-3 border-b border-slate-200">
        <TabBtn active={tab === "orders"} onClick={() => setTab("orders")}>الطلبات</TabBtn>
        <TabBtn active={tab === "drivers"} onClick={() => setTab("drivers")}>المناديب</TabBtn>
        <TabBtn active={tab === "zones"} onClick={() => setTab("zones")}>المناطق</TabBtn>
      </div>

      {tab === "orders" && <OrdersTab />}
      {tab === "drivers" && <DriversTab />}
      {tab === "zones" && <ZonesTab />}
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

// ─── Orders ─────────────────────────────────────────────────────────────

function OrdersTab() {
  const qc = useQueryClient();
  const { confirm, dialog } = useConfirm();
  const [statusFilter, setStatusFilter] = useState<number | "">("");
  const [show, setShow] = useState(false);
  const [assignFor, setAssignFor] = useState<DeliveryOrder | null>(null);
  const [collectFor, setCollectFor] = useState<DeliveryOrder | null>(null);
  const [collected, setCollected] = useState<number>(0);
  const [form, setForm] = useState({
    customerId: "", customerName: "", customerPhone: "",
    address: "", zoneId: "", deliveryFee: 0, cashToCollect: 0,
    driverId: "", notes: "",
  });

  const orders = useQuery({
    queryKey: ["delivery-orders", statusFilter],
    queryFn: async () =>
      (await api.get<DeliveryOrder[]>("/delivery/orders", {
        params: { status: statusFilter === "" ? undefined : statusFilter },
      })).data,
  });

  const drivers = useQuery({
    queryKey: ["drivers", true],
    queryFn: async () => (await api.get<Driver[]>("/delivery/drivers", { params: { activeOnly: true } })).data,
  });

  const zones = useQuery({
    queryKey: ["delivery-zones"],
    queryFn: async () => (await api.get<DeliveryZone[]>("/delivery/zones")).data,
  });

  const customers = useQuery({
    queryKey: ["customers"],
    queryFn: async () => (await api.get<Customer[]>("/Customers")).data,
  });

  const create = useMutation({
    mutationFn: async () =>
      (await api.post("/delivery/orders", {
        ...form,
        customerId: form.customerId || null,
        customerName: form.customerName || null,
        customerPhone: form.customerPhone || null,
        zoneId: form.zoneId || null,
        driverId: form.driverId || null,
        notes: form.notes || null,
      })).data,
    onSuccess: () => {
      toast.success("تم إنشاء الطلب");
      setShow(false);
      setForm({ customerId: "", customerName: "", customerPhone: "", address: "", zoneId: "", deliveryFee: 0, cashToCollect: 0, driverId: "", notes: "" });
      qc.invalidateQueries({ queryKey: ["delivery-orders"] });
    },
    onError: (e) => toast.error(errorMessage(e)),
  });

  const action = useMutation({
    mutationFn: async ({ id, op, body }: { id: string; op: string; body?: object }) =>
      (await api.post(`/delivery/orders/${id}/${op}`, body ?? {})).data,
    onSuccess: () => {
      toast.success("تم");
      qc.invalidateQueries({ queryKey: ["delivery-orders"] });
      qc.invalidateQueries({ queryKey: ["drivers"] });
    },
    onError: (e) => toast.error(errorMessage(e)),
  });

  const del = useMutation({
    mutationFn: async (id: string) => (await api.delete(`/delivery/orders/${id}`)).data,
    onSuccess: () => { toast.success("تم الحذف"); qc.invalidateQueries({ queryKey: ["delivery-orders"] }); },
    onError: (e) => toast.error(errorMessage(e)),
  });

  return (
    <>
      <div className="flex items-center justify-between mb-3">
        <div className="flex items-center gap-3">
          <label className="text-sm">الحالة:</label>
          <select className="!w-auto" value={statusFilter} onChange={(e) => setStatusFilter(e.target.value === "" ? "" : Number(e.target.value))}>
            <option value="">الكل</option>
            {Object.entries(DeliveryStatusLabel).map(([k, v]) => <option key={k} value={k}>{v}</option>)}
          </select>
        </div>
        <button onClick={() => setShow(true)} className="btn">
          <Plus size={16} /> طلب جديد
        </button>
      </div>

      {show && (
        <div className="card mb-4">
          <div className="flex items-center justify-between mb-3">
            <h3 className="font-semibold">طلب توصيل جديد</h3>
            <button onClick={() => setShow(false)} className="text-slate-400 hover:text-slate-700"><X size={20} /></button>
          </div>
          <div className="grid md:grid-cols-3 gap-3">
            <div>
              <label>العميل (اختياري)</label>
              <select value={form.customerId} onChange={(e) => {
                const c = customers.data?.find((x) => x.id === e.target.value);
                setForm({
                  ...form,
                  customerId: e.target.value,
                  customerName: c?.name ?? form.customerName,
                  customerPhone: c?.phone ?? form.customerPhone,
                  address: c?.address ?? form.address,
                });
              }}>
                <option value="">— عميل مؤقت —</option>
                {customers.data?.map((c) => <option key={c.id} value={c.id}>{c.name}</option>)}
              </select>
            </div>
            <div>
              <label>اسم العميل</label>
              <input value={form.customerName} onChange={(e) => setForm({ ...form, customerName: e.target.value })} />
            </div>
            <div>
              <label>الهاتف</label>
              <input value={form.customerPhone} onChange={(e) => setForm({ ...form, customerPhone: e.target.value })} />
            </div>
            <div className="md:col-span-3">
              <label>العنوان *</label>
              <input value={form.address} onChange={(e) => setForm({ ...form, address: e.target.value })} />
            </div>
            <div>
              <label>المنطقة</label>
              <select value={form.zoneId} onChange={(e) => {
                const z = zones.data?.find((x) => x.id === e.target.value);
                setForm({ ...form, zoneId: e.target.value, deliveryFee: z?.fee ?? form.deliveryFee });
              }}>
                <option value="">— غير محدد —</option>
                {zones.data?.filter((z) => z.isActive).map((z) => <option key={z.id} value={z.id}>{z.name} ({formatMoney(z.fee)})</option>)}
              </select>
            </div>
            <div>
              <label>رسوم التوصيل</label>
              <input type="number" step="0.01" value={form.deliveryFee} onChange={(e) => setForm({ ...form, deliveryFee: Number(e.target.value) })} />
            </div>
            <div>
              <label>المبلغ المطلوب تحصيله (COD)</label>
              <input type="number" step="0.01" value={form.cashToCollect} onChange={(e) => setForm({ ...form, cashToCollect: Number(e.target.value) })} />
            </div>
            <div>
              <label>المندوب (اختياري)</label>
              <select value={form.driverId} onChange={(e) => setForm({ ...form, driverId: e.target.value })}>
                <option value="">— لم يُسند بعد —</option>
                {drivers.data?.map((d) => <option key={d.id} value={d.id}>{d.name} ({d.activeOrders})</option>)}
              </select>
            </div>
            <div className="md:col-span-3">
              <label>ملاحظات</label>
              <input value={form.notes} onChange={(e) => setForm({ ...form, notes: e.target.value })} />
            </div>
          </div>
          <div className="flex gap-2 mt-3">
            <button onClick={() => create.mutate()} disabled={!form.address.trim() || create.isPending} className="btn-success">
              {create.isPending ? "جاري..." : "إنشاء"}
            </button>
            <button onClick={() => setShow(false)} className="btn-outline">إلغاء</button>
          </div>
        </div>
      )}

      {assignFor && (
        <div className="card mb-4">
          <div className="flex items-center justify-between mb-3">
            <h3 className="font-semibold">إسناد طلب {assignFor.orderNumber}</h3>
            <button onClick={() => setAssignFor(null)} className="text-slate-400 hover:text-slate-700"><X size={20} /></button>
          </div>
          <div className="grid md:grid-cols-2 gap-2">
            {drivers.data?.map((d) => (
              <button
                key={d.id}
                onClick={() => {
                  action.mutate({ id: assignFor.id, op: "assign", body: { driverId: d.id } },
                    { onSuccess: () => setAssignFor(null) });
                }}
                className="flex items-center justify-between p-2 rounded border border-slate-200 hover:bg-slate-50"
              >
                <span className="font-medium">{d.name}</span>
                <span className="text-xs text-slate-500">{d.activeOrders} طلب نشط</span>
              </button>
            ))}
          </div>
        </div>
      )}

      {collectFor && (
        <div className="card mb-4">
          <div className="flex items-center justify-between mb-3">
            <h3 className="font-semibold">تأكيد تسليم {collectFor.orderNumber}</h3>
            <button onClick={() => setCollectFor(null)} className="text-slate-400 hover:text-slate-700"><X size={20} /></button>
          </div>
          <div className="grid md:grid-cols-2 gap-3">
            <div>
              <label>المبلغ المتوقع</label>
              <input value={formatMoney(collectFor.cashToCollect)} disabled />
            </div>
            <div>
              <label>المبلغ المُستلم فعلاً</label>
              <input type="number" step="0.01" value={collected} onChange={(e) => setCollected(Number(e.target.value))} />
            </div>
          </div>
          {collected !== collectFor.cashToCollect && (
            <div className="text-sm text-amber-700 mt-2">
              فرق: {formatMoney(collected - collectFor.cashToCollect)}
            </div>
          )}
          <div className="flex gap-2 mt-3">
            <button
              onClick={() => {
                action.mutate({ id: collectFor.id, op: "deliver", body: { cashCollected: collected } },
                  { onSuccess: () => setCollectFor(null) });
              }}
              className="btn-success"
            >
              تأكيد التسليم
            </button>
            <button onClick={() => setCollectFor(null)} className="btn-outline">إلغاء</button>
          </div>
        </div>
      )}

      <div className="table-wrap">
        <table>
          <thead>
            <tr>
              <th>الرقم</th>
              <th>العميل</th>
              <th>العنوان</th>
              <th>المنطقة</th>
              <th>المندوب</th>
              <th>للتحصيل</th>
              <th>الحالة</th>
              <th></th>
            </tr>
          </thead>
          <tbody>
            {orders.isLoading ? (
              Array.from({ length: 5 }).map((_, i) => <SkeletonRow key={i} cols={8} />)
            ) : orders.data?.length === 0 ? (
              <tr><td colSpan={8}>
                <EmptyState icon={Truck} title="لا توجد طلبات" description="ابدأ بإنشاء أول طلب توصيل." actionLabel="طلب جديد" onAction={() => setShow(true)} />
              </td></tr>
            ) : (
              orders.data?.map((o) => (
                <tr key={o.id}>
                  <td className="font-mono text-xs">{o.orderNumber}</td>
                  <td>
                    <div className="font-medium">{o.customerName ?? "—"}</div>
                    {o.customerPhone && <div className="text-xs text-slate-500">{o.customerPhone}</div>}
                  </td>
                  <td className="text-xs max-w-xs truncate" title={o.address}>{o.address}</td>
                  <td>{o.zoneName ?? "—"}</td>
                  <td>{o.driverName ?? <span className="text-amber-600 text-xs">غير مُسند</span>}</td>
                  <td className="font-semibold">{o.cashToCollect > 0 ? formatMoney(o.cashToCollect) : "—"}</td>
                  <td>
                    <span className={`text-xs px-2 py-0.5 rounded-full ${
                      o.status === 0 ? "bg-slate-100 text-slate-800"
                      : o.status === 1 ? "bg-blue-100 text-blue-800"
                      : o.status === 2 ? "bg-amber-100 text-amber-800"
                      : o.status === 3 ? "bg-emerald-100 text-emerald-800"
                      : "bg-red-100 text-red-700"
                    }`}>{DeliveryStatusLabel[o.status]}</span>
                  </td>
                  <td className="flex gap-1">
                    {(o.status === 0 || o.status === 1) && (
                      <button onClick={() => setAssignFor(o)} className="btn-outline !px-2 !py-1 text-xs">
                        {o.status === 0 ? "إسناد" : "تغيير المندوب"}
                      </button>
                    )}
                    {o.status === 1 && o.driverId && (
                      <button onClick={() => action.mutate({ id: o.id, op: "pickup" })} className="btn-outline !px-2 !py-1 text-xs text-amber-700">استلام</button>
                    )}
                    {(o.status === 2 || (o.status === 1 && o.driverId)) && (
                      <button
                        onClick={() => { setCollected(o.cashToCollect); setCollectFor(o); }}
                        className="btn-outline !px-2 !py-1 text-xs text-emerald-700"
                      >
                        <Check size={14} /> تسليم
                      </button>
                    )}
                    {o.status === 2 && (
                      <button onClick={() => action.mutate({ id: o.id, op: "return" })} className="btn-outline !px-2 !py-1 text-xs">مرتجع</button>
                    )}
                    {o.status !== 3 && o.status !== 4 && (
                      <button onClick={() => action.mutate({ id: o.id, op: "cancel" })} className="btn-outline !px-2 !py-1 text-xs">إلغاء</button>
                    )}
                    {o.status !== 3 && (
                      <button onClick={async () => { if (await confirm({ title: "حذف؟", message: o.orderNumber })) del.mutate(o.id); }} className="btn-outline !px-2 !py-1 text-xs text-red-600">
                        <Trash2 size={14} />
                      </button>
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

// ─── Drivers ────────────────────────────────────────────────────────────

function DriversTab() {
  const qc = useQueryClient();
  const { confirm, dialog } = useConfirm();
  const [show, setShow] = useState(false);
  const [form, setForm] = useState({
    id: "", name: "", phone: "", nationalId: "", vehicleType: 0,
    vehicleNumber: "", commissionPerDelivery: 0, isActive: true, notes: "",
  });

  const drivers = useQuery({
    queryKey: ["drivers", false],
    queryFn: async () => (await api.get<Driver[]>("/delivery/drivers")).data,
  });

  const save = useMutation({
    mutationFn: async () => {
      const { id, ...payload } = form;
      const body = {
        ...payload,
        phone: payload.phone || null,
        nationalId: payload.nationalId || null,
        vehicleNumber: payload.vehicleNumber || null,
        notes: payload.notes || null,
      };
      if (id) return (await api.put(`/delivery/drivers/${id}`, body)).data;
      return (await api.post("/delivery/drivers", body)).data;
    },
    onSuccess: () => {
      toast.success("تم الحفظ");
      setShow(false);
      setForm({ id: "", name: "", phone: "", nationalId: "", vehicleType: 0, vehicleNumber: "", commissionPerDelivery: 0, isActive: true, notes: "" });
      qc.invalidateQueries({ queryKey: ["drivers"] });
    },
    onError: (e) => toast.error(errorMessage(e)),
  });

  const del = useMutation({
    mutationFn: async (id: string) => (await api.delete(`/delivery/drivers/${id}`)).data,
    onSuccess: () => { toast.success("تم الحذف"); qc.invalidateQueries({ queryKey: ["drivers"] }); },
    onError: (e) => toast.error(errorMessage(e)),
  });

  function edit(d: Driver) {
    setForm({
      id: d.id, name: d.name, phone: d.phone ?? "", nationalId: d.nationalId ?? "",
      vehicleType: d.vehicleType, vehicleNumber: d.vehicleNumber ?? "",
      commissionPerDelivery: d.commissionPerDelivery, isActive: d.isActive,
      notes: d.notes ?? "",
    });
    setShow(true);
  }

  return (
    <>
      <div className="flex items-center justify-end mb-3">
        <button onClick={() => { setForm({ id: "", name: "", phone: "", nationalId: "", vehicleType: 0, vehicleNumber: "", commissionPerDelivery: 0, isActive: true, notes: "" }); setShow(true); }} className="btn">
          <Plus size={16} /> مندوب جديد
        </button>
      </div>

      {show && (
        <div className="card mb-4">
          <div className="flex items-center justify-between mb-3">
            <h3 className="font-semibold">{form.id ? "تعديل مندوب" : "مندوب جديد"}</h3>
            <button onClick={() => setShow(false)} className="text-slate-400 hover:text-slate-700"><X size={20} /></button>
          </div>
          <div className="grid md:grid-cols-3 gap-3">
            <div><label>الاسم *</label><input value={form.name} onChange={(e) => setForm({ ...form, name: e.target.value })} /></div>
            <div><label>الهاتف</label><input value={form.phone} onChange={(e) => setForm({ ...form, phone: e.target.value })} /></div>
            <div><label>الرقم القومي</label><input value={form.nationalId} onChange={(e) => setForm({ ...form, nationalId: e.target.value })} /></div>
            <div>
              <label>نوع المركبة</label>
              <select value={form.vehicleType} onChange={(e) => setForm({ ...form, vehicleType: Number(e.target.value) })}>
                {Object.entries(VehicleTypeLabel).map(([k, v]) => <option key={k} value={k}>{v}</option>)}
              </select>
            </div>
            <div><label>رقم المركبة</label><input value={form.vehicleNumber} onChange={(e) => setForm({ ...form, vehicleNumber: e.target.value })} /></div>
            <div><label>عمولة لكل طلب</label><input type="number" step="0.01" value={form.commissionPerDelivery} onChange={(e) => setForm({ ...form, commissionPerDelivery: Number(e.target.value) })} /></div>
            <label className="flex items-center gap-2 mt-6">
              <input type="checkbox" checked={form.isActive} onChange={(e) => setForm({ ...form, isActive: e.target.checked })} className="!w-auto" />
              <span>نشط</span>
            </label>
            <div className="md:col-span-3"><label>ملاحظات</label><input value={form.notes} onChange={(e) => setForm({ ...form, notes: e.target.value })} /></div>
          </div>
          <div className="flex gap-2 mt-3">
            <button onClick={() => save.mutate()} disabled={!form.name.trim() || save.isPending} className="btn-success">
              {save.isPending ? "جاري..." : "حفظ"}
            </button>
            <button onClick={() => setShow(false)} className="btn-outline">إلغاء</button>
          </div>
        </div>
      )}

      <div className="table-wrap">
        <table>
          <thead>
            <tr><th>الاسم</th><th>الهاتف</th><th>المركبة</th><th>عمولة/طلب</th><th>طلبات نشطة</th><th>الحالة</th><th></th></tr>
          </thead>
          <tbody>
            {drivers.isLoading ? (
              Array.from({ length: 4 }).map((_, i) => <SkeletonRow key={i} cols={7} />)
            ) : drivers.data?.length === 0 ? (
              <tr><td colSpan={7}>
                <EmptyState icon={Bike} title="لا يوجد مناديب" description="أضف أول مندوب توصيل." />
              </td></tr>
            ) : (
              drivers.data?.map((d) => (
                <tr key={d.id}>
                  <td className="font-medium">{d.name}</td>
                  <td>{d.phone ?? "—"}</td>
                  <td>{VehicleTypeLabel[d.vehicleType]} {d.vehicleNumber && `(${d.vehicleNumber})`}</td>
                  <td>{formatMoney(d.commissionPerDelivery)}</td>
                  <td>{d.activeOrders}</td>
                  <td>
                    <span className={`text-xs px-2 py-0.5 rounded-full ${d.isActive ? "bg-emerald-100 text-emerald-800" : "bg-slate-200 text-slate-600"}`}>
                      {d.isActive ? "نشط" : "متوقف"}
                    </span>
                  </td>
                  <td className="flex gap-1">
                    <button onClick={() => edit(d)} className="btn-outline !px-2 !py-1 text-xs"><Pencil size={14} /></button>
                    <button onClick={async () => { if (await confirm({ title: "حذف المندوب؟", message: d.name })) del.mutate(d.id); }} className="btn-outline !px-2 !py-1 text-xs text-red-600"><Trash2 size={14} /></button>
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

// ─── Zones ──────────────────────────────────────────────────────────────

function ZonesTab() {
  const qc = useQueryClient();
  const { confirm, dialog } = useConfirm();
  const [show, setShow] = useState(false);
  const [form, setForm] = useState({ id: "", name: "", fee: 0, estimatedMinutes: 30, isActive: true });

  const zones = useQuery({
    queryKey: ["delivery-zones"],
    queryFn: async () => (await api.get<DeliveryZone[]>("/delivery/zones")).data,
  });

  const save = useMutation({
    mutationFn: async () => {
      const { id, ...payload } = form;
      if (id) return (await api.put(`/delivery/zones/${id}`, payload)).data;
      return (await api.post("/delivery/zones", payload)).data;
    },
    onSuccess: () => {
      toast.success("تم الحفظ");
      setShow(false);
      setForm({ id: "", name: "", fee: 0, estimatedMinutes: 30, isActive: true });
      qc.invalidateQueries({ queryKey: ["delivery-zones"] });
    },
    onError: (e) => toast.error(errorMessage(e)),
  });

  const del = useMutation({
    mutationFn: async (id: string) => (await api.delete(`/delivery/zones/${id}`)).data,
    onSuccess: () => { toast.success("تم الحذف"); qc.invalidateQueries({ queryKey: ["delivery-zones"] }); },
    onError: (e) => toast.error(errorMessage(e)),
  });

  function edit(z: DeliveryZone) {
    setForm({ id: z.id, name: z.name, fee: z.fee, estimatedMinutes: z.estimatedMinutes, isActive: z.isActive });
    setShow(true);
  }

  return (
    <>
      <div className="flex items-center justify-end mb-3">
        <button onClick={() => { setForm({ id: "", name: "", fee: 0, estimatedMinutes: 30, isActive: true }); setShow(true); }} className="btn">
          <Plus size={16} /> منطقة جديدة
        </button>
      </div>

      {show && (
        <div className="card mb-4">
          <div className="flex items-center justify-between mb-3">
            <h3 className="font-semibold">{form.id ? "تعديل منطقة" : "منطقة جديدة"}</h3>
            <button onClick={() => setShow(false)} className="text-slate-400 hover:text-slate-700"><X size={20} /></button>
          </div>
          <div className="grid md:grid-cols-3 gap-3">
            <div><label>الاسم *</label><input value={form.name} onChange={(e) => setForm({ ...form, name: e.target.value })} /></div>
            <div><label>رسوم التوصيل</label><input type="number" step="0.01" value={form.fee} onChange={(e) => setForm({ ...form, fee: Number(e.target.value) })} /></div>
            <div><label>الوقت المتوقع (دقيقة)</label><input type="number" value={form.estimatedMinutes} onChange={(e) => setForm({ ...form, estimatedMinutes: Number(e.target.value) })} /></div>
            <label className="flex items-center gap-2 mt-6">
              <input type="checkbox" checked={form.isActive} onChange={(e) => setForm({ ...form, isActive: e.target.checked })} className="!w-auto" />
              <span>نشطة</span>
            </label>
          </div>
          <div className="flex gap-2 mt-3">
            <button onClick={() => save.mutate()} disabled={!form.name.trim() || save.isPending} className="btn-success">
              {save.isPending ? "جاري..." : "حفظ"}
            </button>
            <button onClick={() => setShow(false)} className="btn-outline">إلغاء</button>
          </div>
        </div>
      )}

      <div className="table-wrap">
        <table>
          <thead><tr><th>الاسم</th><th>رسوم</th><th>الوقت المتوقع</th><th>الحالة</th><th></th></tr></thead>
          <tbody>
            {zones.isLoading ? (
              Array.from({ length: 3 }).map((_, i) => <SkeletonRow key={i} cols={5} />)
            ) : zones.data?.length === 0 ? (
              <tr><td colSpan={5}>
                <EmptyState icon={MapPin} title="لا توجد مناطق" description="أضف مناطق التوصيل لتسريع إنشاء الطلبات." />
              </td></tr>
            ) : (
              zones.data?.map((z) => (
                <tr key={z.id}>
                  <td className="font-medium">{z.name}</td>
                  <td>{formatMoney(z.fee)}</td>
                  <td>{z.estimatedMinutes} د</td>
                  <td>
                    <span className={`text-xs px-2 py-0.5 rounded-full ${z.isActive ? "bg-emerald-100 text-emerald-800" : "bg-slate-200 text-slate-600"}`}>
                      {z.isActive ? "نشطة" : "متوقفة"}
                    </span>
                  </td>
                  <td className="flex gap-1">
                    <button onClick={() => edit(z)} className="btn-outline !px-2 !py-1 text-xs"><Pencil size={14} /></button>
                    <button onClick={async () => { if (await confirm({ title: "حذف المنطقة؟", message: z.name })) del.mutate(z.id); }} className="btn-outline !px-2 !py-1 text-xs text-red-600"><Trash2 size={14} /></button>
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
