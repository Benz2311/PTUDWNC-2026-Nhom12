import RecipeEditor from '@/components/recipe-management/RecipeEditor';

export default async function EditRecipePage({ params }: { params: Promise<{ slug: string }> }) {
  const { slug } = await params;
  return (
    <>
      <div className="flow-kicker">Chỉnh sửa công thức</div>
      <h1 className="flow-title">Cập nhật công thức</h1>
      <RecipeEditor slug={slug} />
    </>
  );
}
