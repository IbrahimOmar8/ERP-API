"use client";

import { useEffect } from "react";
import { useRouter } from "next/navigation";
import { useAuth } from "@/lib/auth";

export default function RootPage() {
  const router = useRouter();
  useEffect(() => {
    const token = useAuth.getState().accessToken;
    router.replace(token ? "/dashboard" : "/login");
  }, [router]);
  return null;
}
