type RecipeTimeInfoProps = {
  prepTimeMinutes: number;
  cookTimeMinutes: number;
  servings?: number;
  compact?: boolean;
};

export default function RecipeTimeInfo({
  prepTimeMinutes,
  cookTimeMinutes,
  servings,
  compact = false,
}: RecipeTimeInfoProps) {
  const totalMinutes = prepTimeMinutes + cookTimeMinutes;

  return (
    <div className={`recipe-time-info${compact ? ' recipe-time-info-compact' : ''}`}>
      <span aria-label={`Tổng thời gian ${totalMinutes} phút`}>◷ {totalMinutes} phút</span>
      {!compact && <span>Chuẩn bị {prepTimeMinutes} phút</span>}
      {!compact && <span>Nấu {cookTimeMinutes} phút</span>}
      {servings !== undefined && <span>{servings} khẩu phần</span>}
    </div>
  );
}
