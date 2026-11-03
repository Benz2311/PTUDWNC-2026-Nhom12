import type { RecipeStatus } from '@/types/recipe';

const statusConfig: Record<RecipeStatus, { label: string; color: string }> = {
  Draft: { label: 'Bản nháp', color: 'bg-gray-100 text-gray-700 border-gray-200' },
  Published: { label: 'Đã xuất bản', color: 'bg-emerald-100 text-emerald-800 border-emerald-200' },
  Archived: { label: 'Đã lưu trữ', color: 'bg-amber-100 text-amber-800 border-amber-200' },
};

const numToStatus: Record<number, RecipeStatus> = {
  0: 'Draft',
  1: 'Published',
  2: 'Archived',
};

export default function RecipeStatusBadge({ status }: { status?: RecipeStatus | string | number }) {
  let normalized: RecipeStatus = 'Draft';
  if (typeof status === 'number' && numToStatus[status]) {
    normalized = numToStatus[status];
  } else if (typeof status === 'string') {
    const capitalized = (status.charAt(0).toUpperCase() + status.slice(1).toLowerCase()) as RecipeStatus;
    if (capitalized in statusConfig) {
      normalized = capitalized;
    }
  }

  const config = statusConfig[normalized];

  return (
    <span
      className={`inline-flex items-center px-2 py-0.5 rounded-full text-[11px] font-semibold border ${config.color} status status-${normalized.toLowerCase()}`}
    >
      {config.label}
    </span>
  );
}
