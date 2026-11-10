import React from 'react';
import { render, screen, fireEvent } from '@testing-library/react';
import { CookingMode } from '../CookingMode';
import type { RecipeDetail } from '@/types/recipe';

const mockPush = jest.fn();
jest.mock('next/navigation', () => ({
  useRouter: () => ({
    push: mockPush,
  }),
}));

const sampleRecipe: RecipeDetail = {
  id: 'rec-test-001',
  title: 'Phở Bò Tái Nạm Truyền Thống',
  slug: 'pho-bo-tai-nam-truyen-thong',
  description: 'Nước dùng phở ngọt sâu từ xương ống bò.',
  content: 'Cách làm phở bò chuẩn vị.',
  prepTimeMinutes: 30,
  cookTimeMinutes: 180,
  servings: 4,
  difficulty: 2,
  status: 'Published',
  publishedAt: '2026-11-01T00:00:00Z',
  createdAt: '2026-11-01T00:00:00Z',
  updatedAt: null,
  category: {
    id: 'cat-001',
    name: 'Bún, Mì & Phở',
    slug: 'bun-mi-pho',
  },
  author: {
    id: 'usr-001',
    userName: 'bep_truong',
    displayName: 'Bếp Trưởng Hà Nội',
    avatarUrl: null,
  },
  ingredients: [
    { id: 'ing-1', name: 'Xương ống bò', quantity: 2, unit: 'kg', notes: 'Rửa sạch chặt khúc' },
    { id: 'ing-2', name: 'Thịt bò tái', quantity: 500, unit: 'g', notes: 'Thái lát mỏng' },
    { id: 'ing-3', name: 'Bánh phở tươi', quantity: 1, unit: 'kg', notes: 'Trần qua nước sôi' },
  ],
  steps: [
    {
      id: 'step-1',
      stepNumber: 1,
      title: 'Sơ chế và ninh xương ống',
      description: 'Chần xương bò với nước sôi gừng đập dập rồi ninh nhỏ lửa 8 tiếng.',
      timerMinutes: 120,
      imageUrl: 'https://example.com/step1.jpg',
    },
    {
      id: 'step-2',
      stepNumber: 2,
      title: 'Rang thảo mộc thơm',
      description: 'Rang hoa hồi, quế chi, thảo quả cho dậy mùi thơm nức.',
      timerMinutes: 15,
      imageUrl: null,
    },
    {
      id: 'step-3',
      stepNumber: 3,
      title: 'Hoàn thiện và thưởng thức',
      description: 'Xếp bánh phở, thịt bò tái và chan nước dùng sôi sùng sục.',
      timerMinutes: 5,
      imageUrl: 'https://example.com/step3.jpg',
    },
  ],
  images: [],
};

