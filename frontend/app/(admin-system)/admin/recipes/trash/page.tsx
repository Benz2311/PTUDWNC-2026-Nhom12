import RecipeTrash from '@/components/admin/RecipeTrash';

export default function AdminRecipeTrashPage() {
  return (
    <>
      <div className="flow-kicker">Quản trị viên · Công thức</div>
      <h1 className="flow-title">Thùng rác công thức</h1>
      <p className="flow-description">Khôi phục công thức đã xóa hoặc xóa vĩnh viễn nội dung và các tệp liên quan.</p>
      <RecipeTrash />
    </>
  );
}
