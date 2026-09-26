import RecipeList from '@/components/recipe-management/RecipeList';

export default function RecipesPage() {
  return <><div className="flow-kicker">Quản lý công thức</div><h1 className="flow-title">Kho công thức của Culinary Blog.</h1><p className="flow-description">Tìm kiếm, lọc và mở nhanh công thức để quản lý nguyên liệu và dinh dưỡng.</p><RecipeList /></>;
}
