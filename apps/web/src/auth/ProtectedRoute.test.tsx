import { fireEvent, render, screen } from '@testing-library/react';
import { describe, expect, it, vi } from 'vitest';
import { MemoryRouter, Route, Routes, useLocation } from 'react-router-dom';
import { request } from '../api/client';
import { AuthProvider } from './AuthContext';
import { ProtectedRoute } from './ProtectedRoute';
import { getAccessToken, saveAccessToken } from './session';

function LoginDestination() {
  const location = useLocation();
  const state = location.state as { from?: { pathname?: string } } | null;
  return <p>Redirected from {state?.from?.pathname}</p>;
}

function ProtectedContent() {
  return (
    <button type="button" onClick={() => void request('/tickets').catch(() => undefined)}>
      Call protected API
    </button>
  );
}

describe('ProtectedRoute', () => {
  it('redirects unauthenticated visitors to login and preserves their requested path', async () => {
    localStorage.clear();

    render(
      <AuthProvider>
        <MemoryRouter initialEntries={['/tickets/42']}>
          <Routes>
            <Route element={<ProtectedRoute />}>
              <Route path="/tickets/:id" element={<p>Private ticket</p>} />
            </Route>
            <Route path="/login" element={<LoginDestination />} />
          </Routes>
        </MemoryRouter>
      </AuthProvider>,
    );

    expect(await screen.findByText('Redirected from /tickets/42')).toBeInTheDocument();
    expect(screen.queryByText('Private ticket')).not.toBeInTheDocument();
  });

  it('clears the session and redirects to login after a protected request returns 401', async () => {
    saveAccessToken('expired-token');
    const fetchMock = vi.fn(async (input: RequestInfo | URL) => {
      if (String(input).endsWith('/auth/me')) {
        return new Response('{"id":1,"fullName":"Alex Turner","email":"alex@example.test"}', {
          status: 200,
          headers: { 'Content-Type': 'application/json' },
        });
      }

      return new Response('{"title":"Unauthorized"}', {
        status: 401,
        headers: { 'Content-Type': 'application/problem+json' },
      });
    });
    vi.stubGlobal('fetch', fetchMock);

    render(
      <AuthProvider>
        <MemoryRouter initialEntries={['/tickets']}>
          <Routes>
            <Route element={<ProtectedRoute />}>
              <Route path="/tickets" element={<ProtectedContent />} />
            </Route>
            <Route path="/login" element={<p>Login page</p>} />
          </Routes>
        </MemoryRouter>
      </AuthProvider>,
    );

    fireEvent.click(await screen.findByRole('button', { name: 'Call protected API' }));

    expect(await screen.findByText('Login page')).toBeInTheDocument();
    expect(getAccessToken()).toBeNull();
  });
});