describe('Cooking Mode Component Tests', () => {
  beforeEach(() => {
    mockPush.mockClear();
  });

  it('1. Render Cooking Mode với tiêu đề, bước 1, step counter và progress bar', () => {
    render(<CookingMode recipe={sampleRecipe} />);

    expect(screen.getByText('Phở Bò Tái Nạm Truyền Thống')).toBeInTheDocument();
    expect(screen.getByTestId('step-counter')).toHaveTextContent('Bước 1 / 3');
    expect(screen.getByText('BƯỚC 01')).toBeInTheDocument();
    expect(screen.getByTestId('step-title')).toHaveTextContent('Sơ chế và ninh xương ống');
    expect(screen.getByTestId('step-description')).toHaveTextContent(
      'Chần xương bò với nước sôi gừng đập dập rồi ninh nhỏ lửa 8 tiếng.'
    );
    expect(screen.getByTestId('cooking-progress-bar')).toHaveStyle({ width: `${(1 / 3) * 100}%` });
  });

  it('2. Nút "Bước trước" bị disabled ở bước đầu tiên (Bước 1)', () => {
    render(<CookingMode recipe={sampleRecipe} />);

    const prevBtn = screen.getByTestId('btn-prev-step');
    expect(prevBtn).toBeDisabled();
  });

  it('3. Bấm "Tiếp theo" chuyển sang bước 2, kích hoạt nút "Bước trước"', () => {
    render(<CookingMode recipe={sampleRecipe} />);

    const nextBtn = screen.getByTestId('btn-next-step');
    fireEvent.click(nextBtn);

    expect(screen.getByTestId('step-counter')).toHaveTextContent('Bước 2 / 3');
    expect(screen.getByText('BƯỚC 02')).toBeInTheDocument();
    expect(screen.getByTestId('step-title')).toHaveTextContent('Rang thảo mộc thơm');

    const prevBtn = screen.getByTestId('btn-prev-step');
    expect(prevBtn).not.toBeDisabled();
  });

  it('4. Bấm "Tiếp theo" đến bước cuối cùng (Bước 3) hiển thị nút "Hoàn thành ✓"', () => {
    render(<CookingMode recipe={sampleRecipe} />);

    // Bước 1 -> Bước 2
    fireEvent.click(screen.getByTestId('btn-next-step'));
    // Bước 2 -> Bước 3
    fireEvent.click(screen.getByTestId('btn-next-step'));

    expect(screen.getByTestId('step-counter')).toHaveTextContent('Bước 3 / 3');
    expect(screen.queryByTestId('btn-next-step')).not.toBeInTheDocument();

    const finishBtn = screen.getByTestId('btn-finish-cooking');
    expect(finishBtn).toBeInTheDocument();

    fireEvent.click(finishBtn);
    expect(mockPush).toHaveBeenCalledWith('/recipes/pho-bo-tai-nam-truyen-thong');
  });

  it('5. Chuyển bước nhanh bằng các nút điều hướng số bước (step nav pills)', () => {
    render(<CookingMode recipe={sampleRecipe} />);

    const jumpStep3 = screen.getByTestId('btn-jump-step-3');
    fireEvent.click(jumpStep3);

    expect(screen.getByTestId('step-counter')).toHaveTextContent('Bước 3 / 3');
    expect(screen.getByText('BƯỚC 03')).toBeInTheDocument();
    expect(screen.getByTestId('step-title')).toHaveTextContent('Hoàn thiện và thưởng thức');
  });

  it('6. Mở và đóng bảng danh sách nguyên liệu (Ingredients Checklist)', () => {
    render(<CookingMode recipe={sampleRecipe} />);

    const toggleBtn = screen.getByTestId('btn-toggle-ingredients');
    fireEvent.click(toggleBtn);

    expect(screen.getByTestId('ingredients-drawer')).toBeInTheDocument();
    expect(screen.getByText('Xương ống bò')).toBeInTheDocument();
    expect(screen.getByText('Thịt bò tái')).toBeInTheDocument();
    expect(screen.getByText('Bánh phở tươi')).toBeInTheDocument();

    const closeBtn = screen.getByTestId('btn-close-ingredients');
    fireEvent.click(closeBtn);

    expect(screen.queryByTestId('ingredients-drawer')).not.toBeInTheDocument();
  });

  it('7. Check và uncheck nguyên liệu cập nhật số lượng đã chuẩn bị', () => {
    render(<CookingMode recipe={sampleRecipe} />);

    fireEvent.click(screen.getByTestId('btn-toggle-ingredients'));

    const checkboxes = screen.getAllByRole('checkbox');
    expect(checkboxes).toHaveLength(3);

    // Check nguyên liệu 1
    fireEvent.click(checkboxes[0]);
    expect(checkboxes[0]).toBeChecked();

    // Check nguyên liệu 2
    fireEvent.click(checkboxes[1]);
    expect(checkboxes[1]).toBeChecked();

    // Uncheck nguyên liệu 1
    fireEvent.click(checkboxes[0]);
    expect(checkboxes[0]).not.toBeChecked();
  });

  it('8. Điều hướng bước bằng phím mũi tên ArrowRight và ArrowLeft trên bàn phím', () => {
    render(<CookingMode recipe={sampleRecipe} />);

    expect(screen.getByTestId('step-counter')).toHaveTextContent('Bước 1 / 3');

    // Nhấn ArrowRight -> Bước 2
    fireEvent.keyDown(window, { key: 'ArrowRight' });
    expect(screen.getByTestId('step-counter')).toHaveTextContent('Bước 2 / 3');

    // Nhấn ArrowLeft -> Bước 1
    fireEvent.keyDown(window, { key: 'ArrowLeft' });
    expect(screen.getByTestId('step-counter')).toHaveTextContent('Bước 1 / 3');
  });

  it('9. Hiển thị thông báo thân thiện khi Recipe không có bước nào', () => {
    const emptyStepsRecipe = { ...sampleRecipe, steps: [] };
    render(<CookingMode recipe={emptyStepsRecipe} />);

    expect(screen.getByText('Công thức chưa có bước nấu')).toBeInTheDocument();
    expect(screen.getByText('← Quay lại công thức')).toBeInTheDocument();
  });

  it('10. Nút Thoát chế độ nấu trỏ về trang chi tiết công thức', () => {
    render(<CookingMode recipe={sampleRecipe} />);

    const exitBtn = screen.getByTestId('btn-exit-cooking');
    expect(exitBtn).toHaveAttribute('href', '/recipes/pho-bo-tai-nam-truyen-thong');
  });
});
