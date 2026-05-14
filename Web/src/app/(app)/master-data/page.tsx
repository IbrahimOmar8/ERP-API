"use client";

import { useState } from "react";
import { useMutation, useQuery, useQueryClient } from "@tanstack/react-query";
import toast from "react-hot-toast";
import { Pencil, Plus, Trash2 } from "lucide-react";
import { api, errorMessage } from "@/lib/api";
import type { Category, Unit, Warehouse } from "@/types/api";
import PageHeader from "@/components/PageHeader";
import Modal from "@/components/Modal";
import { useConfirm } from "@/components/ConfirmDialog";

type Tab = "categories" | "units" | "warehouses";

export default function MasterDataPage() {
  const [tab, setTab] = useState<Tab>("categories");
  return (
    <>
      <PageHeader title="البيانات الأساسية" description="الفئات، الوحدات، المخازن" />
      <div className="flex gap-2 mb-4 border-b border-slate-200">
        <TabButton active={tab === "categories"} onClick={() => setTab("categories")}>الفئات</TabButton>
        <TabButton active={tab === "units"} onClick={() => setTab("units")}>الوحدات</TabButton>
        <TabButton active={tab === "warehouses"} onClick={() => setTab("warehouses")}>المخازن</TabButton>
      </div>
      {tab === "categories" && <CategoriesTab />}
      {tab === "units" && <UnitsTab />}
      {tab === "warehouses" && <WarehousesTab />}
    </>
  );
}

function TabButton({ active, onClick, children }: { active: boolean; onClick: () => void; children: React.ReactNode }) {
  return (
    <button
      onClick={onClick}
      className={`px-4 py-2 text-sm font-medium border-b-2 -mb-px transition ${
        active ? "border-brand text-brand" : "border-transparent text-slate-500 hover:text-slate-700"
      }`}
    >
      {children}
    </button>
  );
}

// ─────────────────────────── Categories ───────────────────────────

interface CatForm { id?: string; nameAr: string; nameEn: string; parentCategoryId: string; isActive: boolean; }
const emptyCat: CatForm = { nameAr: "", nameEn: "", parentCategoryId: "", isActive: true };

function CategoriesTab() {
  const qc = useQueryClient();
  const { confirm, dialog } = useConfirm();
  const [open, setOpen] = useState(false);
  const [form, setForm] = useState<CatForm>(emptyCat);

  const { data, isLoading } = useQuery({
    queryKey: ["categories"],
    queryFn: async () => (await api.get<Category[]>("/Categories")).data,
  });

  const save = useMutation({
    mutationFn: async () => {
      const { id, parentCategoryId, ...rest } = form;
      const payload = { ...rest, parentCategoryId: parentCategoryId || null };
      if (id) return (await api.put(`/Categories/${id}`, payload)).data;
      return (await api.post("/Categories", payload)).data;
    },
    onSuccess: () => {
      toast.success("تم الحفظ");
      setOpen(false);
      qc.invalidateQueries({ queryKey: ["categories"] });
    },
    onError: (e) => toast.error(errorMessage(e)),
  });

  const del = useMutation({
    mutationFn: async (id: string) => api.delete(`/Categories/${id}`),
    onSuccess: () => {
      toast.success("تم الحذف");
      qc.invalidateQueries({ queryKey: ["categories"] });
    },
    onError: (e) => toast.error(errorMessage(e)),
  });

  function edit(c: Category) {
    setForm({
      id: c.id,
      nameAr: c.nameAr,
      nameEn: c.nameEn ?? "",
      parentCategoryId: c.parentCategoryId ?? "",
      isActive: c.isActive,
    });
    setOpen(true);
  }

  async function remove(c: Category) {
    if (await confirm("حذف الفئة", `حذف "${c.nameAr}"؟`)) del.mutate(c.id);
  }

  return (
    <>
      <div className="flex justify-end mb-3">
        <button onClick={() => { setForm(emptyCat); setOpen(true); }} className="btn">
          <Plus size={16} /> فئة جديدة
        </button>
      </div>
      <div className="table-wrap">
        <table>
          <thead><tr><th>الاسم</th><th>الاسم بالإنجليزية</th><th>الفئة الأب</th><th>الحالة</th><th></th></tr></thead>
          <tbody>
            {isLoading ? <tr><td colSpan={5} className="text-center py-6">جاري التحميل...</td></tr> :
              data?.map((c) => (
                <tr key={c.id}>
                  <td className="font-medium">{c.nameAr}</td>
                  <td>{c.nameEn}</td>
                  <td>{c.parentName}</td>
                  <td>{c.isActive ? "نشط" : "غير نشط"}</td>
                  <td className="flex gap-1">
                    <button onClick={() => edit(c)} className="btn-outline !px-2 !py-1 text-xs"><Pencil size={14} /></button>
                    <button onClick={() => remove(c)} className="btn-danger !px-2 !py-1 text-xs"><Trash2 size={14} /></button>
                  </td>
                </tr>
              ))}
          </tbody>
        </table>
      </div>

      <Modal open={open} title={form.id ? "تعديل فئة" : "فئة جديدة"} onClose={() => setOpen(false)}>
        <div className="space-y-3">
          <div><label>الاسم بالعربية *</label><input value={form.nameAr} onChange={(e) => setForm({ ...form, nameAr: e.target.value })} /></div>
          <div><label>الاسم بالإنجليزية</label><input value={form.nameEn} onChange={(e) => setForm({ ...form, nameEn: e.target.value })} /></div>
          <div>
            <label>الفئة الأب</label>
            <select value={form.parentCategoryId} onChange={(e) => setForm({ ...form, parentCategoryId: e.target.value })}>
              <option value="">— لا يوجد —</option>
              {data?.filter((c) => c.id !== form.id).map((c) => (
                <option key={c.id} value={c.id}>{c.nameAr}</option>
              ))}
            </select>
          </div>
          {form.id && (
            <label className="flex items-center gap-2">
              <input type="checkbox" checked={form.isActive} onChange={(e) => setForm({ ...form, isActive: e.target.checked })} className="!w-auto" />
              <span>نشط</span>
            </label>
          )}
        </div>
        <div className="flex justify-end gap-2 mt-4">
          <button onClick={() => setOpen(false)} className="btn-outline">إلغاء</button>
          <button onClick={() => save.mutate()} disabled={!form.nameAr.trim() || save.isPending} className="btn-success">
            {save.isPending ? "..." : "حفظ"}
          </button>
        </div>
      </Modal>
      {dialog}
    </>
  );
}

