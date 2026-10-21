type RecipeTimeInfoProps = {
  prepTimeMinutes?: number;
  cookTimeMinutes?: number;
  servings?: number;
  compact?: boolean;
};

export default function RecipeTimeInfo({
  prepTimeMinutes = 0,
  cookTimeMinutes = 0,
  servings,
  compact = false,
}: RecipeTimeInfoProps) {
  const totalMinutes = (prepTimeMinutes ?? 0) + (cookTimeMinutes ?? 0);

  return (
    <div className={`recipe-time-info${compact ? ' recipe-time-info-compact' : ''}`}>
      <span aria-label={`Tổng thời gian ${totalMinutes} phút`}>◷ {totalMinutes} phút</span>
      {!compact && <span>Chuẩn bị {prepTimeMinutes} phút</span>}
      {!compact && <span>Nấu {cookTimeMinutes} phút</span>}
      {servings !== undefined && <span>{servings} khẩu phần</span>}
    </div>
  );
}
