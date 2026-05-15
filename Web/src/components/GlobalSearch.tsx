"use client";

import { useEffect, useRef, useState } from "react";
import { useRouter } from "next/navigation";
import { Loader2, Package, Receipt, Search, Truck, User } from "lucide-react";
import { api } from "@/lib/api";

interface SearchHit {
  type: "product" | "customer" | "sale" | "supplier";
  id: string;
  title: string;
  subtitle?: string | null;
}

const typeMeta: Record<string, { label: string; icon: typeof Package; href: (id: string) => string }> = {
  product: { label: "صنف", icon: Package, href: () => "/products" },
  customer: { label: "عميل", icon: User, href: () => "/customers" },
  sale: { label: "فاتورة", icon: Receipt, href: (id) => `/sales/${id}` },
  supplier: { label: "مورد", icon: Truck, href: () => "/suppliers" },
};

export default function GlobalSearch() {
  const router = useRouter();
  const [open, setOpen] = useState(false);
  const [q, setQ] = useState("");
  const [hits, setHits] = useState<SearchHit[]>([]);
  const [loading, setLoading] = useState(false);
  const inputRef = useRef<HTMLInputElement>(null);
  const debounceTimer = useRef<ReturnType<typeof setTimeout> | null>(null);

  // Cmd/Ctrl + K to open
  useEffect(() => {
    function onKey(e: KeyboardEvent) {
      if ((e.metaKey || e.ctrlKey) && e.key.toLowerCase() === "k") {
        e.preventDefault();
        setOpen(true);
      }
      if (e.key === "Escape") setOpen(false);
    }
    window.addEventListener("keydown", onKey);
    return () => window.removeEventListener("keydown", onKey);
  }, []);

  useEffect(() => {
    if (open) setTimeout(() => inputRef.current?.focus(), 50);
  }, [open]);

  useEffect(() => {
    if (debounceTimer.current) clearTimeout(debounceTimer.current);
    if (!q.trim()) {
      setHits([]);
      return;
    }
    debounceTimer.current = setTimeout(async () => {
      setLoading(true);
      try {
        const { data } = await api.get<SearchHit[]>("/Search", { params: { q, take: 8 } });
        setHits(data);
      } catch {
        setHits([]);
      } finally {
        setLoading(false);
      }
    }, 200);
  }, [q]);

  function go(hit: SearchHit) {
    setOpen(false);
    setQ("");
    router.push(typeMeta[hit.type].href(hit.id));
  }

  return (
    <>
      <button
        onClick={() => setOpen(true)}
        className="hidden lg:flex items-center gap-2 text-slate-400 hover:text-slate-200 text-sm px-3 py-1.5 rounded-md border border-slate-700 hover:border-slate-500 bg-slate-800/50 w-full"
      >
        <Search size={14} />
        <span>بحث... </span>
        <kbd className="ms-auto text-xs bg-slate-700 px-1.5 py-0.5 rounded">⌘K</kbd>
      </button>

      {open && (
        <div className="fixed inset-0 z-50 bg-black/40 p-4 flex items-start justify-center pt-[10vh]"
             onMouseDown={(e) => e.target === e.currentTarget && setOpen(false)}>
          <div className="bg-white dark:bg-slate-900 rounded-xl shadow-2xl w-full max-w-xl overflow-hidden">
            <div className="flex items-center gap-2 border-b border-slate-200 dark:border-slate-700 px-3 py-2">
              {loading ? <Loader2 className="animate-spin text-slate-400" size={18} /> : <Search className="text-slate-400" size={18} />}
              <input
                ref={inputRef}
                value={q}
                onChange={(e) => setQ(e.target.value)}
                placeholder="ابحث في الأصناف، العملاء، الفواتير، الموردين..."
                className="flex-1 border-0 !p-0 !ring-0 focus:!ring-0 focus:!border-0 bg-transparent text-base"
              />
              <kbd className="text-xs bg-slate-100 dark:bg-slate-700 px-1.5 py-0.5 rounded">Esc</kbd>
            </div>
            <div className="max-h-96 overflow-y-auto">
              {hits.length === 0 ? (
                <div className="text-center text-slate-400 py-12 text-sm">
                  {q.trim() ? "لا توجد نتائج" : "ابدأ بالكتابة للبحث..."}
                </div>
              ) : (
                hits.map((hit) => {
                  const meta = typeMeta[hit.type];
                  const Icon = meta.icon;
                  return (
                    <button
                      key={`${hit.type}-${hit.id}`}
                      onClick={() => go(hit)}
                      className="w-full text-start px-3 py-2 flex items-center gap-3 hover:bg-slate-50 dark:hover:bg-slate-800 transition"
                    >
                      <Icon size={18} className="text-slate-400" />
                      <div className="flex-1 min-w-0">
                        <div className="font-medium truncate">{hit.title}</div>
                        {hit.subtitle && <div className="text-xs text-slate-500 truncate">{hit.subtitle}</div>}
                      </div>
                      <span className="text-xs text-slate-400">{meta.label}</span>
                    </button>
                  );
                })
              )}
            </div>
          </div>
        </div>
      )}
    </>
  );
}
