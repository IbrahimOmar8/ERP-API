"use client";

import { useState } from "react";
import Modal from "./Modal";

export function useConfirm() {
  const [state, setState] = useState<{
    open: boolean;
    title: string;
    message: string;
    resolve?: (v: boolean) => void;
  }>({ open: false, title: "", message: "" });

  const confirm = (title: string, message: string): Promise<boolean> =>
    new Promise((resolve) => setState({ open: true, title, message, resolve }));

  const onClose = (result: boolean) => {
    state.resolve?.(result);
    setState((s) => ({ ...s, open: false }));
  };

  const dialog = (
    <Modal open={state.open} title={state.title} onClose={() => onClose(false)} size="sm">
      <p className="text-slate-700 mb-4">{state.message}</p>
      <div className="flex justify-end gap-2">
        <button onClick={() => onClose(false)} className="btn-outline">
          إلغاء
        </button>
        <button onClick={() => onClose(true)} className="btn-danger">
          تأكيد
        </button>
      </div>
    </Modal>
  );

  return { confirm, dialog };
}
