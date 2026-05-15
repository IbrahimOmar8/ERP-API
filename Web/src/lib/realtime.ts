"use client";

import { HubConnection, HubConnectionBuilder, HubConnectionState, LogLevel } from "@microsoft/signalr";
import { useAuth } from "./auth";

const BASE_URL = process.env.NEXT_PUBLIC_API_URL || "http://localhost:5000/api";
// The hub is hosted under the app root, not /api
const HUB_URL = BASE_URL.replace(/\/api\/?$/, "") + "/hubs/events";

let connection: HubConnection | null = null;
let connecting: Promise<HubConnection> | null = null;
const listeners = new Map<string, Set<(data: unknown) => void>>();

async function ensureConnection(): Promise<HubConnection> {
  if (connection && connection.state === HubConnectionState.Connected) return connection;
  if (connecting) return connecting;

  connecting = (async () => {
    const conn = new HubConnectionBuilder()
      .withUrl(HUB_URL, { accessTokenFactory: () => useAuth.getState().accessToken ?? "" })
      .withAutomaticReconnect()
      .configureLogging(LogLevel.Warning)
      .build();

    // Re-bind all current listeners when the underlying connection is rebuilt
    for (const [event, handlers] of listeners) {
      conn.on(event, (data) => handlers.forEach((h) => h(data)));
    }

    await conn.start();
    connection = conn;
    return conn;
  })();

  try { return await connecting; }
  finally { connecting = null; }
}

// Subscribe to a server event by name. Returns an unsubscribe function.
export function subscribe<T = unknown>(event: string, handler: (data: T) => void): () => void {
  const wrapped = (data: unknown) => handler(data as T);
  let set = listeners.get(event);
  if (!set) {
    set = new Set();
    listeners.set(event, set);
  }
  set.add(wrapped);

  ensureConnection()
    .then((c) => c.on(event, wrapped))
    .catch(() => {});

  return () => {
    set?.delete(wrapped);
    if (connection) connection.off(event, wrapped);
  };
}

export function disconnect(): void {
  if (connection) connection.stop();
  connection = null;
}
