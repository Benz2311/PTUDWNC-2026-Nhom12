import React from 'react';
import { render, screen, fireEvent, waitFor } from '@testing-library/react';
import userEvent from '@testing-library/user-event';
import { ImageManager } from '../ImageManager';
import type { RecipeImage } from '@/types/image';
import * as imagesApi from '@/lib/api/images';

// Mock module @/lib/api/images
jest.mock('@/lib/api/images', () => {
  const actual = jest.requireActual('@/lib/api/images');
  return {
    ...actual,
    getRecipeImages: jest.fn(),
    addRecipeImage: jest.fn(),
    setPrimaryRecipeImage: jest.fn(),
    deleteRecipeImage: jest.fn(),
    uploadImageFile: jest.fn(),
  };
});

const mockImages: RecipeImage[] = [
  {
    id: 'img-1',
    recipeId: 'recipe-100',
    originalUrl: 'https://example.com/pho-bo-1.jpg',
    mediumUrl: 'https://example.com/pho-bo-1-medium.jpg',
    thumbnailUrl: 'https://example.com/pho-bo-1-thumb.jpg',
    altText: 'Bát phở bò truyền thống nóng hổi',
    isPrimary: true,
    orderIndex: 0,
    createdAt: '2026-01-01T00:00:00Z',
  },
  {
    id: 'img-2',
    recipeId: 'recipe-100',
    originalUrl: 'https://example.com/pho-bo-2.jpg',
    mediumUrl: null,
    thumbnailUrl: null,
    altText: 'Góc chụp gần từng lát thịt bò tái mềm',
    isPrimary: false,
    orderIndex: 1,
    createdAt: '2026-01-01T00:00:00Z',
  },
];

