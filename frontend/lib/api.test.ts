import { afterEach, describe, expect, it, vi } from 'vitest';
import { apiFetch } from './api';

describe('API authentication refresh', () => {
  afterEach(() => {
    vi.unstubAllGlobals();
    vi.restoreAllMocks();
  });

  it('refreshes tokens without broadcasting a new sign-in event', async () => {
    const values = new Map<string, string>([
      ['culinary_access_token', 'expired-access-token'],
      ['culinary_refresh_token', 'refresh-token'],
    ]);
    const storage = {
      getItem: (key: string) => values.get(key) ?? null,
      setItem: (key: string, value: string) => values.set(key, value),
      removeItem: (key: string) => values.delete(key),
    };
    const dispatchEvent = vi.fn();
    vi.stubGlobal('localStorage', storage);
    vi.stubGlobal('window', { dispatchEvent });

    const fetchMock = vi.fn()
      .mockResolvedValueOnce(new Response(null, { status: 401 }))
      .mockResolvedValueOnce(new Response(JSON.stringify({
        data: { accessToken: 'new-access-token', refreshToken: 'new-refresh-token' },
      }), { status: 200, headers: { 'Content-Type': 'application/json' } }))
      .mockResolvedValueOnce(new Response(JSON.stringify({ data: { ok: true } }), {
        status: 200,
        headers: { 'Content-Type': 'application/json' },
      }));
    vi.stubGlobal('fetch', fetchMock);

    await expect(apiFetch<{ ok: boolean }>('/protected')).resolves.toEqual({ ok: true });

    expect(values.get('culinary_access_token')).toBe('new-access-token');
    expect(values.get('culinary_refresh_token')).toBe('new-refresh-token');
    expect(dispatchEvent).not.toHaveBeenCalled();
    expect(fetchMock).toHaveBeenCalledTimes(3);
  });
});
