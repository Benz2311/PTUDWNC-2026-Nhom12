"use client";

import { useEffect, useState } from "react";
import { useAuth } from "@/contexts/AuthContext";
import { phaseFourApi } from "@/lib/api/phase-four";

type FollowAuthorButtonProps = {
  authorId: string;
  compact?: boolean;
};

export default function FollowAuthorButton({ authorId, compact = false }: FollowAuthorButtonProps) {
  const { user, isAuthenticated, isLoading } = useAuth();
  const [following, setFollowing] = useState(false);
  const [loading, setLoading] = useState(true);
  const [saving, setSaving] = useState(false);
  const [error, setError] = useState("");

  useEffect(() => {
    let active = true;
    if (!isAuthenticated || user?.id === authorId) {
      setLoading(false);
      return () => {
        active = false;
      };
    }

    phaseFourApi.getFollowStatus(authorId)
      .then((value) => {
        if (active) setFollowing(value);
      })
      .catch(() => {
        if (active) setError("Không thể tải trạng thái theo dõi.");
      })
      .finally(() => {
        if (active) setLoading(false);
      });

    return () => {
      active = false;
    };
  }, [authorId, isAuthenticated, user?.id]);

  if (isLoading || !isAuthenticated || !user || user.id === authorId) return null;

  const toggleFollow = async () => {
    setSaving(true);
    setError("");
    try {
      const next = await phaseFourApi.setFollowing(authorId, !following);
      setFollowing(next);
    } catch {
      setError("Không thể cập nhật theo dõi. Vui lòng thử lại.");
    } finally {
      setSaving(false);
    }
  };

  return (
    <span className="inline-flex flex-col items-start">
      <button
        type="button"
        onClick={toggleFollow}
        disabled={loading || saving}
        aria-pressed={following}
        className={`rounded-full border px-3 py-1.5 text-sm font-semibold transition-colors disabled:cursor-wait disabled:opacity-60 ${
          following
            ? "border-emerald-200 bg-emerald-50 text-emerald-800 hover:bg-white"
            : "border-emerald-700 bg-emerald-700 text-white hover:bg-emerald-800"
        }`}
      >
        {loading ? "Đang tải..." : following ? "Đang theo dõi" : compact ? "Theo dõi" : "Theo dõi tác giả"}
      </button>
      {error && <span role="status" className="mt-1 text-xs text-red-600">{error}</span>}
    </span>
  );
}
