"use client";

import Link from "next/link";
import { useQuery } from "@tanstack/react-query";
import { ClipboardList, Factory, FlaskConical } from "lucide-react";
import { api } from "@/lib/api";
import type { Bom, ProductionOrder } from "@/types/api";
import PageHeader from "@/components/PageHeader";

export default function ProductionHomePage() {
  const boms = useQuery({
    queryKey: ["boms"],
    queryFn: async () => (await api.get<Bom[]>("/production/boms")).data,
  });

  const orders = useQuery({
    queryKey: ["production-orders"],
    queryFn: async () => (await api.get<ProductionOrder[]>("/production/orders")).data,
  });

  const inProgress = (orders.data ?? []).filter((o) => o.status === 0 || o.status === 1).length;
  const completed = (orders.data ?? []).filter((o) => o.status === 2).length;

  return (
    <>
      <PageHeader title="الإنتاج" description="وصفات وأوامر تصنيع المنتجات" />

      <div className="grid grid-cols-2 md:grid-cols-3 gap-3 mb-4">
        <Kpi icon={FlaskConical} label="وصفات نشطة" value={(boms.data?.filter((b) => b.isActive).length ?? 0)} color="bg-violet-50 text-violet-700" />
        <Kpi icon={ClipboardList} label="أوامر مفتوحة" value={inProgress} color="bg-amber-50 text-amber-700" />
        <Kpi icon={Factory} label="أوامر منتهية" value={completed} color="bg-emerald-50 text-emerald-700" />
      </div>

      <div className="grid sm:grid-cols-2 gap-3">
        <Link href="/production/boms" className="card hover:shadow-md transition">
          <div className="flex items-start gap-3">
            <div className="rounded-lg p-2 bg-violet-50 text-violet-700"><FlaskConical size={22} /></div>
            <div>
              <div className="font-semibold">الوصفات</div>
              <div className="text-xs text-slate-500">تعريف مكونات المنتج النهائي ونسب الفاقد</div>
            </div>
          </div>
        </Link>
        <Link href="/production/orders" className="card hover:shadow-md transition">
          <div className="flex items-start gap-3">
            <div className="rounded-lg p-2 bg-amber-50 text-amber-700"><ClipboardList size={22} /></div>
            <div>
              <div className="font-semibold">أوامر الإنتاج</div>
              <div className="text-xs text-slate-500">إصدار أوامر تصنيع وخصم المكونات من المخزون</div>
            </div>
          </div>
        </Link>
      </div>
    </>
  );
}

function Kpi({ icon: Icon, label, value, color }: { icon: typeof FlaskConical; label: string; value: number; color: string }) {
  return (
    <div className="card">
      <div className="flex items-center gap-3">
        <div className={`rounded-lg p-2 ${color} dark:bg-opacity-20`}><Icon size={20} /></div>
        <div>
          <div className="text-xs text-slate-500">{label}</div>
          <div className="text-lg font-bold">{value}</div>
        </div>
      </div>
    </div>
  );
}
