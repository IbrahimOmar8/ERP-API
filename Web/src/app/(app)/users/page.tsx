"use client";

import { useState } from "react";
import { useMutation, useQuery, useQueryClient } from "@tanstack/react-query";
import toast from "react-hot-toast";
import { Pencil, Plus, Trash2, Power } from "lucide-react";
import { api, errorMessage } from "@/lib/api";
import { Roles } from "@/lib/auth";
import type { ApiUser, Warehouse } from "@/types/api";
import PageHeader from "@/components/PageHeader";
import Modal from "@/components/Modal";
import { useConfirm } from "@/components/ConfirmDialog";

interface Form {
  id?: string;
  userName: string;
  fullName: string;
  email: string;
  phone: string;
  password: string;
  defaultWarehouseId: string;
  roles: string[];
}

const emptyForm: Form = {
  userName: "",
  fullName: "",
  email: "",
  phone: "",
  password: "",
  defaultWarehouseId: "",
  roles: [Roles.Cashier],
};

const ALL_ROLES = [Roles.Admin, Roles.Manager, Roles.Cashier, Roles.WarehouseKeeper, Roles.Accountant];

export default function UsersPage() {
  const qc = useQueryClient();
  const { confirm, dialog } = useConfirm();
  const [open, setOpen] = useState(false);
  const [form, setForm] = useState<Form>(emptyForm);

  const users = useQuery({
    queryKey: ["users"],
    queryFn: async () => (await api.get<ApiUser[]>("/Users")).data,
  });
  const warehouses = useQuery({
    queryKey: ["warehouses"],
    queryFn: async () => (await api.get<Warehouse[]>("/Warehouses")).data,
  });

  const save = useMutation({
    mutationFn: async () => {
      const payload = {
        userName: form.userName,
        fullName: form.fullName,
        email: form.email || null,
        phone: form.phone || null,
        password: form.password,
        defaultWarehouseId: form.defaultWarehouseId || null,
        roles: form.roles,
      };
      if (form.id) return (await api.put(`/Users/${form.id}`, payload)).data;
      return (await api.post("/Auth/register", payload)).data;
    },
    onSuccess: () => {
      toast.success("تم الحفظ");
      setOpen(false);
      setForm(emptyForm);
      qc.invalidateQueries({ queryKey: ["users"] });
    },
    onError: (e) => toast.error(errorMessage(e)),
  });

  const del = useMutation({
    mutationFn: async (id: string) => api.delete(`/Users/${id}`),
    onSuccess: () => {
      toast.success("تم الحذف");
      qc.invalidateQueries({ queryKey: ["users"] });
    },
    onError: (e) => toast.error(errorMessage(e)),
  });

  const toggleActive = useMutation({
    mutationFn: async ({ id, active }: { id: string; active: boolean }) =>
      api.post(`/Users/${id}/active/${active}`, {}),
    onSuccess: () => qc.invalidateQueries({ queryKey: ["users"] }),
    onError: (e) => toast.error(errorMessage(e)),
  });

  function edit(u: ApiUser) {
    setForm({
      id: u.id,
      userName: u.userName,
      fullName: u.fullName,
      email: u.email ?? "",
      phone: u.phone ?? "",
      password: "",
      defaultWarehouseId: u.defaultWarehouseId ?? "",
      roles: u.roles,
    });
    setOpen(true);
  }

  async function remove(u: ApiUser) {
    if (await confirm("حذف المستخدم", `حذف "${u.fullName}"؟`)) del.mutate(u.id);
  }

  function toggleRole(r: string) {
    setForm((f) => ({
      ...f,
      roles: f.roles.includes(r) ? f.roles.filter((x) => x !== r) : [...f.roles, r],
    }));
  }

  return (
    <>
      <PageHeader title="المستخدمون" description="إدارة المستخدمين والصلاحيات">
        <button onClick={() => { setForm(emptyForm); setOpen(true); }} className="btn">
          <Plus size={16} /> مستخدم جديد
        </button>
      </PageHeader>

      <div className="table-wrap">
        <table>
          <thead>
            <tr>
              <th>اسم المستخدم</th>
              <th>الاسم الكامل</th>
              <th>البريد</th>
              <th>الأدوار</th>
              <th>الحالة</th>
              <th></th>
            </tr>
          </thead>
          <tbody>
            {users.isLoading ? <tr><td colSpan={6} className="text-center py-6">جاري التحميل...</td></tr> :
              users.data?.map((u) => (
                <tr key={u.id} className={!u.isActive ? "opacity-60" : ""}>
                  <td className="font-mono text-xs">{u.userName}</td>
                  <td className="font-medium">{u.fullName}</td>
                  <td>{u.email}</td>
                  <td>
                    <div className="flex flex-wrap gap-1">
                      {u.roles.map((r) => (
                        <span key={r} className="text-xs px-2 py-0.5 rounded-full bg-blue-100 text-blue-800">{r}</span>
                      ))}
                    </div>
                  </td>
                  <td>{u.isActive ? "🟢 نشط" : "⚫ معطل"}</td>
                  <td className="flex gap-1">
                    <button onClick={() => edit(u)} className="btn-outline !px-2 !py-1 text-xs">
                      <Pencil size={14} /> تعديل
                    </button>
                    <button
                      onClick={() => toggleActive.mutate({ id: u.id, active: !u.isActive })}
                      className="btn-outline !px-2 !py-1 text-xs"
                      title={u.isActive ? "تعطيل" : "تفعيل"}
                    >
                      <Power size={14} />
                    </button>
                    <button onClick={() => remove(u)} className="btn-danger !px-2 !py-1 text-xs">
                      <Trash2 size={14} />
                    </button>
                  </td>
                </tr>
              ))}
          </tbody>
        </table>
      </div>

      <Modal open={open} title={form.id ? "تعديل مستخدم" : "مستخدم جديد"} onClose={() => setOpen(false)} size="lg">
        <div className="grid md:grid-cols-2 gap-3">
          <div>
            <label>اسم المستخدم *</label>
            <input value={form.userName} onChange={(e) => setForm({ ...form, userName: e.target.value })} disabled={!!form.id} />
          </div>
          <div>
            <label>الاسم الكامل *</label>
            <input value={form.fullName} onChange={(e) => setForm({ ...form, fullName: e.target.value })} />
          </div>
          <div>
            <label>البريد الإلكتروني</label>
            <input value={form.email} onChange={(e) => setForm({ ...form, email: e.target.value })} />
          </div>
          <div>
            <label>الهاتف</label>
            <input value={form.phone} onChange={(e) => setForm({ ...form, phone: e.target.value })} />
          </div>
          <div className="md:col-span-2">
            <label>{form.id ? "كلمة المرور (اتركها فارغة لعدم تغييرها)" : "كلمة المرور *"}</label>
            <input type="password" value={form.password} onChange={(e) => setForm({ ...form, password: e.target.value })} />
          </div>
          <div className="md:col-span-2">
            <label>المخزن الافتراضي</label>
            <select value={form.defaultWarehouseId} onChange={(e) => setForm({ ...form, defaultWarehouseId: e.target.value })}>
              <option value="">— لا يوجد —</option>
              {warehouses.data?.map((w) => <option key={w.id} value={w.id}>{w.nameAr}</option>)}
            </select>
          </div>
          <div className="md:col-span-2">
            <label>الأدوار</label>
            <div className="flex flex-wrap gap-2">
              {ALL_ROLES.map((r) => (
                <label
                  key={r}
                  className={`px-3 py-1 rounded-full text-sm cursor-pointer border ${
                    form.roles.includes(r)
                      ? "bg-brand text-white border-brand"
                      : "bg-white text-slate-700 border-slate-300"
                  }`}
                >
                  <input
                    type="checkbox"
                    checked={form.roles.includes(r)}
                    onChange={() => toggleRole(r)}
                    className="hidden"
                  />
                  {r}
                </label>
              ))}
            </div>
          </div>
        </div>
        <div className="flex justify-end gap-2 mt-5 pt-4 border-t border-slate-200">
          <button onClick={() => setOpen(false)} className="btn-outline">إلغاء</button>
          <button
            onClick={() => save.mutate()}
            disabled={
              !form.userName.trim() ||
              !form.fullName.trim() ||
              (!form.id && !form.password) ||
              save.isPending
            }
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
