import RecipeForm from '@/components/recipe-management/RecipeForm';

export default function CreateRecipePage() {
  return <><div className="flow-kicker">Biên soạn công thức</div><h1 className="flow-title">Tạo món ăn mới.</h1><p className="flow-description">Nhập thông tin, nguyên liệu và giá trị dinh dưỡng. Công thức sẽ được lưu ở trạng thái bản nháp.</p><RecipeForm /></>;
}
