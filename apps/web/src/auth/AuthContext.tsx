import { createContext, useCallback, useContext, useEffect, useMemo, useState } from 'react';
import { authApi } from '../api/auth';
import type { AuthenticatedAgent } from '../types/api';
import {
  AUTH_UNAUTHORIZED_EVENT,
  clearAccessToken,
  getAccessToken,
  saveAccessToken,
} from './session';

interface AuthContextValue {
  user: AuthenticatedAgent | null;
  isLoading: boolean;
  login: (email: string, password: string) => Promise<void>;
  logout: () => void;
}

const AuthContext = createContext<AuthContextValue | undefined>(undefined);

export function AuthProvider({ children }: { children: React.ReactNode }) {
  const [user, setUser] = useState<AuthenticatedAgent | null>(null);
  const [isLoading, setIsLoading] = useState(() => Boolean(getAccessToken()));

  useEffect(() => {
    const handleUnauthorized = () => {
      clearAccessToken();
      setUser(null);
      setIsLoading(false);
    };

    window.addEventListener(AUTH_UNAUTHORIZED_EVENT, handleUnauthorized);

    if (!getAccessToken()) {
      return () => window.removeEventListener(AUTH_UNAUTHORIZED_EVENT, handleUnauthorized);
    }

    let isCurrent = true;
    void authApi.me()
      .then((currentUser) => {
        if (isCurrent) setUser(currentUser);
      })
      .catch(() => {
        if (isCurrent) {
          clearAccessToken();
          setUser(null);
        }
      })
      .finally(() => {
        if (isCurrent) setIsLoading(false);
      });

    return () => {
      isCurrent = false;
      window.removeEventListener(AUTH_UNAUTHORIZED_EVENT, handleUnauthorized);
    };
  }, []);

  const login = useCallback(async (email: string, password: string) => {
    clearAccessToken();
    const result = await authApi.login(email, password);
    saveAccessToken(result.token);
    setUser(result.user);
  }, []);

  const logout = useCallback(() => {
    clearAccessToken();
    setUser(null);
  }, []);

  const value = useMemo(() => ({ user, isLoading, login, logout }), [user, isLoading, login, logout]);

  return <AuthContext.Provider value={value}>{children}</AuthContext.Provider>;
}

export function useAuth(): AuthContextValue {
  const context = useContext(AuthContext);
  if (!context) {
    throw new Error('useAuth must be used within an AuthProvider.');
  }

  return context;
}
