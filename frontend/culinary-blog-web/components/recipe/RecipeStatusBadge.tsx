import type { RecipeStatus } from '@/types/recipe';

const statusLabels: Record<RecipeStatus, string> = {
  Draft: 'Bản nháp',
  Published: 'Đã xuất bản',
  Archived: 'Đã lưu trữ',
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
    if (capitalized in statusLabels) {
      normalized = capitalized;
    }
  }

  return (
    <span className={`status status-${normalized.toLowerCase()}`}>
      {statusLabels[normalized]}
    </span>
  );
}
