const ROLE_CONFIG: Record<string, { label: string; className: string }> = {
  Admin: {
    label: "Quản trị viên",
    className: "border-purple-200 bg-purple-50 text-purple-700",
  },
  Author: {
    label: "Tác giả",
    className: "border-blue-200 bg-blue-50 text-blue-700",
  },
  User: {
    label: "Người dùng",
    className: "border-gray-200 bg-gray-50 text-gray-700",
  },
};

export default function RoleBadge({ role }: { role: string }) {
  const config = ROLE_CONFIG[role] ?? {
    label: role,
    className: "border-gray-200 bg-gray-50 text-gray-700",
  };

  return (
    <span
      className={`inline-flex items-center rounded-full border px-2.5 py-0.5 text-xs font-medium ${config.className}`}
    >
      {config.label}
    </span>
  );
}
