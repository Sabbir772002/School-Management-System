'use client';

import { useState } from 'react';
import { useRouter } from 'next/navigation';
import { apiFetch, setToken, setUserInfo } from '../lib/api';

export default function LoginPage() {
  const router = useRouter();
  const [email, setEmail] = useState('');
  const [password, setPassword] = useState('');
  const [error, setError] = useState('');
  const [loading, setLoading] = useState(false);

  // handle user login
  const handleLogin = async (e: React.FormEvent) => {
    e.preventDefault();
    setError('');
    setLoading(true);

    try {
      const data = await apiFetch('/auth/login', {
        method: 'POST',
        body: JSON.stringify({ email, password }),
      });

      setToken(data.token);
      setUserInfo(data);
      router.push('/dashboard');
    } catch (err: any) {
      setError(err.message || 'Login failed. Please check credentials.');
    } finally {
      setLoading(false);
    }
  };

  // quick demo account fill helper
  const quickFill = (demoEmail: string, demoPass: string) => {
    setEmail(demoEmail);
    setPassword(demoPass);
  };

  return (
    <main style={{ minHeight: '100vh', display: 'flex', alignItems: 'center', justifyContent: 'center', padding: '1.5rem' }}>
      <div className="glass-panel" style={{ width: '100%', maxWidth: '440px', padding: '2.5rem' }}>
        
        {/* Header */}
        <div style={{ textAlign: 'center', marginBottom: '2rem' }}>
          <div style={{
            width: '56px', height: '56px', borderRadius: '16px',
            background: 'linear-gradient(135deg, #4f46e5 0%, #06b6d4 100%)',
            display: 'inline-flex', alignItems: 'center', justifyContent: 'center',
            fontSize: '1.75rem', fontWeight: 800, color: '#fff', marginBottom: '1rem',
            boxShadow: '0 8px 20px -4px rgba(79, 70, 229, 0.4)'
          }}>
            AS
          </div>
          <h1 style={{ fontSize: '1.5rem', fontWeight: 800, color: 'var(--text-primary)' }}>
            Assignment Portal
          </h1>
          <p style={{ fontSize: '0.85rem', color: 'var(--text-secondary)', marginTop: '0.25rem' }}>
            School Assignment & Submission Management
          </p>
        </div>

        {/* Error Alert */}
        {error && (
          <div style={{
            background: 'rgba(244, 63, 94, 0.15)', border: '1px solid rgba(244, 63, 94, 0.3)',
            borderRadius: 'var(--radius-sm)', padding: '0.75rem 1rem', color: 'var(--accent-rose)',
            fontSize: '0.85rem', marginBottom: '1.25rem'
          }}>
            {error}
          </div>
        )}

        {/* Login Form */}
        <form onSubmit={handleLogin}>
          <div className="form-group">
            <label>Email Address</label>
            <input
              type="email"
              className="form-control"
              placeholder="e.g. teacher.rahim@school.com"
              value={email}
              onChange={(e) => setEmail(e.target.value)}
              required
            />
          </div>

          <div className="form-group" style={{ marginBottom: '1.5rem' }}>
            <label>Password</label>
            <input
              type="password"
              className="form-control"
              placeholder="••••••••"
              value={password}
              onChange={(e) => setPassword(e.target.value)}
              required
            />
          </div>

          <button type="submit" className="btn btn-primary" style={{ width: '100%' }} disabled={loading}>
            {loading ? 'Signing in...' : 'Sign In to Account'}
          </button>
        </form>

        {/* Quick Demo Fill Buttons for Evaluator */}
        <div style={{ marginTop: '2rem', paddingTop: '1.5rem', borderTop: '1px solid var(--border-color)' }}>
          <p style={{ fontSize: '0.75rem', fontWeight: 700, color: 'var(--text-muted)', textTransform: 'uppercase', letterSpacing: '0.5px', marginBottom: '0.75rem' }}>
            Quick Demo Login Accounts:
          </p>
          <div style={{ display: 'flex', flexDirection: 'column', gap: '0.5rem' }}>
            <button
              type="button"
              className="btn btn-secondary"
              style={{ justifyContent: 'space-between', fontSize: '0.8rem', padding: '0.5rem 0.75rem' }}
              onClick={() => quickFill('admin@school.com', 'Admin123!')}
            >
              <span>Login as <strong style={{ color: 'var(--accent-amber)' }}>Admin</strong></span>
              <span className="badge badge-admin">Admin</span>
            </button>

            <button
              type="button"
              className="btn btn-secondary"
              style={{ justifyContent: 'space-between', fontSize: '0.8rem', padding: '0.5rem 0.75rem' }}
              onClick={() => quickFill('teacher.rahim@school.com', 'Teacher123!')}
            >
              <span>Login as <strong style={{ color: 'var(--accent-cyan)' }}>Teacher</strong></span>
              <span className="badge badge-teacher">Teacher</span>
            </button>

            <button
              type="button"
              className="btn btn-secondary"
              style={{ justifyContent: 'space-between', fontSize: '0.8rem', padding: '0.5rem 0.75rem' }}
              onClick={() => quickFill('student.rafiq@school.com', 'Student123!')}
            >
              <span>Login as <strong style={{ color: 'var(--accent-emerald)' }}>Student</strong></span>
              <span className="badge badge-student">Student</span>
            </button>
          </div>
        </div>

      </div>
    </main>
  );
}