describe('ImageManager Component', () => {
  beforeEach(() => {
    jest.clearAllMocks();
    (imagesApi.getRecipeImages as jest.Mock).mockResolvedValue([]);
    // Mock URL.createObjectURL and URL.revokeObjectURL
    global.URL.createObjectURL = jest.fn(() => 'blob:https://example.com/preview-blob-123');
    global.URL.revokeObjectURL = jest.fn();
  });

  // 1. Render Empty State
  it('1. Hiển thị Empty State khi chưa có ảnh nào', () => {
    render(<ImageManager initialImages={[]} />);
    expect(screen.getByTestId('image-empty-state')).toBeInTheDocument();
    expect(screen.getByText('Chưa có hình ảnh nào cho công thức')).toBeInTheDocument();
  });

  // 2. Render danh sách ảnh
  it('2. Render danh sách ảnh và hiển thị đúng badge Ảnh đại diện cùng Alt text', () => {
    render(<ImageManager initialImages={mockImages} />);

    expect(screen.getByTestId('image-card-0')).toBeInTheDocument();
    expect(screen.getByTestId('image-card-1')).toBeInTheDocument();
    expect(screen.getByTestId('badge-primary-0')).toBeInTheDocument();
    expect(screen.queryByTestId('badge-primary-1')).not.toBeInTheDocument();
    expect(screen.getByText(/Bát phở bò truyền thống nóng hổi/)).toBeInTheDocument();
    expect(screen.getByText(/Góc chụp gần từng lát thịt bò tái mềm/)).toBeInTheDocument();
  });

function selectFileInput(input: HTMLElement, file: File) {
  Object.defineProperty(input, 'files', {
    value: [file],
    configurable: true,
  });
  fireEvent.change(input);
}

  // 3. File validation: Dung lượng vượt quá 5MB
  it('3. Từ chối file có kích thước vượt quá 5 MB', async () => {
    render(<ImageManager />);

    const largeFile = new File(['x'.repeat(100)], 'large-photo.jpg', {
      type: 'image/jpeg',
    });
    Object.defineProperty(largeFile, 'size', { value: 6 * 1024 * 1024 });

    const fileInput = screen.getByTestId('file-input');
    selectFileInput(fileInput, largeFile);

    expect(screen.getByTestId('error-file')).toHaveTextContent('Dung lượng ảnh vượt quá giới hạn 5 MB.');
    expect(screen.queryByTestId('image-preview')).not.toBeInTheDocument();
  });

  // 4. File validation: Sai định dạng MIME
  it('4. Từ chối file có định dạng không được hỗ trợ (chỉ nhận JPEG, PNG, WebP, AVIF)', async () => {
    render(<ImageManager />);

    const invalidFile = new File(['text content'], 'recipe.pdf', {
      type: 'application/pdf',
    });

    const fileInput = screen.getByTestId('file-input');
    selectFileInput(fileInput, invalidFile);

    expect(screen.getByTestId('error-file')).toHaveTextContent(
      'Định dạng ảnh không hợp lệ. Chỉ chấp nhận JPEG, PNG, WebP hoặc AVIF.'
    );
    expect(screen.queryByTestId('image-preview')).not.toBeInTheDocument();
  });

  // 5. Preview ảnh trước upload
  it('5. Hiển thị preview ảnh khi chọn file hợp lệ', async () => {
    render(<ImageManager />);

    const validFile = new File(['dummy-image-binary'], 'pho.png', {
      type: 'image/png',
    });

    const fileInput = screen.getByTestId('file-input');
    selectFileInput(fileInput, validFile);

    expect(screen.getByTestId('image-preview')).toBeInTheDocument();
    expect(screen.getByText(/Đã chọn: pho.png/)).toBeInTheDocument();
  });

  // 6. Upload ảnh thành công và hiển thị trong danh sách (Direct API mode)
  it('6. Thêm ảnh thành công và gọi addRecipeImage trong Direct API mode', async () => {
    const newImage: RecipeImage = {
      id: 'img-new-99',
      recipeId: 'recipe-100',
      originalUrl: 'https://example.com/fresh-pho.jpg',
      altText: 'Bát phở bò hoàn chỉnh',
      isPrimary: true,
      orderIndex: 0,
    };
    (imagesApi.addRecipeImage as jest.Mock).mockResolvedValue(newImage);

    render(<ImageManager recipeId="recipe-100" initialImages={[]} />);

    // Đợi component load xong trạng thái ban đầu
    await waitFor(() => {
      expect(screen.getByTestId('btn-submit-image')).not.toBeDisabled();
    });

    const tabUrl = screen.getByTestId('tab-upload-url');
    fireEvent.click(tabUrl);

    const inputUrl = screen.getByTestId('input-image-url');
    const inputAlt = screen.getByTestId('input-alt-text');
    await userEvent.type(inputUrl, 'https://example.com/fresh-pho.jpg');
    await userEvent.type(inputAlt, 'Bát phở bò hoàn chỉnh');

    const submitBtn = screen.getByTestId('btn-submit-image');
    fireEvent.click(submitBtn);

    await waitFor(() => {
      expect(imagesApi.addRecipeImage).toHaveBeenCalledWith('recipe-100', expect.objectContaining({
        originalUrl: 'https://example.com/fresh-pho.jpg',
        altText: 'Bát phở bò hoàn chỉnh',
      }));
    });

    expect(await screen.findByTestId('image-card-0')).toBeInTheDocument();
  });

  // 7. Nhập và lưu AltText
  it('7. Validation AltText không vượt quá 200 ký tự', async () => {
    render(<ImageManager />);

    const tabUrl = screen.getByTestId('tab-upload-url');
    fireEvent.click(tabUrl);

    const inputUrl = screen.getByTestId('input-image-url');
    await userEvent.type(inputUrl, 'https://example.com/pho.jpg');

    const longAlt = 'A'.repeat(201);
    const inputAlt = screen.getByTestId('input-alt-text');
    await userEvent.type(inputAlt, longAlt);

    expect(inputAlt).toHaveValue('A'.repeat(200));
  });

  // 8. Thiết lập OrderIndex
  it('8. Cho phép tùy chỉnh OrderIndex khi thêm ảnh', async () => {
    (imagesApi.addRecipeImage as jest.Mock).mockResolvedValue({
      id: 'img-custom-order',
      recipeId: 'recipe-100',
      originalUrl: 'https://example.com/pho.jpg',
      altText: null,
      isPrimary: false,
      orderIndex: 5,
    });

    render(<ImageManager recipeId="recipe-100" initialImages={mockImages} />);

    const tabUrl = screen.getByTestId('tab-upload-url');
    fireEvent.click(tabUrl);

    await userEvent.type(screen.getByTestId('input-image-url'), 'https://example.com/pho.jpg');
    await userEvent.type(screen.getByTestId('input-order-index'), '5');

    fireEvent.click(screen.getByTestId('btn-submit-image'));

    await waitFor(() => {
      expect(imagesApi.addRecipeImage).toHaveBeenCalledWith('recipe-100', expect.objectContaining({
        orderIndex: 5,
      }));
    });
  });

  // 9. Set Primary (Đặt làm ảnh đại diện)
  it('9. Đặt làm ảnh đại diện gọi setPrimaryRecipeImage và cập nhật UI', async () => {
    (imagesApi.setPrimaryRecipeImage as jest.Mock).mockResolvedValue({
      ...mockImages[1],
      isPrimary: true,
    });

    render(<ImageManager recipeId="recipe-100" initialImages={mockImages} />);

    const btnSetPrimary = screen.getByTestId('btn-set-primary-1');
    fireEvent.click(btnSetPrimary);

    await waitFor(() => {
      expect(imagesApi.setPrimaryRecipeImage).toHaveBeenCalledWith('recipe-100', 'img-2');
    });

    expect(await screen.findByTestId('badge-primary-1')).toBeInTheDocument();
  });

  // 10. Delete confirmation modal
  it('10. Bấm nút xóa mở modal yêu cầu xác nhận trước khi thực hiện', async () => {
    render(<ImageManager recipeId="recipe-100" initialImages={mockImages} />);

    expect(screen.queryByTestId('modal-delete-image')).not.toBeInTheDocument();

    const deleteBtn = screen.getByTestId('btn-delete-image-1');
    fireEvent.click(deleteBtn);

    expect(screen.getByTestId('modal-delete-image')).toBeInTheDocument();
    expect(screen.getByText('Xác nhận xóa hình ảnh')).toBeInTheDocument();

    fireEvent.click(screen.getByTestId('btn-cancel-delete-image'));
    expect(screen.queryByTestId('modal-delete-image')).not.toBeInTheDocument();
  });

  // 11. Xóa ảnh thành công
  it('11. Xác nhận xóa ảnh gọi deleteRecipeImage và loại bỏ khỏi danh sách', async () => {
    (imagesApi.deleteRecipeImage as jest.Mock).mockResolvedValue(undefined);

    render(<ImageManager recipeId="recipe-100" initialImages={mockImages} />);

    fireEvent.click(screen.getByTestId('btn-delete-image-1'));
    fireEvent.click(screen.getByTestId('btn-confirm-delete-image'));

    await waitFor(() => {
      expect(imagesApi.deleteRecipeImage).toHaveBeenCalledWith('recipe-100', 'img-2');
    });

    await waitFor(() => {
      expect(screen.queryByTestId('image-card-1')).not.toBeInTheDocument();
    });
  });

  // 12. Hiển thị Error Banner khi gọi API thất bại
  it('12. Hiển thị Error Banner màu đỏ khi API trả về lỗi', async () => {
    (imagesApi.setPrimaryRecipeImage as jest.Mock).mockRejectedValue(
      new Error('Bạn không có quyền chỉnh sửa hình ảnh của công thức này.')
    );

    render(<ImageManager recipeId="recipe-100" initialImages={mockImages} />);

    fireEvent.click(screen.getByTestId('btn-set-primary-1'));

    expect(await screen.findByTestId('image-error-banner')).toBeInTheDocument();
    expect(
      screen.getByText(/Bạn không có quyền chỉnh sửa hình ảnh của công thức này./)
    ).toBeInTheDocument();
  });

  // 13. Xử lý lỗi 503 khi Storage MinIO không khả dụng
  it('13. Hiển thị thông báo thân thiện khi dịch vụ lưu trữ trả về HTTP 503', async () => {
    (imagesApi.uploadImageFile as jest.Mock).mockRejectedValue(
      new imagesApi.ImageApiError(
        'Dịch vụ lưu trữ hình ảnh (MinIO / Object Storage) hiện không khả dụng (HTTP 503). Vui lòng thử lại sau.',
        503
      )
    );

    render(<ImageManager recipeId="recipe-100" initialImages={[]} />);

    const validFile = new File(['dummy content'], 'pho.png', { type: 'image/png' });
    const fileInput = screen.getByTestId('file-input');
    selectFileInput(fileInput, validFile);

    const submitBtn = screen.getByTestId('btn-submit-image');
    await waitFor(() => {
      expect(submitBtn).not.toBeDisabled();
    });
    fireEvent.click(submitBtn);

    expect(await screen.findByTestId('image-error-banner')).toBeInTheDocument();
    expect(screen.getByText(/Dịch vụ lưu trữ hình ảnh/)).toBeInTheDocument();
  });

  // 14. Loading and disabled state
  it('14. Vô hiệu hóa nút và form khi prop disabled được truyền từ bên ngoài', () => {
    render(<ImageManager disabled={true} initialImages={mockImages} />);

    expect(screen.getByTestId('btn-submit-image')).toBeDisabled();
    expect(screen.getByTestId('btn-set-primary-1')).toBeDisabled();
    expect(screen.getByTestId('btn-delete-image-0')).toBeDisabled();
  });
});
