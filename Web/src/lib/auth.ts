import { create } from "zustand";
import { persist } from "zustand/middleware";

export interface AuthUser {
  id: string;
  userName: string;
  fullName: string;
  email?: string | null;
  roles: string[];
}

interface AuthState {
  accessToken: string | null;
  refreshToken: string | null;
  user: AuthUser | null;
  setSession: (s: { accessToken: string; refreshToken: string; user: AuthUser }) => void;
  clear: () => void;
}

export const useAuth = create<AuthState>()(
  persist(
    (set) => ({
      accessToken: null,
      refreshToken: null,
      user: null,
      setSession: ({ accessToken, refreshToken, user }) =>
        set({ accessToken, refreshToken, user }),
      clear: () => set({ accessToken: null, refreshToken: null, user: null }),
    }),
    { name: "erp-auth" }
  )
);

export function hasRole(roles: string[] | undefined, ...required: string[]): boolean {
  if (!roles) return false;
  return required.some((r) => roles.includes(r));
}

export const Roles = {
  Admin: "Admin",
  Manager: "Manager",
  Cashier: "Cashier",
  WarehouseKeeper: "WarehouseKeeper",
  Accountant: "Accountant",
} as const;