// ─────────────────────────── Units ───────────────────────────

interface UnitForm { id?: string; nameAr: string; nameEn: string; code: string; }
const emptyUnit: UnitForm = { nameAr: "", nameEn: "", code: "" };

function UnitsTab() {
  const qc = useQueryClient();
  const { confirm, dialog } = useConfirm();
  const [open, setOpen] = useState(false);
  const [form, setForm] = useState<UnitForm>(emptyUnit);

  const { data, isLoading } = useQuery({
    queryKey: ["units"],
    queryFn: async () => (await api.get<Unit[]>("/Units")).data,
  });

  const save = useMutation({
    mutationFn: async () => {
      const { id, ...payload } = form;
      // UnitsController only supports POST (create) and DELETE; no PUT — so update via re-create not possible
      if (id) throw new Error("تعديل الوحدات غير متاح حالياً");
      return (await api.post("/Units", payload)).data;
    },
    onSuccess: () => {
      toast.success("تم الحفظ");
      setOpen(false);
      qc.invalidateQueries({ queryKey: ["units"] });
    },
    onError: (e) => toast.error(errorMessage(e)),
  });

  const del = useMutation({
    mutationFn: async (id: string) => api.delete(`/Units/${id}`),
    onSuccess: () => {
      toast.success("تم الحذف");
      qc.invalidateQueries({ queryKey: ["units"] });
    },
    onError: (e) => toast.error(errorMessage(e)),
  });

  async function remove(u: Unit) {
    if (await confirm("حذف الوحدة", `حذف "${u.nameAr}"؟`)) del.mutate(u.id);
  }

  return (
    <>
      <div className="flex justify-end mb-3">
        <button onClick={() => { setForm(emptyUnit); setOpen(true); }} className="btn">
          <Plus size={16} /> وحدة جديدة
        </button>
      </div>
      <div className="table-wrap">
        <table>
          <thead><tr><th>الاسم</th><th>الكود</th><th>الحالة</th><th></th></tr></thead>
          <tbody>
            {isLoading ? <tr><td colSpan={4} className="text-center py-6">جاري التحميل...</td></tr> :
              data?.map((u) => (
                <tr key={u.id}>
                  <td className="font-medium">{u.nameAr}</td>
                  <td className="font-mono">{u.code}</td>
                  <td>{u.isActive ? "نشط" : "غير نشط"}</td>
                  <td>
                    <button onClick={() => remove(u)} className="btn-danger !px-2 !py-1 text-xs"><Trash2 size={14} /></button>
                  </td>
                </tr>
              ))}
          </tbody>
        </table>
      </div>

      <Modal open={open} title="وحدة جديدة" onClose={() => setOpen(false)}>
        <div className="space-y-3">
          <div><label>الاسم بالعربية *</label><input value={form.nameAr} onChange={(e) => setForm({ ...form, nameAr: e.target.value })} /></div>
          <div><label>الاسم بالإنجليزية</label><input value={form.nameEn} onChange={(e) => setForm({ ...form, nameEn: e.target.value })} /></div>
          <div><label>الكود *</label><input value={form.code} onChange={(e) => setForm({ ...form, code: e.target.value })} /></div>
        </div>
        <div className="flex justify-end gap-2 mt-4">
          <button onClick={() => setOpen(false)} className="btn-outline">إلغاء</button>
          <button onClick={() => save.mutate()} disabled={!form.nameAr.trim() || !form.code.trim() || save.isPending} className="btn-success">
            {save.isPending ? "..." : "حفظ"}
          </button>
        </div>
      </Modal>
      {dialog}
    </>
  );
}

