import DiscoveryBrowser from '@/components/discovery/DiscoveryBrowser';

export default function SearchPage() {
  return <><div className="flow-kicker">Tìm kiếm công thức</div><h1 className="flow-title">Bạn đang muốn nấu món gì?</h1><p className="flow-description">Tìm nhanh theo tên món ăn, mô tả và danh mục.</p><DiscoveryBrowser mode="search" /></>;
}
