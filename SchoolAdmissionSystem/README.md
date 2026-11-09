# School Admission System

A take-home project: an admission system for a school with a .NET 8 API, PostgreSQL, a React UI, and Docker.

## Requirements

Only **Docker Desktop** (or Docker Engine with the Compose plugin). Nothing else needs to be installed.

## Run the app

```bash
docker compose up --build
```

When it is running:

| What        | Address                          |
|-------------|----------------------------------|
| Web app     | http://localhost:3000            |
| API         | http://localhost:8080            |
| PostgreSQL  | localhost:5433 (user `postgres`, password `postgres`, database `school_admission`) |

### Try it

1. Open http://localhost:3000.
2. Go to **Class তৈরি (school admin)** and create a class, for example "Class 6 - Morning Shift" with 120 seats.
3. Type a 17-digit birth registration number at the top of the page, for example `20152691234567890`.
4. Go to **Class তালিকা** and press **আবেদন করুন**. The seat is held for 10 minutes.
5. Press **ভর্তি ফি দিন** to confirm, or **বাতিল** to cancel.
6. Go to **আমার আবেদন** to see all of that student's applications.

### Stop, restart, keep data

```bash
docker compose down        # stops everything, keeps the database
docker compose up -d       # starts again with the same data
docker compose down -v     # stops AND deletes all data
```

## Change the backend URL used by the frontend

The frontend needs to know where the browser can reach the API. Edit `API_BASE_URL` under `frontend` in `docker-compose.yml`, then run:

```bash
docker compose up -d frontend
```

No rebuild is needed. The container writes the URL when it starts.

## Change the hold duration

Edit `HOLD_DURATION_SECONDS` under `backend` in `docker-compose.yml` (default `600`, which is 10 minutes), then run `docker compose up -d backend`.

## Database connection

The backend reads `ConnectionStrings__DefaultConnection`. Inside Docker it points at the `db` service. If you run the backend outside Docker against your own PostgreSQL, set that variable to your connection string, for example:

```
Host=localhost;Port=5432;Database=school_admission;Username=postgres;Password=yourpassword
```

The tables are created automatically the first time the API starts.

## Run the tests

Both test suites run inside Docker too:

```bash
docker compose --profile test run --rm backend-tests
docker compose --profile test run --rm frontend-tests
```

- **Backend** (`backend/SchoolAdmission.Tests`): integration tests that call the real API against PostgreSQL. They create their own database, `school_admission_test`. This includes the concurrency test, which sends 30 simultaneous applications for 5 seats and checks that exactly 5 succeed.
- **Frontend** (`frontend/tests`): unit tests for the logic helpers.

## Project layout

```
school-admission-system/
├── docker-compose.yml
├── README.md
├── ASSUMPTIONS.md
├── AI_USAGE.md
├── backend/
│   ├── Dockerfile
│   ├── SchoolAdmission.Api/        ASP.NET Core API (controllers, services, EF Core)
│   └── SchoolAdmission.Tests/      xUnit integration tests
└── frontend/
    ├── Dockerfile                  builds with Vite, serves with nginx
    ├── src/                        React app
    └── tests/                      Vitest unit tests
```

## API quick reference

| # | Method | Path | Header | Success |
|---|--------|------|--------|---------|
| 1 | POST | `/classes` | | 201 |
| 2 | GET | `/classes` | | 200 |
| 3 | GET | `/classes/{classId}` | | 200 |
| 4 | POST | `/classes/{classId}/holds` | `X-Student-Id` | 201 (409 if no seat) |
| 5 | GET | `/holds/{holdId}` | | 200 |
| 6 | POST | `/holds/{holdId}/confirm` | `X-Student-Id` | 200 |
| 7 | DELETE | `/holds/{holdId}` | `X-Student-Id` | 204 |
| 8 | GET | `/me/holds` | `X-Student-Id` | 200 |

See `ASSUMPTIONS.md` for the rules behind each response.

## Troubleshooting

- **Port already in use:** change the left-hand number in the `ports:` entries of `docker-compose.yml`.
- **Backend exits on start:** run `docker compose logs backend`. The most common cause is the database not being ready yet. Compose waits for it, but a slow machine can still need a restart: `docker compose restart backend`.
- **Browser shows "Server-এর সাথে যোগাযোগ করা যাচ্ছে না":** check http://localhost:8080/classes opens in the browser, and that `API_BASE_URL` matches it.
