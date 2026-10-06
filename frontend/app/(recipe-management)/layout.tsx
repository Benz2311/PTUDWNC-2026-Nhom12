import SiteHeader from '@/components/SiteHeader';

export default function RecipeManagementLayout({ children }: Readonly<{ children: React.ReactNode }>) {
  return (
    <div className="flow-layout">
      <SiteHeader />
      <main className="flow-main">{children}</main>
    </div>
  );
}
