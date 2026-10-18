# NextGen Graduate IT Coaching — Project Phase Status

_Status as of the current build. The codebase lives at `/home/claude/NextGen-main/`; the latest zip is `NextGen-main.zip`._

---

## Executive summary

**What works end-to-end today:**
A visitor lands on the public site, browses courses, can either register for a webinar (becomes a Lead) or sign up directly for an account. A counsellor sees the Lead and works it through `/admin/leads`, converts to student. The student pays via Razorpay, gets enrolled, and lands on `/student`. They submit assignments and projects. An instructor signs in to `/instructor`, sees their grading queue, grades the submission — the student gets notified. An HR reviewer signs in to `/hr`, reviews resumes, posts jobs, and tracks placement referrals through the funnel. An admin can manage the course catalogue and batch list through `/admin/courses` and `/admin/batches`.

**What's not yet done:**
No emails are sent — the entire system is silent. No scheduled jobs. No file uploads (every submission is URL-based). No tests, no monitoring, no deployment infrastructure. Course curriculum (assignments, projects, tools, journey steps) must still be created via SQL or the seeder rather than the admin UI.

**Status at a glance:**

| Phase | Status | Summary |
|-------|--------|---------|
| 0. Foundation | ✅ Complete | Solution structure, Docker, Tailwind |
| 1. Data Model | ✅ Complete | 30+ entities, EF Core, soft-delete, audit |
| 2. Public Site | ✅ Complete | Home, courses, webinar, contact, about |
| 3. Auth | ✅ Complete | Identity, JWT, refresh tokens |
| 4. Admin Shell | ✅ Complete | Sidebar, layout, top bar |
| 5. Admin Operations | 🟡 Mostly done | Leads + webinars done; reminders/Hangfire pending |
| 6. Admin Catalogue CRUD | 🟡 Part 1 done | Courses + batches done; curriculum + others pending |
| 7. Payments | ✅ Complete | Razorpay integration, invoices, webhooks |
| 8. Student Portal | 🟡 Mostly done | Dashboard, submissions, profile done; file upload pending |
| 9. Instructor & HR | 🟡 Mostly done | Grading + resume review done; attendance + mock booking pending |
| 9.5. Public sign-up | 🟡 Functional, unhardened | Works; needs rate-limiting + verification |
| 10. Hardening | 🟡 P0 done | Persistent auth + silent token refresh done; migrations, concurrency, tests pending |
| 11. Deployment | ❌ Not started | Dockerfiles, IaC, secrets management |
| 12. Post-launch | ❌ Not started | WhatsApp, referrals, AI features |

---

## Completed phases

### Phase 0 — Foundation
**Done.**

- Solution with four projects: `NextGenCoaching.Api`, `NextGenCoaching.Core`, `NextGenCoaching.Shared`, `NextGenCoaching.Web`
- Dependency rule enforced: `Web → Shared` only, `Web` never references `Core`
- `docker-compose.yml` provisioning Postgres 16 + Seq
- Tailwind toolchain compiled (~25 KB `app.output.css`)
- `Directory.Build.props` with documented `NoWarn` exclusions for known-unreachable advisories (NU1902, NU1903)

### Phase 1 — Data Model
**Done.**

- 30+ entities under `Core/Entities/`, all inheriting `BaseEntity` (Guid Id, audit timestamps, soft-delete)
- `AppDbContext` with snake_case naming convention, `IsDeleted` global query filter, `AuditableEntityInterceptor`
- Identity tables with `Guid` primary keys via `ApplicationUser : IdentityUser<Guid>` and `ApplicationRole : IdentityRole<Guid>`
- All enums stored as strings via `.HasConversion<string>()` — schema-safe for additions
- Entity coverage: Course, Batch, Session, Enrollment, Assignment + Submission, Project + Submission, Lead + LeadActivity, Webinar + Registration, Resume, JobOpening, JobReferral, Notification, PaymentTransaction, StudentProfile, InstructorProfile, CounsellorProfile, Testimonial, FAQ, BlogPost, ContactEnquiry

### Phase 2 — Public Site
**Done.**

- Pages: Home, Courses list, Course detail, Free Webinar (with registration), Contact, About
- `MainLayout` + `TopBar` + `MainNav` + `SiteFooter` shared components
- Public API endpoints: `/public/courses`, `/public/testimonials`, `/public/faqs`, `/public/webinars`, `/public/enquiries`
- All public POST endpoints create `Lead` rows by convention, so counsellors see every signal
- 6 seeded courses: Data Science (flagship), SQL, Python, Power BI, Snowflake, Azure Data Factory

