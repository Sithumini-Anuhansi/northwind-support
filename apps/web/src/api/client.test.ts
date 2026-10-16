import { afterEach, describe, expect, it, vi } from 'vitest';
import { AUTH_UNAUTHORIZED_EVENT, getAccessToken, saveAccessToken } from '../auth/session';
import { ApiError, request } from './client';

afterEach(() => {
  localStorage.clear();
  vi.unstubAllGlobals();
});

describe('request', () => {
  it('attaches the stored bearer token to API calls', async () => {
    saveAccessToken('signed-in-token');
    const fetchMock = vi.fn(async (_input: RequestInfo | URL, _init?: RequestInit) => new Response('{"ok":true}', {
      status: 200,
      headers: { 'Content-Type': 'application/json' },
    }));
    vi.stubGlobal('fetch', fetchMock);

    await expect(request<{ ok: boolean }>('/tickets')).resolves.toEqual({ ok: true });

    const requestInit = fetchMock.mock.calls[0]?.[1];
    expect(new Headers(requestInit?.headers).get('Authorization')).toBe('Bearer signed-in-token');
  });

  it('signals session expiry when a protected API call returns 401', async () => {
    saveAccessToken('expired-token');
    const fetchMock = vi.fn(async (_input: RequestInfo | URL, _init?: RequestInit) => new Response('{"title":"Unauthorized"}', {
      status: 401,
      headers: { 'Content-Type': 'application/problem+json' },
    }));
    vi.stubGlobal('fetch', fetchMock);
    const unauthorized = vi.fn();
    window.addEventListener(AUTH_UNAUTHORIZED_EVENT, unauthorized);

    await expect(request('/tickets')).rejects.toBeInstanceOf(ApiError);

    expect(unauthorized).toHaveBeenCalledOnce();
    expect(getAccessToken()).toBe('expired-token');
    window.removeEventListener(AUTH_UNAUTHORIZED_EVENT, unauthorized);
  });
});
