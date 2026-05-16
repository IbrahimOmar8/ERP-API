"use client";

import { QueryClient, QueryClientProvider } from "@tanstack/react-query";
import { useEffect, useState } from "react";
import { Toaster } from "react-hot-toast";
import { useTheme } from "@/lib/theme";

export default function Providers({ children }: { children: React.ReactNode }) {
  const [client] = useState(
    () =>
      new QueryClient({
        defaultOptions: {
          queries: {
            staleTime: 60_000,
            gcTime: 5 * 60_000,
            refetchOnWindowFocus: false,
            refetchOnMount: false,
            retry: 1,
          },
          mutations: { retry: 0 },
        },
      })
  );

  // Sync theme class to <html> on mount (after hydration)
  const theme = useTheme((s) => s.theme);
  useEffect(() => {
    if (theme === "dark") document.documentElement.classList.add("dark");
    else document.documentElement.classList.remove("dark");
  }, [theme]);

  return (
    <QueryClientProvider client={client}>
      {children}
      <Toaster
        position="top-center"
        toastOptions={{
          style: { fontFamily: "Cairo, sans-serif" },
          className: "dark:!bg-slate-800 dark:!text-slate-100",
        }}
      />
    </QueryClientProvider>
  );
}
