import { useCallback, useEffect, useState } from 'react';
import { api } from '../src/api.js';
import { messageFor } from '../src/messages.js';
import { effectiveStatus, statusLabel, formatCountdown } from '../src/utils.js';
import { useNow } from '../src/hooks.js';

export default function ApplicationDetail({ studentId, holdId, onBack }) {
  const [hold, setHold] = useState(null);
  const [notFound, setNotFound] = useState(false);
  const [error, setError] = useState('');
  const [message, setMessage] = useState('');
  const [busy, setBusy] = useState(false);
  const now = useNow(1000);

  const load = useCallback(async () => {
    if (!studentId) return;
    try {
      const list = await api.myHolds(studentId);
      const found = list.find((h) => h.holdId === holdId) ?? null;
      setNotFound(!found);
      setHold(found);
      setError('');
    } catch (err) {
      setError(messageFor(err));
    }
  }, [studentId, holdId]);

  useEffect(() => {
    load();
    const timer = setInterval(load, 3000);
    return () => clearInterval(timer);
  }, [load]);

  if (!studentId) {
    return <p className="alert info">আবেদনের তথ্য দেখতে উপরে ১৭ অঙ্কের জন্ম নিবন্ধন নম্বর লিখুন।</p>;
  }
  if (notFound) {
    return (
      <section className="panel">
        <p className="alert error">এই আবেদনটি এই শিক্ষার্থীর নামে পাওয়া যায়নি।</p>
        <button type="button" onClick={onBack}>আমার আবেদনে ফিরে যান</button>
      </section>
    );
  }
  if (!hold) {
    return <p className="muted">{error || 'লোড হচ্ছে…'}</p>;
  }

  const status = effectiveStatus(hold.status, hold.expiresAt, now);
  const remaining = new Date(hold.expiresAt).getTime() - now;
  const canAct = status === 'HELD' && !busy;

  async function payAdmissionFee() {
    setBusy(true);
    setError('');
    setMessage('');
    try {
      await api.confirmHold(holdId, studentId);
      setMessage('ভর্তি ফি জমা সফল হয়েছে। আপনার ভর্তি নিশ্চিত হয়েছে।');
    } catch (err) {
      setError(messageFor(err));
    } finally {
      setBusy(false);
      load();
    }
  }

  async function cancelApplication() {
    if (!window.confirm('আবেদনটি বাতিল করতে চান? Seat আবার অন্যদের জন্য খুলে যাবে।')) return;
    setBusy(true);
    setError('');
    try {
      await api.cancelHold(holdId, studentId);
      onBack();
    } catch (err) {
      setError(messageFor(err));
      setBusy(false);
      load();
    }
  }

  return (
    <section className="panel detail">
      <p className="muted">{hold.className}</p>
      <h2 className={`status-line status-${status.toLowerCase()}`}>{statusLabel(status)}</h2>

      {status === 'HELD' ? (
        <div className="timer">
          <span className="timer-label">মেয়াদ শেষ হতে বাকি</span>
          <span className="timer-value" aria-live="polite">{formatCountdown(remaining)}</span>
        </div>
      ) : (
        <p className="muted">
          {status === 'EXPIRED'
            ? 'সময়সীমা পেরিয়ে গেছে। Seat এখন অন্যদের জন্য খোলা।'
            : status === 'CONFIRMED'
              ? 'আপনার ভর্তি সম্পন্ন হয়েছে।'
              : 'এই আবেদনটি বাতিল করা হয়েছে।'}
        </p>
      )}

      <div className="actions">
        <button type="button" className="primary" disabled={!canAct} onClick={payAdmissionFee}>
          {busy ? 'জমা হচ্ছে…' : 'ভর্তি ফি দিন'}
        </button>
        <button type="button" className="danger" disabled={!canAct} onClick={cancelApplication}>
          বাতিল
        </button>
        <button type="button" onClick={onBack}>আমার আবেদনে ফিরে যান</button>
      </div>

      {message && <p role="status" className="alert success">{message}</p>}
      {error && <p role="alert" className="alert error">{error}</p>}
    </section>
  );
}
