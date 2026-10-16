import { Navigate, Outlet, useLocation } from 'react-router-dom';
import { Spinner } from '../components/Spinner';
import { useAuth } from './AuthContext';

export function ProtectedRoute() {
  const { user, isLoading } = useAuth();
  const location = useLocation();

  if (isLoading) {
    return <Spinner label="Checking your sign-in" />;
  }

  if (!user) {
    return <Navigate to="/login" replace state={{ from: location }} />;
  }

  return <Outlet />;
}
