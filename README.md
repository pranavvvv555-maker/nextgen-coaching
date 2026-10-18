# NextGen Graduate IT Coaching

**Current local setup:** the website and persistent database-backed API can be started together with `./run.ps1 -Mobile`. See [BACKEND-SETUP.md](BACKEND-SETUP.md) for current startup instructions, admin access, verified flows and remaining external integrations.

The API uses migrations and an isolated local development database. The older scaffold notes below describe earlier phases and are retained as historical context; follow the current setup guide above.

Multi-course IT training platform. .NET 10 · Blazor Web App · ASP.NET Core Web API · PostgreSQL 16 · EF Core 10.

This repository is the **foundation scaffold** — Phase 0, Phase 1, and the core of Phases 2 & 3 from the build plan. It compiles, runs, seeds six courses with their full 12-step journeys, exposes the public API, and renders the approved homepage mockup.

Everything from Phase 4 onwards (admin portal, payments, student portal, hardening, deploy) is documented in [`NextGen_Final_Build_Plan.md`](../NextGen_Final_Build_Plan.md) and slots cleanly onto this scaffold.

---

## Solution layout

```
NextGenCoaching.sln
├── docker-compose.yml             ← Postgres 16 + Seq for local dev
├── global.json                    ← Pins .NET 10.0.100 SDK
├── Directory.Build.props          ← Shared TargetFramework, Nullable
└── src/
    ├── NextGenCoaching.Shared/    ← DTOs, requests, responses, enums, constants
    ├── NextGenCoaching.Core/      ← Entities, EF Core, Identity, services, DI
    ├── NextGenCoaching.Api/       ← Web API (Minimal APIs)
    └── NextGenCoaching.Web/       ← Blazor Web App (talks to Api over HTTP)
```

Dependency rule: **`Web → Shared` only**. The browser never sees domain code. `Api → Core + Shared`. `Core → Shared`.

---

## Prerequisites

- .NET 10 SDK (version `10.0.100` or later — pinned in `global.json`)
- Docker Desktop (Postgres + Seq for local logs)
- Node.js 18+ (for Tailwind build — only when iterating on styles)

---

## Run it locally

### 1. Start Postgres

```bash
docker compose up -d
```

This brings up:
- Postgres at `localhost:5432` (user `nextgen`, password `nextgen_dev_password`, db `nextgen_coaching`)
- Seq at `http://localhost:5341` (structured log viewer)

### 2. Restore + build

```bash
dotnet restore
dotnet build
```

### 3. Create the schema & seed

The Api builds the schema directly from the entity model on startup (via `EnsureCreatedAsync`) and runs the seeder. The first time you boot the Api it will:

- Create all tables in the `nextgen_coaching` database
- Seed 5 roles (Admin, Counsellor, Instructor, HrReviewer, Student)
- Seed the admin user (default: `admin@nextgencoaching.local` / `ChangeMe!2026`)
- Seed all 6 courses with their journey steps, tools, features, projects and assignments
- Seed 3 testimonials, 6 FAQs, 3 email templates, 2 SMS templates

> **Why `EnsureCreatedAsync` instead of migrations?** No migration files exist yet — the scaffold is early enough that schema churn is expected. `EnsureCreatedAsync` reads your current entity model and creates matching tables. The trade-off: it doesn't track migrations, so every schema change means dropping the DB. That's fine while you're iterating on the model.

**To reset the database** (re-runs the seed on next start):

```bash
docker compose down -v   # removes the postgres volume
docker compose up -d     # starts fresh — next dotnet run will create + seed
```

### Switching to real migrations (do this once you have data you can't lose)

```bash
# 1. Generate the Initial migration from the current model
dotnet ef migrations add Initial \
  --project src/NextGenCoaching.Core \
  --startup-project src/NextGenCoaching.Api \
  --output-dir Persistence/Migrations

# 2. In DbSeeder.cs replace
#       await ctx.Database.EnsureCreatedAsync();
#    with
#       await ctx.Database.MigrateAsync();

# 3. If the DB already has tables from EnsureCreated, baseline it:
dotnet ef migrations script --idempotent ... > baseline.sql
#    then mark Initial as already-applied:
INSERT INTO "__EFMigrationsHistory" ("MigrationId","ProductVersion") VALUES ('<timestamp>_Initial','10.0.1');
```

From that point on, `dotnet ef migrations add <Name>` for every schema change.

### 4. Run the API

```bash
dotnet run --project src/NextGenCoaching.Api
```

- Swagger: <https://localhost:7101/swagger>
- Ping: <https://localhost:7101/ping>

### 5. Run the Blazor Web App (in a second terminal)

```bash
dotnet run --project src/NextGenCoaching.Web
```

