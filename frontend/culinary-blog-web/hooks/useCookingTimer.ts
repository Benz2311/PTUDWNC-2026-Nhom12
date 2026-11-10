'use client';

import { useState, useEffect, useRef, useCallback } from 'react';

export type TimerStatus = 'idle' | 'running' | 'paused' | 'completed';

export interface StepTimerState {
  stepNumber: number;
  totalSeconds: number;
  remainingSeconds: number;
  status: TimerStatus;
  targetEndTime: number | null; // Timestamp (ms) khi timer kết thúc
}

/**
 * Phát âm thanh thông báo kết thúc nấu ăn bằng Web Audio API
 * Đảm bảo 0 phụ thuộc file MP3 ngoài, không gây 404, chạy offline mượt mà.
 */
export function playTimerCompletionSound(): void {
  if (typeof window === 'undefined') return;
  try {
    const AudioContextClass = window.AudioContext || (window as unknown as { webkitAudioContext: typeof AudioContext }).webkitAudioContext;
    if (!AudioContextClass) return;

    const ctx = new AudioContextClass();
    if (ctx.state === 'suspended') {
      ctx.resume().catch(() => {});
    }

    const now = ctx.currentTime;
    
    // Nốt 1 (E5 - 659.25Hz)
    const osc1 = ctx.createOscillator();
    const gain1 = ctx.createGain();
    osc1.type = 'sine';
    osc1.frequency.setValueAtTime(659.25, now);
    gain1.gain.setValueAtTime(0.3, now);
    gain1.gain.exponentialRampToValueAtTime(0.001, now + 0.35);
    osc1.connect(gain1);
    gain1.connect(ctx.destination);
    osc1.start(now);
    osc1.stop(now + 0.35);

    // Nốt 2 (A5 - 880Hz)
    const osc2 = ctx.createOscillator();
    const gain2 = ctx.createGain();
    osc2.type = 'sine';
    osc2.frequency.setValueAtTime(880, now + 0.2);
    gain2.gain.setValueAtTime(0.35, now + 0.2);
    gain2.gain.exponentialRampToValueAtTime(0.001, now + 0.6);
    osc2.connect(gain2);
    gain2.connect(ctx.destination);
    osc2.start(now + 0.2);
    osc2.stop(now + 0.6);
  } catch {
    // Trình duyệt chặn autoplay hoặc không có audio context
  }
}

/**
 * Hook quản lý Timer thông minh cho Cooking Mode:
 * - Lưu mốc thời gian thực targetEndTime (chống lệch giờ khi chuyển tab / background throttling).
 * - Mỗi bước (Step) có trạng thái timer độc lập.
 * - Hỗ trợ Start, Pause, Resume, Reset và Thêm/Bớt phút.
 */
