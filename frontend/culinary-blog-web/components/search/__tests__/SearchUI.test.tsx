import React from 'react';
import { render, screen, fireEvent, waitFor } from '@testing-library/react';
import userEvent from '@testing-library/user-event';
import { SearchContainer } from '../SearchContainer';
import { SearchBox } from '../SearchBox';
import * as searchApi from '@/lib/api/search';
import type { SearchResponse } from '@/types/search';

// Mock module @/lib/api/search
jest.mock('@/lib/api/search', () => {
  const actual = jest.requireActual('@/lib/api/search');
  return {
    ...actual,
    searchRecipes: jest.fn(),
  };
});

const mockSearchResponse: SearchResponse = {
  items: [
    {
      id: 'rec-1',
      title: 'Phở bò Hà Nội truyền thống',
      slug: 'pho-bo-ha-noi-truyen-thong',
      description: 'Nước dùng trong vắt, đậm đà từ xương bò hầm 8 tiếng cùng quế hồi thảo quả.',
      prepTimeMinutes: 30,
      cookTimeMinutes: 180,
      servings: 4,
      difficulty: 'Medium',
      publishedAt: '2026-03-01T08:00:00Z',
      primaryImageUrl: 'https://example.com/pho-bo.jpg',
      authorName: 'Võ Hùng Mạnh',
      categoryName: 'Món Nước',
      categorySlug: 'mon-nuoc',
      matchType: 'FullTextSearch',
    },
    {
      id: 'rec-2',
      title: 'Phở xào bò rau cải',
      slug: 'pho-xao-bo-rau-cai',
      description: 'Bánh phở xào cháy cạnh thơm nức mũi kết hợp với thịt bò mềm ngọt.',
      prepTimeMinutes: 15,
      cookTimeMinutes: 20,
      servings: 2,
      difficulty: 'Easy',
      publishedAt: '2026-03-02T10:00:00Z',
      primaryImageUrl: 'https://example.com/pho-xao.jpg',
      authorName: 'Nguyễn Văn A',
      categoryName: 'Món Xào',
      categorySlug: 'mon-xao',
      matchType: 'FuzzyTrigram',
    },
  ],
  page: 1,
  pageSize: 10,
  totalCount: 2,
  totalPages: 1,
  hasPreviousPage: false,
  hasNextPage: false,
};

