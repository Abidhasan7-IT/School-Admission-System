import { useEffect, useState } from 'react';
import ClassCreate from '../components/ClassCreate.jsx';
import ClassList from '../components/ClassList.jsx';
import MyApplications from '../components/MyApplications.jsx';
import ApplicationDetail from '../components/ApplicationDetail.jsx';
import { isValidStudentId, normalizeDigits } from './utils.js';

const STORAGE_KEY = 'school-admission:studentId';

const TABS = [
  { id: 'classes', label: 'Class তালিকা' },
  { id: 'mine', label: 'আমার আবেদন' },
  { id: 'admin', label: 'Class তৈরি (school admin)' },
];

export default function App() {
  const [studentId, setStudentId] = useState(() => localStorage.getItem(STORAGE_KEY) ?? '');
  const [view, setView] = useState('classes');
  const [holdId, setHoldId] = useState(null);

  useEffect(() => {
    localStorage.setItem(STORAGE_KEY, studentId);
  }, [studentId]);

  const validId = isValidStudentId(studentId) ? studentId : '';
  const showingDetail = view === 'application';

  function openApplication(id) {
    setHoldId(id);
    setView('application');
  }

  return (
    <div className="app">
      <header className="masthead">
        <div className="brand">
          <a href='/'><h1>School Admission</h1></a>
          <p>অনলাইন ভর্তি ব্যবস্থা</p>
        </div>

        <div className="student-box">
          <label htmlFor="student-id">শিক্ষার্থীর জন্ম নিবন্ধন নম্বর</label>
          <input
            id="student-id"
            inputMode="numeric"
            autoComplete="off"
            maxLength={17}
            placeholder="১৭ অঙ্ক"
            value={studentId}
            onChange={(e) => setStudentId(normalizeDigits(e.target.value).replace(/\s/g, ''))}
          />
          {studentId && !validId && <span className="field-hint">১৭ অঙ্ক লিখলে আবেদন করা যাবে।</span>}
        </div>
      </header>

      <nav className="tabs" aria-label="মূল মেনু">
        {TABS.map((tab) => (
          <button
            key={tab.id}
            type="button"
            className={view === tab.id ? 'tab active' : 'tab'}
            onClick={() => setView(tab.id)}
          >
            {tab.label}
          </button>
        ))}
      </nav>

      <main>
        {view === 'classes' && <ClassList studentId={validId} onApplied={openApplication} />}
        {view === 'mine' && <MyApplications studentId={validId} onOpen={openApplication} />}
        {view === 'admin' && <ClassCreate />}
        {showingDetail && (
          <ApplicationDetail
            studentId={validId}
            holdId={holdId}
            onBack={() => setView('mine')}
          />
        )}
      </main>
    </div>
  );
}
