import { LucideIcon } from "lucide-react";

export default function KpiCard({
  label,
  value,
  icon: Icon,
  color = "blue",
}: {
  label: string;
  value: string | number;
  icon: LucideIcon;
  color?: "blue" | "green" | "red" | "amber" | "violet" | "teal";
}) {
  const colors: Record<string, string> = {
    blue: "bg-blue-50 text-blue-700",
    green: "bg-emerald-50 text-emerald-700",
    red: "bg-red-50 text-red-700",
    amber: "bg-amber-50 text-amber-700",
    violet: "bg-violet-50 text-violet-700",
    teal: "bg-teal-50 text-teal-700",
  };
  return (
    <div className="card flex items-center gap-3">
      <div className={`shrink-0 rounded-lg p-3 ${colors[color]}`}>
        <Icon size={22} />
      </div>
      <div className="flex-1 min-w-0">
        <div className="text-xs text-slate-500">{label}</div>
        <div className="text-xl font-bold truncate">{value}</div>
      </div>
    </div>
  );
}
