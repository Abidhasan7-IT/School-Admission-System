import { useCallback, useEffect, useState } from 'react';
import { api } from '../src/api.js';
import { messageFor } from '../src/messages.js';

const REFRESH_MS = 3000;

export default function ClassList({ studentId, onApplied }) {
  const [classes, setClasses] = useState(null);
  const [error, setError] = useState('');
  const [busyId, setBusyId] = useState(null);

  // Seat numbers refresh on their own, so nobody needs a page reload to see changes.
  const load = useCallback(async () => {
    try {
      setClasses(await api.listClasses());
      setError('');
    } catch (err) {
      setError(messageFor(err));
    }
  }, []);

  useEffect(() => {
    load();
    const timer = setInterval(load, REFRESH_MS);
    return () => clearInterval(timer);
  }, [load]);

  async function apply(cls) {
    setBusyId(cls.id);
    setError('');
    try {
      const hold = await api.createHold(cls.id, studentId);
      onApplied(hold.holdId);
    } catch (err) {
      setError(messageFor(err));
      load();
    } finally {
      setBusyId(null);
    }
  }

  return (
    <section className="panel">
      <h2>Class তালিকা</h2>
      <p className="muted">আবেদন করলে seat ১০ মিনিটের জন্য আপনার নামে থাকবে। এর মধ্যে ভর্তি ফি দিলে ভর্তি নিশ্চিত হবে।</p>

      {!studentId && <p className="alert info">আবেদন করতে উপরে ১৭ অঙ্কের জন্ম নিবন্ধন নম্বর লিখুন।</p>}
      {error && <p role="alert" className="alert error">{error}</p>}
      {!classes && !error && <p className="muted">লোড হচ্ছে…</p>}

      {classes && classes.length === 0 && <p className="muted">এখনো কোনো class তৈরি হয়নি।</p>}

      {classes && classes.length > 0 && (
        <div className="table-wrap">
          <table>
            <thead>
              <tr>
                <th>Class</th>
                <th className="num">মোট</th>
                <th className="num">Hold-এ</th>
                <th className="num">নিশ্চিত</th>
                <th className="num">খালি</th>
                <th aria-label="কাজ" />
              </tr>
            </thead>
            <tbody>
              {classes.map((cls) => {
                const full = cls.availableSeats <= 0;
                const used = cls.totalSeats > 0 ? ((cls.heldSeats + cls.confirmedSeats) / cls.totalSeats) * 100 : 0;
                return (
                  <tr key={cls.id}>
                    <td>
                      <strong>{cls.name}</strong>
                      <div className="meter" aria-hidden="true">
                        <span style={{ width: `${Math.min(100, used)}%` }} />
                      </div>
                    </td>
                    <td className="num">{cls.totalSeats}</td>
                    <td className="num">{cls.heldSeats}</td>
                    <td className="num">{cls.confirmedSeats}</td>
                    <td className="num available">{cls.availableSeats}</td>
                    <td className="action">
                      <button
                        type="button"
                        className="primary"
                        disabled={!studentId || full || busyId === cls.id}
                        onClick={() => apply(cls)}
                      >
                        {full ? 'seat নেই' : busyId === cls.id ? 'অপেক্ষা করুন…' : 'আবেদন করুন'}
                      </button>
                    </td>
                  </tr>
                );
              })}
            </tbody>
          </table>
        </div>
      )}
    </section>
  );
}
