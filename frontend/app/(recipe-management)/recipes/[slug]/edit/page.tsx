import RecipeEditor from '@/components/recipe-management/RecipeEditor';
import RecipeActions from '@/components/recipe-management/RecipeActions';

export default async function EditRecipePage({ params }: { params: Promise<{ slug: string }> }) {
  const { slug } = await params;
  return (
    <>
      <div className="flow-kicker">Chỉnh sửa công thức</div>
      <h1 className="flow-title">Cập nhật công thức</h1>
      <RecipeActions slug={slug} />
      <RecipeEditor slug={slug} />
    </>
  );
}
