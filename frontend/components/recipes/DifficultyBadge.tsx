import type { DifficultyLevel } from '@/types/recipe';

const difficultyLabels: Record<DifficultyLevel, string> = {
  Easy: 'Dễ',
  Medium: 'Trung bình',
  Hard: 'Khó',
  Expert: 'Nâng cao',
};

export default function DifficultyBadge({ difficulty }: { difficulty: DifficultyLevel }) {
  return <span className={`difficulty-badge difficulty-${difficulty.toLowerCase()}`}>{difficultyLabels[difficulty]}</span>;
}
