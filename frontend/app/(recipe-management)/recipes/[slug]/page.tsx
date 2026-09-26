import RecipeDetail from '@/components/recipe-management/RecipeDetail';
import RecipeActions from '@/components/recipe-management/RecipeActions';
import RecipeSteps from '@/components/recipe-management/RecipeSteps';

export default async function RecipeDetailPage({ params }: { params: Promise<{ slug: string }> }) {
  const { slug } = await params;
  return <><div className="flow-kicker">Chi tiết công thức</div><RecipeActions slug={slug} /><RecipeDetail slug={slug} /><RecipeSteps slug={slug} /></>;
}