- Site: <https://localhost:7100>

### 6. Build the Tailwind CSS (only if you change styles)

```bash
cd src/NextGenCoaching.Web
npm install
npm run build:css     # one-off build
npm run watch:css     # rebuild on change
```

The repo ships with a minimal placeholder `wwwroot/css/app.output.css` so the site renders before Tailwind is built — run `npm run build:css` to get the full utility set.

---

## What's implemented (Phase 0 → Phase 3 partial)

### Phase 0 — Setup ✅
- Solution + 4 projects with proper references and NuGet packages
- Docker Compose for Postgres + Seq
- Directory.Build.props, global.json, .gitignore
- Tailwind toolchain with locked design tokens

### Phase 1 — Domain, Persistence, Seed ✅
- All 30+ entities (`Course`, `Lead`, `Webinar`, `Batch`, `Enrollment`, `Session`, `Assignment`, `Project`, `MockInterview`, `Placement`, `JobReferral`, `Resume`, etc.)
- All Core enums (`BatchStatus`, `SessionType`, `PaymentStatus`, …)
- `AppDbContext` with full `OnModelCreating` configuration: max-lengths, indexes, decimal precision, enum-to-string conversions, JSONB columns where needed, snake_case naming, soft-delete global query filter
- `AuditableEntityInterceptor` auto-populates `CreatedAtUtc`/`UpdatedAtUtc`/`CreatedBy`/`UpdatedBy`, converts deletes to soft-deletes
- `ApplicationUser` (Guid keys), `ApplicationRole`, JWT `TokenService`, Identity tables mapped to snake_case
- `DbSeeder` populates roles, admin user, **all 6 courses** with full content

### Phase 2 — Public API ✅ (core)
- `GET  /api/v1/public/courses` (with optional `?category=` filter)
- `GET  /api/v1/public/courses/categories`
- `GET  /api/v1/public/courses/{slug}` (with journey, tools, features)
- `GET  /api/v1/public/testimonials/featured`
- `GET  /api/v1/public/faqs`
- `GET  /api/v1/public/webinars/upcoming`
- `POST /api/v1/public/webinars/{id}/register` (creates lead + registration, dedupes)
- `POST /api/v1/public/enquiries` (creates lead from contact form)
- `POST /api/v1/auth/login`, `/auth/refresh`, `/auth/logout`
- Uniform `ProblemDetails` via `ExceptionHandlingMiddleware`
- Rate limiter wired (`public-post` policy ready to use)
- Swagger UI with Bearer auth

### Phase 3 — Public Website ✅ (home + course pages)
- Home page implementing the approved multi-course mockup (hero, 12-step journey, course catalogue, CTA, testimonials)
- Courses listing page with category filter
- Course details page (per-course hero, journey, tools, features, pricing)
- Webinar registration page with upcoming-session picker
- Contact page with enquiry form
- About page
- Shared layout: `TopBar`, `MainNav`, `SiteFooter`
- Reusable components: `CourseCard`, `JourneyStepCard`, `Stat`
- Typed API clients: `ApiClient`, `CourseApiClient`, `WebinarApiClient`, `LeadApiClient`, `ContentApiClient`

---

## What's still to build (Phase 4 → 12)

These all slot onto the existing scaffold. Build them in order following the phase plan.

### Phase 4 — Auth & Admin Shell *(week 6)*
- `AdminLayout.razor`, `StudentLayout.razor` with navy sidebar + top bar
- Login page consuming `POST /auth/login`
- `CookieAuthenticationStateProvider` storing JWT in HttpOnly cookie
- `AuthorizationMessageHandler` to attach tokens to outgoing requests
- Empty admin dashboard at `/admin` behind `[Authorize(Policy="RequireAdmin")]`

### Phase 5 — Admin: Leads & Webinars *(weeks 7-8)*
- Admin endpoints: `GET /admin/leads` (paged + filtered), `PATCH /admin/leads/{id}`, lead activities, webinar CRUD
- Admin pages: leads table with side-drawer, webinar list/edit, email/SMS template editor
- Hangfire on PostgreSQL for reminder jobs

### Phase 6 — Admin: Catalogue & Content *(week 9)*
- Course CRUD, batch CRUD, trainer CRUD, testimonial/FAQ/blog editors
- Drag-to-reorder for journey steps
- File upload through `IFileStorage` (swap `LocalFileStorage` for Azure Blob in prod)

### Phase 7 — Enrollment & Payments *(week 10)*
- Razorpay integration (`IPaymentGateway` interface already in `Core/Interfaces`)
- Lead → student conversion flow
- Webhook handler with idempotency
- Invoice PDF generation

