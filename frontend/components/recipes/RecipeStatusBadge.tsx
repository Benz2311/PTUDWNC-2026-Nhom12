import type { RecipeStatus } from '@/types/recipe';

const statusLabels: Record<RecipeStatus, string> = {
  Draft: 'Bản nháp',
  Published: 'Đã xuất bản',
  Archived: 'Đã lưu trữ',
};

export default function RecipeStatusBadge({ status }: { status: RecipeStatus }) {
  return (
    <span className={`status status-${status.toLowerCase()}`}>
      {statusLabels[status]}
    </span>
  );
}
