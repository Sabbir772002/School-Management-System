// api client helper for backend calls
const API_BASE = '/api';

export function getToken(): string | null {
  if (typeof window === 'undefined') return null;
  return localStorage.getItem('assignment_jwt_token');
}

export function setToken(token: string) {
  if (typeof window !== 'undefined') {
    localStorage.setItem('assignment_jwt_token', token);
  }
}

export function clearToken() {
  if (typeof window !== 'undefined') {
    localStorage.removeItem('assignment_jwt_token');
    localStorage.removeItem('assignment_user_info');
  }
}

export function getUserInfo() {
  if (typeof window === 'undefined') return null;
  const data = localStorage.getItem('assignment_user_info');
  return data ? JSON.parse(data) : null;
}

export function setUserInfo(user: any) {
  if (typeof window !== 'undefined') {
    localStorage.setItem('assignment_user_info', JSON.stringify(user));
  }
}

export async function apiFetch(endpoint: string, options: RequestInit = {}) {
  const token = getToken();
  const headers: Record<string, string> = {
    ...(options.headers as Record<string, string>),
  };

  if (token) {
    headers['Authorization'] = `Bearer ${token}`;
  }

  // default to json body if not FormData
  if (options.body && !(options.body instanceof FormData) && !headers['Content-Type']) {
    headers['Content-Type'] = 'application/json';
  }

  const response = await fetch(`${API_BASE}${endpoint}`, {
    ...options,
    headers,
  });

  if (!response.ok) {
    const errorData = await response.json().catch(() => ({ message: 'API Request failed' }));
    throw new Error(errorData.message || `Error ${response.status}`);
  }

  return response.json();
}