### Phase 3 — Authentication
**Done.**

- ASP.NET Core Identity with `RequireConfirmedEmail = false` (flag preserved for future verification flow)
- Password policy: 8+ chars, must include a digit, no uppercase/special required
- Lockout: 5 attempts, 15 min freeze
- `ITokenService` issues HS256 JWTs — 30-min access tokens, 14-day refresh tokens
- Endpoints: `/auth/login`, `/auth/register`, `/auth/refresh`, `/auth/logout`, `/auth/reset-password`
- Role policies: `RequireAdmin`, `RequireCounsellor`, `RequireInstructor`, `RequireHrReviewer`, `RequireStudent`
- Seeded users: admin, student (Aarav Sharma), instructor (Priya Iyer), HR (Meera Krishnan)

### Phase 4 — Admin Shell
**Done.**

- `AdminLayout` with navy sidebar and white content area
- `AdminTopBar` with user avatar, role display, sign-out dropdown
- `AdminSidebar` with navigation items for Dashboard, Leads, Webinars, Courses, Batches, Students, Reports, Payments
- Active-state highlighting based on current URL

### Phase 5 — Admin Operations
**Mostly done.** Operational core works; reminders + template editors deferred.

**Delivered:**
- `/admin` dashboard with stat cards (leads by status, conversion rate, revenue this month)
- Recent leads list and upcoming mock sessions widget
- `/admin/leads` filterable table (status, source, date range)
- Lead detail drawer: activity timeline, log-note form, "Convert to student" action that creates `ApplicationUser` + `StudentProfile` + sends set-password URL
- `/admin/webinars` CRUD with create/edit modal and registrations side drawer with attendance toggles
- API endpoints: `AdminLeadsEndpoints`, `AdminWebinarsEndpoints`, `AdminDashboardEndpoints`

**Open items (would close out Phase 5):**
- Counsellor assignment dropdown on lead drawer — needs a `/admin/users?role=Counsellor` lookup endpoint
- Confirm-delete dialog on webinar deletion
- Hangfire scheduled reminder jobs (24h + 1h webinar reminders, lead follow-up nudges) — this depends on the email pipeline being built first
- Email/SMS template editor UI for the admin to maintain copy
- All four items together: ~1 response, after email pipeline lands

### Phase 6 — Admin Catalogue CRUD
**Part 1 done.** Part 2+ pending.

**Part 1 (delivered):**
- `/admin/courses`: list, create/edit modal, slug uniqueness validation, publish/unpublish toggle, flagship flag
- `/admin/batches`: list with course and status filters, create/edit modal with course + instructor dropdowns, inline status changes, capacity tracking
- Endpoints: `AdminCoursesEndpoints` (6), `AdminBatchesEndpoints` (5), `AdminInstructorsEndpoints` (1 — lookup-only)
- Pattern established: list DTO for tables, request DTO returned by GET-by-id so edit modals bind directly

**Part 2 (pending):**
- **Instructor management** — currently you can't create new instructors via UI. Needs a flow similar to lead-conversion: pick from existing users or create new `ApplicationUser`, attach role, create `InstructorProfile`. ~1 response.
- **Course curriculum CRUD** — children of Course need their own mini-CRUDs:
  - `CourseTool` (e.g. SQL, Python, Tableau attached to a course)
  - `CourseJourneyStep` (the marketing-page roadmap)
  - `CourseFeature` (bullet-list highlights)
  - `Assignment` (per course, with description and resources)
  - `Project` (capstone with brief and rubric)
  - Each is a separate page or tabbed UI within a course detail screen. ~2 responses total to clear all five.
- **Other content** — Testimonials, FAQs, Blog editor. Lower priority; the seeded content is fine for launch. ~1 response.

### Phase 7 — Payments
**Done.**

- `Core/Payments/` package with `RazorpayOptions`, `RazorpayClient` (direct HttpClient, no SDK), `PaymentService`, `InvoiceGenerator` (QuestPDF-based)
- `PaymentEndpoints`: public checkout init, signed-payload verification, webhook receiver at `/api/v1/webhooks/razorpay`
- `AdminPaymentEndpoints`: list, refund, invoice download, lead conversion at `/admin/leads/{id}/convert`
- `Student/Payments` extended with `/student/payments` list and `/student/payments/{id}/invoice` PDF
- Web: `Checkout.razor` (Razorpay JS modal via interop), `SetPassword.razor` (consumed by the set-password URL counsellors send after conversion), `Student/Payments.razor`, `Admin/Payments.razor`
- `wwwroot/js/razorpay-interop.js` lazy-loads `checkout.js`
- Configuration in `appsettings.json` (Razorpay placeholder section); keys set via user-secrets in dev

