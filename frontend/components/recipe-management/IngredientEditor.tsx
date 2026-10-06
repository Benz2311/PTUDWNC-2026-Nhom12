'use client';

export type IngredientDraft = { name: string; quantity: string; unit: string; notes: string };

type IngredientEditorProps = {
  ingredients: IngredientDraft[];
  onChange: (ingredients: IngredientDraft[]) => void;
};

export default function IngredientEditor({ ingredients, onChange }: IngredientEditorProps) {
  function update(index: number, key: keyof IngredientDraft, value: string) {
    onChange(ingredients.map((item, itemIndex) =>
      itemIndex === index ? { ...item, [key]: value } : item));
  }

  function move(index: number, offset: -1 | 1) {
    const target = index + offset;
    if (target < 0 || target >= ingredients.length) return;
    const reordered = [...ingredients];
    [reordered[index], reordered[target]] = [reordered[target], reordered[index]];
    onChange(reordered);
  }

  return (
    <div className="ingredient-editor">
      <div className="ingredient-header"><span>Nguyên liệu</span><span>Số lượng</span><span>Đơn vị</span><span>Ghi chú</span><span>Thứ tự</span></div>
      {ingredients.map((ingredient, index) => (
        <div className="ingredient-row" key={index}>
          <input maxLength={200} value={ingredient.name} onChange={(event) => update(index, 'name', event.target.value)} placeholder="Tên nguyên liệu" />
          <input min="0.001" step="any" type="number" value={ingredient.quantity} onChange={(event) => update(index, 'quantity', event.target.value)} placeholder="500" />
          <input maxLength={50} value={ingredient.unit} onChange={(event) => update(index, 'unit', event.target.value)} placeholder="g" />
          <input maxLength={500} value={ingredient.notes} onChange={(event) => update(index, 'notes', event.target.value)} placeholder="Ghi chú" />
          <div className="ingredient-row-actions">
            <button type="button" aria-label="Chuyển nguyên liệu lên" disabled={index === 0} onClick={() => move(index, -1)}>↑</button>
            <button type="button" aria-label="Chuyển nguyên liệu xuống" disabled={index === ingredients.length - 1} onClick={() => move(index, 1)}>↓</button>
            <button type="button" aria-label="Xóa nguyên liệu" onClick={() => onChange(ingredients.length === 1 ? [{ name: '', quantity: '', unit: '', notes: '' }] : ingredients.filter((_, itemIndex) => itemIndex !== index))}>×</button>
          </div>
        </div>
      ))}
      <button className="small-button" type="button" onClick={() => onChange([...ingredients, { name: '', quantity: '', unit: '', notes: '' }])}>+ Thêm nguyên liệu</button>
    </div>
  );
}
