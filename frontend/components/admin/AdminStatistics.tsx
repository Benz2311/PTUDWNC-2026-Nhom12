export default function AdminStatistics() {
  const stats = [
    { label: 'Công thức', value: '1,284' },
    { label: 'Người dùng', value: '8,420' },
    { label: 'Đánh giá', value: '12,990' },
    { label: 'Báo cáo', value: '160' },
  ];

  return (
    <div className="stats-grid">
      {stats.map((stat) => (
        <div className="stat-card" key={stat.label}>
          <span>{stat.label}</span>
          <strong>{stat.value}</strong>
        </div>
      ))}
    </div>
  );
}
