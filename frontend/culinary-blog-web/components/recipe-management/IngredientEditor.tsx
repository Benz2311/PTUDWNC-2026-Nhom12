'use client';

export type IngredientDraft = {
  id?: string;
  name: string;
  quantity: string;
  unit: string;
  notes: string;
  orderIndex?: number;
};

type IngredientEditorProps = {
  ingredients: IngredientDraft[];
  onChange: (ingredients: IngredientDraft[]) => void;
  disabled?: boolean;
};

export default function IngredientEditor({
  ingredients,
  onChange,
  disabled = false,
}: IngredientEditorProps) {
  function update(index: number, key: keyof IngredientDraft, value: string) {
    onChange(
      ingredients.map((item, itemIndex) =>
        itemIndex === index ? { ...item, [key]: value, orderIndex: itemIndex } : item,
      ),
    );
  }

  function move(index: number, offset: -1 | 1) {
    const target = index + offset;
    if (target < 0 || target >= ingredients.length) return;
    const reordered = [...ingredients];
    [reordered[index], reordered[target]] = [reordered[target], reordered[index]];
    // Re-index orderIndex for all items
    const updated = reordered.map((item, idx) => ({ ...item, orderIndex: idx }));
    onChange(updated);
  }

  function remove(index: number) {
    if (ingredients.length === 1) {
      onChange([{ name: '', quantity: '', unit: '', notes: '', orderIndex: 0 }]);
    } else {
      const filtered = ingredients.filter((_, itemIndex) => itemIndex !== index);
      onChange(filtered.map((item, idx) => ({ ...item, orderIndex: idx })));
    }
  }

  function add() {
    onChange([
      ...ingredients,
      { name: '', quantity: '', unit: '', notes: '', orderIndex: ingredients.length },
    ]);
  }

  return (
    <div className="ingredient-editor" aria-label="Bảng quản lý danh sách nguyên liệu">
      <div className="ingredient-header">
        <span className="ingredient-col-idx">#</span>
        <span>Tên nguyên liệu *</span>
        <span>Số lượng</span>
        <span>Đơn vị</span>
        <span>Ghi chú</span>
        <span className="ingredient-col-actions">Thao tác</span>
      </div>

      <div className="ingredient-rows-container">
        {ingredients.map((ingredient, index) => {
          const currentOrder = ingredient.orderIndex !== undefined ? ingredient.orderIndex + 1 : index + 1;
          return (
            <div className="ingredient-row" key={ingredient.id || index}>
              <span className="ingredient-idx-badge" title={`Thứ tự ưu tiên: ${currentOrder}`}>
                {currentOrder}
              </span>
              <input
                maxLength={200}
                value={ingredient.name}
                onChange={(event) => update(index, 'name', event.target.value)}
                placeholder="Ví dụ: Thịt bò thăn, Hành lá..."
                required
                disabled={disabled}
                className="ingredient-input-name"
                aria-label={`Tên nguyên liệu ${currentOrder}`}
              />
              <input
                min="0.001"
                step="any"
                type="number"
                value={ingredient.quantity}
                onChange={(event) => update(index, 'quantity', event.target.value)}
                placeholder="500"
                disabled={disabled}
                className="ingredient-input-qty"
                aria-label={`Số lượng nguyên liệu ${currentOrder}`}
              />
              <input
                maxLength={50}
                value={ingredient.unit}
                onChange={(event) => update(index, 'unit', event.target.value)}
                placeholder="g, ml, muỗng..."
                disabled={disabled}
                className="ingredient-input-unit"
                aria-label={`Đơn vị tính nguyên liệu ${currentOrder}`}
              />
              <input
                maxLength={500}
                value={ingredient.notes}
                onChange={(event) => update(index, 'notes', event.target.value)}
                placeholder="Thái lát mỏng, ướp tiêu..."
                disabled={disabled}
                className="ingredient-input-notes"
                aria-label={`Ghi chú nguyên liệu ${currentOrder}`}
              />
              <div className="ingredient-row-actions">
                <button
                  type="button"
                  aria-label={`Chuyển nguyên liệu ${currentOrder} lên`}
                  title="Di chuyển lên"
                  disabled={disabled || index === 0}
                  onClick={() => move(index, -1)}
                >
                  ↑
                </button>
                <button
                  type="button"
                  aria-label={`Chuyển nguyên liệu ${currentOrder} xuống`}
                  title="Di chuyển xuống"
                  disabled={disabled || index === ingredients.length - 1}
                  onClick={() => move(index, 1)}
                >
                  ↓
                </button>
                <button
                  type="button"
                  aria-label={`Xóa nguyên liệu ${currentOrder}`}
                  title="Xóa nguyên liệu này"
                  disabled={disabled}
                  onClick={() => remove(index)}
                  className="btn-delete-row"
                >
                  ×
                </button>
              </div>
            </div>
          );
        })}
      </div>

      <div className="ingredient-footer-actions">
        <button
          className="small-button add-ingredient-btn"
          type="button"
          onClick={add}
          disabled={disabled}
        >
          ＋ Thêm nguyên liệu
        </button>
        <span className="ingredient-count-hint">
          Tổng cộng: <strong>{ingredients.filter((i) => i.name.trim()).length}</strong> nguyên liệu hợp lệ
        </span>
      </div>
    </div>
  );
}
