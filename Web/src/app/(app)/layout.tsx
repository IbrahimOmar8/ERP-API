import Sidebar from "@/components/Sidebar";
import RequireAuth from "@/components/RequireAuth";

export default function AppLayout({ children }: { children: React.ReactNode }) {
  return (
    <RequireAuth>
      <div className="flex flex-col lg:flex-row-reverse h-screen overflow-hidden">
        <Sidebar />
        <main className="flex-1 overflow-y-auto bg-slate-50 dark:bg-slate-950 p-4 lg:p-6">
          {children}
        </main>
      </div>
    </RequireAuth>
  );
}
