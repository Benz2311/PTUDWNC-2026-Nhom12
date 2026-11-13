'use client';

import { useEffect, useState } from 'react';
import {
  compareRecipeRevisions,
  getRecipeRevisions,
  type RecipeRevisionSummary,
  type RevisionComparison,
} from '@/lib/api/recipes';

function formatDate(iso: string): string {
  return new Intl.DateTimeFormat('vi-VN', {
    dateStyle: 'medium',
    timeStyle: 'short',
  }).format(new Date(iso));
}

const changeTypeLabel: Record<string, string> = {
  Added: '🟢 Thêm mới',
  Deleted: '🔴 Đã xóa',
  Modified: '🟡 Thay đổi',
};

function formatFieldLabel(raw: string): string {
  const map: Record<string, string> = {
    '$.Title': 'Tên món ăn (Title)',
    '$.Description': 'Mô tả ngắn (Description)',
    '$.Content': 'Nội dung chi tiết (Content)',
    '$.CategoryId': 'Danh mục (Category)',
    '$.CoverImageUrl': 'Ảnh bìa (Cover Image)',
    '$.PrepTimeMinutes': 'Thời gian chuẩn bị (Prep Time)',
    '$.CookTimeMinutes': 'Thời gian nấu (Cook Time)',
    '$.Servings': 'Khẩu phần (Servings)',
    '$.Difficulty': 'Độ khó (Difficulty)',
    '$.Nutrition.Calories': 'Calo (Calories)',
    '$.Nutrition.Protein': 'Chất đạm (Protein)',
    '$.Nutrition.Fat': 'Chất béo (Fat)',
    '$.Nutrition.Carbohydrates': 'Tinh bột (Carbs)',
    '$.Nutrition.Fiber': 'Chất xơ (Fiber)',
    '$.Nutrition.Sodium': 'Muối/Natri (Sodium)',
  };
  if (map[raw]) return map[raw];
  if (raw.startsWith('$.Ingredients[')) {
    const prop = raw.split('.').pop();
    const propMap: Record<string, string> = {
      Name: 'Tên nguyên liệu',
      Quantity: 'Định lượng',
      Unit: 'Đơn vị tính',
      Notes: 'Ghi chú',
      SortOrder: 'Thứ tự',
    };
    return prop && propMap[prop] ? `Nguyên liệu → ${propMap[prop]}` : `Nguyên liệu (${raw})`;
  }
  if (raw.startsWith('$.Steps[')) {
    const prop = raw.split('.').pop();
    const propMap: Record<string, string> = {
      Title: 'Tiêu đề bước',
      Description: 'Mô tả bước làm',
      TimerMinutes: 'Hẹn giờ nấu (phút)',
      StepNumber: 'Số thứ tự bước',
    };
    return prop && propMap[prop] ? `Bước nấu → ${propMap[prop]}` : `Bước nấu (${raw})`;
  }
  return raw.replace(/^\$\./, '');
}

