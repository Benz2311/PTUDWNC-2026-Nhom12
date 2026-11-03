import type { DifficultyLevel } from '@/types/recipe';

const difficultyConfig: Record<DifficultyLevel, { label: string; color: string }> = {
  Easy: { label: 'Dễ', color: 'bg-emerald-100/90 text-emerald-800 border-emerald-200' },
  Medium: { label: 'Trung bình', color: 'bg-amber-100/90 text-amber-800 border-amber-200' },
  Hard: { label: 'Khó', color: 'bg-rose-100/90 text-rose-800 border-rose-200' },
  Expert: { label: 'Nâng cao', color: 'bg-purple-100/90 text-purple-800 border-purple-200' },
};

const numToDifficulty: Record<number, DifficultyLevel> = {
  1: 'Easy',
  2: 'Medium',
  3: 'Hard',
  4: 'Expert',
};

export default function DifficultyBadge({ difficulty }: { difficulty?: DifficultyLevel | string | number }) {
  let normalized: DifficultyLevel = 'Medium';
  if (typeof difficulty === 'number' && numToDifficulty[difficulty]) {
    normalized = numToDifficulty[difficulty];
  } else if (typeof difficulty === 'string') {
    const capitalized = (difficulty.charAt(0).toUpperCase() + difficulty.slice(1).toLowerCase()) as DifficultyLevel;
    if (capitalized in difficultyConfig) {
      normalized = capitalized;
    }
  }

  const config = difficultyConfig[normalized];

  return (
    <span
      className={`inline-flex items-center px-2.5 py-0.5 rounded-full text-xs font-semibold border backdrop-blur-xs shadow-2xs ${config.color} difficulty-badge difficulty-${normalized.toLowerCase()}`}
    >
      {config.label}
    </span>
  );
}
