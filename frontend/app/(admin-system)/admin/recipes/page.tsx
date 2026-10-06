import RecipeList from '@/components/recipe-management/RecipeList';

export default function AdminRecipesPage() {
  return (
    <>
      <div className="flow-kicker">Quản trị viên · Hệ thống</div>
      <h1 className="flow-title">Quản lý công thức</h1>
      <p className="flow-description">Duyệt, chỉnh sửa, xuất bản hoặc lưu trữ nội dung công thức trong hệ thống.</p>
      <div className="admin-page-actions"><a className="outline-button" href="/admin/recipes/trash">Mở thùng rác công thức</a></div>
      <RecipeList admin />
    </>
  );
}
