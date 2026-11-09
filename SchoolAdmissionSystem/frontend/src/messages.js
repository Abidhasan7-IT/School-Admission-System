// Maps the API's error codes to plain Bengali messages for the user.
const MESSAGES = {
  SEAT_FULL: 'দুঃখিত, এই class-এ আর কোনো seat খালি নেই।',
  ALREADY_ACTIVE: 'এই class-এ আপনার একটি আবেদন বা ভর্তি ইতোমধ্যে আছে।',
  HOLD_EXPIRED: 'আবেদনের মেয়াদ শেষ হয়ে গেছে। seat আবার অন্যদের জন্য খুলে গেছে।',
  HOLD_NOT_ACTIVE: 'এই আবেদনটি আর সক্রিয় নেই।',
  HOLD_ALREADY_CONFIRMED: 'ভর্তি ইতোমধ্যে নিশ্চিত হয়েছে, তাই বাতিল করা যাবে না।',
  HOLD_NOT_FOUND: 'আবেদনটি খুঁজে পাওয়া যায়নি।',
  CLASS_NOT_FOUND: 'Class-টি খুঁজে পাওয়া যায়নি।',
  FORBIDDEN: 'এই আবেদনটি আপনার নয়।',
  INVALID_STUDENT_ID: 'জন্ম নিবন্ধন নম্বর ১৭ অঙ্কের হতে হবে।',
  VALIDATION_ERROR: 'দেওয়া তথ্যগুলো আবার যাচাই করে দেখুন।',
  NETWORK_ERROR: 'Server-এর সাথে যোগাযোগ করা যাচ্ছে না। একটু পরে আবার চেষ্টা করুন।',
  INTERNAL_ERROR: 'কিছু একটা সমস্যা হয়েছে। একটু পরে আবার চেষ্টা করুন।',
};

export function messageFor(error) {
  return MESSAGES[error?.code] ?? MESSAGES.INTERNAL_ERROR;
}
