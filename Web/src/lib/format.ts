const moneyFmt = new Intl.NumberFormat("ar-EG", {
  minimumFractionDigits: 2,
  maximumFractionDigits: 2,
});

export const formatMoney = (v: number | string | null | undefined): string => {
  if (v == null) return "0.00";
  const n = typeof v === "string" ? Number(v) : v;
  if (!isFinite(n)) return "0.00";
  return `${moneyFmt.format(n)} ج.م`;
};

export const formatNumber = (v: number | string | null | undefined, digits = 2): string => {
  if (v == null) return "0";
  const n = typeof v === "string" ? Number(v) : v;
  if (!isFinite(n)) return "0";
  return n.toLocaleString("ar-EG", { minimumFractionDigits: digits, maximumFractionDigits: digits });
};

export const formatDateTime = (v: string | Date | null | undefined): string => {
  if (!v) return "—";
  const d = typeof v === "string" ? new Date(v) : v;
  return d.toLocaleString("ar-EG", { hour12: false });
};

export const formatDate = (v: string | Date | null | undefined): string => {
  if (!v) return "—";
  const d = typeof v === "string" ? new Date(v) : v;
  return d.toLocaleDateString("ar-EG");
};
