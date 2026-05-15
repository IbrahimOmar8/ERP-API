"use client";

import { useEffect, useState } from "react";
import { useMutation, useQuery, useQueryClient } from "@tanstack/react-query";
import toast from "react-hot-toast";
import { Pencil, Plus, Trash2 } from "lucide-react";
import { api, errorMessage } from "@/lib/api";
import { formatMoney, formatNumber } from "@/lib/format";
import type { Category, Product, Unit } from "@/types/api";
import PageHeader from "@/components/PageHeader";
import Modal from "@/components/Modal";
import { useConfirm } from "@/components/ConfirmDialog";
import ImageUpload from "@/components/ImageUpload";

interface Form {
  id?: string;
  sku: string;
  barcode: string;
  nameAr: string;
  nameEn: string;
  description: string;
  categoryId: string;
  unitId: string;
  purchasePrice: number;
  salePrice: number;
  minSalePrice: number;
  vatRate: number;
  itemCode: string;
  gs1Code: string;
  minStockLevel: number;
  maxStockLevel: number;
  trackStock: boolean;
  isActive: boolean;
  imageUrl: string | null;
}

const emptyForm: Form = {
  sku: "",
  barcode: "",
  nameAr: "",
  nameEn: "",
  description: "",
  categoryId: "",
  unitId: "",
  purchasePrice: 0,
  salePrice: 0,
  minSalePrice: 0,
  vatRate: 14,
  itemCode: "",
  gs1Code: "",
  minStockLevel: 0,
  maxStockLevel: 0,
  trackStock: true,
  isActive: true,
  imageUrl: null,
};

