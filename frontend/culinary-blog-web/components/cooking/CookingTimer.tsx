'use client';

import React from 'react';
import { StepTimerState, formatTimeDisplay } from '@/hooks/useCookingTimer';

export interface CookingTimerProps {
  timer?: StepTimerState;
  stepNumber: number;
  onStart: () => void;
  onPause: () => void;
  onResume: () => void;
  onReset: () => void;
  onAdjust: (deltaSeconds: number) => void;
}

export function CookingTimer({
  timer,
  stepNumber,
  onStart,
  onPause,
  onResume,
  onReset,
  onAdjust,
}: CookingTimerProps) {
  const total = timer?.totalSeconds ?? 0;
  const remaining = timer?.remainingSeconds ?? 0;
  const status = timer?.status ?? 'idle';

  // Tính phần trăm tiến trình đếm ngược
  const progressPercent = total > 0 ? Math.min(100, Math.max(0, ((total - remaining) / total) * 100)) : 0;

  // Nếu bước này không có thời gian ban đầu (0 giây) và chưa bắt đầu
  if (total === 0 && status === 'idle') {
    return (
      <div
        data-testid={`cooking-timer-step-${stepNumber}`}
        className="p-4 rounded-2xl bg-slate-50 border border-slate-200/80 flex flex-col sm:flex-row items-start sm:items-center justify-between gap-3"
      >
        <div className="flex items-center gap-2.5 text-slate-600 text-sm">
          <svg className="w-5 h-5 text-slate-400 shrink-0" fill="none" stroke="currentColor" viewBox="0 0 24 24">
            <circle cx="12" cy="12" r="10" strokeWidth="2" />
            <polyline points="12 6 12 12 16 14" strokeWidth="2" />
          </svg>
          <span>Bước này không yêu cầu thời gian nấu cụ thể.</span>
        </div>
        <div className="flex items-center gap-1.5 shrink-0">
          <button
            type="button"
            data-testid="btn-add-quick-timer-1m"
            onClick={() => {
              onAdjust(60);
              onStart();
            }}
            className="text-xs font-semibold px-2.5 py-1.5 rounded-lg bg-emerald-50 text-emerald-800 hover:bg-emerald-100 border border-emerald-200 transition-colors"
          >
            +1 phút
          </button>
          <button
            type="button"
            data-testid="btn-add-quick-timer-5m"
            onClick={() => {
              onAdjust(300);
              onStart();
            }}
            className="text-xs font-semibold px-2.5 py-1.5 rounded-lg bg-emerald-50 text-emerald-800 hover:bg-emerald-100 border border-emerald-200 transition-colors"
          >
            +5 phút
          </button>
        </div>
      </div>
    );
  }

  const isCompleted = status === 'completed';
  const isRunning = status === 'running';
  const isPaused = status === 'paused';

  return (
    <div
      data-testid={`cooking-timer-step-${stepNumber}`}
      className={`relative overflow-hidden p-5 rounded-2xl border transition-all duration-300 ${
        isCompleted
          ? 'bg-rose-50/70 border-rose-300 shadow-md ring-2 ring-rose-400/50'
          : isRunning
          ? 'bg-gradient-to-r from-emerald-50/80 via-white to-emerald-50/80 border-emerald-300 shadow-sm'
          : 'bg-white border-slate-200 shadow-xs'
      }`}
    >
      {/* Background progress bar */}
      <div
        className="absolute top-0 left-0 bottom-0 bg-emerald-100/40 pointer-events-none transition-all duration-300"
        style={{ width: `${progressPercent}%` }}
      />

      <div className="relative z-10 flex flex-col sm:flex-row items-center justify-between gap-4">
        {/* Left: Clock Icon & Title */}
        <div className="flex items-center gap-3">
          <div
            className={`w-12 h-12 rounded-xl flex items-center justify-center shrink-0 transition-colors ${
              isCompleted
                ? 'bg-rose-600 text-white animate-bounce'
                : isRunning
                ? 'bg-emerald-700 text-white animate-pulse'
                : isPaused
                ? 'bg-amber-100 text-amber-800'
                : 'bg-emerald-100 text-emerald-800'
            }`}
          >
            {isCompleted ? (
              <svg className="w-6 h-6" fill="none" stroke="currentColor" viewBox="0 0 24 24">
                <path strokeLinecap="round" strokeLinejoin="round" strokeWidth="2.5" d="M15 17h5l-1.405-1.405A2.032 2.032 0 0118 14.158V11a6.002 6.002 0 00-4-5.659V5a2 2 0 10-4 0v.341C7.67 6.165 6 8.388 6 11v3.159c0 .538-.214 1.055-.595 1.436L4 17h5m6 0v1a3 3 0 11-6 0v-1m6 0H9" />
              </svg>
            ) : (
              <svg className="w-6 h-6" fill="none" stroke="currentColor" viewBox="0 0 24 24">
                <circle cx="12" cy="12" r="10" strokeWidth="2" />
                <polyline points="12 6 12 12 16 14" strokeWidth="2" />
              </svg>
            )}
          </div>

          <div>
            <div className="flex items-center gap-2">
              <span className="text-xs font-semibold uppercase tracking-wider text-slate-500">
                Đồng hồ hẹn giờ bước {stepNumber}
              </span>
              {isRunning && (
                <span className="inline-flex items-center gap-1 text-[11px] font-bold px-2 py-0.5 rounded-full bg-emerald-100 text-emerald-800 border border-emerald-200">
                  <span className="w-1.5 h-1.5 rounded-full bg-emerald-500 animate-ping" />
                  Đang đếm
                </span>
              )}
              {isPaused && (
                <span className="text-[11px] font-bold px-2 py-0.5 rounded-full bg-amber-100 text-amber-800 border border-amber-200">
                  Tạm dừng
                </span>
              )}
              {isCompleted && (
                <span
                  data-testid="timer-alarm-badge"
                  className="text-[11px] font-bold px-2 py-0.5 rounded-full bg-rose-600 text-white animate-pulse"
                >
                  HẾT GIỜ!
                </span>
              )}
            </div>

            {/* Time display text */}
            <div
              data-testid="timer-display"
              className={`text-3xl sm:text-4xl font-extrabold tracking-tight font-mono transition-colors ${
                isCompleted
                  ? 'text-rose-600'
                  : isRunning
                  ? 'text-emerald-800'
                  : 'text-slate-900'
              }`}
            >
              {formatTimeDisplay(remaining)}
            </div>
          </div>
        </div>

        {/* Right: Action Buttons */}
        <div className="flex flex-wrap items-center justify-center sm:justify-end gap-2 w-full sm:w-auto">
          {/* Quick adjust buttons */}
          <div className="flex items-center gap-1">
            <button
              type="button"
              data-testid="btn-adjust-minus-1m"
              onClick={() => onAdjust(-60)}
              disabled={remaining <= 60}
              className="px-2 py-1.5 rounded-lg text-xs font-semibold bg-slate-100 hover:bg-slate-200 text-slate-700 disabled:opacity-40 disabled:cursor-not-allowed transition-colors"
              title="Giảm 1 phút"
            >
              -1m
            </button>
            <button
              type="button"
              data-testid="btn-adjust-plus-1m"
              onClick={() => onAdjust(60)}
              className="px-2 py-1.5 rounded-lg text-xs font-semibold bg-slate-100 hover:bg-slate-200 text-slate-700 transition-colors"
              title="Thêm 1 phút"
            >
              +1m
            </button>
          </div>

          {/* Primary State Button */}
          {status === 'idle' && (
            <button
              type="button"
              data-testid="btn-timer-start"
              onClick={onStart}
              className="inline-flex items-center gap-1.5 px-4 py-2 rounded-xl text-sm font-bold text-white bg-emerald-700 hover:bg-emerald-800 transition-colors shadow-xs"
            >
              <svg className="w-4 h-4 fill-current" viewBox="0 0 24 24">
                <polygon points="5 3 19 12 5 21 5 3" />
              </svg>
              <span>Bắt đầu</span>
            </button>
          )}

          {status === 'running' && (
            <button
              type="button"
              data-testid="btn-timer-pause"
              onClick={onPause}
              className="inline-flex items-center gap-1.5 px-4 py-2 rounded-xl text-sm font-bold text-amber-900 bg-amber-300 hover:bg-amber-400 transition-colors shadow-xs"
            >
              <svg className="w-4 h-4 fill-current" viewBox="0 0 24 24">
                <rect x="6" y="4" width="4" height="16" />
                <rect x="14" y="4" width="4" height="16" />
              </svg>
              <span>Tạm dừng</span>
            </button>
          )}

          {status === 'paused' && (
            <button
              type="button"
              data-testid="btn-timer-resume"
              onClick={onResume}
              className="inline-flex items-center gap-1.5 px-4 py-2 rounded-xl text-sm font-bold text-white bg-emerald-700 hover:bg-emerald-800 transition-colors shadow-xs"
            >
              <svg className="w-4 h-4 fill-current" viewBox="0 0 24 24">
                <polygon points="5 3 19 12 5 21 5 3" />
              </svg>
              <span>Tiếp tục</span>
            </button>
          )}

          {isCompleted && (
            <button
              type="button"
              data-testid="btn-timer-restart"
              onClick={onReset}
              className="inline-flex items-center gap-1.5 px-4 py-2 rounded-xl text-sm font-bold text-white bg-rose-600 hover:bg-rose-700 transition-colors shadow-xs"
            >
              <svg className="w-4 h-4" fill="none" stroke="currentColor" viewBox="0 0 24 24">
                <path strokeLinecap="round" strokeLinejoin="round" strokeWidth="2" d="M4 4v5h.582m15.356 2A8.001 8.001 0 004.582 9m0 0H9m11 11v-5h-.581m0 0a8.003 8.003 0 01-15.357-2m15.357 2H15" />
              </svg>
              <span>Bắt đầu lại</span>
            </button>
          )}

          {/* Reset button */}
          {(status === 'running' || status === 'paused') && (
            <button
              type="button"
              data-testid="btn-timer-reset"
              onClick={onReset}
              className="px-3 py-2 rounded-xl text-sm font-medium text-slate-600 hover:text-slate-900 hover:bg-slate-100 transition-colors"
              title="Đặt lại về thời gian ban đầu"
            >
              Đặt lại
            </button>
          )}
        </div>
      </div>
    </div>
  );
}

export default CookingTimer;

