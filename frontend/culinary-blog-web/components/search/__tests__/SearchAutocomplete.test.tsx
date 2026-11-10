import React from 'react';
import { render, screen, fireEvent, waitFor } from '@testing-library/react';
import { SearchBox } from '../SearchBox';
import * as searchApi from '@/lib/api/search';
import type { SearchResponse } from '@/types/search';

// Mock hàm searchRecipes
jest.mock('@/lib/api/search', () => ({
  searchRecipes: jest.fn(),
}));

const mockSearchRecipes = searchApi.searchRecipes as jest.MockedFunction<typeof searchApi.searchRecipes>;

const mockSuggestionsResponse: SearchResponse = {
  items: [
    {
      id: 'rec-1',
      title: 'Phở Bò Tái Nạm Hà Nội',
      slug: 'pho-bo-tai-nam-ha-noi',
      description: 'Phở bò truyền thống thơm phức.',
      prepTimeMinutes: 30,
      cookTimeMinutes: 180,
      servings: 4,
      difficulty: 1,
      primaryImageUrl: 'https://example.com/pho-bo.jpg',
      authorName: 'Bếp Trưởng Hà Nội',
      categoryName: 'Bún, Mì & Phở',
      categorySlug: 'bun-mi-pho',
      matchType: 'FullTextSearch',
    },
    {
      id: 'rec-2',
      title: 'Bún Bò Huế Cố Đô',
      slug: 'bun-bo-hue-co-do',
      description: 'Bún bò Huế cay nồng thơm mùi mắm ruốc.',
      prepTimeMinutes: 40,
      cookTimeMinutes: 120,
      servings: 4,
      difficulty: 2,
      primaryImageUrl: null,
      authorName: 'Đầu Bếp Cố Đô',
      categoryName: 'Bún, Mì & Phở',
      categorySlug: 'bun-mi-pho',
      matchType: 'FuzzyTrigram',
    },
  ],
  page: 1,
  pageSize: 6,
  totalCount: 2,
  totalPages: 1,
  hasPreviousPage: false,
  hasNextPage: false,
};

