import { useState } from 'react';
import { useLocation, useNavigate } from 'react-router-dom';
import { ApiError, toErrorMessage } from '../../api/client';
import { Field } from '../../components/Field';
import { useAuth } from '../../auth/useAuth';

interface ReturnLocation {
  from?: { pathname?: string; search?: string; hash?: string };
}

export function LoginPage() {
  const { login } = useAuth();
  const location = useLocation();
  const navigate = useNavigate();
  const [email, setEmail] = useState('');
  const [password, setPassword] = useState('');
  const [errors, setErrors] = useState<{ email?: string; password?: string }>({});
  const [submitError, setSubmitError] = useState<string>();
  const [isSubmitting, setIsSubmitting] = useState(false);

  const submit = async (event: React.FormEvent<HTMLFormElement>) => {
    event.preventDefault();
    setSubmitError(undefined);

    const nextErrors: { email?: string; password?: string } = {};
    if (!email.trim()) {
      nextErrors.email = 'Enter your work email.';
    } else if (!/^[^\s@]+@[^\s@]+\.[^\s@]+$/.test(email.trim())) {
      nextErrors.email = 'Enter a valid email address.';
    }

    if (!password) {
      nextErrors.password = 'Enter your password.';
    }

    setErrors(nextErrors);
    if (Object.keys(nextErrors).length > 0) return;

    setIsSubmitting(true);
    try {
      await login(email.trim(), password);
      const state = location.state as ReturnLocation | null;
      const target = state?.from;
      const destination = target?.pathname
        ? `${target.pathname}${target.search ?? ''}${target.hash ?? ''}`
        : '/tickets';
      await navigate(destination !== '/login' ? destination : '/tickets', { replace: true });
    } catch (error) {
      setSubmitError(
        error instanceof ApiError && error.status === 401
          ? 'Email or password is incorrect.'
          : toErrorMessage(error, 'Could not sign in.'),
      );
    } finally {
      setIsSubmitting(false);
    }
  };

  return (
    <main className="login-page">
      <section className="card login-card" aria-labelledby="login-heading">
        <h1 id="login-heading">Sign in to Northwind Support</h1>
        <p className="field__hint">Use your support agent account to continue.</p>
        <form className="form" onSubmit={submit} noValidate>
          <Field id="login-email" label="Work email" error={errors.email}>
            {(fieldProps) => (
              <input
                {...fieldProps}
                type="email"
                autoComplete="username"
                value={email}
                onChange={(event) => {
                  setEmail(event.target.value);
                  setErrors((current) => ({ ...current, email: undefined }));
                  setSubmitError(undefined);
                }}
              />
            )}
          </Field>
          <Field id="login-password" label="Password" error={errors.password}>
            {(fieldProps) => (
              <input
                {...fieldProps}
                type="password"
                autoComplete="current-password"
                value={password}
                onChange={(event) => {
                  setPassword(event.target.value);
                  setErrors((current) => ({ ...current, password: undefined }));
                  setSubmitError(undefined);
                }}
              />
            )}
          </Field>
          {submitError && <p className="field__error" role="alert">{submitError}</p>}
          <button className="button button--primary" type="submit" disabled={isSubmitting}>
            {isSubmitting ? 'Signing in...' : 'Sign in'}
          </button>
        </form>
      </section>
    </main>
  );
}
