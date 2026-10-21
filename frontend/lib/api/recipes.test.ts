import { describe, expect, it } from 'vitest';
import { buildRecipeQuery } from './recipes';

describe('Recipe API query serialization', () => {
  it('serializes pagination, filters, status, sorting, and encoded search text', () => {
    expect(buildRecipeQuery({
      page: 2,
      pageSize: 12,
      search: ' soup & stew ',
      categoryId: '22222222-2222-4222-8222-222222222222',
      status: 'Published',
      sort: 'cooktime',
    })).toBe(
      '?page=2&pageSize=12&search=soup+%26+stew&categoryId=22222222-2222-4222-8222-222222222222&status=Published&sort=cooktime',
    );
  });

  it('omits unset filters and trims blank search text', () => {
    expect(buildRecipeQuery({ search: '  ' })).toBe('');
  });
});