### Phase 8 — Student Portal
**Mostly done.** File uploads + past-sessions library deferred.

**Delivered:**
- `/student` dashboard with personalized welcome, stat tiles, upcoming sessions, pending assignments, recent payments
- `/student/courses` list of enrollments
- `/student/sessions` list of upcoming and past sessions
- `/student/assignments` list + `/student/assignments/{id}` detail with submission form (URL-based)
- `/student/projects` list + `/student/projects/{id}` detail with repo/deploy/presentation URL submission
- `/student/profile` edit form covering identity, career goals, education, social links
- `/student/notifications` inbox + 60-second-polling unread badge on the bell icon
- `/student/payments` history with invoice download
- `StudentTopBar`, `StudentSidebar`, `StudentLayout`
- `StudentApiClient` covering all endpoints

**Open items:**
- **File uploads** — every submission/resume is URL-based today. To accept binary PDFs, ZIPs, screenshots needs Azure Blob Storage (or S3 / Cloudflare R2), `FileService` abstraction, signed-URL retrieval, multipart upload UI. ~1–2 responses depending on cleanliness of provider abstraction.
- **Past sessions library with recordings** — schema additions needed (recording URLs, downloadable resources per session). ~1 response.
- **Calendar widget** — the design mockup has one but it's not built. ~half a response.
- **Mock interview detail page** — schema has slot booking but no UI. Part of Phase 9 leftovers.

### Phase 9 — Instructor & HR portals
**Mostly done.** Attendance UI + mock booking + refer-student picker deferred.

**Delivered (Instructor side):**
- `/instructor` dashboard with stat tiles (active batches, students, pending grades), today's sessions card, two grading queues
- `/instructor/batches` list of batches the instructor is primary on
- `/instructor/grading` combined queue (Assignments + Projects tabs) with age indicators
- `/instructor/grading/assignment/{id}` detail page — view brief + submission, score + feedback form
- `/instructor/grading/project/{id}` detail page — view repo/deploy/presentation URLs + brief + rubric, score + feedback + evaluation JSON
- Ownership guards on every grading endpoint via `Submission → Enrollment → Batch.PrimaryInstructorId`
- Each grade creates a `Notification` row for the student
- `InstructorLayout`, `InstructorSidebar`, `InstructorTopBar`, `InstructorApiClient`

**Delivered (HR side):**
- `/hr` dashboard with stat tiles (resumes pending, open jobs, active referrals, placed this year)
- `/hr/resumes` queue + `/hr/resumes/{id}` review page with student profile snapshot, approve/send-back actions
- `/hr/jobs` CRUD with create/edit modal — company, role, skills, CTC range, work mode, application URL
- `/hr/referrals` list with inline status pipeline updates (Referred → Screening → Interviewing → Offered → Joined/Rejected/Declined)
- Each resume review creates a `Notification` row for the student
- `HrLayout`, `HrSidebar`, `HrTopBar`, `HrApiClient`, 10 endpoints in `HrEndpoints`

**Open items:**
- **Batch detail page with attendance marking** — `SessionAttendance` entity exists, no UI. Click into a batch from `/instructor/batches`, see students enrolled, mark each as present/absent for a given session. ~1 response.
- **Mock interview slot booking** — needs new schema (instructor availability windows + student bookings). ~1 response including schema + endpoints + UI.
- **HR "refer student to job" picker** — pick from students with approved resumes, attach them to a job opening as a new referral row. ~half a response.
- **Public job board for students** — `/student/jobs` listing open positions filtered by the student's target role. ~half a response.

### Phase 9.5 — Public sign-up
**Functional, not hardened.** Late addition.

**Delivered:**
- `POST /api/v1/auth/register` creates `ApplicationUser` + `StudentProfile` + a `Lead` row with source `SelfSignUp` so counsellors get the signal
- `/signup` page with full name, email, phone, city, course-of-interest deep-link (`?course=…`), password confirmation
- Auto-signs in on success, routes to `/checkout?course=…` if a course was specified, else `/student`
- Cross-links: Sign up ↔ Sign in, "Sign up" button in main nav
- Validation: email uniqueness (409), Identity password-policy errors surfaced verbatim, client-side disabled-state until all required fields valid

