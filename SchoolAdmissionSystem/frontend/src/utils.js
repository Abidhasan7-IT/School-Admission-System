export function normalizeDigits(value) {
  return value.replace(/[\u09E6-\u09EF]/g, (d) => String(d.charCodeAt(0) - 0x09e6));
}

export function isValidStudentId(value) {
  return /^[0-9]{17}$/.test(value ?? '');
}

export function effectiveStatus(status, expiresAt, now) {
  if (status === 'HELD' && new Date(expiresAt).getTime() <= now) return 'EXPIRED';
  return status;
}

export function formatCountdown(ms) {
  if (ms <= 0) return '00:00';
  const total = Math.floor(ms / 1000);
  const hours = Math.floor(total / 3600);
  const minutes = Math.floor((total % 3600) / 60);
  const seconds = total % 60;
  const pad = (n) => String(n).padStart(2, '0');
  return hours > 0 ? `${hours}:${pad(minutes)}:${pad(seconds)}` : `${pad(minutes)}:${pad(seconds)}`;
}

export function statusLabel(status) {
  switch (status) {
    case 'HELD':
      return 'ভর্তি ফি বাকি';
    case 'CONFIRMED':
      return 'ভর্তি নিশ্চিত';
    case 'EXPIRED':
      return 'মেয়াদ শেষ';
    case 'CANCELLED':
      return 'বাতিল';
    default:
      return status;
  }
}
