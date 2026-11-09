import { useCallback, useEffect, useState } from 'react';
import { api } from '../src/api.js';
import { messageFor } from '../src/messages.js';
import { effectiveStatus, statusLabel, formatCountdown } from '../src/utils.js';
import { useNow } from '../src/hooks.js';

export default function MyApplications({ studentId, onOpen }) {
  const [holds, setHolds] = useState(null);
  const [error, setError] = useState('');
  const now = useNow(1000);

  const load = useCallback(async () => {
    if (!studentId) return;
    try {
      setHolds(await api.myHolds(studentId));
      setError('');
    } catch (err) {
      setError(messageFor(err));
    }
  }, [studentId]);

  useEffect(() => {
    setHolds(null);
    load();
    const timer = setInterval(load, 3000);
    return () => clearInterval(timer);
  }, [load]);

  if (!studentId) {
    return (
      <section className="panel">
        <h2>আমার আবেদন</h2>
        <p className="alert info">আপনার আবেদন দেখতে উপরে ১৭ অঙ্কের জন্ম নিবন্ধন নম্বর লিখুন।</p>
      </section>
    );
  }

  return (
    <section className="panel">
      <h2>আমার আবেদন</h2>
      {error && <p role="alert" className="alert error">{error}</p>}
      {!holds && !error && <p className="muted">লোড হচ্ছে…</p>}
      {holds && holds.length === 0 && <p className="muted">এখনো কোনো আবেদন করা হয়নি।</p>}

      {holds && holds.length > 0 && (
        <ul className="application-list">
          {holds.map((h) => {
            const status = effectiveStatus(h.status, h.expiresAt, now);
            const remaining = new Date(h.expiresAt).getTime() - now;
            return (
              <li key={h.holdId}>
                <div>
                  <strong>{h.className}</strong>
                  <span className={`badge badge-${status.toLowerCase()}`}>{statusLabel(status)}</span>
                  {status === 'HELD' && <span className="countdown">বাকি {formatCountdown(remaining)}</span>}
                </div>
                <button type="button" onClick={() => onOpen(h.holdId)}>বিস্তারিত দেখুন</button>
              </li>
            );
          })}
        </ul>
      )}
    </section>
  );
}
