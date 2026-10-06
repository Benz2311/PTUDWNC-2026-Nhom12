import AdminStatistics from '@/components/admin/AdminStatistics';

export default function AdminReportsPage() {
  return (
    <>
      <div className="flow-kicker">Quản trị viên · Hệ thống</div>
      <h1 className="flow-title">Thống kê & báo cáo</h1>
      <p className="flow-description">Theo dõi số lượng công thức, trạng thái xuất bản và phân bổ nội dung theo danh mục.</p>
      <AdminStatistics />
    </>
  );
}
