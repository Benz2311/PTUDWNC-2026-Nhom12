import CategoryPage from '@/components/discovery/CategoryPage';

export default function CategoryRoute({ params }: { params: { slug: string } }) {
  return <CategoryPage slug={params.slug} />;
}
