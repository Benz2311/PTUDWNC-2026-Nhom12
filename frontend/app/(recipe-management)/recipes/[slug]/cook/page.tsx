import CookingGuide from '@/components/recipe-management/CookingGuide';

export default async function CookingGuidePage({ params }: { params: Promise<{ slug: string }> }) {
  const { slug } = await params;
  return (
    <>
      <div className="flow-kicker">Hướng dẫn nấu ăn</div>
      <p className="flow-description">Chuẩn bị nguyên liệu và theo dõi từng bước ngay trong khi nấu.</p>
      <CookingGuide slug={slug} />
    </>
  );
}
