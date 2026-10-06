type AdminDataPlaceholderProps = {
  title: string;
  description: string;
  columns: string[];
};

export default function AdminDataPlaceholder({ title, description, columns }: AdminDataPlaceholderProps) {
  return (
    <section className="flow-panel admin-data-panel">
      <div className="admin-panel-heading">
        <div><h2>{title}</h2><p>{description}</p></div>
        <span className="status status-draft">Chờ tích hợp API</span>
      </div>
      <div className="table-scroll">
        <table className="admin-table">
          <thead><tr>{columns.map((column) => <th key={column}>{column}</th>)}</tr></thead>
          <tbody><tr><td colSpan={columns.length}><div className="empty-state"><strong>Chưa có dữ liệu để hiển thị</strong><span>Giao diện đã sẵn sàng; backend hiện chưa cung cấp API cho module này.</span></div></td></tr></tbody>
        </table>
      </div>
    </section>
  );
}
