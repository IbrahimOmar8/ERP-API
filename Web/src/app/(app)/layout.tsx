import Sidebar from "@/components/Sidebar";
import RequireAuth from "@/components/RequireAuth";

export default function AppLayout({ children }: { children: React.ReactNode }) {
  return (
    <RequireAuth>
      <div className="flex h-screen overflow-hidden flex-row-reverse">
        <Sidebar />
        <main className="flex-1 overflow-y-auto bg-slate-50 p-6">{children}</main>
      </div>
    </RequireAuth>
  );
}
