import DiscoveryBrowser from '@/components/discovery/DiscoveryBrowser';

export default function ExplorePage() {
  return <><div className="flow-kicker">Khám phá ẩm thực</div><h1 className="flow-title">Tìm cảm hứng cho bữa ăn tiếp theo.</h1><p className="flow-description">Khám phá công thức đã xuất bản và lọc theo danh mục.</p><DiscoveryBrowser mode="explore" /></>;
}
