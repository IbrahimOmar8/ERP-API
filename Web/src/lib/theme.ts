import { create } from "zustand";
import { persist } from "zustand/middleware";

type Theme = "light" | "dark";

interface ThemeState {
  theme: Theme;
  setTheme: (t: Theme) => void;
  toggle: () => void;
}

export const useTheme = create<ThemeState>()(
  persist(
    (set, get) => ({
      theme: "light",
      setTheme: (theme) => {
        apply(theme);
        set({ theme });
      },
      toggle: () => {
        const next: Theme = get().theme === "light" ? "dark" : "light";
        apply(next);
        set({ theme: next });
      },
    }),
    {
      name: "erp-theme",
      onRehydrateStorage: () => (state) => {
        if (state) apply(state.theme);
      },
    }
  )
);

function apply(theme: Theme) {
  if (typeof document === "undefined") return;
  const root = document.documentElement;
  if (theme === "dark") root.classList.add("dark");
  else root.classList.remove("dark");
}
