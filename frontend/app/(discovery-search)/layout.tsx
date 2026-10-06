import SiteHeader from '@/components/SiteHeader';

export default function DiscoverySearchLayout({ children }: Readonly<{ children: React.ReactNode }>) {
  return (
    <div className="flow-layout">
      <SiteHeader />
      <main className="flow-main">{children}</main>
    </div>
  );
}
