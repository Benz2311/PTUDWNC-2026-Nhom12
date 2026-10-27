import type { DifficultyLevel } from '@/types/recipe';

const difficultyLabels: Record<DifficultyLevel, string> = {
  Easy: 'Dễ',
  Medium: 'Trung bình',
  Hard: 'Khó',
  Expert: 'Nâng cao',
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
    if (capitalized in difficultyLabels) {
      normalized = capitalized;
    }
  }

  return <span className={`difficulty-badge difficulty-${normalized.toLowerCase()}`}>{difficultyLabels[normalized]}</span>;
}
