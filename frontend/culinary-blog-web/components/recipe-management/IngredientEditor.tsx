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
    <div className="ingredient-editor space-y-4" aria-label="Bảng quản lý danh sách nguyên liệu">
      <div className="overflow-x-auto pb-2 -mx-1 px-1">
        <div className="min-w-[700px]">
          <div className="ingredient-header grid grid-cols-[36px_1.8fr_1fr_1fr_1.6fr_110px] items-center gap-2.5 text-xs font-bold text-gray-500 uppercase tracking-wider mb-2.5 px-3">
            <span className="ingredient-col-idx text-center">#</span>
            <span>Tên nguyên liệu *</span>
            <span>Số lượng</span>
            <span>Đơn vị</span>
            <span>Ghi chú sơ chế</span>
            <span className="ingredient-col-actions text-center">Thao tác</span>
          </div>

          <div className="ingredient-rows-container space-y-2.5">
            {ingredients.map((ingredient, index) => {
              const currentOrder = ingredient.orderIndex !== undefined ? ingredient.orderIndex + 1 : index + 1;
              return (
                <div
                  className="ingredient-row grid grid-cols-[36px_1.8fr_1fr_1fr_1.6fr_110px] items-center gap-2.5 p-2 rounded-xl bg-gray-50/70 border border-gray-200 hover:border-emerald-200 hover:bg-white transition-all shadow-2xs"
                  key={ingredient.id || index}
                >
                  <span
                    className="ingredient-idx-badge flex items-center justify-center w-7 h-7 rounded-full bg-emerald-100 text-emerald-800 text-xs font-bold shrink-0 shadow-2xs"
                    title={`Thứ tự ưu tiên: ${currentOrder}`}
                  >
                    {currentOrder}
                  </span>
                  <input
                    maxLength={200}
                    value={ingredient.name}
                    onChange={(event) => update(index, 'name', event.target.value)}
                    placeholder="Ví dụ: Thịt bò thăn, Hành lá..."
                    required
                    disabled={disabled}
                    className="ingredient-input-name w-full px-3 py-2 rounded-lg border border-gray-300 bg-white text-gray-900 text-sm focus:border-emerald-600 focus:ring-2 focus:ring-emerald-600/20 outline-none transition-all"
                    aria-label={`Tên nguyên liệu ${currentOrder}`}
                  />
                  <input
                    type="text"
                    value={ingredient.quantity}
                    onChange={(event) => update(index, 'quantity', event.target.value)}
                    placeholder="500 hoặc 1/2, 1.5"
                    disabled={disabled}
                    className="ingredient-input-qty w-full px-3 py-2 rounded-lg border border-gray-300 bg-white text-gray-900 text-sm focus:border-emerald-600 focus:ring-2 focus:ring-emerald-600/20 outline-none transition-all"
                    aria-label={`Số lượng nguyên liệu ${currentOrder}`}
                    title="Nhập số thập phân (500, 1.5) hoặc phân số (1/2, 1 1/2)"
                  />
                  <input
                    maxLength={50}
                    value={ingredient.unit}
                    onChange={(event) => update(index, 'unit', event.target.value)}
                    placeholder="g, ml, muỗng..."
                    disabled={disabled}
                    className="ingredient-input-unit w-full px-3 py-2 rounded-lg border border-gray-300 bg-white text-gray-900 text-sm focus:border-emerald-600 focus:ring-2 focus:ring-emerald-600/20 outline-none transition-all"
                    aria-label={`Đơn vị tính nguyên liệu ${currentOrder}`}
                  />
                  <input
                    maxLength={500}
                    value={ingredient.notes}
                    onChange={(event) => update(index, 'notes', event.target.value)}
                    placeholder="Thái lát mỏng, ướp tiêu..."
                    disabled={disabled}
                    className="ingredient-input-notes w-full px-3 py-2 rounded-lg border border-gray-300 bg-white text-gray-900 text-sm focus:border-emerald-600 focus:ring-2 focus:ring-emerald-600/20 outline-none transition-all"
                    aria-label={`Ghi chú nguyên liệu ${currentOrder}`}
                  />
                  <div className="ingredient-row-actions flex items-center justify-center gap-1.5">
                    <button
                      type="button"
                      aria-label={`Chuyển nguyên liệu ${currentOrder} lên`}
                      title="Di chuyển lên"
                      disabled={disabled || index === 0}
                      onClick={() => move(index, -1)}
                      className="p-1.5 text-xs font-bold rounded-lg border border-gray-200 hover:bg-emerald-50 hover:text-emerald-700 disabled:opacity-30 transition-colors cursor-pointer"
                    >
                      ↑
                    </button>
                    <button
                      type="button"
                      aria-label={`Chuyển nguyên liệu ${currentOrder} xuống`}
                      title="Di chuyển xuống"
                      disabled={disabled || index === ingredients.length - 1}
                      onClick={() => move(index, 1)}
                      className="p-1.5 text-xs font-bold rounded-lg border border-gray-200 hover:bg-emerald-50 hover:text-emerald-700 disabled:opacity-30 transition-colors cursor-pointer"
                    >
                      ↓
                    </button>
                    <button
                      type="button"
                      aria-label={`Xóa nguyên liệu ${currentOrder}`}
                      title="Xóa nguyên liệu này"
                      disabled={disabled}
                      onClick={() => remove(index)}
                      className="btn-delete-row p-1.5 text-xs font-bold rounded-lg border border-rose-200 text-rose-600 hover:bg-rose-50 transition-colors cursor-pointer"
                    >
                      ×
                    </button>
                  </div>
                </div>
              );
            })}
          </div>
        </div>
      </div>

      <div className="ingredient-footer-actions flex flex-col sm:flex-row items-start sm:items-center justify-between gap-3 pt-3 border-t border-gray-100">
        <button
          className="small-button add-ingredient-btn inline-flex items-center gap-2 px-4 py-2.5 rounded-xl text-sm font-semibold text-emerald-800 bg-emerald-50 hover:bg-emerald-100 border border-emerald-200 transition-all cursor-pointer shadow-2xs"
          type="button"
          onClick={add}
          disabled={disabled}
        >
          ＋ Thêm nguyên liệu
        </button>
        <span className="ingredient-count-hint text-xs text-gray-500 font-medium">
          Tổng cộng: <strong className="text-emerald-900 font-bold">{ingredients.filter((i) => i.name.trim()).length}</strong> nguyên liệu hợp lệ
        </span>
      </div>
    </div>
  );
}