export function useCookingTimer(steps: Array<{ stepNumber: number; timerMinutes?: number | null }>) {
  // Khởi tạo state timer cho từng bước dựa trên timerMinutes
  const [timers, setTimers] = useState<Record<number, StepTimerState>>(() => {
    const initial: Record<number, StepTimerState> = {};
    steps.forEach((s) => {
      const minutes = s.timerMinutes && s.timerMinutes > 0 ? s.timerMinutes : 0;
      const totalSec = Math.round(minutes * 60);
      initial[s.stepNumber] = {
        stepNumber: s.stepNumber,
        totalSeconds: totalSec,
        remainingSeconds: totalSec,
        status: 'idle',
        targetEndTime: null,
      };
    });
    return initial;
  });

  const onCompleteRef = useRef<((stepNumber: number) => void) | null>(null);

  // Cập nhật timers khi danh sách steps thay đổi
  useEffect(() => {
    setTimers((prev) => {
      const updated = { ...prev };
      steps.forEach((s) => {
        if (!updated[s.stepNumber]) {
          const minutes = s.timerMinutes && s.timerMinutes > 0 ? s.timerMinutes : 0;
          const totalSec = Math.round(minutes * 60);
          updated[s.stepNumber] = {
            stepNumber: s.stepNumber,
            totalSeconds: totalSec,
            remainingSeconds: totalSec,
            status: 'idle',
            targetEndTime: null,
          };
        }
      });
      return updated;
    });
  }, [steps]);

  // Vòng lặp đếm giờ chính xác bằng mốc thời gian thực Date.now()
  useEffect(() => {
    const interval = setInterval(() => {
      const now = Date.now();
      setTimers((currentTimers) => {
        let hasChanges = false;
        const nextTimers = { ...currentTimers };

        Object.keys(nextTimers).forEach((stepNumStr) => {
          const stepNum = Number(stepNumStr);
          const t = nextTimers[stepNum];

          if (t && t.status === 'running' && t.targetEndTime !== null) {
            const diffMs = t.targetEndTime - now;
            const remaining = Math.max(0, Math.ceil(diffMs / 1000));

            if (remaining !== t.remainingSeconds) {
              hasChanges = true;
              if (remaining <= 0) {
                // Timer hết giờ
                nextTimers[stepNum] = {
                  ...t,
                  remainingSeconds: 0,
                  status: 'completed',
                  targetEndTime: null,
                };
                playTimerCompletionSound();
                if (onCompleteRef.current) {
                  onCompleteRef.current(stepNum);
                }
              } else {
                nextTimers[stepNum] = {
                  ...t,
                  remainingSeconds: remaining,
                };
              }
            }
          }
        });

        return hasChanges ? nextTimers : currentTimers;
      });
    }, 250);

    return () => clearInterval(interval);
  }, []);

  const startTimer = useCallback((stepNumber: number, customSeconds?: number) => {
    setTimers((prev) => {
      const current = prev[stepNumber];
      const duration = customSeconds ?? (current ? current.remainingSeconds : 0);
      if (duration <= 0) return prev;

      const targetEnd = Date.now() + duration * 1000;
      return {
        ...prev,
        [stepNumber]: {
          stepNumber,
          totalSeconds: current?.totalSeconds && current.totalSeconds >= duration ? current.totalSeconds : duration,
          remainingSeconds: duration,
          status: 'running',
          targetEndTime: targetEnd,
        },
      };
    });
  }, []);

  const pauseTimer = useCallback((stepNumber: number) => {
    setTimers((prev) => {
      const current = prev[stepNumber];
      if (!current || current.status !== 'running' || current.targetEndTime === null) return prev;

      const remaining = Math.max(0, Math.ceil((current.targetEndTime - Date.now()) / 1000));
      return {
        ...prev,
        [stepNumber]: {
          ...current,
          remainingSeconds: remaining,
          status: 'paused',
          targetEndTime: null,
        },
      };
    });
  }, []);

  const resumeTimer = useCallback((stepNumber: number) => {
    setTimers((prev) => {
      const current = prev[stepNumber];
      if (!current || current.status !== 'paused' || current.remainingSeconds <= 0) return prev;

      const targetEnd = Date.now() + current.remainingSeconds * 1000;
      return {
        ...prev,
        [stepNumber]: {
          ...current,
          status: 'running',
          targetEndTime: targetEnd,
        },
      };
    });
  }, []);

  const resetTimer = useCallback((stepNumber: number, newTotalSeconds?: number) => {
    setTimers((prev) => {
      const current = prev[stepNumber];
      const initialStep = steps.find((s) => s.stepNumber === stepNumber);
      const defaultSec = Math.round((initialStep?.timerMinutes ?? 0) * 60);
      const targetSec = newTotalSeconds !== undefined ? newTotalSeconds : (current?.totalSeconds || defaultSec);

      return {
        ...prev,
        [stepNumber]: {
          stepNumber,
          totalSeconds: targetSec,
          remainingSeconds: targetSec,
          status: 'idle',
          targetEndTime: null,
        },
      };
    });
  }, [steps]);

  const adjustTimer = useCallback((stepNumber: number, deltaSeconds: number) => {
    setTimers((prev) => {
      const current = prev[stepNumber];
      if (!current) return prev;

      const newRemaining = Math.max(0, current.remainingSeconds + deltaSeconds);
      const newTotal = Math.max(current.totalSeconds, newRemaining);

      if (current.status === 'running') {
        return {
          ...prev,
          [stepNumber]: {
            ...current,
            totalSeconds: newTotal,
            remainingSeconds: newRemaining,
            targetEndTime: Date.now() + newRemaining * 1000,
          },
        };
      }

      return {
        ...prev,
        [stepNumber]: {
          ...current,
          totalSeconds: newTotal,
          remainingSeconds: newRemaining,
          status: newRemaining === 0 ? 'completed' : current.status,
        },
      };
    });
  }, []);

  return {
    timers,
    startTimer,
    pauseTimer,
    resumeTimer,
    resetTimer,
    adjustTimer,
    setOnComplete: (cb: (stepNumber: number) => void) => {
      onCompleteRef.current = cb;
    },
  };
}

export function formatTimeDisplay(seconds: number): string {
  const clamped = Math.max(0, Math.floor(seconds));
  const h = Math.floor(clamped / 3600);
  const m = Math.floor((clamped % 3600) / 60);
  const s = clamped % 60;

  if (h > 0) {
    return `${String(h).padStart(2, '0')}:${String(m).padStart(2, '0')}:${String(s).padStart(2, '0')}`;
  }
  return `${String(m).padStart(2, '0')}:${String(s).padStart(2, '0')}`;
}

