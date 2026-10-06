'use client';

import { FormEvent, useState } from 'react';
import { usePathname, useRouter } from 'next/navigation';
import AuthNav from '@/components/auth/AuthNav';

const navigation = [
  { href: '/recipes', label: 'Khám phá ẩm thực' },
  { href: '/recipes/new', label: 'Học nấu ăn' },
  { href: '/about', label: 'Kết nối cộng đồng' },
];

export default function SiteHeader({ admin = false }: { admin?: boolean }) {
  const router = useRouter();
  const pathname = usePathname();
  const [search, setSearch] = useState('');

  function submitSearch(event: FormEvent<HTMLFormElement>) {
    event.preventDefault();
    const query = search.trim();
    router.push(query ? `/search?q=${encodeURIComponent(query)}` : '/search');
  }

  return (
    <header className={`site-header${admin ? ' site-header-admin' : ''}`}>
      <a className="brand" href="/" aria-label="Culinary Blog - Trang chủ">
        <span className="brand-mark" aria-hidden="true">♨</span>
        <span className="brand-copy">
          <span className="brand-name">Culinary Blog</span>
          <span className="brand-subtitle">Hành trình trải nghiệm ứng dụng</span>
        </span>
      </a>
      <nav className="main-nav" aria-label="Điều hướng chính">
        {navigation.map((item) => (
          <a className={pathname === item.href ? 'is-current' : ''} href={item.href} key={item.href}>{item.label}</a>
        ))}
      </nav>
      {!admin && (
        <form className="header-search" onSubmit={submitSearch} role="search">
          <input aria-label="Tìm công thức" placeholder="Tìm công thức..." value={search} onChange={(event) => setSearch(event.target.value)} />
          <button type="submit" aria-label="Tìm kiếm">⌕</button>
        </form>
      )}
      <AuthNav />
    </header>
  );
}
