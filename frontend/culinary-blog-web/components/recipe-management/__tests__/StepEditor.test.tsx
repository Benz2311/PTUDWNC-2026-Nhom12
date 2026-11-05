import React from 'react';
import { render, screen, fireEvent, waitFor } from '@testing-library/react';
import userEvent from '@testing-library/user-event';
import { StepEditor } from '../StepEditor';
import type { RecipeStep } from '@/types/step';
import * as stepsApi from '@/lib/api/steps';
import { useAuthStore } from '@/lib/auth-store';

// Mock toàn bộ module @/lib/api/steps
jest.mock('@/lib/api/steps', () => {
  const actual = jest.requireActual('@/lib/api/steps');
  return {
    ...actual,
    getRecipeSteps: jest.fn(),
    createRecipeStep: jest.fn(),
    updateRecipeStep: jest.fn(),
    deleteRecipeStep: jest.fn(),
    reorderRecipeSteps: jest.fn(),
    uploadStepImageFile: jest.fn(),
  };
});

const mockSteps: RecipeStep[] = [
  {
    id: 'step-1',
    recipeId: 'recipe-100',
    stepNumber: 1,
    title: 'Sơ chế nguyên liệu',
    description: 'Rửa sạch hành lá và thái nhỏ.',
    timerMinutes: 10,
    imageUrl: 'https://example.com/step1.jpg',
    createdAt: '2026-01-01T00:00:00Z',
  },
  {
    id: 'step-2',
    recipeId: 'recipe-100',
    stepNumber: 2,
    title: 'Xào thịt bò',
    description: 'Bật bếp lửa lớn và đảo đều nhanh tay.',
    timerMinutes: 15,
    imageUrl: null,
    createdAt: '2026-01-01T00:00:00Z',
  },
];