describe('Search UI Module Tests', () => {
  beforeEach(() => {
    jest.clearAllMocks();
    (searchApi.searchRecipes as jest.Mock).mockResolvedValue(mockSearchResponse);
    window.scrollTo = jest.fn();
  });

  // 1. Render Search Box
  it('1. Render Search Box với đầy đủ ô nhập liệu, icon và nút tìm kiếm', () => {
    const handleSearch = jest.fn();
    render(<SearchBox onSearch={handleSearch} />);

    expect(screen.getByTestId('search-box')).toBeInTheDocument();
    expect(screen.getByTestId('search-input')).toBeInTheDocument();
    expect(screen.getByTestId('btn-submit-search')).toBeInTheDocument();
    expect(
      screen.getByPlaceholderText('Tìm kiếm công thức (vd: phở bò, bún chả, thịt kho...)')
    ).toBeInTheDocument();
  });

  // 2. Query validation: Nhỏ hơn 2 ký tự
  it('2. Báo lỗi khi từ khóa tìm kiếm ít hơn 2 ký tự', async () => {
    const handleSearch = jest.fn();
    render(<SearchBox onSearch={handleSearch} />);

    const input = screen.getByTestId('search-input');
    await userEvent.type(input, 'a');

    const submitBtn = screen.getByTestId('btn-submit-search');
    fireEvent.click(submitBtn);

    expect(screen.getByTestId('search-validation-error')).toHaveTextContent(
      'Từ khóa tìm kiếm phải có ít nhất 2 ký tự.'
    );
    expect(handleSearch).not.toHaveBeenCalled();
  });

  // 3. Query validation: Giới hạn 100 ký tự
  it('3. Giới hạn độ dài ô nhập liệu tối đa 100 ký tự', async () => {
    const handleSearch = jest.fn();
    render(<SearchBox onSearch={handleSearch} />);

    const input = screen.getByTestId('search-input');
    const longText = 'x'.repeat(120);
    await userEvent.type(input, longText);

    expect(input).toHaveValue('x'.repeat(100));
  });

  // 4. Submit bằng nút bấm
  it('4. Kích hoạt tìm kiếm khi người dùng nhấn nút Tìm kiếm', async () => {
    const handleSearch = jest.fn();
    render(<SearchBox onSearch={handleSearch} />);

    const input = screen.getByTestId('search-input');
    await userEvent.type(input, 'phở gà');

    fireEvent.click(screen.getByTestId('btn-submit-search'));

    expect(handleSearch).toHaveBeenCalledWith('phở gà');
  });

  // 5. Submit bằng phím Enter
  it('5. Kích hoạt tìm kiếm khi người dùng nhấn phím Enter trên bàn phím', async () => {
    const handleSearch = jest.fn();
    render(<SearchBox onSearch={handleSearch} />);

    const input = screen.getByTestId('search-input');
    await userEvent.type(input, 'bún bò huế{enter}');

    expect(handleSearch).toHaveBeenCalledWith('bún bò huế');
  });

  // 6. Hiển thị trạng thái Loading
  it('6. Hiển thị trạng thái Loading Skeletons trong khi đang chờ kết quả từ API', async () => {
    let resolvePromise: (val: any) => void;
    const pendingPromise = new Promise((resolve) => {
      resolvePromise = resolve;
    });
    (searchApi.searchRecipes as jest.Mock).mockReturnValue(pendingPromise);

    render(<SearchContainer initialQuery="pho bo" />);

    expect(screen.getByTestId('search-loading')).toBeInTheDocument();

    // Resolve API call
    resolvePromise!(mockSearchResponse);
    await waitFor(() => {
      expect(screen.queryByTestId('search-loading')).not.toBeInTheDocument();
    });
  });

  // 7. Render danh sách kết quả (Cards)
  it('7. Render đầy đủ các thẻ kết quả công thức với thông tin chuẩn DTO', async () => {
    render(<SearchContainer initialQuery="pho bo" />);

    expect(await screen.findByTestId('search-card-0')).toBeInTheDocument();
    expect(screen.getByTestId('search-card-1')).toBeInTheDocument();
    expect(screen.getByText('Phở bò Hà Nội truyền thống')).toBeInTheDocument();
    expect(screen.getByText('Phở xào bò rau cải')).toBeInTheDocument();
    expect(screen.getByText('Võ Hùng Mạnh')).toBeInTheDocument();
  });

  // 8. Badge FullTextSearch (FTS)
  it('8. Hiển thị badge "Khớp toàn văn (FTS)" màu xanh lá cho kết quả FTS', async () => {
    render(<SearchContainer initialQuery="pho bo" />);

    expect(await screen.findByTestId('badge-fts-0')).toHaveTextContent('Khớp toàn văn (FTS)');
  });

  // 9. Badge FuzzyTrigram (Gợi ý tương đồng)
  it('9. Hiển thị badge "Gợi ý tương đồng (Fuzzy)" màu vàng cho kết quả Trigram', async () => {
    render(<SearchContainer initialQuery="pho bo" />);

    expect(await screen.findByTestId('badge-fuzzy-1')).toHaveTextContent('Gợi ý tương đồng (Fuzzy)');
  });

  // 10. Luồng tìm kiếm từ khóa "pho bo" gọi đúng API
  it('10. Tìm kiếm từ khóa không dấu "pho bo" gọi đúng hàm searchRecipes', async () => {
    render(<SearchContainer />);

    const input = screen.getByTestId('search-input');
    await userEvent.type(input, 'pho bo');
    fireEvent.click(screen.getByTestId('btn-submit-search'));

    await waitFor(() => {
      expect(searchApi.searchRecipes).toHaveBeenCalledWith('pho bo', 1, 10);
    });
  });

  // 11. Empty state khi không có kết quả
  it('11. Hiển thị thông điệp "Không tìm thấy công thức phù hợp" khi API trả về danh sách rỗng', async () => {
    (searchApi.searchRecipes as jest.Mock).mockResolvedValue({
      items: [],
      page: 1,
      pageSize: 10,
      totalCount: 0,
      totalPages: 0,
      hasPreviousPage: false,
      hasNextPage: false,
    });

    render(<SearchContainer initialQuery="mon-an-khong-ton-tai" />);

    expect(await screen.findByTestId('search-no-results')).toBeInTheDocument();
    expect(screen.getByText('Không tìm thấy công thức phù hợp')).toBeInTheDocument();
  });

  // 12. API Error handling
  it('12. Hiển thị Error Banner màu đỏ thân thiện khi API trả về lỗi', async () => {
    (searchApi.searchRecipes as jest.Mock).mockRejectedValue(
      new Error('Máy chủ tìm kiếm đang bận. Vui lòng thử lại sau.')
    );

    render(<SearchContainer initialQuery="pho bo" />);

    expect(await screen.findByTestId('search-error-banner')).toBeInTheDocument();
    expect(
      screen.getByText(/Máy chủ tìm kiếm đang bận. Vui lòng thử lại sau./)
    ).toBeInTheDocument();
  });

  // 13. Phân trang
  it('13. Điều khiển phân trang gọi đúng trang tiếp theo', async () => {
    const multiPageResponse: SearchResponse = {
      ...mockSearchResponse,
      page: 1,
      totalPages: 3,
      hasNextPage: true,
      hasPreviousPage: false,
    };
    (searchApi.searchRecipes as jest.Mock).mockResolvedValue(multiPageResponse);

    render(<SearchContainer initialQuery="pho" />);

    expect(await screen.findByTestId('search-pagination')).toBeInTheDocument();
    expect(screen.getByTestId('pagination-info')).toHaveTextContent('Trang 1 / 3');

    const nextBtn = screen.getByTestId('btn-next-page');
    expect(nextBtn).toBeEnabled();
    expect(screen.getByTestId('btn-prev-page')).toBeDisabled();

    fireEvent.click(nextBtn);

    await waitFor(() => {
      expect(searchApi.searchRecipes).toHaveBeenCalledWith('pho', 2, 10);
    });
  });

  // 14. Bộ lọc theo Độ khó (Difficulty)
  it('14. Lọc kết quả danh sách theo độ khó Easy', async () => {
    render(<SearchContainer initialQuery="pho bo" />);

    await screen.findByTestId('search-toolbar');

    const selectDiff = screen.getByTestId('select-difficulty');
    fireEvent.change(selectDiff, { target: { value: 'easy' } });

    // Chỉ còn 1 card Easy
    expect(screen.getByText('Phở xào bò rau cải')).toBeInTheDocument();
    expect(screen.queryByText('Phở bò Hà Nội truyền thống')).not.toBeInTheDocument();
  });

  // 15. Sắp xếp theo Thời gian nấu
  it('15. Sắp xếp các công thức theo thời gian nấu tăng dần', async () => {
    render(<SearchContainer initialQuery="pho bo" />);

    await screen.findByTestId('search-toolbar');

    const selectSort = screen.getByTestId('select-sort');
    fireEvent.change(selectSort, { target: { value: 'cookTime' } });

    const titles = screen.getAllByTestId(/recipe-title-/);
    // Món 20 phút (Phở xào) phải đứng trước món 180 phút (Phở bò truyền thống)
    expect(titles[0]).toHaveTextContent('Phở xào bò rau cải');
    expect(titles[1]).toHaveTextContent('Phở bò Hà Nội truyền thống');
  });
});