describe('Search Autocomplete Component Tests', () => {
  beforeEach(() => {
    jest.clearAllMocks();
  });

  it('1. Không kích hoạt gọi API gợi ý khi từ khóa ít hơn 2 ký tự', async () => {
    render(<SearchBox onSearch={jest.fn()} />);

    const input = screen.getByTestId('search-input');
    fireEvent.change(input, { target: { value: 'p' } });

    await new Promise<void>((r) => {
      setTimeout(r, 400);
    });
    expect(mockSearchRecipes).not.toHaveBeenCalled();
    expect(screen.queryByTestId('search-autocomplete-dropdown')).not.toBeInTheDocument();
  });

  it('2. Gọi API gợi ý sau khoảng debounce 300ms khi từ khóa >= 2 ký tự', async () => {
    mockSearchRecipes.mockResolvedValueOnce(mockSuggestionsResponse);

    render(<SearchBox onSearch={jest.fn()} />);

    const input = screen.getByTestId('search-input');
    fireEvent.change(input, { target: { value: 'pho' } });

    // Trước debounce 300ms chưa gọi
    expect(mockSearchRecipes).not.toHaveBeenCalled();

    // Sau debounce 350ms gọi API
    await waitFor(() => {
      expect(mockSearchRecipes).toHaveBeenCalledTimes(1);
    });

    expect(mockSearchRecipes).toHaveBeenCalledWith(
      expect.objectContaining({
        q: 'pho',
        pageSize: 6,
      })
    );
  });

  it('3. Hiển thị dropdown gợi ý với danh sách kết quả trả về từ API', async () => {
    mockSearchRecipes.mockResolvedValueOnce(mockSuggestionsResponse);

    render(<SearchBox onSearch={jest.fn()} />);

    const input = screen.getByTestId('search-input');
    fireEvent.change(input, { target: { value: 'pho bo' } });

    await waitFor(() => {
      expect(screen.getByTestId('search-autocomplete-dropdown')).toBeInTheDocument();
    });

    expect(screen.getByText('Phở Bò Tái Nạm Hà Nội')).toBeInTheDocument();
    expect(screen.getByText('Bún Bò Huế Cố Đô')).toBeInTheDocument();
    expect(screen.getAllByText('Bún, Mì & Phở').length).toBeGreaterThan(0);
  });

  it('4. Điều hướng danh sách gợi ý bằng phím ArrowDown và ArrowUp', async () => {
    mockSearchRecipes.mockResolvedValueOnce(mockSuggestionsResponse);

    render(<SearchBox onSearch={jest.fn()} />);

    const input = screen.getByTestId('search-input');
    fireEvent.change(input, { target: { value: 'pho' } });

    await waitFor(() => {
      expect(screen.getByTestId('search-autocomplete-dropdown')).toBeInTheDocument();
    });

    const item0 = screen.getByTestId('autocomplete-item-0');
    const item1 = screen.getByTestId('autocomplete-item-1');

    // Nhấn ArrowDown -> chọn mục 0
    fireEvent.keyDown(input, { key: 'ArrowDown' });
    expect(item0).toHaveAttribute('aria-selected', 'true');
    expect(item1).toHaveAttribute('aria-selected', 'false');

    // Nhấn ArrowDown tiếp -> chọn mục 1
    fireEvent.keyDown(input, { key: 'ArrowDown' });
    expect(item1).toHaveAttribute('aria-selected', 'true');

    // Nhấn ArrowUp -> quay lại mục 0
    fireEvent.keyDown(input, { key: 'ArrowUp' });
    expect(item0).toHaveAttribute('aria-selected', 'true');
  });

  it('5. Nhấn phím Escape đóng dropdown gợi ý', async () => {
    mockSearchRecipes.mockResolvedValueOnce(mockSuggestionsResponse);

    render(<SearchBox onSearch={jest.fn()} />);

    const input = screen.getByTestId('search-input');
    fireEvent.change(input, { target: { value: 'pho' } });

    await waitFor(() => {
      expect(screen.getByTestId('search-autocomplete-dropdown')).toBeInTheDocument();
    });

    fireEvent.keyDown(input, { key: 'Escape' });
    expect(screen.queryByTestId('search-autocomplete-dropdown')).not.toBeInTheDocument();
  });

  it('6. Nhấn Enter khi đang chọn gợi ý gọi callback onSelectSuggestion', async () => {
    mockSearchRecipes.mockResolvedValueOnce(mockSuggestionsResponse);
    const handleSelect = jest.fn();

    render(<SearchBox onSearch={jest.fn()} onSelectSuggestion={handleSelect} />);

    const input = screen.getByTestId('search-input');
    fireEvent.change(input, { target: { value: 'pho' } });

    await waitFor(() => {
      expect(screen.getByTestId('search-autocomplete-dropdown')).toBeInTheDocument();
    });

    // Chọn mục 0 bằng ArrowDown rồi nhấn Enter
    fireEvent.keyDown(input, { key: 'ArrowDown' });
    fireEvent.keyDown(input, { key: 'Enter' });

    expect(handleSelect).toHaveBeenCalledWith(mockSuggestionsResponse.items[0]);
    expect(screen.queryByTestId('search-autocomplete-dropdown')).not.toBeInTheDocument();
  });

  it('7. Hiển thị Empty State khi API trả về danh sách rỗng', async () => {
    mockSearchRecipes.mockResolvedValueOnce({
      ...mockSuggestionsResponse,
      items: [],
      totalCount: 0,
    });

    render(<SearchBox onSearch={jest.fn()} />);

    const input = screen.getByTestId('search-input');
    fireEvent.change(input, { target: { value: 'mon la khong co' } });

    await waitFor(() => {
      expect(screen.getByTestId('autocomplete-empty')).toBeInTheDocument();
    });

    expect(screen.getByText(/Không tìm thấy công thức phù hợp với/)).toBeInTheDocument();
  });

  it('8. Click vào một gợi ý gọi callback onSelectSuggestion và đóng dropdown', async () => {
    mockSearchRecipes.mockResolvedValueOnce(mockSuggestionsResponse);
    const handleSelect = jest.fn();

    render(<SearchBox onSearch={jest.fn()} onSelectSuggestion={handleSelect} />);

    const input = screen.getByTestId('search-input');
    fireEvent.change(input, { target: { value: 'pho' } });

    await waitFor(() => {
      expect(screen.getByTestId('autocomplete-item-1')).toBeInTheDocument();
    });

    fireEvent.click(screen.getByTestId('autocomplete-item-1'));

    expect(handleSelect).toHaveBeenCalledWith(mockSuggestionsResponse.items[1]);
    expect(screen.queryByTestId('search-autocomplete-dropdown')).not.toBeInTheDocument();
  });

  it('9. Bấm nút Clear xóa từ khóa, đóng dropdown và reset state', async () => {
    mockSearchRecipes.mockResolvedValueOnce(mockSuggestionsResponse);

    render(<SearchBox onSearch={jest.fn()} />);

    const input = screen.getByTestId('search-input');
    fireEvent.change(input, { target: { value: 'pho bo' } });

    await waitFor(() => {
      expect(screen.getByTestId('search-autocomplete-dropdown')).toBeInTheDocument();
    });

    const clearBtn = screen.getByTestId('btn-clear-search');
    fireEvent.click(clearBtn);

    expect(input).toHaveValue('');
    expect(screen.queryByTestId('search-autocomplete-dropdown')).not.toBeInTheDocument();
  });

  it('10. Hỗ trợ tắt Autocomplete qua prop enableAutocomplete={false}', async () => {
    render(<SearchBox onSearch={jest.fn()} enableAutocomplete={false} />);

    const input = screen.getByTestId('search-input');
    fireEvent.change(input, { target: { value: 'pho bo' } });

    await new Promise<void>((r) => {
      setTimeout(r, 400);
    });
    expect(mockSearchRecipes).not.toHaveBeenCalled();
    expect(screen.queryByTestId('search-autocomplete-dropdown')).not.toBeInTheDocument();
  });
});
