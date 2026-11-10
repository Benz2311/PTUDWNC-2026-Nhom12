'use client';

export type NutritionData = {
  calories: string;
  protein: string;
  carbohydrates: string;
  fat: string;
  fiber: string;
  sodium: string;
};

type NutritionEditorProps = {
  nutrition: NutritionData;
  onChange: (nutrition: NutritionData) => void;
  disabled?: boolean;
};

const NUTRITION_FIELDS: Array<{
  key: keyof NutritionData;
  label: string;
  unit: string;
  step: string;
  placeholder: string;
}> = [
  { key: 'calories', label: 'Calories', unit: 'kcal', step: '0.01', placeholder: '350' },
  { key: 'protein', label: 'Protein (Đạm)', unit: 'g', step: '0.01', placeholder: '25.5' },
  { key: 'carbohydrates', label: 'Carbs (Đường bột)', unit: 'g', step: '0.01', placeholder: '45.0' },
  { key: 'fat', label: 'Fat (Chất béo)', unit: 'g', step: '0.01', placeholder: '12.0' },
  { key: 'fiber', label: 'Fiber (Chất xơ)', unit: 'g', step: '0.01', placeholder: '4.5' },
  { key: 'sodium', label: 'Sodium (Natri)', unit: 'mg', step: '0.01', placeholder: '480' },
];

export default function NutritionEditor({
  nutrition,
  onChange,
  disabled = false,
}: NutritionEditorProps) {
  function handleFieldChange(key: keyof NutritionData, value: string) {
    // Prevent negative numbers
    if (value !== '' && Number(value) < 0) {
      value = '0';
    }
    onChange({
      ...nutrition,
      [key]: value,
    });
  }

  return (
    <div className="nutrition-editor space-y-4" aria-label="Bảng chỉnh sửa giá trị dinh dưỡng">
      <div className="nutrition-editor-grid grid grid-cols-2 sm:grid-cols-3 lg:grid-cols-6 gap-3 sm:gap-4">
        {NUTRITION_FIELDS.map((field) => (
          <div
            className="nutrition-card p-3.5 bg-gray-50/80 border border-gray-200/90 rounded-2xl hover:border-emerald-300 hover:bg-emerald-50/20 transition-all shadow-2xs"
            key={field.key}
          >
            <label className="field block" htmlFor={`nutrition-${field.key}`}>
              <div className="nutrition-field-header flex items-baseline justify-between mb-1.5">
                <span className="nutrition-field-name text-xs font-bold text-gray-800 truncate" title={field.label}>
                  {field.label}
                </span>
                <span className="nutrition-field-unit text-[11px] font-semibold text-emerald-700">
                  ({field.unit})
                </span>
              </div>
              <input
                id={`nutrition-${field.key}`}
                type="number"
                min="0"
                step={field.step}
                value={nutrition[field.key]}
                onChange={(event) => handleFieldChange(field.key, event.target.value)}
                placeholder={field.placeholder}
                disabled={disabled}
                className="nutrition-input w-full px-3 py-2 bg-white border border-gray-300 rounded-xl text-sm font-bold text-gray-900 focus:border-emerald-600 focus:ring-2 focus:ring-emerald-600/20 outline-none text-right transition-all"
              />
            </label>
          </div>
        ))}
      </div>
      <p className="nutrition-editor-hint text-xs text-gray-500 flex items-center gap-1.5 mt-2">
        <span>💡</span>
        <span>Giá trị dinh dưỡng tính trên một khẩu phần ăn (Servings). Các giá trị không được là số âm.</span>
      </p>
    </div>
  );
}
