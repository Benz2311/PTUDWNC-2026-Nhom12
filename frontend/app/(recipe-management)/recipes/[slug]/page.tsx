import RecipeDetail from '@/components/recipe-management/RecipeDetail';

export default async function RecipeDetailPage({ params }: { params: Promise<{ slug: string }> }) {
  const { slug } = await params;
  return <><div className="flow-kicker">Chi tiết công thức</div><RecipeDetail slug={slug} /></>;
}