export default function ProductsPage() {
  const qc = useQueryClient();
  const { confirm, dialog } = useConfirm();
  const [search, setSearch] = useState("");
  const [open, setOpen] = useState(false);
  const [form, setForm] = useState<Form>(emptyForm);

  const products = useQuery({
    queryKey: ["products", search],
    queryFn: async () =>
      (await api.get<Product[]>("/Products", {
        params: { search: search || undefined, pageSize: 300 },
      })).data,
  });

  const categories = useQuery({
    queryKey: ["categories"],
    queryFn: async () => (await api.get<Category[]>("/Categories")).data,
  });

  const units = useQuery({
    queryKey: ["units"],
    queryFn: async () => (await api.get<Unit[]>("/Units")).data,
  });

  // pick sensible defaults when opening "new" form
  useEffect(() => {
    if (open && !form.id) {
      setForm((f) => ({
        ...f,
        categoryId: f.categoryId || categories.data?.[0]?.id || "",
        unitId: f.unitId || units.data?.[0]?.id || "",
      }));
    }
  }, [open, categories.data, units.data, form.id]);

  const save = useMutation({
    mutationFn: async () => {
      const { id, ...payload } = form;
      if (id) return (await api.put(`/Products/${id}`, payload)).data;
      return (await api.post("/Products", payload)).data;
    },
    onSuccess: () => {
      toast.success("تم الحفظ");
      setOpen(false);
      setForm(emptyForm);
      qc.invalidateQueries({ queryKey: ["products"] });
    },
    onError: (e) => toast.error(errorMessage(e)),
  });

  const del = useMutation({
    mutationFn: async (id: string) => api.delete(`/Products/${id}`),
    onSuccess: () => {
      toast.success("تم الحذف");
      qc.invalidateQueries({ queryKey: ["products"] });
    },
    onError: (e) => toast.error(errorMessage(e)),
  });

  function edit(p: Product) {
    setForm({
      id: p.id,
      sku: p.sku,
      barcode: p.barcode ?? "",
      nameAr: p.nameAr,
      nameEn: p.nameEn ?? "",
      description: "",
      categoryId: p.categoryId,
      unitId: p.unitId,
      purchasePrice: p.purchasePrice,
      salePrice: p.salePrice,
      minSalePrice: p.minSalePrice,
      vatRate: p.vatRate,
      itemCode: "",
      gs1Code: "",
      minStockLevel: p.minStockLevel,
      maxStockLevel: p.maxStockLevel,
      trackStock: p.trackStock,
      isActive: p.isActive,
      imageUrl: p.imageUrl ?? null,
    });
    setOpen(true);
  }

  function newForm() {
    setForm(emptyForm);
    setOpen(true);
  }

  async function remove(p: Product) {
    if (await confirm("حذف الصنف", `هل أنت متأكد من حذف "${p.nameAr}"؟`)) {
      del.mutate(p.id);
    }
  }

  return (
    <>
      <PageHeader title="الأصناف" description="إدارة الأصناف وأسعار البيع والمخزون">
        <button onClick={newForm} className="btn">
          <Plus size={16} /> صنف جديد
        </button>
      </PageHeader>

      <div className="card mb-3">
        <input
          placeholder="ابحث بالاسم أو SKU أو الباركود..."
          value={search}
          onChange={(e) => setSearch(e.target.value)}
        />
      </div>

      <div className="table-wrap">
        <table>
          <thead>
            <tr>
              <th></th>
              <th>الكود</th>
              <th>الباركود</th>
              <th>الاسم</th>
              <th>الفئة</th>
              <th>سعر البيع</th>
              <th>الضريبة</th>
              <th>الرصيد</th>
              <th></th>
            </tr>
          </thead>
          <tbody>
            {products.isLoading ? (
              <tr><td colSpan={9} className="text-center py-6">جاري التحميل...</td></tr>
            ) : (
              products.data?.map((p) => {
                const apiOrigin = (process.env.NEXT_PUBLIC_API_URL || "http://localhost:5000/api").replace(/\/api\/?$/, "");
                const img = p.imageUrl
                  ? (p.imageUrl.startsWith("http") ? p.imageUrl : `${apiOrigin}${p.imageUrl}`)
                  : null;
                return (
                <tr key={p.id} className={!p.isActive ? "opacity-50" : ""}>
                  <td>
                    {img ? (
                      <img src={img} alt="" className="w-10 h-10 rounded object-cover" />
                    ) : (
                      <div className="w-10 h-10 rounded bg-slate-100 dark:bg-slate-800" />
                    )}
                  </td>
                  <td className="font-mono text-xs">{p.sku}</td>
                  <td className="font-mono text-xs">{p.barcode}</td>
                  <td className="font-medium">{p.nameAr}</td>
                  <td>{p.categoryName}</td>
                  <td>{formatMoney(p.salePrice)}</td>
                  <td>{p.vatRate.toFixed(1)}%</td>
                  <td className={p.currentStock <= p.minStockLevel ? "text-red-600 font-semibold" : ""}>
                    {formatNumber(p.currentStock)}
                  </td>
                  <td className="flex gap-1">
                    <button onClick={() => edit(p)} className="btn-outline !px-2 !py-1 text-xs">
                      <Pencil size={14} /> تعديل
                    </button>
                    <button onClick={() => remove(p)} className="btn-danger !px-2 !py-1 text-xs">
                      <Trash2 size={14} />
                    </button>
                  </td>
                </tr>
                );
              })
            )}
          </tbody>
        </table>
      </div>

      <Modal open={open} title={form.id ? "تعديل صنف" : "صنف جديد"} onClose={() => setOpen(false)} size="lg">
        <div className="mb-4">
          <label>صورة الصنف</label>
          <ImageUpload value={form.imageUrl} onChange={(url) => setForm({ ...form, imageUrl: url })} />
        </div>
        <div className="grid md:grid-cols-3 gap-3">
          <div>
            <label>الكود (SKU) *</label>
            <input value={form.sku} onChange={(e) => setForm({ ...form, sku: e.target.value })} />
          </div>
          <div>
            <label>الباركود</label>
            <input value={form.barcode} onChange={(e) => setForm({ ...form, barcode: e.target.value })} />
          </div>
          <div className="md:col-span-1" />
          <div className="md:col-span-2">
            <label>الاسم بالعربية *</label>
            <input value={form.nameAr} onChange={(e) => setForm({ ...form, nameAr: e.target.value })} />
          </div>
          <div>
            <label>الاسم بالإنجليزية</label>
            <input value={form.nameEn} onChange={(e) => setForm({ ...form, nameEn: e.target.value })} />
          </div>
          <div>
            <label>الفئة *</label>
            <select value={form.categoryId} onChange={(e) => setForm({ ...form, categoryId: e.target.value })}>
              <option value="">اختر</option>
              {categories.data?.map((c) => (
                <option key={c.id} value={c.id}>{c.nameAr}</option>
              ))}
            </select>
          </div>
          <div>
            <label>الوحدة *</label>
            <select value={form.unitId} onChange={(e) => setForm({ ...form, unitId: e.target.value })}>
              <option value="">اختر</option>
              {units.data?.map((u) => (
                <option key={u.id} value={u.id}>{u.nameAr}</option>
              ))}
            </select>
          </div>
          <div>
            <label>ضريبة %</label>
            <input
              type="number"
              step="0.01"
              value={form.vatRate}
              onChange={(e) => setForm({ ...form, vatRate: Number(e.target.value) })}
            />
          </div>
          <div>
            <label>سعر الشراء</label>
            <input
              type="number"
              step="0.01"
              value={form.purchasePrice}
              onChange={(e) => setForm({ ...form, purchasePrice: Number(e.target.value) })}
            />
          </div>
          <div>
            <label>سعر البيع *</label>
            <input
              type="number"
              step="0.01"
              value={form.salePrice}
              onChange={(e) => setForm({ ...form, salePrice: Number(e.target.value) })}
            />
          </div>
          <div>
            <label>أقل سعر بيع</label>
            <input
              type="number"
              step="0.01"
              value={form.minSalePrice}
              onChange={(e) => setForm({ ...form, minSalePrice: Number(e.target.value) })}
            />
          </div>
          <div>
            <label>الحد الأدنى للمخزون</label>
            <input
              type="number"
              step="0.01"
              value={form.minStockLevel}
              onChange={(e) => setForm({ ...form, minStockLevel: Number(e.target.value) })}
            />
          </div>
          <div>
            <label>الحد الأقصى للمخزون</label>
            <input
              type="number"
              step="0.01"
              value={form.maxStockLevel}
              onChange={(e) => setForm({ ...form, maxStockLevel: Number(e.target.value) })}
            />
          </div>
          <label className="flex items-center gap-2 mt-6">
            <input
              type="checkbox"
              checked={form.trackStock}
              onChange={(e) => setForm({ ...form, trackStock: e.target.checked })}
              className="!w-auto"
            />
            <span>تتبع المخزون</span>
          </label>
          {form.id && (
            <label className="flex items-center gap-2 mt-6">
              <input
                type="checkbox"
                checked={form.isActive}
                onChange={(e) => setForm({ ...form, isActive: e.target.checked })}
                className="!w-auto"
              />
              <span>نشط</span>
            </label>
          )}
        </div>
        <div className="flex justify-end gap-2 mt-5 pt-4 border-t border-slate-200">
          <button onClick={() => setOpen(false)} className="btn-outline">إلغاء</button>
          <button
            onClick={() => save.mutate()}
            disabled={!form.nameAr.trim() || !form.sku.trim() || !form.categoryId || !form.unitId || save.isPending}
            className="btn-success"
          >
            {save.isPending ? "جاري الحفظ..." : "حفظ"}
          </button>
        </div>
      </Modal>
      {dialog}
    </>
  );
}
