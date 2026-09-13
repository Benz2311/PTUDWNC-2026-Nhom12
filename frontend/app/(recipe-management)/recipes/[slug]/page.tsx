export default async function RecipeDetailPage({ params }: { params: Promise<{ slug: string }> }) {
  const { slug } = await params;
  return <><div className="flow-kicker">Chi tiết công thức</div><h1 className="flow-title">Công thức: {slug}</h1><p className="flow-description">Trang hiển thị đầy đủ nội dung, nguyên liệu, các bước nấu, ảnh và đánh giá.</p></>;
}
