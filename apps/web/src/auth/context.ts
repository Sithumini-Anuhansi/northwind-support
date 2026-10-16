import { createContext } from 'react';
import type { AuthenticatedAgent } from '../types/api';

export interface AuthContextValue {
  user: AuthenticatedAgent | null;
  isLoading: boolean;
  login: (email: string, password: string) => Promise<void>;
  logout: () => void;
}

export const AuthContext = createContext<AuthContextValue | undefined>(undefined);
