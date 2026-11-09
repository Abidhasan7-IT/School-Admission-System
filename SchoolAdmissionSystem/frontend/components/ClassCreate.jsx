import { useState } from 'react';
import { api } from '../src/api.js';
import { messageFor } from '../src/messages.js';

export default function ClassCreate() {
  const [name, setName] = useState('');
  const [seats, setSeats] = useState('');
  const [error, setError] = useState('');
  const [success, setSuccess] = useState('');
  const [saving, setSaving] = useState(false);

  async function submit(event) {
    event.preventDefault();
    setError('');
    setSuccess('');

    const trimmed = name.trim();
    const count = Number(seats);
    if (!trimmed) {
      setError('Class-এর নাম লিখুন।');
      return;
    }
    if (!Number.isInteger(count) || count < 1) {
      setError('Seat সংখ্যা ১ বা তার বেশি পূর্ণসংখ্যা হতে হবে।');
      return;
    }

    setSaving(true);
    try {
      const created = await api.createClass(trimmed, count);
      setSuccess(`"${created.name}" তৈরি হয়েছে। মোট ${created.totalSeats} টি seat।`);
      setName('');
      setSeats('');
    } catch (err) {
      setError(messageFor(err));
    } finally {
      setSaving(false);
    }
  }

  return (
    <section className="panel">
      <h2>নতুন Class তৈরি</h2>
      <p className="muted">প্রতিটি shift আলাদা class হিসেবে তৈরি করুন। যেমন "Class 6 - Morning Shift" আর "Class 6 - Evening Shift"।</p>

      <form className="create-form" onSubmit={submit}>
        <label>
          Class-এর নাম
          <input value={name} onChange={(e) => setName(e.target.value)} placeholder="Class 6 - Morning Shift" />
        </label>
        <label>
          মোট seat
          <input
            type="number"
            min="1"
            value={seats}
            onChange={(e) => setSeats(e.target.value)}
            placeholder="120"
          />
        </label>
        <button type="submit" className="primary" disabled={saving}>
          {saving ? 'তৈরি হচ্ছে…' : 'Class তৈরি করুন'}
        </button>
      </form>

      {error && <p role="alert" className="alert error">{error}</p>}
      {success && <p role="status" className="alert success">{success}</p>}
    </section>
  );
}
