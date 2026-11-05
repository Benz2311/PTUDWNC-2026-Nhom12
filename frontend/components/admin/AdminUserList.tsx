'use client';

import { useEffect, useMemo, useState } from 'react';
import { apiFetch } from '@/lib/api';
import ConfirmationDialog from '@/components/recipe-management/ConfirmationDialog';

export type AdminUser = {
  id: string;
  userName: string;
  displayName: string;
  email: string;
  avatarUrl: string | null;
  roles: string[];
  isActive: boolean;
  createdAt: string;
  recipeCount: number;
};

type UserAction = {
  type: 'toggle-status' | 'toggle-role';
  user: AdminUser;
};

export default function AdminUserList() {
  const [users, setUsers] = useState<AdminUser[]>([]);
  const [loading, setLoading] = useState(true);
  const [error, setError] = useState('');
  const [feedback, setFeedback] = useState('');
  const [busyId, setBusyId] = useState('');
  const [searchTerm, setSearchTerm] = useState('');
  const [roleFilter, setRoleFilter] = useState<'all' | 'Admin' | 'Author'>('all');
  const [statusFilter, setStatusFilter] = useState<'all' | 'active' | 'inactive'>('all');

  const [pendingAction, setPendingAction] = useState<UserAction | null>(null);
  const [isProcessing, setIsProcessing] = useState(false);

  async function loadUsers() {
    setLoading(true);
    setError('');
    try {
      const response = await apiFetch<AdminUser[] | { data: AdminUser[] }>(
        '/api/v1/admin/users?page=1&pageSize=100',
      );
      setUsers(Array.isArray(response) ? response : response.data);
    } catch (err) {
      setError(err instanceof Error ? err.message : 'Không tải được danh sách thành viên.');
    } finally {
      setLoading(false);
    }
  }

  useEffect(() => {
    void loadUsers();
  }, []);

  const filteredUsers = useMemo(() => {
    return users.filter((user) => {
      const term = searchTerm.toLowerCase().trim();
      const matchSearch =
        !term ||
        user.displayName.toLowerCase().includes(term) ||
        user.userName.toLowerCase().includes(term) ||
        user.email.toLowerCase().includes(term);

      const matchRole =
        roleFilter === 'all' ||
        (roleFilter === 'Admin' && user.roles.includes('Admin')) ||
        (roleFilter === 'Author' && !user.roles.includes('Admin'));

      const matchStatus =
        statusFilter === 'all' ||
        (statusFilter === 'active' && user.isActive) ||
        (statusFilter === 'inactive' && !user.isActive);

      return matchSearch && matchRole && matchStatus;
    });
  }, [users, searchTerm, roleFilter, statusFilter]);

  async function handleConfirmAction() {
    if (!pendingAction) return;
    const { type, user } = pendingAction;
    setIsProcessing(true);
    setBusyId(user.id);
    setError('');
    setFeedback('');

    try {
      if (type === 'toggle-status') {
        const nextStatus = !user.isActive;
        await apiFetch(`/api/v1/admin/users/${user.id}/status`, {
          method: 'PATCH',
          headers: { 'Content-Type': 'application/json' },
          body: JSON.stringify({ isActive: nextStatus }),
        });

        setUsers((prev) =>
          prev.map((u) => (u.id === user.id ? { ...u, isActive: nextStatus } : u))
        );
        setFeedback(
          nextStatus
            ? `Đã kích hoạt lại tài khoản "${user.displayName}".`
            : `Đã khóa tài khoản "${user.displayName}".`
        );
      } else if (type === 'toggle-role') {
        const isAdmin = user.roles.includes('Admin');
        const nextRoles = isAdmin
          ? user.roles.filter((r) => r !== 'Admin')
          : [...user.roles, 'Admin'];

        await apiFetch(`/api/v1/admin/users/${user.id}/roles`, {
          method: 'PATCH',
          headers: { 'Content-Type': 'application/json' },
          body: JSON.stringify({ roles: nextRoles }),
        });

        setUsers((prev) =>
          prev.map((u) => (u.id === user.id ? { ...u, roles: nextRoles } : u))
        );
        setFeedback(
          isAdmin
            ? `Đã gỡ quyền Quản trị viên khỏi "${user.displayName}".`
            : `Đã cấp quyền Quản trị viên cho "${user.displayName}".`
        );
      }
    } catch (err) {
      setError(err instanceof Error ? err.message : 'Thao tác không thành công.');
    } finally {
      setIsProcessing(false);
      setBusyId('');
      setPendingAction(null);
    }
  }

  const stats = useMemo(() => {
    return {
      total: users.length,
      active: users.filter((u) => u.isActive).length,
      inactive: users.filter((u) => !u.isActive).length,
      admins: users.filter((u) => u.roles.includes('Admin')).length,
    };
  }, [users]);

  function getDialogProps() {
    if (!pendingAction) {
      return {
        title: '',
        message: '',
        confirmText: 'Xác nhận',
        variant: 'primary' as const,
      };
    }

    const { type, user } = pendingAction;
    if (type === 'toggle-status') {
      return {
        title: user.isActive ? 'Xác nhận khóa tài khoản' : 'Xác nhận mở khóa tài khoản',
        message: user.isActive
          ? `Bạn có chắc muốn khóa tài khoản "${user.displayName}" (${user.email})? Người dùng này sẽ không thể đăng nhập hoặc tạo công thức.`
          : `Bạn có chắc muốn mở khóa lại tài khoản "${user.displayName}"?`,
        confirmText: user.isActive ? 'Khóa tài khoản' : 'Mở khóa',
        variant: user.isActive ? ('danger' as const) : ('primary' as const),
      };
    }

    const isAdmin = user.roles.includes('Admin');
    return {
      title: isAdmin ? 'Xác nhận hạ quyền quản trị' : 'Xác nhận cấp quyền quản trị',
      message: isAdmin
        ? `Bạn có chắc muốn gỡ bỏ quyền Quản trị viên của "${user.displayName}"?`
        : `Bạn có chắc muốn cấp quyền Quản trị viên (Admin) cho "${user.displayName}"? Người dùng này sẽ có toàn quyền truy cập trang quản trị.`,
      confirmText: 'Xác nhận',
      variant: 'primary' as const,
    };
  }

  const dialogProps = getDialogProps();

  return (
    <section className="flow-panel admin-data-panel">
      <div className="admin-panel-heading">
        <div>
          <h2>Danh sách thành viên</h2>
          <p>Quản lý tài khoản, vai trò và quyền truy cập trong hệ thống.</p>
        </div>
        <button
          type="button"
          className="small-button"
          onClick={() => void loadUsers()}
          disabled={loading}
        >
          {loading ? 'Đang làm mới...' : '⟳ Làm mới'}
        </button>
      </div>

      {/* Thống kê nhanh */}
      <div className="stats-grid" style={{ marginTop: '16px', marginBottom: '20px' }}>
        <article className="stat-card">
          <span>Tổng thành viên</span>
          <strong>{stats.total}</strong>
        </article>
        <article className="stat-card">
          <span>Đang hoạt động</span>
          <strong style={{ color: 'var(--teal)' }}>{stats.active}</strong>
        </article>
        <article className="stat-card">
          <span>Bị khóa</span>
          <strong style={{ color: stats.inactive > 0 ? '#b64d35' : 'var(--ink-soft)' }}>
            {stats.inactive}
          </strong>
        </article>
        <article className="stat-card">
          <span>Quản trị viên</span>
          <strong style={{ color: 'var(--color-heading)' }}>{stats.admins}</strong>
        </article>
      </div>

      {/* Thanh tìm kiếm và lọc */}
      <div
        className="recipe-toolbar"
        style={{
          display: 'grid',
          gridTemplateColumns: 'minmax(0, 1.8fr) 180px 180px',
          gap: '12px',
          marginBottom: '20px',
        }}
      >
        <label className="field">
          <span>TÌM KIẾM THEO TÊN / EMAIL</span>
          <input
            type="search"
            placeholder="Nhập tên, username hoặc email..."
            value={searchTerm}
            onChange={(e) => setSearchTerm(e.target.value)}
          />
        </label>
        <label className="field">
          <span>VAI TRÒ</span>
          <select
            value={roleFilter}
            onChange={(e) => setRoleFilter(e.target.value as 'all' | 'Admin' | 'Author')}
          >
            <option value="all">Tất cả vai trò</option>
            <option value="Admin">Quản trị viên (Admin)</option>
            <option value="Author">Tác giả (Author)</option>
          </select>
        </label>
        <label className="field">
          <span>TRẠNG THÁI</span>
          <select
            value={statusFilter}
            onChange={(e) => setStatusFilter(e.target.value as 'all' | 'active' | 'inactive')}
          >
            <option value="all">Tất cả trạng thái</option>
            <option value="active">Đang hoạt động</option>
            <option value="inactive">Đã bị khóa</option>
          </select>
        </label>
      </div>

      {feedback && (
        <div className="form-success-banner" role="status" style={{ marginBottom: '16px' }}>
          <span>✓</span>
          <span>{feedback}</span>
        </div>
      )}

      {error && (
        <div
          className="conflict-alert"
          role="alert"
          style={{ marginBottom: '16px', background: '#fdf2f2', borderColor: '#f87171' }}
        >
          <div className="conflict-alert-content">
            <strong style={{ color: '#b91c1c' }}>Đã xảy ra lỗi</strong>
            <p style={{ color: '#991b1b' }}>{error}</p>
          </div>
        </div>
      )}

      {/* Bảng danh sách */}
      <div className="table-scroll">
        <table className="admin-table">
          <thead>
            <tr>
              <th style={{ width: '280px' }}>Thành viên</th>
              <th>Email</th>
              <th>Vai trò</th>
              <th style={{ textAlign: 'center' }}>Công thức</th>
              <th>Trạng thái</th>
              <th>Ngày tham gia</th>
              <th style={{ textAlign: 'right' }}>Thao tác</th>
            </tr>
          </thead>
          <tbody>
            {loading ? (
              <tr>
                <td colSpan={7}>
                  <div className="loading-state">Đang tải danh sách thành viên...</div>
                </td>
              </tr>
            ) : filteredUsers.length === 0 ? (
              <tr>
                <td colSpan={7}>
                  <div className="empty-state">
                    <strong>Không tìm thấy thành viên phù hợp</strong>
                    <span>
                      {searchTerm || roleFilter !== 'all' || statusFilter !== 'all'
                        ? 'Thử thay đổi từ khóa hoặc điều kiện lọc.'
                        : 'Hệ thống chưa có thành viên nào.'}
                    </span>
                  </div>
                </td>
              </tr>
            ) : (
              filteredUsers.map((user) => {
                const isAdmin = user.roles.includes('Admin');
                const isBusy = busyId === user.id;

                function getDialogProps() {
    if (!pendingAction) {
      return {
        title: '',
        message: '',
        confirmText: 'Xác nhận',
        variant: 'primary' as const,
      };
    }

    const { type, user } = pendingAction;
    if (type === 'toggle-status') {
      return {
        title: user.isActive ? 'Xác nhận khóa tài khoản' : 'Xác nhận mở khóa tài khoản',
        message: user.isActive
          ? `Bạn có chắc muốn khóa tài khoản "${user.displayName}" (${user.email})? Người dùng này sẽ không thể đăng nhập hoặc tạo công thức.`
          : `Bạn có chắc muốn mở khóa lại tài khoản "${user.displayName}"?`,
        confirmText: user.isActive ? 'Khóa tài khoản' : 'Mở khóa',
        variant: user.isActive ? ('danger' as const) : ('primary' as const),
      };
    }

    const isAdmin = user.roles.includes('Admin');
    return {
      title: isAdmin ? 'Xác nhận hạ quyền quản trị' : 'Xác nhận cấp quyền quản trị',
      message: isAdmin
        ? `Bạn có chắc muốn gỡ bỏ quyền Quản trị viên của "${user.displayName}"?`
        : `Bạn có chắc muốn cấp quyền Quản trị viên (Admin) cho "${user.displayName}"? Người dùng này sẽ có toàn quyền truy cập trang quản trị.`,
      confirmText: 'Xác nhận',
      variant: 'primary' as const,
    };
  }

  const dialogProps = getDialogProps();

  return (
                  <tr key={user.id} style={{ opacity: user.isActive ? 1 : 0.65 }}>
                    <td>
                      <div style={{ display: 'flex', alignItems: 'center', gap: '12px' }}>
                        {user.avatarUrl ? (
                          <img
                            src={user.avatarUrl}
                            alt={user.displayName}
                            style={{
                              width: '38px',
                              height: '38px',
                              borderRadius: '50%',
                              objectFit: 'cover',
                            }}
                          />
                        ) : (
                          <div
                            style={{
                              width: '38px',
                              height: '38px',
                              borderRadius: '50%',
                              background: 'var(--mint)',
                              color: 'var(--teal)',
                              display: 'flex',
                              alignItems: 'center',
                              justifyContent: 'center',
                              fontWeight: 700,
                              fontSize: '15px',
                              fontFamily: 'var(--font-serif)',
                            }}
                          >
                            {(user.displayName || user.userName || 'U')
                              .charAt(0)
                              .toUpperCase()}
                          </div>
                        )}
                        <div>
                          <strong
                            style={{
                              display: 'block',
                              color: 'var(--ink)',
                              fontSize: '14px',
                            }}
                          >
                            {user.displayName || user.userName}
                          </strong>
                          <span
                            style={{
                              color: 'var(--ink-muted)',
                              fontSize: '11px',
                            }}
                          >
                            @{user.userName}
                          </span>
                        </div>
                      </div>
                    </td>
                    <td>
                      <span style={{ color: 'var(--ink-soft)' }}>{user.email}</span>
                    </td>
                    <td>
                      <div style={{ display: 'flex', gap: '6px', flexWrap: 'wrap' }}>
                        {user.roles.map((r) => (
                          <span
                            key={r}
                            className="status"
                            style={{
                              background: r === 'Admin' ? 'var(--mint)' : 'var(--cream)',
                              color: r === 'Admin' ? 'var(--teal-dark)' : 'var(--ink-soft)',
                              border: '1px solid var(--line)',
                              fontWeight: r === 'Admin' ? 700 : 500,
                            }}
                          >
                            {r === 'Admin' ? 'Quản trị viên' : r === 'Author' ? 'Tác giả' : r}
                          </span>
                        ))}
                      </div>
                    </td>
                    <td style={{ textAlign: 'center' }}>
                      <strong
                        style={{
                          color: user.recipeCount > 0 ? 'var(--teal-dark)' : 'var(--ink-muted)',
                          fontFamily: 'var(--font-serif)',
                          fontSize: '15px',
                        }}
                      >
                        {user.recipeCount}
                      </strong>
                    </td>
                    <td>
                      <span
                        className={`status ${
                          user.isActive ? 'status-published' : 'status-archived'
                        }`}
                      >
                        {user.isActive ? '● Hoạt động' : '✕ Bị khóa'}
                      </span>
                    </td>
                    <td>
                      <span style={{ color: 'var(--ink-muted)', fontSize: '12px' }}>
                        {new Date(user.createdAt).toLocaleDateString('vi-VN')}
                      </span>
                    </td>
                    <td style={{ textAlign: 'right' }}>
                      <div
                        style={{
                          display: 'flex',
                          gap: '6px',
                          justifyContent: 'flex-end',
                        }}
                      >
                        <button
                          type="button"
                          className="small-button"
                          style={{
                            fontSize: '11px',
                            padding: '5px 10px',
                            borderColor: isAdmin ? '#fcd34d' : 'var(--mint-border)',
                          }}
                          disabled={isBusy}
                          onClick={() =>
                            setPendingAction({
                              type: 'toggle-role',
                              user,
                            })
                          }
                          title={isAdmin ? 'Hạ quyền xuống Tác giả' : 'Cấp quyền Quản trị viên'}
                        >
                          {isAdmin ? 'Hạ quyền' : 'Thăng Admin'}
                        </button>
                        <button
                          type="button"
                          className="small-button"
                          style={{
                            fontSize: '11px',
                            padding: '5px 10px',
                            color: user.isActive ? '#b64d35' : 'var(--teal)',
                            borderColor: user.isActive ? '#fecaca' : 'var(--mint-border)',
                          }}
                          disabled={isBusy}
                          onClick={() =>
                            setPendingAction({
                              type: 'toggle-status',
                              user,
                            })
                          }
                        >
                          {user.isActive ? 'Khóa' : 'Mở khóa'}
                        </button>
                      </div>
                    </td>
                  </tr>
                );
              })
            )}
          </tbody>
        </table>
      </div>

      {/* Confirmation Dialog */}
      <ConfirmationDialog
        isOpen={Boolean(pendingAction)}
        title={dialogProps.title}
        message={dialogProps.message}
        confirmText={dialogProps.confirmText}
        variant={dialogProps.variant}
        isLoading={isProcessing}
        onConfirm={() => void handleConfirmAction()}
        onCancel={() => setPendingAction(null)}
      />
    </section>
  );
}