**Open items (real abuse vectors — must address before public launch):**
- **No rate limiting** — endpoint can be hammered. ASP.NET Core has built-in rate limiting; ~half a response to wire up.
- **No CAPTCHA** — bots will eventually find this. Cloudflare Turnstile or hCaptcha integration. ~half a response.
- **No email verification** — `EmailConfirmed = false` is set but nothing consumes it. Needs the email pipeline first. ~half a response after email exists.
- **No T&C consent capture** — DPDP Act compliance will want explicit checkbox. ~quarter response.
- **No UTM/campaign tracking** — `Lead.SourceCampaign` field is empty. ~quarter response.

---

## Pending phases

### Phase 10 — Hardening
**Not started.** Production-readiness work.

**P0 (blocks real users): ✅ DONE**

- **Persistent JWT storage** — ✅ done. Sessions persist in `ProtectedLocalStorage` (encrypted via server Data Protection keys), surviving page refresh *and* browser restart for the refresh token's 14-day window. `AuthStateService` is storage-backed; `AuthSessionRestore` (mounted in `Routes.razor`) rehydrates once per circuit; the four protected layouts gate on `AuthState.Initialized` to avoid flashing the sign-in prompt during restore.
- **Token refresh in `AuthorizationMessageHandler`** — ✅ done. The handler proactively rotates the access token via the new single-flight `TokenRefreshService` (`POST /auth/refresh` over a handler-free named HttpClient) when <90s of life remains, with a 401-retry safety net for GETs. Definitive rejection of the refresh token signs the user out; sign-out also revokes server-side via `/auth/logout`.
- **Bonus fix discovered during this work** — the old wiring registered `AuthorizationMessageHandler` through `IHttpClientFactory` (`AddHttpMessageHandler`). Factory pipelines are built in the factory's own DI scope and cached ~2 minutes across circuits, so the handler saw a *different* `AuthStateService` than the circuit the user signed into — a correctness bug and a potential cross-user token-leak hazard. `ApiClient` is now constructed per circuit over a shared singleton `SocketsHttpHandler` (connection pooling preserved).

**P1 (production polish):**

- **EF migrations baseline** — switch `DbSeeder` from `EnsureCreatedAsync` to `MigrateAsync`. Generate initial migration. Until this lands, every schema change requires `docker compose down -v`. ~half a response.
- **Confirm-delete dialogs** — destructive actions currently fire immediately. Modal confirmation step. ~half a response across all destructive actions.
- **Row-version concurrency** — two admins editing the same record simultaneously: last-write-wins, no warning. Add `[Timestamp]` columns. ~half a response plus migration.
- **Webhook event audit log** — Razorpay webhooks fire and we trust them, no record of what came in. Critical if payment disputes happen. ~half a response.
- **Real-time push (SignalR)** — replace 60-second notification polling with hubs. Useful but not blocking. ~1 response.

**P2 (production maturity):**

- **xUnit unit tests** on Core services
- **Testcontainers integration tests** on each endpoint group
- **Playwright end-to-end test** on the happy-path flow
- **OpenTelemetry traces** to Seq (already in compose, currently unused)
- **Health checks** beyond the default `/health` endpoint
- 2–3 responses to hit reasonable coverage; more if you want it thorough

**My recommendation:** Don't write tests yet. The surface is still moving. Wait until Phase 11 stabilizes deployment shapes around it.

### Phase 11 — Deployment
**Not started.**