describe('StepEditor Component', () => {
  beforeEach(() => {
    jest.clearAllMocks();
    (stepsApi.getRecipeSteps as jest.Mock).mockResolvedValue([]);
    useAuthStore.setState({
      user: null,
      accessToken: null,
      refreshToken: null,
      roles: [],
      isAuthenticated: false,
      isLoading: false,
    });
  });

  // 1. Render đúng StepNumber
  it('1. Render đúng số thứ tự StepNumber (01, 02) và nội dung các bước', () => {
    render(<StepEditor initialSteps={mockSteps} />);

    expect(screen.getByText('01')).toBeInTheDocument();
    expect(screen.getByText('02')).toBeInTheDocument();
    expect(screen.getByText('Sơ chế nguyên liệu')).toBeInTheDocument();
    expect(screen.getByText('Xào thịt bò')).toBeInTheDocument();
    expect(screen.getByText('Rửa sạch hành lá và thái nhỏ.')).toBeInTheDocument();
  });

  // 2. Empty State
  it('2. Hiển thị Empty State khi chưa có bước nào', () => {
    render(<StepEditor initialSteps={[]} />);

    expect(screen.getByTestId('step-empty-state')).toBeInTheDocument();
    expect(screen.getByText('Chưa có bước thực hiện nào')).toBeInTheDocument();
  });

  // 3. Validation Description
  it('3. Validation Description: Bắt buộc và không quá 2000 ký tự', async () => {
    const user = userEvent.setup();
    render(<StepEditor initialSteps={[]} />);

    await user.click(screen.getByTestId('btn-add-step'));
    expect(screen.getByTestId('step-form')).toBeInTheDocument();

    // Để trống description và submit
    await user.click(screen.getByTestId('btn-save-step'));
    expect(screen.getByTestId('error-step-description')).toHaveTextContent(
      'Nội dung hướng dẫn bước là bắt buộc.'
    );

    // Vượt quá 2000 ký tự (dùng fireEvent để tránh gõ chậm 2001 chars)
    const longDesc = 'A'.repeat(2001);
    const textarea = screen.getByTestId('input-step-description');
    fireEvent.change(textarea, { target: { value: longDesc } });
    await user.click(screen.getByTestId('btn-save-step'));
    expect(screen.getByTestId('error-step-description')).toHaveTextContent(
      'Nội dung hướng dẫn không được vượt quá 2000 ký tự.'
    );
  });

  // 4. Validation Title max length
  it('4. Validation Title: Không vượt quá 200 ký tự', async () => {
    const user = userEvent.setup();
    render(<StepEditor initialSteps={[]} />);

    await user.click(screen.getByTestId('btn-add-step'));
    const titleInput = screen.getByTestId('input-step-title');
    const descInput = screen.getByTestId('input-step-description');

    const longTitle = 'B'.repeat(201);
    fireEvent.change(titleInput, { target: { value: longTitle } });
    await user.type(descInput, 'Nội dung hợp lệ');

    await user.click(screen.getByTestId('btn-save-step'));
    expect(screen.getByTestId('error-step-title')).toHaveTextContent(
      'Tiêu đề bước không được vượt quá 200 ký tự.'
    );
  });

  // 5. Validation TimerMinutes 0-1440
  it('5. Validation TimerMinutes: Hợp lệ trong khoảng 0 - 1440', async () => {
    const user = userEvent.setup();
    render(<StepEditor initialSteps={[]} />);

    await user.click(screen.getByTestId('btn-add-step'));
    const timerInput = screen.getByTestId('input-step-timer');
    const descInput = screen.getByTestId('input-step-description');

    await user.type(descInput, 'Nấu súp');

    // Test số âm
    fireEvent.change(timerInput, { target: { value: '-5' } });
    await user.click(screen.getByTestId('btn-save-step'));
    expect(screen.getByTestId('error-step-timer')).toHaveTextContent(
      'Thời gian hẹn giờ phải từ 0 đến 1440 phút'
    );

    // Test số lớn hơn 1440
    fireEvent.change(timerInput, { target: { value: '1441' } });
    await user.click(screen.getByTestId('btn-save-step'));
    expect(screen.getByTestId('error-step-timer')).toHaveTextContent(
      'Thời gian hẹn giờ phải từ 0 đến 1440 phút'
    );
  });

  // 6. Validation ImageUrl
  it('6. Validation ImageUrl: Phải là URL hợp lệ http/https và không quá 500 ký tự (SRS Bảng 7.3)', async () => {
    const user = userEvent.setup();
    render(<StepEditor initialSteps={[]} />);

    await user.click(screen.getByTestId('btn-add-step'));
    const imageInput = screen.getByTestId('input-step-image');
    const descInput = screen.getByTestId('input-step-description');

    await user.type(descInput, 'Chiên cá');

    // Nhập URL sai định dạng
    await user.type(imageInput, 'invalid-url-string');
    await user.click(screen.getByTestId('btn-save-step'));
    expect(screen.getByTestId('error-step-image')).toHaveTextContent(
      'Đường dẫn ảnh không hợp lệ'
    );

    // Nhập URL dài hơn 500 ký tự
    await user.clear(imageInput);
    const longUrl = 'https://example.com/' + 'a'.repeat(500);
    // Directly fire change event to bypass HTML maxLength attribute during test
    fireEvent.change(imageInput, { target: { value: longUrl } });
    await user.click(screen.getByTestId('btn-save-step'));
    expect(screen.getByTestId('error-step-image')).toHaveTextContent(
      'Đường dẫn ảnh không được vượt quá 500 ký tự.'
    );
  });

  // 7. Add Step (Direct API mode)
  it('7. Add Step trong Direct API mode: Gọi createRecipeStep và render step mới', async () => {
    const user = userEvent.setup();
    const createdStep: RecipeStep = {
      id: 'step-3',
      recipeId: 'recipe-100',
      stepNumber: 3,
      title: 'Trình bày món ăn',
      description: 'Múc ra đĩa và rắc tiêu lên trên.',
      timerMinutes: 2,
      imageUrl: null,
      createdAt: '2026-01-01T00:00:00Z',
    };
    (stepsApi.createRecipeStep as jest.Mock).mockResolvedValueOnce(createdStep);

    render(<StepEditor recipeId="recipe-100" initialSteps={mockSteps} />);

    await user.click(screen.getByTestId('btn-add-step'));
    await user.type(screen.getByTestId('input-step-title'), 'Trình bày món ăn');
    await user.type(screen.getByTestId('input-step-description'), 'Múc ra đĩa và rắc tiêu lên trên.');
    await user.type(screen.getByTestId('input-step-timer'), '2');

    await user.click(screen.getByTestId('btn-save-step'));

    await waitFor(() => {
      expect(stepsApi.createRecipeStep).toHaveBeenCalledWith('recipe-100', {
        title: 'Trình bày món ăn',
        description: 'Múc ra đĩa và rắc tiêu lên trên.',
        timerMinutes: 2,
        imageUrl: null,
      });
      // Server là nguồn sự thật: frontend không tự gửi stepNumber
      expect(screen.getByText('Trình bày món ăn')).toBeInTheDocument();
      expect(screen.getByText('03')).toBeInTheDocument();
    });
  });

  // 8. Update Step (Direct API mode)
  it('8. Update Step trong Direct API mode: Gọi updateRecipeStep và cập nhật nội dung', async () => {
    const user = userEvent.setup();
    const updatedStep: RecipeStep = {
      ...mockSteps[0],
      title: 'Sơ chế rau củ kỹ càng',
      description: 'Rửa 3 lần với nước muối.',
    };
    (stepsApi.updateRecipeStep as jest.Mock).mockResolvedValueOnce(updatedStep);

    render(<StepEditor recipeId="recipe-100" initialSteps={mockSteps} />);

    // Nhấn nút Sửa ở bước 1
    await user.click(screen.getByTestId('btn-edit-step-0'));

    const titleInput = screen.getByTestId('input-step-title');
    const descInput = screen.getByTestId('input-step-description');

    expect(titleInput).toHaveValue('Sơ chế nguyên liệu');
    await user.clear(titleInput);
    await user.type(titleInput, 'Sơ chế rau củ kỹ càng');

    await user.clear(descInput);
    await user.type(descInput, 'Rửa 3 lần với nước muối.');

    await user.click(screen.getByTestId('btn-save-step'));

    await waitFor(() => {
      expect(stepsApi.updateRecipeStep).toHaveBeenCalledWith('recipe-100', 'step-1', {
        title: 'Sơ chế rau củ kỹ càng',
        description: 'Rửa 3 lần với nước muối.',
        timerMinutes: 10,
        imageUrl: 'https://example.com/step1.jpg',
      });
      expect(screen.getByText('Sơ chế rau củ kỹ càng')).toBeInTheDocument();
    });
  });

  // 9. Delete + Confirmation
  it('9. Delete + Confirmation: Mở modal hỏi xác nhận trước khi xóa', async () => {
    const user = userEvent.setup();
    (stepsApi.deleteRecipeStep as jest.Mock).mockResolvedValueOnce(undefined);

    render(<StepEditor recipeId="recipe-100" initialSteps={mockSteps} />);

    // Nhấn Xóa ở bước 1
    await user.click(screen.getByTestId('btn-delete-step-0'));

    // Kiểm tra modal xuất hiện
    expect(screen.getByTestId('delete-confirmation-dialog')).toBeInTheDocument();
    expect(screen.getByText(/Xác nhận xóa bước thực hiện/i)).toBeInTheDocument();

    // Bấm nút Hủy bỏ
    await user.click(screen.getByTestId('btn-cancel-delete'));
    expect(screen.queryByTestId('delete-confirmation-dialog')).not.toBeInTheDocument();
    expect(stepsApi.deleteRecipeStep).not.toHaveBeenCalled();

    // Bấm lại Xóa và bấm Xác nhận
    await user.click(screen.getByTestId('btn-delete-step-0'));
    await user.click(screen.getByTestId('btn-confirm-delete'));

    await waitFor(() => {
      expect(stepsApi.deleteRecipeStep).toHaveBeenCalledWith('recipe-100', 'step-1');
      // Sau khi xóa, chỉ còn lại 1 bước và được renumber thành 01
      expect(screen.queryByText('Sơ chế nguyên liệu')).not.toBeInTheDocument();
      expect(screen.getByText('Xào thịt bò')).toBeInTheDocument();
    });
  });

  // 10. Reorder Up / Down
  it('10. Reorder Up/Down: Gửi toàn bộ active step IDs theo thứ tự mới lên server', async () => {
    const user = userEvent.setup();
    const reorderedServerResult: RecipeStep[] = [
      { ...mockSteps[1], stepNumber: 1 },
      { ...mockSteps[0], stepNumber: 2 },
    ];
    (stepsApi.reorderRecipeSteps as jest.Mock).mockResolvedValueOnce(reorderedServerResult);

    render(<StepEditor recipeId="recipe-100" initialSteps={mockSteps} />);

    // Nút Move Down ở bước đầu tiên
    const moveDownBtn = screen.getByTestId('btn-move-down-0');
    expect(moveDownBtn).not.toBeDisabled();

    // Nút Move Up ở bước đầu tiên phải bị disable
    expect(screen.getByTestId('btn-move-up-0')).toBeDisabled();

    await user.click(moveDownBtn);

    await waitFor(() => {
      // Body gửi lên server phải chứa đúng toàn bộ IDs theo thứ tự mới
      expect(stepsApi.reorderRecipeSteps).toHaveBeenCalledWith('recipe-100', ['step-2', 'step-1']);
    });
  });

  // 11. API Error
  it('11. Hiển thị Error Banner khi gọi API thất bại', async () => {
    const user = userEvent.setup();
    (stepsApi.createRecipeStep as jest.Mock).mockRejectedValueOnce(
      new Error('Lỗi kết nối máy chủ.')
    );

    render(<StepEditor recipeId="recipe-100" initialSteps={[]} />);

    await user.click(screen.getByTestId('btn-add-step'));
    await user.type(screen.getByTestId('input-step-description'), 'Mô tả hợp lệ');
    await user.click(screen.getByTestId('btn-save-step'));

    await waitFor(() => {
      expect(screen.getByTestId('step-error-banner')).toBeInTheDocument();
      expect(screen.getByText('Lỗi kết nối máy chủ.')).toBeInTheDocument();
    });

    // Có thể đóng banner lỗi
    await user.click(screen.getByTestId('btn-close-error'));
    expect(screen.queryByTestId('step-error-banner')).not.toBeInTheDocument();
  });

  // 12. Disable action khi request đang chạy
  it('12. Disable các nút thao tác khi đang trong trạng thái loading', async () => {
    const user = userEvent.setup();
    let resolvePromise: (value: RecipeStep) => void;
    const pendingPromise = new Promise<RecipeStep>((resolve) => {
      resolvePromise = resolve;
    });

    (stepsApi.createRecipeStep as jest.Mock).mockReturnValueOnce(pendingPromise);

    render(<StepEditor recipeId="recipe-100" initialSteps={mockSteps} />);

    await user.click(screen.getByTestId('btn-add-step'));
    await user.type(screen.getByTestId('input-step-description'), 'Nội dung đang lưu');
    await user.click(screen.getByTestId('btn-save-step'));

    // Kiểm tra trạng thái loading hiển thị
    expect(screen.getByTestId('step-loading-indicator')).toBeInTheDocument();

    // Nút Lưu và Hủy bị disabled
    expect(screen.getByTestId('btn-save-step')).toBeDisabled();
    expect(screen.getByTestId('btn-cancel-step-form')).toBeDisabled();

    // Sau khi resolve
    resolvePromise!({
      id: 'step-new',
      recipeId: 'recipe-100',
      stepNumber: 3,
      title: null,
      description: 'Nội dung đang lưu',
      timerMinutes: null,
      imageUrl: null,
      createdAt: '2026-01-01T00:00:00Z',
    });

    await waitFor(() => {
      expect(screen.queryByTestId('step-loading-indicator')).not.toBeInTheDocument();
    });
  });

  // 13. Controlled mode không gọi API khi chưa có recipeId
  it('13. Controlled mode: Quản lý local state và không gọi API khi chưa có recipeId', async () => {
    const user = userEvent.setup();
    const handleStepsChange = jest.fn();

    render(<StepEditor onStepsChange={handleStepsChange} />);

    // Thêm bước mới
    await user.click(screen.getByTestId('btn-add-step'));
    await user.type(screen.getByTestId('input-step-title'), 'Chuẩn bị nước sốt');
    await user.type(screen.getByTestId('input-step-description'), 'Pha mắm, đường và tỏi ớt.');
    await user.click(screen.getByTestId('btn-save-step'));

    // Không được gọi bất kỳ API backend nào
    expect(stepsApi.createRecipeStep).not.toHaveBeenCalled();
    expect(stepsApi.getRecipeSteps).not.toHaveBeenCalled();

    // Callback onStepsChange được gọi với danh sách có 1 phần tử
    expect(handleStepsChange).toHaveBeenCalledTimes(1);
    expect(handleStepsChange).toHaveBeenCalledWith(
      expect.arrayContaining([
        expect.objectContaining({
          stepNumber: 1,
          title: 'Chuẩn bị nước sốt',
          description: 'Pha mắm, đường và tỏi ớt.',
        }),
      ])
    );
  });

  // 14. RBAC: Guest (không đăng nhập) xem Recipe -> hiển thị forbidden banner, ẩn nút thêm/sửa/xóa
  it('14. RBAC: Guest xem Recipe có authorId -> hiển thị forbidden-banner và không cho phép chỉnh sửa', () => {
    render(<StepEditor recipeId="recipe-100" authorId="user-author-99" initialSteps={mockSteps} />);

    expect(screen.getByTestId('forbidden-banner')).toBeInTheDocument();
    expect(screen.getByText(/Chế độ chỉ xem/i)).toBeInTheDocument();
    expect(screen.queryByTestId('btn-add-step')).not.toBeInTheDocument();
    expect(screen.getByTestId('btn-edit-step-0')).toBeDisabled();
    expect(screen.getByTestId('btn-delete-step-0')).toBeDisabled();
    expect(screen.getByTestId('btn-move-down-0')).toBeDisabled();
  });

  // 15. RBAC: Author khác không sở hữu -> hiển thị forbidden banner
  it('15. RBAC: Author không sở hữu Recipe -> hiển thị forbidden-banner và không cho phép chỉnh sửa', () => {
    useAuthStore.setState({
      user: {
        id: 'user-author-other',
        email: 'other@example.com',
        fullName: 'Other Author',
        roles: ['Author'],
      } as any,
      isAuthenticated: true,
      roles: ['Author'],
    });

    render(<StepEditor recipeId="recipe-100" authorId="user-author-99" initialSteps={mockSteps} />);

    expect(screen.getByTestId('forbidden-banner')).toBeInTheDocument();
    expect(screen.queryByTestId('btn-add-step')).not.toBeInTheDocument();
    expect(screen.getByTestId('btn-edit-step-0')).toBeDisabled();
    expect(screen.getByTestId('btn-delete-step-0')).toBeDisabled();
  });

  // 16. RBAC: Author sở hữu Recipe -> đầy đủ quyền thao tác
  it('16. RBAC: Author sở hữu Recipe -> đầy đủ quyền Thêm, Sửa, Xóa, không có forbidden banner', () => {
    useAuthStore.setState({
      user: {
        id: 'user-author-99',
        email: 'author99@example.com',
        fullName: 'Owner Author',
        roles: ['Author'],
      } as any,
      isAuthenticated: true,
      roles: ['Author'],
    });

    render(<StepEditor recipeId="recipe-100" authorId="user-author-99" initialSteps={mockSteps} />);

    expect(screen.queryByTestId('forbidden-banner')).not.toBeInTheDocument();
    expect(screen.getByTestId('btn-add-step')).toBeInTheDocument();
    expect(screen.getByTestId('btn-edit-step-0')).toBeInTheDocument();
    expect(screen.getByTestId('btn-delete-step-0')).toBeInTheDocument();
  });

  // 17. RBAC: Admin -> toàn quyền chỉnh sửa bất kể authorId
  it('17. RBAC: Admin -> toàn quyền chỉnh sửa bất kể authorId', () => {
    useAuthStore.setState({
      user: {
        id: 'user-admin-1',
        email: 'admin@example.com',
        fullName: 'System Admin',
        roles: ['Admin'],
      } as any,
      isAuthenticated: true,
      roles: ['Admin'],
    });

    render(<StepEditor recipeId="recipe-100" authorId="user-author-99" initialSteps={mockSteps} />);

    expect(screen.queryByTestId('forbidden-banner')).not.toBeInTheDocument();
    expect(screen.getByTestId('btn-add-step')).toBeInTheDocument();
  });

  // 18. Upload Step Image: Tải ảnh hợp lệ qua uploadStepImageFile
  it('18. Upload Step Image: Tải ảnh máy tính qua uploadStepImageFile thành công', async () => {
    const user = userEvent.setup();
    (stepsApi.uploadStepImageFile as jest.Mock).mockResolvedValueOnce({
      url: 'https://minio.culinaryblog.local/culinaryblog/step-img-123.jpg',
    });

    render(<StepEditor recipeId="recipe-100" initialSteps={[]} />);

    await user.click(screen.getByTestId('btn-add-step'));
    const fileInput = screen.getByTestId('input-step-file');
    const validFile = new File(['fake-image-binary'], 'step-photo.jpg', { type: 'image/jpeg' });

    await user.upload(fileInput, validFile);

    await waitFor(() => {
      expect(stepsApi.uploadStepImageFile).toHaveBeenCalledWith('recipe-100', validFile);
      expect(screen.getByTestId('input-step-image')).toHaveValue(
        'https://minio.culinaryblog.local/culinaryblog/step-img-123.jpg'
      );
    });
  });

  // 19. Upload Step Image: Báo lỗi khi chọn file quá 5MB
  it('19. Upload Step Image: Báo lỗi khi chọn file quá 5MB và không gọi API upload', async () => {
    const user = userEvent.setup();
    render(<StepEditor recipeId="recipe-100" initialSteps={[]} />);

    await user.click(screen.getByTestId('btn-add-step'));
    const fileInput = screen.getByTestId('input-step-file');

    // Tạo file giả kích thước 6MB
    const bigFile = new File([new ArrayBuffer(6 * 1024 * 1024)], 'big-image.jpg', {
      type: 'image/jpeg',
    });

    await user.upload(fileInput, bigFile);

    expect(screen.getByTestId('error-step-image')).toHaveTextContent(
      /vượt quá giới hạn tối đa cho phép là 5 MB/i
    );
    expect(stepsApi.uploadStepImageFile).not.toHaveBeenCalled();
  });

  // 20. Reorder Rollback: Tự động hoàn tác danh sách khi API reorder thất bại
  it('20. Reorder Rollback: Tự động hoàn tác khi reorderRecipeSteps thất bại', async () => {
    const user = userEvent.setup();
    (stepsApi.reorderRecipeSteps as jest.Mock).mockRejectedValueOnce(
      new Error('Lỗi sắp xếp từ máy chủ.')
    );

    render(<StepEditor recipeId="recipe-100" initialSteps={mockSteps} />);

    // Nhấn Move Down bước 1
    const moveDownBtn = screen.getByTestId('btn-move-down-0');
    await user.click(moveDownBtn);

    // Chờ thông báo lỗi hiển thị
    await waitFor(() => {
      expect(screen.getByTestId('step-error-banner')).toHaveTextContent(
        'Lỗi sắp xếp từ máy chủ.'
      );
    });

    // Danh sách vẫn được giữ nguyên thứ tự ban đầu sau khi rollback
    const stepTitles = screen.getAllByRole('heading', { level: 4 });
    expect(stepTitles[0]).toHaveTextContent('Sơ chế nguyên liệu');
    expect(stepTitles[1]).toHaveTextContent('Xào thịt bò');
  });

  // 21. Retry fetch steps khi getRecipeSteps gặp lỗi
  it('21. Retry fetch steps: Hiển thị nút Thử lại khi tải danh sách ban đầu thất bại', async () => {
    const user = userEvent.setup();
    (stepsApi.getRecipeSteps as jest.Mock).mockRejectedValueOnce(
      new Error('Không thể tải danh sách bước.')
    );

    render(<StepEditor recipeId="recipe-100" />);

    await waitFor(() => {
      expect(screen.getByTestId('step-error-banner')).toBeInTheDocument();
      expect(screen.getByTestId('btn-retry-fetch-steps')).toBeInTheDocument();
    });

    // Mock thành công cho lần gọi thứ hai
    (stepsApi.getRecipeSteps as jest.Mock).mockResolvedValueOnce(mockSteps);

    // Nhấn nút Thử lại
    await user.click(screen.getByTestId('btn-retry-fetch-steps'));

    await waitFor(() => {
      expect(stepsApi.getRecipeSteps).toHaveBeenCalledTimes(2);
      expect(screen.getByText('Sơ chế nguyên liệu')).toBeInTheDocument();
    });
  });
});
