"use client";

import { useRef, useState } from "react";
import { ImagePlus, X } from "lucide-react";
import toast from "react-hot-toast";
import { api, errorMessage } from "@/lib/api";

interface Props {
  value?: string | null;
  onChange: (url: string | null) => void;
}

export default function ImageUpload({ value, onChange }: Props) {
  const inputRef = useRef<HTMLInputElement>(null);
  const [uploading, setUploading] = useState(false);
  const apiOrigin = (process.env.NEXT_PUBLIC_API_URL || "http://localhost:5000/api").replace(/\/api\/?$/, "");

  async function upload(file: File) {
    setUploading(true);
    try {
      const fd = new FormData();
      fd.append("file", file);
      const { data } = await api.post<{ url: string }>("/Files/images", fd, {
        headers: { "Content-Type": "multipart/form-data" },
      });
      onChange(data.url);
    } catch (err) {
      toast.error(errorMessage(err));
    } finally {
      setUploading(false);
    }
  }

  const previewSrc = value
    ? (value.startsWith("http") ? value : `${apiOrigin}${value}`)
    : null;

  return (
    <div className="flex items-center gap-3">
      {previewSrc ? (
        <div className="relative w-24 h-24 rounded-lg overflow-hidden border border-slate-200 dark:border-slate-700">
          <img src={previewSrc} alt="" className="w-full h-full object-cover" />
          <button
            type="button"
            onClick={() => onChange(null)}
            className="absolute top-1 left-1 bg-black/60 text-white rounded-full p-1 hover:bg-red-600"
            title="إزالة"
          >
            <X size={14} />
          </button>
        </div>
      ) : (
        <div
          onClick={() => inputRef.current?.click()}
          className="w-24 h-24 rounded-lg border-2 border-dashed border-slate-300 dark:border-slate-600 flex items-center justify-center cursor-pointer hover:bg-slate-50 dark:hover:bg-slate-800"
        >
          <ImagePlus className="text-slate-400" size={28} />
        </div>
      )}
      <div>
        <button
          type="button"
          onClick={() => inputRef.current?.click()}
          disabled={uploading}
          className="btn-outline text-xs !px-3"
        >
          {uploading ? "جاري الرفع..." : value ? "تغيير الصورة" : "رفع صورة"}
        </button>
        <p className="text-xs text-slate-500 mt-1">PNG/JPG/WEBP — حتى 5MB</p>
      </div>
      <input
        ref={inputRef}
        type="file"
        accept="image/png,image/jpeg,image/webp,image/gif"
        className="hidden"
        onChange={(e) => {
          const f = e.target.files?.[0];
          if (f) upload(f);
          if (inputRef.current) inputRef.current.value = "";
        }}
      />
    </div>
  );
}