// ─────────────────────────── Warehouses ───────────────────────────

interface WhForm { id?: string; nameAr: string; nameEn: string; code: string; address: string; phone: string; isMain: boolean; }
const emptyWh: WhForm = { nameAr: "", nameEn: "", code: "", address: "", phone: "", isMain: false };

function WarehousesTab() {
  const qc = useQueryClient();
  const { confirm, dialog } = useConfirm();
  const [open, setOpen] = useState(false);
  const [form, setForm] = useState<WhForm>(emptyWh);

  const { data, isLoading } = useQuery({
    queryKey: ["warehouses"],
    queryFn: async () => (await api.get<Warehouse[]>("/Warehouses")).data,
  });

  const save = useMutation({
    mutationFn: async () => {
      const { id, ...payload } = form;
      if (id) return (await api.put(`/Warehouses/${id}`, payload)).data;
      return (await api.post("/Warehouses", payload)).data;
    },
    onSuccess: () => {
      toast.success("تم الحفظ");
      setOpen(false);
      qc.invalidateQueries({ queryKey: ["warehouses"] });
    },
    onError: (e) => toast.error(errorMessage(e)),
  });

  const del = useMutation({
    mutationFn: async (id: string) => api.delete(`/Warehouses/${id}`),
    onSuccess: () => {
      toast.success("تم الحذف");
      qc.invalidateQueries({ queryKey: ["warehouses"] });
    },
    onError: (e) => toast.error(errorMessage(e)),
  });

  function edit(w: Warehouse) {
    setForm({
      id: w.id,
      nameAr: w.nameAr,
      nameEn: w.nameEn ?? "",
      code: w.code,
      address: "",
      phone: "",
      isMain: w.isMain,
    });
    setOpen(true);
  }

  async function remove(w: Warehouse) {
    if (await confirm("حذف المخزن", `حذف "${w.nameAr}"؟`)) del.mutate(w.id);
  }

  return (
    <>
      <div className="flex justify-end mb-3">
        <button onClick={() => { setForm(emptyWh); setOpen(true); }} className="btn">
          <Plus size={16} /> مخزن جديد
        </button>
      </div>
      <div className="table-wrap">
        <table>
          <thead><tr><th>الاسم</th><th>الكود</th><th>رئيسي</th><th>الحالة</th><th></th></tr></thead>
          <tbody>
            {isLoading ? <tr><td colSpan={5} className="text-center py-6">جاري التحميل...</td></tr> :
              data?.map((w) => (
                <tr key={w.id}>
                  <td className="font-medium">{w.nameAr}</td>
                  <td className="font-mono">{w.code}</td>
                  <td>{w.isMain ? "✓" : ""}</td>
                  <td>{w.isActive ? "نشط" : "غير نشط"}</td>
                  <td className="flex gap-1">
                    <button onClick={() => edit(w)} className="btn-outline !px-2 !py-1 text-xs"><Pencil size={14} /></button>
                    <button onClick={() => remove(w)} className="btn-danger !px-2 !py-1 text-xs"><Trash2 size={14} /></button>
                  </td>
                </tr>
              ))}
          </tbody>
        </table>
      </div>

      <Modal open={open} title={form.id ? "تعديل مخزن" : "مخزن جديد"} onClose={() => setOpen(false)}>
        <div className="space-y-3">
          <div><label>الاسم بالعربية *</label><input value={form.nameAr} onChange={(e) => setForm({ ...form, nameAr: e.target.value })} /></div>
          <div><label>الاسم بالإنجليزية</label><input value={form.nameEn} onChange={(e) => setForm({ ...form, nameEn: e.target.value })} /></div>
          <div><label>الكود *</label><input value={form.code} onChange={(e) => setForm({ ...form, code: e.target.value })} /></div>
          <div><label>العنوان</label><input value={form.address} onChange={(e) => setForm({ ...form, address: e.target.value })} /></div>
          <div><label>الهاتف</label><input value={form.phone} onChange={(e) => setForm({ ...form, phone: e.target.value })} /></div>
          <label className="flex items-center gap-2">
            <input type="checkbox" checked={form.isMain} onChange={(e) => setForm({ ...form, isMain: e.target.checked })} className="!w-auto" />
            <span>مخزن رئيسي</span>
          </label>
        </div>
        <div className="flex justify-end gap-2 mt-4">
          <button onClick={() => setOpen(false)} className="btn-outline">إلغاء</button>
          <button onClick={() => save.mutate()} disabled={!form.nameAr.trim() || !form.code.trim() || save.isPending} className="btn-success">
            {save.isPending ? "..." : "حفظ"}
          </button>
        </div>
      </Modal>
      {dialog}
    </>
  );
}
