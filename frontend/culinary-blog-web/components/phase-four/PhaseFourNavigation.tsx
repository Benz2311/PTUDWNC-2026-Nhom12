import Link from "next/link";

const links = [
  { href: "/feed", label: "Bảng tin" },
  { href: "/suggestions", label: "Hôm nay ăn gì?" },
  { href: "/nutrition", label: "Dinh dưỡng" },
];

export default function PhaseFourNavigation() {
  return (
    <nav aria-label="Tính năng cộng đồng và dinh dưỡng" className="mb-6 flex flex-wrap gap-2">
      {links.map((link) => (
        <Link key={link.href} href={link.href} className="rounded-full border border-emerald-200 bg-white px-4 py-2 text-sm font-semibold text-emerald-800 hover:bg-emerald-50">
          {link.label}
        </Link>
      ))}
    </nav>
  );
}