### Phase 8 — Student Portal *(weeks 11-12)*
- `StudentLayout` matching mockup
- Dashboard with stat cards + sessions + progress + announcements + calendar
- My Courses, Sessions, Assignments, Projects, Mock Interviews, Resources, Payments

### Phase 9 — Instructor & HR Portals *(week 13)*
- Instructor view: batches, grading queue
- HR view: resume review queue, job openings, referrals

### Phase 10 P0 — Auth hardening ✅ (persistent sessions + silent token refresh)
- Sessions persist across page refresh and browser restart: `AuthStateService` stores the
  token pair in `ProtectedLocalStorage` (encrypted with the server's Data Protection keys)
  and `AuthSessionRestore` (in `Routes.razor`) rehydrates it once per circuit
- Silent refresh: `AuthorizationMessageHandler` proactively rotates the access token via
  `TokenRefreshService` (single-flight `POST /auth/refresh`) when it has <90s of life left,
  plus a 401-retry safety net for GETs — the 60s notification poller no longer dies at the
  30-minute mark
- Refresh-token rejection (revoked/expired) signs the user out everywhere; sign-out also
  revokes the token server-side via `POST /auth/logout`
- Fixed a handler-scoping hazard: `ApiClient` is now built per circuit over a shared
  `SocketsHttpHandler` instead of registering `AuthorizationMessageHandler` through
  `IHttpClientFactory`, whose pipelines are cached across circuits and can't see (or could
  leak) circuit-scoped auth state
- Protected layouts gate on `AuthState.Initialized` so restore shows a brief loading state
  instead of flashing the sign-in prompt

### Phase 10 — Hardening *(week 14)*
- Security pass (CSP, account lockout, secrets to Key Vault)
- Test coverage (Testcontainers + Playwright)
- Observability (App Insights / OpenTelemetry)
- Backup + restore drill

### Phase 11 — Deploy *(week 15)*
- Multi-stage Dockerfiles
- IaC (Bicep / Terraform)
- Blue-green deploy
- DNS + SSL + CDN

### Phase 12 — Post-launch *(ongoing)*
- WhatsApp Business API
- Referral programme
- AI resume reviewer
- Mobile (MAUI Blazor Hybrid)

---

## Useful EF Core commands

```bash
# Add a new migration
dotnet ef migrations add <Name> \
  --project src/NextGenCoaching.Core \
  --startup-project src/NextGenCoaching.Api \
  --output-dir Persistence/Migrations

# Apply migrations
dotnet ef database update \
  --project src/NextGenCoaching.Core \
  --startup-project src/NextGenCoaching.Api

# Revert the last migration
dotnet ef migrations remove \
  --project src/NextGenCoaching.Core \
  --startup-project src/NextGenCoaching.Api
```

---

## Default credentials (for local dev only)

| Role  | Email                          | Password       |
|-------|--------------------------------|----------------|
| Admin | admin@nextgencoaching.local    | ChangeMe!2026  |

Change these immediately for any non-local environment via `appsettings.json` / user secrets / Key Vault.

---

## Configuration that needs filling in before staging

| Key                                | Where                                   | Purpose                            |
|------------------------------------|-----------------------------------------|------------------------------------|
| `ConnectionStrings:DefaultConnection` | `appsettings.json` (Api)             | Postgres connection                |
| `Jwt:Secret`                       | **User secrets or Key Vault** (not repo) | At least 32 chars, randomly generated |
| `Email:*`                          | `appsettings.json` (Api)                | Email provider (SES / SendGrid)    |
| `Storage:*`                        | `appsettings.json` (Api)                | Switch to Azure Blob / S3 in prod  |
| `Cors:WebOrigin`                   | `appsettings.json` (Api)                | Allowed origin for the Blazor app  |
| `Api:BaseUrl`                      | `appsettings.json` (Web)                | URL the Web app calls              |
| `Seed:AdminEmail`, `Seed:AdminPassword` | `appsettings.json` (Api)           | First-run admin user               |

---

## Project conventions

- All entities inherit `BaseEntity` (Guid Id + audit + soft-delete)
- Soft-delete is automatic via `SaveChangesInterceptor` — calling `db.Set<T>().Remove()` flips `IsDeleted = true` instead of issuing a `DELETE`
- DB column names are snake_case (`EFCore.NamingConventions`)
- Enum columns stored as strings via `.HasConversion<string>()`
- DTOs in `Shared` are immutable (`init` setters) and never reference domain entities
- API responses are wrapped in `ApiResponse<T>` envelope
- Public POST endpoints flow into `Lead` records so the counsellor team sees every signal

---

## Reference

- Schema and architecture details: `NextGen_Final_Build_Plan.md`
- Mockup direction: locked, see plan §1.3 for design tokens
