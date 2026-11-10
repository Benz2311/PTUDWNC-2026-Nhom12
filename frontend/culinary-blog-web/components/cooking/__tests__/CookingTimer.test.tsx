import React from 'react';
import { render, screen, fireEvent, act } from '@testing-library/react';
import { CookingTimer } from '../CookingTimer';
import { useCookingTimer } from '@/hooks/useCookingTimer';

// Component wrapper để test hook useCookingTimer cùng với CookingTimer
function TimerTestHarness({
  initialSteps,
  stepNumber = 1,
}: {
  initialSteps: Array<{ stepNumber: number; timerMinutes?: number | null }>;
  stepNumber?: number;
}) {
  const { timers, startTimer, pauseTimer, resumeTimer, resetTimer, adjustTimer } =
    useCookingTimer(initialSteps);

  const currentTimer = timers[stepNumber];

  return (
    <div>
      <CookingTimer
        timer={currentTimer}
        stepNumber={stepNumber}
        onStart={() => startTimer(stepNumber)}
        onPause={() => pauseTimer(stepNumber)}
        onResume={() => resumeTimer(stepNumber)}
        onReset={() => resetTimer(stepNumber)}
        onAdjust={(delta) => adjustTimer(stepNumber, delta)}
      />
    </div>
  );
}

describe('Smart Cooking Timer Tests', () => {
  beforeEach(() => {
    jest.useFakeTimers();
  });

  afterEach(() => {
    act(() => {
      jest.clearAllTimers();
    });
    jest.useRealTimers();
  });

  it('1. Hiển thị thông báo khi bước nấu không yêu cầu hẹn giờ (0 phút)', () => {
    render(<TimerTestHarness initialSteps={[{ stepNumber: 1, timerMinutes: 0 }]} />);

    expect(screen.getByText('Bước này không yêu cầu thời gian nấu cụ thể.')).toBeInTheDocument();
    expect(screen.getByTestId('btn-add-quick-timer-1m')).toBeInTheDocument();
    expect(screen.getByTestId('btn-add-quick-timer-5m')).toBeInTheDocument();
  });

  it('2. Khởi tạo với thời gian chuẩn từ step.timerMinutes và hiển thị dạng MM:SS', () => {
    render(<TimerTestHarness initialSteps={[{ stepNumber: 1, timerMinutes: 10 }]} />);

    expect(screen.getByText('Đồng hồ hẹn giờ bước 1')).toBeInTheDocument();
    expect(screen.getByTestId('timer-display')).toHaveTextContent('10:00');
    expect(screen.getByTestId('btn-timer-start')).toBeInTheDocument();
  });

  it('3. Bắt đầu bộ đếm giờ (Start) và chuyển sang trạng thái "Đang đếm"', () => {
    render(<TimerTestHarness initialSteps={[{ stepNumber: 1, timerMinutes: 5 }]} />);

    const startBtn = screen.getByTestId('btn-timer-start');
    fireEvent.click(startBtn);

    expect(screen.getByText('Đang đếm')).toBeInTheDocument();
    expect(screen.getByTestId('btn-timer-pause')).toBeInTheDocument();

    // Giả lập trôi qua 3 giây
    act(() => {
      jest.advanceTimersByTime(3000);
    });

    expect(screen.getByTestId('timer-display')).toHaveTextContent('04:57');
  });

  it('4. Tạm dừng bộ đếm giờ (Pause) và bảo toàn số giây còn lại', () => {
    render(<TimerTestHarness initialSteps={[{ stepNumber: 1, timerMinutes: 5 }]} />);

    // Start
    fireEvent.click(screen.getByTestId('btn-timer-start'));

    act(() => {
      jest.advanceTimersByTime(5000);
    });
    expect(screen.getByTestId('timer-display')).toHaveTextContent('04:55');

    // Pause
    fireEvent.click(screen.getByTestId('btn-timer-pause'));
    expect(screen.getByText('Tạm dừng')).toBeInTheDocument();
    expect(screen.getByTestId('btn-timer-resume')).toBeInTheDocument();

    // Trong lúc pause, thời gian không được giảm
    act(() => {
      jest.advanceTimersByTime(5000);
    });
    expect(screen.getByTestId('timer-display')).toHaveTextContent('04:55');
  });

  it('5. Tiếp tục bộ đếm giờ (Resume) và tính lại mốc thời gian kết thúc', () => {
    render(<TimerTestHarness initialSteps={[{ stepNumber: 1, timerMinutes: 2 }]} />);

    fireEvent.click(screen.getByTestId('btn-timer-start'));
    act(() => {
      jest.advanceTimersByTime(10000); // Còn 1:50 (110s)
    });
    expect(screen.getByTestId('timer-display')).toHaveTextContent('01:50');

    fireEvent.click(screen.getByTestId('btn-timer-pause'));

    // Resume
    fireEvent.click(screen.getByTestId('btn-timer-resume'));
    expect(screen.getByText('Đang đếm')).toBeInTheDocument();

    act(() => {
      jest.advanceTimersByTime(5000); // Còn 1:45
    });
    expect(screen.getByTestId('timer-display')).toHaveTextContent('01:45');
  });

  it('6. Đặt lại (Reset) bộ đếm giờ về thời gian ban đầu của bước', () => {
    render(<TimerTestHarness initialSteps={[{ stepNumber: 1, timerMinutes: 3 }]} />);

    fireEvent.click(screen.getByTestId('btn-timer-start'));
    act(() => {
      jest.advanceTimersByTime(40000);
    });
    expect(screen.getByTestId('timer-display')).toHaveTextContent('02:20');

    // Bấm đặt lại
    fireEvent.click(screen.getByTestId('btn-timer-reset'));
    expect(screen.getByTestId('timer-display')).toHaveTextContent('03:00');
    expect(screen.getByTestId('btn-timer-start')).toBeInTheDocument();
  });

  it('7. Nút tăng giảm nhanh (+1m, -1m) cập nhật thời gian đếm ngược chính xác', () => {
    render(<TimerTestHarness initialSteps={[{ stepNumber: 1, timerMinutes: 5 }]} />);

    const plusBtn = screen.getByTestId('btn-adjust-plus-1m');
    fireEvent.click(plusBtn);
    expect(screen.getByTestId('timer-display')).toHaveTextContent('06:00');

    const minusBtn = screen.getByTestId('btn-adjust-minus-1m');
    fireEvent.click(minusBtn);
    expect(screen.getByTestId('timer-display')).toHaveTextContent('05:00');
  });

  it('8. Hiển thị huy hiệu "HẾT GIỜ!" và nút Bắt đầu lại khi timer về 0', () => {
    render(<TimerTestHarness initialSteps={[{ stepNumber: 1, timerMinutes: 1 }]} />);

    fireEvent.click(screen.getByTestId('btn-timer-start'));

    // Cho timer chạy hết 60s
    act(() => {
      jest.advanceTimersByTime(61000);
    });

    expect(screen.getByTestId('timer-display')).toHaveTextContent('00:00');
    expect(screen.getByTestId('timer-alarm-badge')).toHaveTextContent('HẾT GIỜ!');
    expect(screen.getByTestId('btn-timer-restart')).toBeInTheDocument();
  });
});
