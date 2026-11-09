# ASSUMPTIONS

Where the brief was silent or open, these are the choices made and why.

## Seats and holds

1. **Seat rule.** For each class, `held + confirmed <= totalSeats` at all times. Seat counts are never stored. They are computed from the holds table, so there is no counter that can drift.
2. **Hold lifetime.** `HOLD_DURATION_SECONDS` (default 600). A hold expires at `createdAt + duration`.
3. **Automatic release.** No client call is needed to free a seat. Three things guarantee this: (a) seat counts only count holds that are still unexpired, (b) a background job marks expired holds `EXPIRED` every 2 seconds, and (c) `GET` responses report `EXPIRED` for any hold past its time, even before the job runs.
4. **One active application per student per class.** "Active" means `HELD` or `CONFIRMED`. This is enforced twice: in the application logic, and by a partial unique index on `(class_id, student_id) WHERE status IN ('HELD','CONFIRMED')`. A second application returns `409 ALREADY_ACTIVE`.
5. **Students can apply to several different classes** (for example Morning and Day shift of Class 6 are separate classes). The one-per-class rule only applies within a single class.

## Concurrency

6. **Lock per class.** Creating a hold runs in one transaction that first takes a row lock on the class (`SELECT ... FOR UPDATE`). Applications for the same class therefore run one at a time, and the seat count is read after the lock is held. This makes the "never over-allocate" rule hold under parallel load. The concurrency test checks this with 30 simultaneous requests for 5 seats.
7. **Confirm and cancel use conditional updates.** `UPDATE holds SET status='CONFIRMED' WHERE id=? AND status='HELD' AND expires_at > now()`. If the hold expired a moment earlier, the update matches zero rows and the request is refused. This avoids a race between confirm and expiry.
8. **All state lives in PostgreSQL, not in process memory.** Two backend processes pointed at the same database follow the same rules, because the locks and the conditional updates are enforced by the database. This is the basis for the bonus item in the brief.

## Confirm, cancel and errors

9. **Payment is simulated.** "ভর্তি ফি দিন" calls the confirm endpoint directly. No payment gateway is used, as the brief asks.
10. **Confirm is idempotent.** Confirming an already-confirmed hold returns `200` and does not increase `confirmedSeats` again.
11. **Confirm on an expired hold** returns `409 HOLD_EXPIRED`.
12. **Cancel rules.** A `HELD` hold can be cancelled (`204`). A `CONFIRMED` admission cannot be cancelled (`409 HOLD_ALREADY_CONFIRMED`), because the brief does not say otherwise and a confirmed seat is a real admission. Cancelling an already cancelled or expired hold returns `204`, since it no longer holds a seat.
13. **Ownership.** A student can only confirm or cancel their own holds. Anyone else gets `403 FORBIDDEN`.
14. **Error shape.** Every error is `{ "code": "...", "message": "..." }`. Status codes: `400` bad input, `403` not your hold, `404` unknown ID, `409` business rule (seat full, expired, already active), `500` only for true server faults. Bad input never produces `500`.
15. **`GET /holds/{holdId}` needs no header**, because the brief's table does not list one for it. The `X-Student-Id` header is required on the other endpoints listed in the brief.

## Identity

16. **No login.** The student enters their 17-digit birth registration number in the page header. It is sent as `X-Student-Id`. This is identification, not authentication. The brief says there is no login, so anyone who knows the number can act for that student. That is accepted for this exercise and would not be acceptable in production.
17. **Validation.** The ID must be exactly 17 ASCII digits (`[0-9]`). The backend uses `[0-9]` rather than `\d` because `\d` would also match Bengali digits. The frontend converts Bengali digits to ASCII as they are typed, so students can type either script.
18. **Browser memory.** The UI stores the entered number in `localStorage`, so a page refresh still shows the student's applications. This is a convenience, not a security feature.

## Data and infrastructure

19. **Classes are per shift.** Each shift is its own class row with its own seat count. Class names are not required to be unique.
20. **Schema creation.** `EnsureCreated()` builds the schema on start-up. Migrations are not used, to keep the take-home small. In a real project, EF Core migrations would be used instead.
21. **Persistence.** Data lives in the named Docker volume `pgdata`. `docker compose down` keeps it, and only `docker compose down -v` deletes it.
22. **Database name.** `school_admission`, created by the compose file. Tests use a separate `school_admission_test` database, so demo data is never touched.
23. **Times** are UTC, in ISO 8601 format (for example `2026-11-01T04:10:00Z`). The countdown is computed on the client against the server's `expiresAt`. Small clock differences between machines can shift the displayed time by a few seconds, but the server is always the authority.
24. **API URL configuration.** The frontend reads its API URL at container start from the `API_BASE_URL` variable. Changing it does not require a rebuild.
25. **CORS** is open to all origins, for simplicity in this exercise.
26. **Admin screen.** "School admin" is simply a tab. The brief has no admin login, so the screen is not protected.

## Frontend

27. **Refresh behaviour.** Class seat numbers and the student's applications refresh every 3 seconds. The countdown ticks every second and switches to `EXPIRED` on the client as soon as time runs out, with no page reload needed.
28. **Error text** is shown in Bengali, mapped from the API's error codes, not copied from the server.

## Bonus items

29. **Two backend processes sharing one database** keep the same limits (see points 6–8).
30. **Frontend automated tests** cover the pure logic: student ID rules, Bengali digit conversion, expiry, countdown formatting, and error mapping. Component rendering is not tested.
