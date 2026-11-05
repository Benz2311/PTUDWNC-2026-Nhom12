"use client";

import { useEffect, useState } from "react";
import {
  phaseFourApi,
  type NutritionActivityLevel,
  type NutritionGender,
  type NutritionGoal,
  type NutritionGoalType,
  type NutritionSummary,
} from "@/lib/api/phase-four";
import PhaseFourNavigation from "@/components/phase-four/PhaseFourNavigation";

const initialForm = {
  age: 30,
  gender: "Female" as NutritionGender,
  heightCm: 165,
  weightKg: 65,
  activityLevel: "Moderate" as NutritionActivityLevel,
  goalType: "Maintenance" as NutritionGoalType,
};

const fieldClass = "mt-1 w-full rounded-lg border border-slate-300 bg-white px-3 py-2 text-slate-900";

function todayString() {
  const date = new Date();
  const localDate = new Date(date.getTime() - date.getTimezoneOffset() * 60_000);
  return localDate.toISOString().slice(0, 10);
}

export default function NutritionDashboard() {
  const [form, setForm] = useState(initialForm);
  const [goal, setGoal] = useState<NutritionGoal | null>(null);
  const [summary, setSummary] = useState<NutritionSummary | null>(null);
  const [date, setDate] = useState(todayString);
  const [period, setPeriod] = useState<"day" | "week">("day");
  const [loading, setLoading] = useState(true);
  const [saving, setSaving] = useState(false);
  const [error, setError] = useState("");
  const [message, setMessage] = useState("");

  useEffect(() => {
    let active = true;
    Promise.all([phaseFourApi.getNutritionGoals(), phaseFourApi.getNutritionSummary()])
      .then(([savedGoal, currentSummary]) => {
        if (!active) return;
        if (savedGoal) {
          setGoal(savedGoal);
          setForm({
            age: savedGoal.age,
            gender: savedGoal.gender,
            heightCm: savedGoal.heightCm,
            weightKg: savedGoal.weightKg,
            activityLevel: savedGoal.activityLevel,
            goalType: savedGoal.goalType,
          });
        }
        setSummary(currentSummary);
      })
      .catch(() => {
        if (active) setError("Không thể tải dữ liệu dinh dưỡng.");
      })
      .finally(() => {
        if (active) setLoading(false);
      });
    return () => {
      active = false;
    };
  }, []);

  const saveGoal = async (event: React.FormEvent<HTMLFormElement>) => {
    event.preventDefault();
    setSaving(true);
    setError("");
    setMessage("");
    try {
      const updated = await phaseFourApi.updateNutritionGoals(form);
      setGoal(updated);
      setMessage("Đã cập nhật mục tiêu dinh dưỡng.");
    } catch {
      setError("Không thể cập nhật mục tiêu. Hãy kiểm tra các chỉ số và thử lại.");
    } finally {
      setSaving(false);
    }
  };

  const loadSummary = async (selectedDate: string, selectedPeriod = period) => {
    setError("");
    try {
      setSummary(await phaseFourApi.getNutritionSummary(selectedDate, selectedPeriod));
    } catch {
      setError("Không thể tải thống kê dinh dưỡng cho ngày đã chọn.");
    }
  };

  const progress = goal && summary
    ? Math.min(100, Math.round((summary.calories / Math.max(1, goal.targetCalories * (period === "week" ? 7 : 1))) * 100))
    : 0;
  const calorieTarget = goal ? goal.targetCalories * (period === "week" ? 7 : 1) : null;

  return (
    <main className="mx-auto max-w-5xl px-4 py-10">
      <PhaseFourNavigation />
      <header className="mb-8">
        <p className="text-sm font-semibold uppercase tracking-wide text-emerald-700">Sức khỏe cá nhân</p>
        <h1 className="mt-1 text-3xl font-bold text-slate-950">Mục tiêu dinh dưỡng</h1>
        <p className="mt-2 text-slate-600">Ước tính nhu cầu năng lượng theo công thức Mifflin–St Jeor.</p>
      </header>

      {error && <p role="alert" className="mb-5 rounded-lg bg-red-50 p-4 text-red-700">{error}</p>}
      {message && <p role="status" className="mb-5 rounded-lg bg-emerald-50 p-4 text-emerald-800">{message}</p>}
      {loading ? (
        <p role="status">Đang tải dữ liệu dinh dưỡng…</p>
      ) : (
        <div className="grid gap-6 lg:grid-cols-[1fr_1.2fr]">
          <form onSubmit={saveGoal} className="space-y-4 rounded-2xl border border-slate-200 bg-white p-6 shadow-sm">
            <h2 className="text-xl font-bold text-slate-900">Thông tin cơ thể</h2>
            <label className="block text-sm font-medium">Tuổi<input className={fieldClass} type="number" min={13} max={120} required value={form.age} onChange={(event) => setForm({ ...form, age: Number(event.target.value) })} /></label>
            <label className="block text-sm font-medium">Giới tính dùng cho công thức
              <select className={fieldClass} value={form.gender} onChange={(event) => setForm({ ...form, gender: event.target.value as NutritionGender })}>
                <option value="Female">Nữ</option><option value="Male">Nam</option>
              </select>
            </label>
            <div className="grid grid-cols-2 gap-3">
              <label className="block text-sm font-medium">Chiều cao (cm)<input className={fieldClass} type="number" min={100} max={250} step="0.1" required value={form.heightCm} onChange={(event) => setForm({ ...form, heightCm: Number(event.target.value) })} /></label>
              <label className="block text-sm font-medium">Cân nặng (kg)<input className={fieldClass} type="number" min={30} max={350} step="0.1" required value={form.weightKg} onChange={(event) => setForm({ ...form, weightKg: Number(event.target.value) })} /></label>
            </div>
            <label className="block text-sm font-medium">Mức độ vận động
              <select className={fieldClass} value={form.activityLevel} onChange={(event) => setForm({ ...form, activityLevel: event.target.value as NutritionActivityLevel })}>
                <option value="Sedentary">Ít vận động</option><option value="Light">Nhẹ</option><option value="Moderate">Vừa phải</option><option value="Active">Năng động</option>
              </select>
            </label>
            <label className="block text-sm font-medium">Mục tiêu
              <select className={fieldClass} value={form.goalType} onChange={(event) => setForm({ ...form, goalType: event.target.value as NutritionGoalType })}>
                <option value="WeightLoss">Giảm cân</option><option value="Maintenance">Duy trì</option><option value="MuscleGain">Tăng cơ</option>
              </select>
            </label>
            <button type="submit" disabled={saving} className="w-full rounded-lg bg-emerald-700 px-4 py-2.5 font-semibold text-white disabled:opacity-60">
              {saving ? "Đang tính…" : "Tính và lưu mục tiêu"}
            </button>
          </form>

          <section className="space-y-6">
            <div className="rounded-2xl border border-slate-200 bg-white p-6 shadow-sm">
              <h2 className="text-xl font-bold text-slate-900">Mục tiêu hằng ngày</h2>
              {goal ? (
                <>
                  <p className="mt-4 text-3xl font-bold text-emerald-800">{Math.round(goal.targetCalories).toLocaleString("vi-VN")} kcal</p>
                  <p className="mt-1 text-sm text-slate-500">BMR {Math.round(goal.basalMetabolicRate ?? 0)} kcal · TDEE {Math.round(goal.totalDailyEnergyExpenditure ?? 0)} kcal</p>
                  <dl className="mt-5 grid grid-cols-3 gap-3 text-center">
                    <div className="rounded-xl bg-slate-50 p-3"><dt className="text-xs text-slate-500">Đạm</dt><dd className="mt-1 font-bold">{Math.round(goal.targetProteinG)} g</dd></div>
                    <div className="rounded-xl bg-slate-50 p-3"><dt className="text-xs text-slate-500">Tinh bột</dt><dd className="mt-1 font-bold">{Math.round(goal.targetCarbG)} g</dd></div>
                    <div className="rounded-xl bg-slate-50 p-3"><dt className="text-xs text-slate-500">Chất béo</dt><dd className="mt-1 font-bold">{Math.round(goal.targetFatG)} g</dd></div>
                  </dl>
                </>
              ) : <p className="mt-4 text-slate-600">Nhập thông tin để tính mục tiêu cá nhân.</p>}
            </div>

            <div className="rounded-2xl border border-slate-200 bg-white p-6 shadow-sm">
              <div className="flex flex-wrap items-end justify-between gap-3">
                <div><h2 className="text-xl font-bold text-slate-900">Đã tiêu thụ</h2><p className="text-sm text-slate-500">Tổng hợp từ bữa ăn được đánh dấu đã dùng.</p></div>
                <div className="flex items-end gap-2">
                  <label className="text-sm font-medium">Ngày<input className={`${fieldClass} mt-1`} type="date" value={date} onChange={(event) => { setDate(event.target.value); void loadSummary(event.target.value); }} /></label>
                  <label className="text-sm font-medium">Kỳ
                    <select className={`${fieldClass} mt-1`} value={period} onChange={(event) => {
                      const nextPeriod = event.target.value as "day" | "week";
                      setPeriod(nextPeriod);
                      void loadSummary(date, nextPeriod);
                    }}>
                      <option value="day">Ngày</option><option value="week">Tuần</option>
                    </select>
                  </label>
                </div>
              </div>
              {summary && (
                <>
                  <p className="mt-5 text-2xl font-bold">{Math.round(summary.calories).toLocaleString("vi-VN")} <span className="text-base font-medium text-slate-500">/ {calorieTarget ? Math.round(calorieTarget).toLocaleString("vi-VN") : "—"} kcal</span></p>
                  <div className="mt-2 h-3 overflow-hidden rounded-full bg-slate-100" role="progressbar" aria-valuenow={progress} aria-valuemin={0} aria-valuemax={100} aria-label="Mức hoàn thành mục tiêu calo">
                    <div className="h-full rounded-full bg-emerald-600 transition-all" style={{ width: `${progress}%` }} />
                  </div>
                  <p className="mt-2 text-xs text-slate-500">{summary.consumedMeals} bữa đã ghi nhận</p>
                  <dl className="mt-4 grid grid-cols-3 gap-3 text-sm">
                    <div><dt className="text-slate-500">Đạm</dt><dd className="font-semibold">{Math.round(summary.proteinG)} g</dd></div>
                    <div><dt className="text-slate-500">Tinh bột</dt><dd className="font-semibold">{Math.round(summary.carbohydratesG)} g</dd></div>
                    <div><dt className="text-slate-500">Chất béo</dt><dd className="font-semibold">{Math.round(summary.fatG)} g</dd></div>
                  </dl>
                </>
              )}
            </div>
          </section>
        </div>
      )}
      <p className="mt-6 text-xs text-slate-500">Đây là ước tính tham khảo, không thay thế tư vấn y tế hoặc dinh dưỡng chuyên môn.</p>
    </main>
  );
}
