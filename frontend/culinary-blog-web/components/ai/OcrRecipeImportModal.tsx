"use client";

import { useState } from "react";
import { phaseFourApi, type OcrRecipeDraft } from "@/lib/api/phase-four";

type OcrRecipeImportModalProps = {
  onImport: (draft: OcrRecipeDraft) => void;
};

export default function OcrRecipeImportModal({ onImport }: OcrRecipeImportModalProps) {
  const [open, setOpen] = useState(false);
  const [file, setFile] = useState<File | null>(null);
  const [loading, setLoading] = useState(false);
  const [error, setError] = useState("");

  const extract = async () => {
    if (!file) {
      setError("Chọn ảnh công thức trước khi tiếp tục.");
      return;
    }
    if (file.size > 5 * 1024 * 1024) {
      setError("Ảnh không được vượt quá 5 MB.");
      return;
    }

    setLoading(true);
    setError("");
    try {
      onImport(await phaseFourApi.readRecipeImage(file));
      setOpen(false);
      setFile(null);
    } catch {
      setError("Không thể trích xuất công thức. Hãy kiểm tra ảnh và thử lại.");
    } finally {
      setLoading(false);
    }
  };

  return (
    <>
      <button type="button" onClick={() => setOpen(true)} className="rounded-lg border border-emerald-700 px-4 py-2 font-semibold text-emerald-800 hover:bg-emerald-50">
        Nhập công thức từ ảnh
      </button>
      {open && (
        <div className="fixed inset-0 z-50 flex items-center justify-center bg-slate-950/50 p-4" role="presentation" onMouseDown={(event) => {
          if (event.target === event.currentTarget && !loading) setOpen(false);
        }}>
          <section role="dialog" aria-modal="true" aria-labelledby="ocr-dialog-title" className="w-full max-w-lg rounded-2xl bg-white p-6 shadow-xl">
            <div className="flex items-start justify-between gap-4">
              <div>
                <h2 id="ocr-dialog-title" className="text-xl font-bold text-slate-950">Nhận diện công thức</h2>
                <p className="mt-1 text-sm text-slate-600">Tải ảnh JPEG, PNG, WebP hoặc AVIF, tối đa 5 MB.</p>
              </div>
              <button type="button" aria-label="Đóng" disabled={loading} onClick={() => setOpen(false)} className="rounded px-2 text-2xl text-slate-500 disabled:opacity-50">×</button>
            </div>
            <input
              type="file"
              accept="image/jpeg,image/png,image/webp,image/avif"
              onChange={(event) => {
                setFile(event.target.files?.[0] ?? null);
                setError("");
              }}
              className="mt-5 block w-full text-sm"
            />
            {file && <p className="mt-2 text-sm text-slate-500">{file.name}</p>}
            {error && <p role="alert" className="mt-3 text-sm text-red-700">{error}</p>}
            <div className="mt-6 flex justify-end gap-3">
              <button type="button" disabled={loading} onClick={() => setOpen(false)} className="rounded-lg border px-4 py-2 text-sm disabled:opacity-50">Hủy</button>
              <button type="button" disabled={loading} onClick={() => void extract()} className="rounded-lg bg-emerald-700 px-4 py-2 text-sm font-semibold text-white disabled:opacity-50">
                {loading ? "Đang nhận diện…" : "Trích xuất"}
              </button>
            </div>
          </section>
        </div>
      )}
    </>
  );
}
