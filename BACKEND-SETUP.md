# NextGen local backend

The website now connects to a running ASP.NET Core API and a persistent PostgreSQL database. The existing PostgreSQL service on port 5432 is left unchanged; this project uses its own database cluster in `.local/postgres` on loopback port **55432**.

## Start everything

From `D:\NG website\NextGen-main`:

```powershell
.\run.ps1 -Mobile
```

This starts the development database, applies migrations, seeds initial data, waits for API readiness, and starts the website. Healthy running services are reused. Background windows stay hidden.

- Website: http://localhost:5102
- Phone: use your computer's Wi-Fi IPv4 address and port 5102 on the same Wi-Fi.
- API documentation: http://localhost:5101/swagger
- Database readiness: http://localhost:5101/health/ready

The API stays on loopback. Blazor makes API calls from the computer hosting the website, so phones do not need direct access to port 5101. `-Mobile` exposes the website to the LAN; without it, a newly started website binds to loopback.

PostgreSQL command-line tools are currently installed at `E:\postgresql\bin`. On another computer, supply `-PostgresBin` with the correct folder. The .NET 10 SDK is required. Node.js runs the API verification script and the existing stylesheet tooling.

## Accounts and configuration

Open `.local/DEVELOPMENT-ACCESS.txt` for the admin email and generated password. Create a student account at `/signup`. Demo student, instructor and HR account credentials are in the `Seed` section of `.local/backend.json`.

The launcher generates local database, JWT and account secrets once. `.local/` is excluded from Git. Keep this folder private; it contains credentials and database records. Do not delete it to restart the app.

In Development, the API reads `.local/backend.json`; environment variables and command-line settings override it. Demo accounts are seeded only when `Seed:IncludeDemoData` is enabled. SMTP is disabled by default, and verification checks enforce that emails are not sent.

## Working flows

- Six database-backed courses, their curriculum, category filters and detail pages.
- Student sign-up with a student profile and lead, atomic database writes, sign-in, session restoration, refresh-token rotation and logout revocation.
- Role-restricted admin, student, instructor and HR APIs and dashboards.
- Contact enquiries saved with linked leads visible to the admin.
- Masterclass creation through admin tools, registration deduplication, rejection of expired sessions, and capacity checks that handle concurrent registrations.
- When there is no scheduled masterclass, the website saves an enquiry requesting the next session update instead of claiming a seat.
- DTO validation, account lockout, public/authentication rate limits and usable HTTP error messages.
- Liveness and database readiness checks. Database initialization failure prevents the API from reporting a successful startup.

Initial course data and the offline preview share `ProgramCatalog` in the Shared project. Normal startup does not overwrite admin-edited courses. The explicit `Sync-DevelopmentCatalogue.mjs --apply` helper was used once to align this new isolated database with the approved website; do not run it to preserve subsequent admin edits.

## Stop, rebuild and restart

```powershell
.\scripts\Stop-Local.ps1
dotnet build .\NextGenCoaching.sln --no-restore
.\run.ps1 -Mobile -NoBuild
```

Stopping application processes preserves the database and records. `Stop-Local.ps1 -Database` also stops this project's isolated database; it does not stop the existing PostgreSQL service. `-ApiOnly` stops just the API. Windows can briefly retain a closed listener; wait for the process to exit before rebuilding.

## Verification

```powershell
node .\scripts\Verify-Backend.mjs
```

This uses clearly marked local verification records and `.invalid` email addresses. It verifies real database persistence, all available parameter-free read endpoints using the matching role, invalid requests, permissions, token rotation/revocation, lockout, registration deduplication and concurrent session capacity. Temporary webinar fixtures are soft-deleted. Test accounts and leads remain clearly marked in the local database.

Reports are in `.local/backend-checks.json`, `.local/backend-browser-checks.json` and `.local/persistence-check.json`. Desktop/mobile browser checks cover real form submissions, student session restoration and admin sign-in. No payment was charged or message sent.

The completed verification passed 120 API checks and 17 desktop/mobile browser checks. A separate first-time setup test (`scripts/Verify-FreshDatabase.ps1`) created a disposable database, applied migrations, verified all six canonical course IDs and curriculum relationships, ran the API suite, and removed only that test database. Saved records in the main development database were also verified after an API restart.

## Integrations still requiring configuration

The public enrollment flow now uses `/enroll?course=<slug>` (with `/checkout` retained as a compatibility alias). It collects an enrollment request without prices or payment UI. Submissions use the enquiry endpoint and appear in Admin → Leads with the selected published course, city and background. Submitting this form does not create a paid enrollment or grant course access.

- **Razorpay:** supply test keys before payment testing, then production keys and a verified webhook for live payments. Missing configuration returns an error; the app does not simulate a paid enrollment.
- **Email:** configure SMTP through admin settings before enabling delivery. Disabled delivery is logged locally.
- **SMS/WhatsApp:** SMS currently uses the existing logging stub. A provider integration is still required for actual notifications. The public WhatsApp contact link is separate.

This is a working local development backend. Production hosting, operational backups, external provider verification and a production security review remain separate deployment work.
