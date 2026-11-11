'use client';

import { useCallback, useEffect, useState } from 'react';
import { AUTH_CHANGE_EVENT } from '@/lib/api';
import {
  deletePersonalRecipeNote,
  getPersonalRecipeNote,
  upsertPersonalRecipeNote,
  type PersonalRecipeNote,
} from '@/lib/api/recipes';

function isLoggedIn(): boolean {
  if (typeof window === 'undefined') return false;
  return Boolean(localStorage.getItem('culinary_access_token'));
}

export default function PersonalNotePanel({ recipeId }: { recipeId: string }) {
  const [loggedIn, setLoggedIn] = useState(false);
  const [note, setNote] = useState<PersonalRecipeNote | null>(null);
  const [content, setContent] = useState('');
  const [loading, setLoading] = useState(false);
  const [noteLoaded, setNoteLoaded] = useState(false);
  const [saving, setSaving] = useState(false);
  const [deleting, setDeleting] = useState(false);
  const [error, setError] = useState('');
  const [loadError, setLoadError] = useState('');
  const [success, setSuccess] = useState('');

  useEffect(() => {
    const syncLoginState = () => setLoggedIn(isLoggedIn());
    syncLoginState();
    window.addEventListener(AUTH_CHANGE_EVENT, syncLoginState);
    window.addEventListener('storage', syncLoginState);
    return () => {
      window.removeEventListener(AUTH_CHANGE_EVENT, syncLoginState);
      window.removeEventListener('storage', syncLoginState);
    };
  }, []);

  const loadNote = useCallback(async () => {
    if (!loggedIn) return;
    setLoading(true);
    setLoadError('');
    setNoteLoaded(false);
    try {
      const data = await getPersonalRecipeNote(recipeId);
      setNote(data);
      setContent(data?.content ?? '');
      setNoteLoaded(true);
    } catch (reason) {
      setLoadError(reason instanceof Error ? reason.message : 'Không thể tải ghi chú.');
    } finally {
      setLoading(false);
    }
  }, [recipeId, loggedIn]);

  useEffect(() => {
    void loadNote();
  }, [loadNote]);

  async function handleSave() {
    if (!content.trim()) {
      setError('Nội dung ghi chú không được để trống.');
      return;
    }
    setSaving(true);
    setError('');
    setSuccess('');
    try {
      const saved = await upsertPersonalRecipeNote(recipeId, content.trim());
      setNote(saved);
      setContent(saved.content);
      setSuccess('Đã lưu ghi chú thành công.');
    } catch (err: unknown) {
      setError(err instanceof Error ? err.message : 'Không thể lưu ghi chú.');
    } finally {
      setSaving(false);
    }
  }

  async function handleDelete() {
    if (!note) return;
    if (!window.confirm('Bạn có chắc muốn xóa ghi chú này?')) return;
    setDeleting(true);
    setError('');
    setSuccess('');
    try {
      await deletePersonalRecipeNote(recipeId, note.id);
      setNote(null);
      setContent('');
      setSuccess('Đã xóa ghi chú.');
    } catch (err: unknown) {
      setError(err instanceof Error ? err.message : 'Không thể xóa ghi chú.');
    } finally {
      setDeleting(false);
    }
  }

  return (
    <section className="personal-note-panel" aria-label="Ghi chú cá nhân">
      <div className="personal-note-header">
        <h3>📝 Ghi chú cá nhân của bạn</h3>
        <span className="personal-note-badge">Riêng tư</span>
      </div>
      <p className="note-hint">Ghi chú này được lưu trữ riêng cho tài khoản của bạn và không công khai cho người khác.</p>

      {!loggedIn ? (
        <p className="note-hint">
          <a href="/login">Đăng nhập</a> để tạo và lưu ghi chú riêng cho công thức này.
        </p>
      ) : loading ? (
        <div className="loading-state" role="status">
          <span className="spinner" aria-hidden="true" /> Đang tải ghi chú...
        </div>
      ) : loadError ? (
        <div className="form-error" role="alert" style={{ marginTop: 12 }}>
          <p>❌ Không thể tải ghi chú: {loadError}</p>
          <button className="outline-button" type="button" onClick={() => void loadNote()}>
            Thử tải lại
          </button>
        </div>
      ) : (
        <>
          <textarea
            className="note-textarea"
            value={content}
            onChange={(e) => {
              setContent(e.target.value);
              setSuccess('');
            }}
            placeholder="Ghi lại các mẹo, lưu ý hoặc biến tấu riêng của bạn khi nấu món này... (tối đa 5.000 ký tự)"
            maxLength={5000}
            rows={5}
            aria-label="Nội dung ghi chú cá nhân"
          />

          <div className="personal-note-footer">
            <div className="note-char-count" aria-live="polite">
              {content.length} / 5000 ký tự
            </div>

            <div className="note-actions">
              <button
                className="primary-button"
                type="button"
                onClick={() => void handleSave()}
                disabled={!noteLoaded || saving || deleting}
              >
                {saving ? '⏳ Đang lưu...' : '💾 Lưu ghi chú'}
              </button>
              {note && (
                <button
                  className="delete-action"
                  type="button"
                  onClick={() => void handleDelete()}
                  disabled={!noteLoaded || saving || deleting}
                >
                  {deleting ? '⏳ Đang xóa...' : '🗑️ Xóa ghi chú'}
                </button>
              )}
            </div>
          </div>

          {error && (
            <div className="form-error" role="alert" style={{ marginTop: 12 }}>
              ❌ {error}
            </div>
          )}
          {success && (
            <div className="form-success-banner" role="status" style={{ marginTop: 12 }}>
              ✅ {success}
            </div>
          )}

          {note && (
            <div className="note-meta">
              <span>🕒</span>
              <span>
                {note.updatedAt
                  ? `Cập nhật lần cuối: ${new Intl.DateTimeFormat('vi-VN', { dateStyle: 'medium', timeStyle: 'short' }).format(new Date(note.updatedAt))}`
                  : `Tạo lúc: ${new Intl.DateTimeFormat('vi-VN', { dateStyle: 'medium', timeStyle: 'short' }).format(new Date(note.createdAt))}`}
              </span>
            </div>
          )}
        </>
      )}
    </section>
  );
}
