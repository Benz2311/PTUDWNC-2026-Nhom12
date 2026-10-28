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
  { key: 'calories', label: 'Calories', unit: 'kcal', step: '0.01', placeholder: 'Ví dụ: 350' },
  { key: 'protein', label: 'Protein (Đạm)', unit: 'g', step: '0.01', placeholder: 'Ví dụ: 25.5' },
  { key: 'carbohydrates', label: 'Carbohydrates (Đường bột)', unit: 'g', step: '0.01', placeholder: 'Ví dụ: 45.0' },
  { key: 'fat', label: 'Fat (Chất béo)', unit: 'g', step: '0.01', placeholder: 'Ví dụ: 12.0' },
  { key: 'fiber', label: 'Fiber (Chất xơ)', unit: 'g', step: '0.01', placeholder: 'Ví dụ: 4.5' },
  { key: 'sodium', label: 'Sodium (Natri)', unit: 'mg', step: '0.01', placeholder: 'Ví dụ: 480' },
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
    <div className="nutrition-editor" aria-label="Bảng chỉnh sửa giá trị dinh dưỡng">
      <div className="nutrition-editor-grid">
        {NUTRITION_FIELDS.map((field) => (
          <div className="nutrition-card" key={field.key}>
            <label className="field" htmlFor={`nutrition-${field.key}`}>
              <div className="nutrition-field-header">
                <span className="nutrition-field-name">{field.label}</span>
                <span className="nutrition-field-unit">({field.unit})</span>
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
                className="nutrition-input"
              />
            </label>
          </div>
        ))}
      </div>
      <p className="nutrition-editor-hint">
        💡 Giá trị dinh dưỡng tính trên một khẩu phần ăn (Servings). Các giá trị không được là số âm.
      </p>
    </div>
  );
}
