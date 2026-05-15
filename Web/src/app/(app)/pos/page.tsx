"use client";

import { useEffect, useMemo, useRef, useState } from "react";
import { useRouter } from "next/navigation";
import { useQuery } from "@tanstack/react-query";
import toast from "react-hot-toast";
import { PauseCircle, Plus, Minus, RotateCcw, Trash2, ShoppingBag } from "lucide-react";
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
  const [couponCode, setCouponCode] = useState("");
  const [appliedCoupon, setAppliedCoupon] = useState<{ code: string; discount: number } | null>(null);
  const [pointsToRedeem, setPointsToRedeem] = useState(0);
  const searchRef = useRef<HTMLInputElement>(null);

  const session = useQuery({
    queryKey: ["current-session"],
    queryFn: async () =>
      (await api.get<CashSession | null>(`/CashSessions/current`)).data,
  });

  const loyaltyStatus = useQuery({
    enabled: !!customerId,
    queryKey: ["customer-loyalty", customerId],
    queryFn: async () =>
      (await api.get<{ currentPoints: number; pointsValue: number }>(`/Loyalty/customers/${customerId}`)).data,
  });

  const loyaltySettings = useQuery({
    queryKey: ["loyalty-settings"],
    queryFn: async () =>
      (await api.get<{ enabled: boolean; pointValueEgp: number; minRedeemPoints: number; maxRedeemPercent: number }>("/Loyalty/settings")).data,
  });

  const heldOrders = useQuery({
    queryKey: ["held-orders"],
    queryFn: async () =>
      (await api.get<{ id: string; label?: string | null; customerName?: string | null; itemCount: number; totalEstimate: number; createdAt: string }[]>("/held-orders")).data,
    refetchInterval: 30_000,
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
  const couponDiscount = appliedCoupon?.discount ?? 0;
  const pointsValue = (loyaltySettings.data?.pointValueEgp ?? 0) * pointsToRedeem;
  const total = Math.max(0, subTotal + vat - discount - couponDiscount - pointsValue);

  async function applyCoupon() {
    if (!couponCode.trim()) return;
    try {
      const { data } = await api.post<{ valid: boolean; error?: string; discountAmount: number }>(
        "/Coupons/validate",
        { code: couponCode.trim(), subtotal: subTotal - discount, customerId: customerId || null }
      );
      if (!data.valid) {
        toast.error(data.error ?? "كوبون غير صالح");
        return;
      }
      setAppliedCoupon({ code: couponCode.trim().toUpperCase(), discount: data.discountAmount });
      toast.success(`خصم ${data.discountAmount.toFixed(2)} ج.م`);
    } catch (e) {
      toast.error(errorMessage(e));
    }
  }

  function clearCoupon() {
    setAppliedCoupon(null);
    setCouponCode("");
  }

  const maxRedeemable = (() => {
    if (!loyaltyStatus.data || !loyaltySettings.data?.enabled) return 0;
    const cap = ((subTotal + vat - discount - couponDiscount) * (loyaltySettings.data.maxRedeemPercent / 100))
      / (loyaltySettings.data.pointValueEgp || 1);
    return Math.min(loyaltyStatus.data.currentPoints, Math.floor(cap));
  })();

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

  async function holdOrder() {
    if (!session.data || cart.length === 0) return;
    try {
      await api.post("/held-orders", {
        cashSessionId: session.data.id,
        customerId: customerId || null,
        items: cart.map((l) => ({
          productId: l.product.id,
          productName: l.product.nameAr,
          quantity: l.quantity,
          unitPrice: l.product.salePrice,
          discountAmount: 0,
          discountPercent: 0,
        })),
      });
      toast.success("تم تعليق الفاتورة");
      setCart([]);
      setDiscount(0);
      setPaid("");
      setAppliedCoupon(null);
      setCouponCode("");
      setPointsToRedeem(0);
      heldOrders.refetch();
    } catch (e) {
      toast.error(errorMessage(e));
    }
  }

  async function restoreHeldOrder(id: string) {
    try {
      const { data } = await api.get<{
        customerId?: string | null;
        items: { productId: string; quantity: number }[];
      }>(`/held-orders/${id}`);
      const newCart: CartLine[] = [];
      for (const item of data.items) {
        const product = products.data?.find((p) => p.id === item.productId);
        if (product) newCart.push({ product, quantity: item.quantity });
      }
      if (newCart.length === 0) {
        toast.error("لا توجد أصناف متاحة في هذه السلة");
        return;
      }
      setCart(newCart);
      if (data.customerId) setCustomerId(data.customerId);
      await api.delete(`/held-orders/${id}`);
      heldOrders.refetch();
      toast.success("تم استعادة الفاتورة");
    } catch (e) {
      toast.error(errorMessage(e));
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
        couponCode: appliedCoupon?.code ?? null,
        pointsToRedeem: pointsToRedeem,
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
      setAppliedCoupon(null);
      setCouponCode("");
      setPointsToRedeem(0);
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
          {heldOrders.data && heldOrders.data.length > 0 && (
            <div className="mb-3 -mx-1 px-1 py-2 bg-amber-50 dark:bg-amber-950/30 rounded-lg">
              <div className="text-xs font-medium text-amber-800 dark:text-amber-300 mb-1 flex items-center gap-1 px-2">
                <PauseCircle size={14} /> فواتير معلّقة ({heldOrders.data.length})
              </div>
              <div className="flex gap-1 overflow-x-auto px-1 pb-1">
                {heldOrders.data.map((h) => (
                  <button
                    key={h.id}
                    onClick={() => restoreHeldOrder(h.id)}
                    className="shrink-0 text-xs px-2 py-1.5 rounded bg-white dark:bg-slate-800 border border-amber-200 dark:border-amber-700 hover:border-amber-400 flex items-center gap-1 whitespace-nowrap"
                    title={`${h.itemCount} صنف · ${formatMoney(h.totalEstimate)}`}
                  >
                    <RotateCcw size={12} />
                    <span>{h.customerName ?? h.label ?? `#${h.id.slice(0, 4)}`}</span>
                    <span className="text-slate-400">({formatMoney(h.totalEstimate)})</span>
                  </button>
                ))}
              </div>
            </div>
          )}
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

          {/* Coupon + Loyalty mini-panel */}
          <div className="space-y-2 text-sm mb-2 border-t border-slate-200 dark:border-slate-700 pt-2">
            {!appliedCoupon ? (
              <div className="flex gap-1">
                <input
                  placeholder="كوبون خصم..."
                  value={couponCode}
                  onChange={(e) => setCouponCode(e.target.value.toUpperCase())}
                  className="font-mono !py-1 text-xs flex-1"
                />
                <button onClick={applyCoupon} className="btn-outline !px-2 !py-1 text-xs">
                  تطبيق
                </button>
              </div>
            ) : (
              <div className="flex items-center justify-between bg-emerald-50 dark:bg-emerald-950/30 rounded px-2 py-1">
                <span className="text-xs text-emerald-700 dark:text-emerald-400">
                  ✓ {appliedCoupon.code} (−{formatMoney(appliedCoupon.discount)})
                </span>
                <button onClick={clearCoupon} className="text-emerald-700 text-xs hover:underline">إزالة</button>
              </div>
            )}
            {customerId && loyaltyStatus.data && loyaltySettings.data?.enabled && (
              <div className="bg-violet-50 dark:bg-violet-950/30 rounded p-2">
                <div className="flex justify-between text-xs">
                  <span>نقاط العميل: <b>{loyaltyStatus.data.currentPoints}</b></span>
                  <span>قيمتها: {formatMoney(loyaltyStatus.data.pointsValue)}</span>
                </div>
                {maxRedeemable >= (loyaltySettings.data.minRedeemPoints ?? 0) && (
                  <div className="flex gap-1 mt-2 items-center">
                    <input
                      type="number"
                      min={0}
                      max={maxRedeemable}
                      value={pointsToRedeem}
                      onChange={(e) => setPointsToRedeem(Math.min(maxRedeemable, Math.max(0, Number(e.target.value))))}
                      className="!py-1 text-xs flex-1"
                      placeholder={`استبدل (حد ${maxRedeemable})`}
                    />
                    <button
                      onClick={() => setPointsToRedeem(maxRedeemable)}
                      className="btn-outline !px-2 !py-1 text-xs"
                    >الكل</button>
                  </div>
                )}
              </div>
            )}
          </div>

          <div className="space-y-1 text-sm">
            <Row label="المجموع" value={formatMoney(subTotal)} />
            <div className="flex justify-between items-center">
              <span>خصم يدوي</span>
              <input
                type="number"
                value={discount}
                onChange={(e) => setDiscount(Math.max(0, Number(e.target.value)))}
                className="w-28 text-end !py-1"
              />
            </div>
            {couponDiscount > 0 && (
              <Row label="خصم كوبون" value={`−${formatMoney(couponDiscount)}`} />
            )}
            {pointsValue > 0 && (
              <Row label={`استبدال ${pointsToRedeem} نقطة`} value={`−${formatMoney(pointsValue)}`} />
            )}
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
            <div className="flex gap-2 mt-2">
              <button
                onClick={submit}
                disabled={cart.length === 0 || saving}
                className="btn-success flex-1 !py-3 text-base"
              >
                {saving ? "جاري الحفظ..." : `تسجيل البيع (${formatMoney(total)})`}
              </button>
              <button
                onClick={holdOrder}
                disabled={cart.length === 0 || saving}
                className="btn-outline !py-3"
                title="تعليق الفاتورة"
              >
                <PauseCircle size={18} />
              </button>
            </div>
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