export default function RecipeVersionHistory({
  recipeId,
  refreshKey = 0,
}: {
  recipeId: string;
  refreshKey?: number;
}) {
  const [revisions, setRevisions] = useState<RecipeRevisionSummary[]>([]);
  const [loading, setLoading] = useState(false);
  const [error, setError] = useState('');
  const [selectedA, setSelectedA] = useState<number | null>(null);
  const [selectedB, setSelectedB] = useState<number | null>(null);
  const [comparing, setComparing] = useState(false);
  const [comparison, setComparison] = useState<RevisionComparison | null>(null);
  const [compareError, setCompareError] = useState('');
  const [isOpen, setIsOpen] = useState(false);

  useEffect(() => {
    if (!isOpen) return;
    setLoading(true);
    setError('');
    getRecipeRevisions(recipeId)
      .then((data) => {
        const sorted = [...data].sort((a, b) => b.version - a.version);
        setRevisions(sorted);
      })
      .catch((err: unknown) => {
        setError(err instanceof Error ? err.message : 'Không thể tải lịch sử phiên bản.');
      })
      .finally(() => setLoading(false));
  }, [recipeId, isOpen, refreshKey]);

  async function handleCompare() {
    if (selectedA === null || selectedB === null) return;
    if (selectedA === selectedB) {
      setCompareError('Hãy chọn hai phiên bản khác nhau để so sánh.');
      return;
    }
    setComparing(true);
    setCompareError('');
    setComparison(null);
    try {
      const vMin = Math.min(selectedA, selectedB);
      const vMax = Math.max(selectedA, selectedB);
      const result = await compareRecipeRevisions(recipeId, vMin, vMax);
      setComparison(result);
    } catch (err: unknown) {
      setCompareError(err instanceof Error ? err.message : 'Không thể so sánh phiên bản.');
    } finally {
      setComparing(false);
    }
  }

  return (
    <section className="version-history-panel" aria-label="Lịch sử chỉnh sửa">
      <button
        type="button"
        className="outline-button version-history-toggle"
        onClick={() => setIsOpen((v) => !v)}
        aria-expanded={isOpen}
      >
        🕐 {isOpen ? 'Ẩn lịch sử chỉnh sửa' : 'Xem lịch sử chỉnh sửa'}
      </button>

      {isOpen && (
        <div className="version-history-content">
          {loading && (
            <div className="loading-state" role="status">
              <span className="spinner" aria-hidden="true" /> Đang tải lịch sử phiên bản...
            </div>
          )}

          {error && (
            <div className="form-error" role="alert">
              ❌ {error}
            </div>
          )}

          {!loading && !error && revisions.length === 0 && (
            <div className="empty-state">
              <p>Chưa có lịch sử chỉnh sửa nào được ghi lại.</p>
            </div>
          )}

          {!loading && revisions.length > 0 && (
            <>
              <p className="version-history-hint">
                Chọn 2 phiên bản (A và B) để so sánh sự khác biệt về tiêu đề, mô tả và nguyên liệu.
              </p>

              <div className="version-list" role="list" aria-label="Danh sách phiên bản">
                {revisions.map((rev) => {
                  const isA = selectedA === rev.version;
                  const isB = selectedB === rev.version;
                  return (
                    <div
                      key={rev.id}
                      className={`version-item${isA || isB ? ' version-item-selected' : ''}`}
                      role="listitem"
                    >
                      <div className="version-item-header">
                        <strong className="version-badge">v{rev.version}</strong>
                        <span className="version-title">{rev.title}</span>
                        <time className="version-date">{formatDate(rev.createdAt)}</time>
                      </div>
                      {rev.changeSummary && (
                        <p className="version-summary">💬 {rev.changeSummary}</p>
                      )}
                      <div className="version-select-actions">
                        <button
                          type="button"
                          className={`small-button${isA ? ' active' : ''}`}
                          onClick={() => {
                            setSelectedA(isA ? null : rev.version);
                            setComparison(null);
                            setCompareError('');
                          }}
                        >
                          {isA ? '✓ Phiên bản A' : 'Chọn làm A'}
                        </button>
                        <button
                          type="button"
                          className={`small-button${isB ? ' active' : ''}`}
                          onClick={() => {
                            setSelectedB(isB ? null : rev.version);
                            setComparison(null);
                            setCompareError('');
                          }}
                        >
                          {isB ? '✓ Phiên bản B' : 'Chọn làm B'}
                        </button>
                      </div>
                    </div>
                  );
                })}
              </div>

              <div className="version-compare-bar">
                <span>
                  So sánh:{' '}
                  <strong>{selectedA !== null ? `v${selectedA}` : '(chưa chọn A)'}</strong>
                  {' ↔ '}
                  <strong>{selectedB !== null ? `v${selectedB}` : '(chưa chọn B)'}</strong>
                </span>
                <button
                  type="button"
                  className="primary-button"
                  disabled={selectedA === null || selectedB === null || comparing}
                  onClick={() => void handleCompare()}
                >
                  {comparing ? '⏳ Đang so sánh...' : '🔍 So sánh phiên bản'}
                </button>
              </div>

              {compareError && (
                <div className="form-error" role="alert">
                  ❌ {compareError}
                </div>
              )}

              {comparison && (
                <div className="version-diff" aria-label="Kết quả so sánh phiên bản">
                  <div style={{ display: 'flex', justifyContent: 'space-between', alignItems: 'center', marginBottom: 12 }}>
                    <h4>
                      Kết quả so sánh: v{comparison.versionA} → v{comparison.versionB}
                    </h4>
                    <span style={{ fontSize: 13, color: '#059669', background: '#ecfdf5', padding: '4px 8px', borderRadius: 6 }}>
                      💡 Chỉ hiển thị các mục có sự thay đổi
                    </span>
                  </div>
                  {comparison.differences.length === 0 ? (
                    <p className="diff-no-change">
                      ✅ Không có thay đổi nào giữa hai phiên bản này.
                    </p>
                  ) : (
                    <div className="diff-table-wrapper">
                      <table className="diff-table">
                        <thead>
                          <tr>
                            <th scope="col">Mục thay đổi</th>
                            <th scope="col">Loại thay đổi</th>
                            <th scope="col">Nội dung cũ (v{comparison.versionA})</th>
                            <th scope="col">Nội dung mới (v{comparison.versionB})</th>
                          </tr>
                        </thead>
                        <tbody>
                          {comparison.differences.map((diff, idx) => (
                            <tr
                              key={idx}
                              className={`diff-row diff-${diff.changeType.toLowerCase()}`}
                            >
                              <td className="diff-field">
                                <strong>{formatFieldLabel(diff.field)}</strong>
                                <small style={{ display: 'block', color: '#94a3b8', fontSize: 11 }}>{diff.field}</small>
                              </td>
                              <td className="diff-type">
                                {changeTypeLabel[diff.changeType] ?? diff.changeType}
                              </td>
                              <td className="diff-old">
                                {diff.oldValue != null ? (
                                  <span className="diff-value diff-removed">{diff.oldValue}</span>
                                ) : (
                                  <span className="diff-empty" aria-label="Không có giá trị">—</span>
                                )}
                              </td>
                              <td className="diff-new">
                                {diff.newValue != null ? (
                                  <span className="diff-value diff-added">{diff.newValue}</span>
                                ) : (
                                  <span className="diff-empty" aria-label="Không có giá trị">—</span>
                                )}
                              </td>
                            </tr>
                          ))}
                        </tbody>
                      </table>
                    </div>
                  )}
                </div>
              )}
            </>
          )}
        </div>
      )}
    </section>
  );
}

