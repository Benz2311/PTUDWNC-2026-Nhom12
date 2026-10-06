'use client';

export type StepDraft = { title: string; description: string };

type StepEditorProps = {
  steps: StepDraft[];
  onChange: (steps: StepDraft[]) => void;
};

export default function StepEditor({ steps, onChange }: StepEditorProps) {
  function update(index: number, key: keyof StepDraft, value: string) {
    onChange(steps.map((step, stepIndex) =>
      stepIndex === index ? { ...step, [key]: value } : step));
  }

  function move(index: number, offset: -1 | 1) {
    const target = index + offset;
    if (target < 0 || target >= steps.length) return;
    const reordered = [...steps];
    [reordered[index], reordered[target]] = [reordered[target], reordered[index]];
    onChange(reordered);
  }

  return (
    <div className="step-editor">
      {steps.map((step, index) => (
        <div className="step-row" key={index}>
          <strong>{String(index + 1).padStart(2, '0')}</strong>
          <div className="form-grid">
            <label className="field field-wide"><span>Tiêu đề bước</span><input maxLength={200} value={step.title} onChange={(event) => update(index, 'title', event.target.value)} placeholder="Sơ chế nguyên liệu" /></label>
            <label className="field field-wide"><span>Hướng dẫn</span><textarea maxLength={2000} rows={3} value={step.description} onChange={(event) => update(index, 'description', event.target.value)} placeholder="Mô tả thao tác cần thực hiện..." /></label>
          </div>
          <div className="ingredient-row-actions">
            <button type="button" aria-label="Chuyển bước lên" disabled={index === 0} onClick={() => move(index, -1)}>↑</button>
            <button type="button" aria-label="Chuyển bước xuống" disabled={index === steps.length - 1} onClick={() => move(index, 1)}>↓</button>
            <button className="small-button" type="button" onClick={() => onChange(steps.length === 1 ? [{ title: '', description: '' }] : steps.filter((_, stepIndex) => stepIndex !== index))}>Xóa bước</button>
          </div>
        </div>
      ))}
      <button className="small-button" type="button" onClick={() => onChange([...steps, { title: '', description: '' }])}>+ Thêm bước</button>
    </div>
  );
}
