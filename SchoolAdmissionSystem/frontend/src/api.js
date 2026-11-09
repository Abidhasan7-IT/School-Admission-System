import { API_BASE_URL } from './config.js';

export class ApiError extends Error {
  constructor(status, code, message) {
    super(message);
    this.status = status;
    this.code = code;
  }
}

async function request(path, { method = 'GET', body, studentId } = {}) {
  const headers = { Accept: 'application/json' };
  if (body !== undefined) headers['Content-Type'] = 'application/json';
  if (studentId) headers['X-Student-Id'] = studentId;

  let response;
  try {
    response = await fetch(`${API_BASE_URL}${path}`, {
      method,
      headers,
      body: body !== undefined ? JSON.stringify(body) : undefined,
    });
  } catch {
    throw new ApiError(0, 'NETWORK_ERROR', 'Network error');
  }

  if (response.status === 204) return null;

  let data = null;
  try {
    data = await response.json();
  } catch {
    data = null;
  }

  if (!response.ok) {
    throw new ApiError(
      response.status,
      data?.code ?? `HTTP_${response.status}`,
      data?.message ?? 'Request failed',
    );
  }
  return data;
}

const enc = encodeURIComponent;

export const api = {
  listClasses: () => request('/classes'),
  createClass: (name, totalSeats) => request('/classes', { method: 'POST', body: { name, totalSeats } }),
  createHold: (classId, studentId) =>
    request(`/classes/${enc(classId)}/holds`, { method: 'POST', studentId }),
  confirmHold: (holdId, studentId) =>
    request(`/holds/${enc(holdId)}/confirm`, { method: 'POST', studentId }),
  cancelHold: (holdId, studentId) =>
    request(`/holds/${enc(holdId)}`, { method: 'DELETE', studentId }),
  myHolds: (studentId) => request('/me/holds', { studentId }),
};
