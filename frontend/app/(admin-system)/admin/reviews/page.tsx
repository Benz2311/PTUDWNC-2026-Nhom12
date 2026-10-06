import AdminDataPlaceholder from '@/components/admin/AdminDataPlaceholder';

export default function AdminReviewsPage() {
  return (
    <>
      <div className="flow-kicker">Quản trị viên · Hệ thống</div>
      <h1 className="flow-title">Đánh giá & bình luận</h1>
      <p className="flow-description">Không gian kiểm duyệt phản hồi và giữ cho cộng đồng ẩm thực luôn tích cực.</p>
      <AdminDataPlaceholder title="Hàng chờ kiểm duyệt" description="Các đánh giá và bình luận cần được xem xét." columns={['Người gửi', 'Công thức', 'Nội dung', 'Đánh giá', 'Ngày gửi', 'Thao tác']} />
    </>
  );
}