- **Dockerfiles** for Api + Web with multi-stage builds and non-root users
- **docker-compose for production** — separate from dev
- **IaC** (Bicep if Azure, Terraform if AWS/GCP)
- **Cloudflare in front** for CDN + WAF + DDoS protection
- **Secrets management** via Azure Key Vault, AWS Secrets Manager, or Doppler
- **Domain + SSL** (Let's Encrypt or platform-managed certs)
- **Real Razorpay account** (currently using test keys)
- **Real email provider** (SendGrid, Postmark, or AWS SES — depends on email pipeline)
- **Backup strategy** — Postgres `pg_dump` on schedule to S3/Azure Blob
- **Runbook** for common operations (deploy a fix, rotate keys, restore from backup, debug a stuck webhook)
- ~1 response per platform you target. Multiple responses for the runbook.

### Phase 12 — Post-launch expansions
**Not started.** Each item is multi-response.

- **WhatsApp Business API** integration for lead nurture and student support
- **Referral programme** — students earn payouts when they refer paying enrolments
- **AI resume reviewer** — call your own model, give first-pass feedback before HR review
- **Mock interview AI coach** — same idea, voice-based
- **More courses** — content work, not platform work
- **Native mobile app** — React Native or .NET MAUI consuming the same API
- **Affiliate / partner programme** — separate portal for content creators promoting NextGen

---

## Cross-cutting items (don't fit neatly in a phase)

### Email pipeline — P0, **highest priority**
**Not started.** Single biggest user-facing gap.

Without this, the platform is silent:
- Password reset URLs get logged to the API console, not emailed — meaning the lead-conversion flow doesn't actually work for real users
- No welcome email after enrollment
- No grading notification reaches the student via email (only in-app)
- No webinar reminders
- No payment receipt by email

Needs:
- SMTP configuration with a real provider (SendGrid, Postmark, or AWS SES recommended)
- MailKit-based `EmailService`
- Razor-based template rendering (subject + HTML + plain-text body)
- Templates for: welcome, password reset, payment success, lead conversion, resume reviewed, assignment graded, webinar reminder
- Outbox pattern (write to DB → background sender) for reliability
- Bounce + complaint handling via webhooks from the provider

~1–2 responses depending on how much template work you want done up front.

### Hangfire scheduled jobs
**Not started.** Depends on email pipeline.

- 24h + 1h webinar reminders
- "Your batch starts tomorrow" emails
- Lead follow-up nudges for leads with no activity in 7 days
- Daily summary email for counsellors with their pipeline
- Cleanup jobs (expired refresh tokens, orphaned uploads, etc.)

~1 response after email is in place.

### Instructor & HR self-service profile editing
**Not started.** Small gap.

- Students have `/student/profile`
- Instructors have no way to update their `InstructorProfile` (Headline, Bio, ExpertiseTags, LinkedinUrl) — currently only the admin can edit via direct SQL
- HR has no profile to edit
- ~half a response.

### Search across all admin lists
**Not started.**

- Admin pages have status filters but no free-text search
- Once you have hundreds of leads / students / payments, you'll want it
- ~half a response per entity.

### Reports & exports
**Not started.**

- `/admin/reports` link exists in the sidebar but nothing's behind it
- Revenue by month, conversion funnel, batch-wise placement rate, instructor performance
- CSV/Excel export of any admin list
- ~1 response for a basic set of canned reports.

---

## Known technical debt

1. **`StudentProfile.User` and `InstructorProfile.User` navigation properties couple domain entities to ASP.NET Identity.** Pragmatic now, harder to migrate to an external IdP (Auth0, Cognito) later. Refactor cost is real but not urgent.

2. **`EnsureCreatedAsync` instead of migrations.** Every schema change requires a DB reset in development. Will be a real problem once production data exists. Phase 10 P1 item.

3. **No optimistic concurrency.** Concurrent edits silently overwrite each other. Phase 10 P1 item.

4. **Grading is final.** Once an instructor saves a grade, there's no UI path to revise it. Schema permits an update; UI just doesn't expose it.

5. **DateTime timezone handling assumes browser timezone matches server.** Fine while both are India, breaks the moment you have an instructor in a different timezone.

6. **No background processing for slow operations.** Invoice generation runs synchronously inside the request. Fine at low scale; will become a problem.

7. **No file storage abstraction.** Every "URL" field is just a string in the database. Adding S3/Blob support means designing the abstraction now or refactoring callers later.

8. **The seeder logs and continues on individual failures** — usually good, but masks issues like the HR-password-too-short bug that recently slipped through. Worth keeping but watch the logs.

9. **`Course.Mode` is a free string** (`online`/`hybrid`/`onsite`) while `Batch.Mode` is an enum (`Weekend`/`Daily`/`Hybrid`). Semantically different concepts but the overlapping name causes confusion. Worth renaming one.

10. **No CSP, HSTS, or other security headers.** Phase 11 work.

---

## Recommended sequencing for what's left

If you want to launch publicly:

1. **Email pipeline** (P0)
2. ~~**Phase 10 P0 — persistent JWT + token refresh**~~ ✅ done
3. **Phase 9.5 hardening — rate limit + CAPTCHA** (abuse prevention)
4. **Phase 11 — minimum-viable deployment** (Dockerfiles, one cloud provider, real keys)
5. **Phase 5 cleanup — Hangfire reminders** (depends on email pipeline)
6. **Phase 10 P1 — migrations baseline, audit log, confirm dialogs**

If you want to round out the product before launching:

7. **Phase 6 Part 2 — instructor management + curriculum CRUD** (so non-developers can actually run the catalogue)
8. **Phase 8 leftovers — file uploads** (so students can submit binaries)
9. **Phase 9 leftovers — attendance, mock booking, referral picker**

Tests, monitoring, and Phase 12 expansions come after launch.

**Minimum viable path to "we have paid students using the platform":** items 1, 2, 3, 4 above. Three to four responses of focused work, plus the platform-specific deployment time.
