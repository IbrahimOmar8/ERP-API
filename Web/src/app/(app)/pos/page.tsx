"use client";

import { useEffect, useMemo, useRef, useState } from "react";
import { useRouter } from "next/navigation";
import { useQuery } from "@tanstack/react-query";
import toast from "react-hot-toast";
import { Plus, Minus, Trash2, ShoppingBag } from "lucide-react";
import { api, errorMessage } from "@/lib/api";
import { formatMoney } from "@/lib/format";
import { useAuth } from "@/lib/auth";
import type { CashSession, Customer, Product, Sale } from "@/types/api";
import PageHeader from "@/components/PageHeader";

interface CartLine {
  product: Product;
  quantity: number;
}

const PAY_METHODS: { value: number; label: string }[] = [
  { value: 1, label: "نقدي" },
  { value: 2, label: "بطاقة" },
  { value: 3, label: "إنستا باي" },
  { value: 4, label: "محفظة" },
];

export default function PosPage() {
  const router = useRouter();
  const user = useAuth((s) => s.user);
  const [search, setSearch] = useState("");
  const [cart, setCart] = useState<CartLine[]>([]);
  const [customerId, setCustomerId] = useState("");
  const [discount, setDiscount] = useState(0);
  const [paid, setPaid] = useState<number | "">("");
  const [paymentMethod, setPaymentMethod] = useState(1);
  const [saving, setSaving] = useState(false);
  const searchRef = useRef<HTMLInputElement>(null);

  const session = useQuery({
    queryKey: ["current-session"],
    queryFn: async () =>
      (await api.get<CashSession | null>(`/CashSessions/current`)).data,
  });

  const products = useQuery({
    queryKey: ["products"],
    queryFn: async () =>
      (await api.get<Product[]>("/Products", { params: { pageSize: 500 } })).data,
  });

  const customers = useQuery({
    queryKey: ["customers"],
    queryFn: async () => (await api.get<Customer[]>("/Customers")).data,
  });

  const filtered = useMemo(() => {
    if (!products.data) return [];
    if (!search.trim()) return products.data;
    const q = search.toLowerCase();
    return products.data.filter(
      (p) =>
        p.nameAr.toLowerCase().includes(q) ||
        p.sku.toLowerCase().includes(q) ||
        (p.barcode ?? "").includes(search)
    );
  }, [products.data, search]);

  const subTotal = cart.reduce((s, l) => s + l.product.salePrice * l.quantity, 0);
  const vat = cart.reduce(
    (s, l) => s + l.product.salePrice * l.quantity * (l.product.vatRate / 100),
    0
  );
  const total = Math.max(0, subTotal + vat - discount);

  function add(p: Product) {
    setCart((c) => {
      const found = c.find((l) => l.product.id === p.id);
      if (found) return c.map((l) => (l.product.id === p.id ? { ...l, quantity: l.quantity + 1 } : l));
      return [...c, { product: p, quantity: 1 }];
    });
  }

  function changeQty(id: string, delta: number) {
    setCart((c) =>
      c
        .map((l) => (l.product.id === id ? { ...l, quantity: Math.max(0, l.quantity + delta) } : l))
        .filter((l) => l.quantity > 0)
    );
  }

  function remove(id: string) {
    setCart((c) => c.filter((l) => l.product.id !== id));
  }

  async function onScanKey(e: React.KeyboardEvent<HTMLInputElement>) {
    if (e.key !== "Enter" || !search.trim()) return;
    try {
      const { data } = await api.get<Product | null>(`/Products/barcode/${encodeURIComponent(search.trim())}`);
      if (data) {
        add(data);
        setSearch("");
      }
    } catch {
      /* fall through to text-search filter */
    }
  }

  async function submit() {
    if (!session.data) return;
    if (cart.length === 0) return;
    setSaving(true);
    try {
      const payload = {
        warehouseId: session.data.warehouseId,
        cashSessionId: session.data.id,
        customerId: customerId || null,
        discountAmount: discount,
        items: cart.map((l) => ({
          productId: l.product.id,
          quantity: l.quantity,
          unitPrice: l.product.salePrice,
        })),
        payments: [{ method: paymentMethod, amount: paid === "" ? total : Number(paid) }],
      };
      const { data } = await api.post<Sale>("/Sales", payload);
      toast.success("تم تسجيل الفاتورة");
      setCart([]);
      setDiscount(0);
      setPaid("");
      setCustomerId("");
      router.push(`/sales/${data.id}`);
    } catch (err) {
      toast.error(errorMessage(err));
    } finally {
      setSaving(false);
    }
  }

  useEffect(() => {
    searchRef.current?.focus();
  }, []);

  if (session.isLoading) return <p>جاري التحميل...</p>;
  if (!session.data) {
    return (
      <>
        <PageHeader title="نقطة البيع" />
        <div className="card">
          <p className="text-slate-600 mb-3">
            لا توجد جلسة كاش مفتوحة. افتح جلسة من شاشة جلسات الكاش أولاً.
          </p>
          <button className="btn" onClick={() => router.push("/cash-sessions")}>
            الذهاب لجلسات الكاش
          </button>
        </div>
      </>
    );
  }

  return (
    <>
      <PageHeader
        title="نقطة البيع"
        description={`جلسة ${session.data.cashRegisterName} · رصيد افتتاحي ${formatMoney(
          session.data.openingBalance
        )} · ${user?.fullName ?? user?.userName}`}
      />

      <div className="grid grid-cols-1 lg:grid-cols-[1fr_400px] gap-4 h-[calc(100vh-160px)]">
        {/* Products */}
        <div className="card overflow-hidden flex flex-col">
          <input
            ref={searchRef}
            placeholder="ابحث بالاسم أو الباركود... (اضغط Enter لقراءة الباركود)"
            value={search}
            onChange={(e) => setSearch(e.target.value)}
            onKeyDown={onScanKey}
            className="mb-3"
          />
          <div className="overflow-y-auto -mx-2 px-2">
            <div className="grid grid-cols-2 sm:grid-cols-3 md:grid-cols-4 gap-2">
              {filtered.map((p) => (
                <button
                  key={p.id}
                  onClick={() => add(p)}
                  className="text-right rounded-lg border border-slate-200 bg-slate-50 hover:bg-blue-50 hover:border-brand p-3 transition"
                >
                  <div className="text-sm font-semibold leading-tight min-h-[2.5rem]">
                    {p.nameAr}
                  </div>
                  <div className="text-xs text-slate-400 mt-1">{p.sku}</div>
                  <div className="text-brand font-bold mt-1">{formatMoney(p.salePrice)}</div>
                </button>
              ))}
            </div>
            {filtered.length === 0 && (
              <p className="text-center text-slate-400 py-12">لا توجد أصناف</p>
            )}
          </div>
        </div>

        {/* Cart */}
        <div className="card flex flex-col overflow-hidden">
          <select value={customerId} onChange={(e) => setCustomerId(e.target.value)} className="mb-3">
            <option value="">عميل نقدي</option>
            {customers.data?.map((c) => (
              <option key={c.id} value={c.id}>
                {c.name}
              </option>
            ))}
          </select>

          <div className="flex-1 overflow-y-auto -mx-1 px-1 mb-2 border-y border-slate-100 divide-y divide-slate-100">
            {cart.length === 0 ? (
              <div className="text-center text-slate-400 py-12 flex flex-col items-center gap-2">
                <ShoppingBag size={40} />
                <span>السلة فارغة — اضغط على صنف لإضافته</span>
              </div>
            ) : (
              cart.map((line) => (
                <div key={line.product.id} className="py-2 flex items-center gap-2">
                  <div className="flex-1 min-w-0">
                    <div className="text-sm font-medium truncate">{line.product.nameAr}</div>
                    <div className="text-xs text-slate-500">
                      {formatMoney(line.product.salePrice)} × {line.quantity}
                    </div>
                  </div>
                  <div className="flex items-center gap-1">
                    <button
                      onClick={() => changeQty(line.product.id, -1)}
                      className="w-7 h-7 rounded bg-slate-100 hover:bg-slate-200"
                    >
                      <Minus size={14} className="mx-auto" />
                    </button>
                    <input
                      type="number"
                      value={line.quantity}
                      onChange={(e) =>
                        setCart((c) =>
                          c.map((l) =>
                            l.product.id === line.product.id
                              ? { ...l, quantity: Math.max(0, Number(e.target.value)) }
                              : l
                          )
                        )
                      }
                      className="w-14 text-center !py-1"
                    />
                    <button
                      onClick={() => changeQty(line.product.id, 1)}
                      className="w-7 h-7 rounded bg-slate-100 hover:bg-slate-200"
                    >
                      <Plus size={14} className="mx-auto" />
                    </button>
                  </div>
                  <div className="text-sm font-semibold w-20 text-end">
                    {formatMoney(
                      line.product.salePrice * line.quantity * (1 + line.product.vatRate / 100)
                    )}
                  </div>
                  <button
                    onClick={() => remove(line.product.id)}
                    className="text-red-500 hover:text-red-700 p-1"
                  >
                    <Trash2 size={16} />
                  </button>
                </div>
              ))
            )}
          </div>

          <div className="space-y-1 text-sm">
            <Row label="المجموع" value={formatMoney(subTotal)} />
            <div className="flex justify-between items-center">
              <span>خصم</span>
              <input
                type="number"
                value={discount}
                onChange={(e) => setDiscount(Math.max(0, Number(e.target.value)))}
                className="w-28 text-end !py-1"
              />
            </div>
            <Row label="الضريبة" value={formatMoney(vat)} />
            <div className="flex justify-between font-bold text-lg border-t pt-2 mt-2">
              <span>الإجمالي</span>
              <span>{formatMoney(total)}</span>
            </div>
            <div className="flex gap-2 mt-2">
              <select
                value={paymentMethod}
                onChange={(e) => setPaymentMethod(Number(e.target.value))}
                className="flex-1"
              >
                {PAY_METHODS.map((m) => (
                  <option key={m.value} value={m.value}>
                    {m.label}
                  </option>
                ))}
              </select>
              <input
                type="number"
                placeholder="المدفوع"
                value={paid}
                onChange={(e) => setPaid(e.target.value === "" ? "" : Number(e.target.value))}
                className="w-32"
              />
            </div>
            <button
              onClick={submit}
              disabled={cart.length === 0 || saving}
              className="btn-success w-full !py-3 mt-2 text-base"
            >
              {saving ? "جاري الحفظ..." : `تسجيل البيع (${formatMoney(total)})`}
            </button>
          </div>
        </div>
      </div>
    </>
  );
}

function Row({ label, value }: { label: string; value: string }) {
  return (
    <div className="flex justify-between">
      <span>{label}</span>
      <span>{value}</span>
    </div>
  );
}
