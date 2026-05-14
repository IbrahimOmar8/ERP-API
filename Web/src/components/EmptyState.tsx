import type { LucideIcon } from "lucide-react";
import { Inbox } from "lucide-react";

export default function EmptyState({
  icon: Icon = Inbox,
  title,
  description,
  actionLabel,
  onAction,
}: {
  icon?: LucideIcon;
  title: string;
  description?: string;
  actionLabel?: string;
  onAction?: () => void;
}) {
  return (
    <div className="text-center py-12 px-4">
      <div className="mx-auto inline-flex items-center justify-center w-16 h-16 rounded-full bg-brand/10 text-brand mb-3">
        <Icon size={28} />
      </div>
      <h3 className="text-lg font-semibold mb-1 text-slate-900 dark:text-slate-100">{title}</h3>
      {description && (
        <p className="text-sm text-slate-500 dark:text-slate-400 mb-4 max-w-md mx-auto">
          {description}
        </p>
      )}
      {actionLabel && onAction && (
        <button onClick={onAction} className="btn">
          {actionLabel}
        </button>
      )}
    </div>
  );
}
