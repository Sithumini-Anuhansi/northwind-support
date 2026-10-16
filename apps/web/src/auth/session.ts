export const AUTH_UNAUTHORIZED_EVENT = 'support-desk:unauthorized';

const ACCESS_TOKEN_KEY = 'northwind-support.access-token';

export function getAccessToken(): string | null {
  return localStorage.getItem(ACCESS_TOKEN_KEY);
}

export function saveAccessToken(token: string): void {
  localStorage.setItem(ACCESS_TOKEN_KEY, token);
}

export function clearAccessToken(): void {
  localStorage.removeItem(ACCESS_TOKEN_KEY);
}
