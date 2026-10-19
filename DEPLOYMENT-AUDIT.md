# NextGen deployment audit

Updated 20 October 2026. Status: release candidate for friend testing. Source changes are saved; no new public deployment or tunnel was started during this audit.

## What changed

- Preserved the approved artwork, compact logo and price-free enrollment journey. Added mobile navigation to all four workspaces, contained wide tables, improved form sizes and focus access, and replaced the generic missing-page experience.
- Replaced placeholder resource articles with readable guides. The administrator dashboard now uses actual placement records. Corrected stale ratings, sample contact addresses, enrollment wording and visible template notes. Added Marathi and Hindi translations for the new copy.
- Made resume uploads and downloads private. Downloads require the owning student, HR or an administrator; the raw upload directory is no longer public. Added file signature checks, safe generated filenames and blank-path handling.
- Added immediate account-disable and removed-role checks for existing tokens. Restricted attendance to the assigned instructor or administrator, rejected invalid and duplicate roster entries, and added grading and job-range validation.
- Updated MailKit to 4.18.1 and the XML/PKCS crypto packages to 10.0.13. Removed suppression of package vulnerability warnings and prevented SMTP authentication over an unencrypted connection. Demo passwords are no longer written to startup logs.
- Prepared separate website/API Docker images, private PostgreSQL networking and persistent state volumes. Added production configuration guards, readiness endpoints, response security headers, key persistence and preview indexing controls. Corrected the Render configuration and replaced outdated deployment instructions.
- Production startup now excludes sample alumni testimonials as well as demo accounts. Fresh FAQ seed content reflects online learning and avoids unsupported EMI or placement guarantees. Existing database records are preserved.

## Verified results

| Check | Result | Scope |
| --- | --- | --- |
| Debug build | Pass, zero warnings/errors | Final source |
| Release publication | Pass, zero warnings/errors | Final API and website |
| Fresh database API checks | 120 passed | Authentication, authorization, public forms, registrations, capacity and seeded catalogue |
| Connected learning workflows | 61 passed | Isolated database: submissions, grading, attendance ownership, jobs/referrals, resume approval and notifications |
| API readiness checks | 47 passed | Validation, admin updates, private uploads/downloads and disabled-account rejection |
| Production verification | 11 passed | Two migrations, safe secrets, no demo users/testimonials, restart, headers and backup restoration |
| Responsive browser matrix | 288 cases, no detected issues | Six main public pages, eight widths, three languages, two themes |
| Browser interactions | Pass | Phone menu opens/closes, program search, selected-course enrollment without prices, saved mobile enrollment, animation pause/resume, readable guide expansion and corrected terms contacts |
| Backend dependency audit | No reported vulnerable packages | Direct and transitive NuGet dependencies |
| Shipped JavaScript dependency audit | No reported vulnerabilities | `npm audit --omit=dev` |

The responsive matrix covers `/`, `/courses`, `/services`, `/about`, `/contact` and `/webinar` at 320, 360, 390, 430, 540, 768, 1024 and 1440 pixels in English, Marathi and Hindi, in light and dark mode. It checks the actual viewport, horizontal overflow, clipped headings/fields, broken loaded images, mobile field font sizes and visible application errors. Career support retains 12 features, three columns on desktop and one on phones.

The mobile enrollment test was confirmed in Admin → Leads with Data Science & AI Flagship and the submitted message. All test records use clearly labeled verification names or `nextgen.invalid` addresses. External email was disabled during write tests.

Saved regression results from 19 October also include 88 role-workspace browser checks, 57 masterclass checks, 60 enrollment checks and a 256-case public responsive matrix. They are retained as earlier evidence, not counted as fresh runs on 20 October. The latest production run used `-SkipBrowser`; the earlier production run tested browser session restoration after restart. Current screen testing is browser emulation, not a physical Android/iPhone device test.

Reports, screenshots, verification logs and database backups are private under `.local/`; older public-layout screenshots also remain in `../audit/`. A fresh local database backup is in `.local/backups/`.

## Remaining deployment work and limits

1. Choose and configure the hosting provider, PostgreSQL, HTTPS/WebSockets, shared proxy secret and persistent storage for database records, private resume files and encryption keys. The files are prepared; provider infrastructure has not been provisioned.
2. Docker is not installed on this computer, so container images and Compose have not been executed. Native production builds and isolated production startup/restore tests passed.
3. SMTP delivery needs a real provider and an end-to-end delivery test. SMS remains a logging implementation. WhatsApp is a contact link. No real messages or payments were sent. Public enrollment creates an admissions lead and does not take payment or create an active student enrollment automatically.
4. Seven CSS build-tool dependency entries remain flagged (five high and two moderate), involving the Tailwind 3 dependency chain. Compatible fixes were applied. npm's remaining suggested fix is a breaking Tailwind 4 upgrade; that migration was not forced into the approved design. These are development/build dependencies and are absent from the shipped JavaScript dependency audit. Build only trusted project source, and plan a separately verified styling-tool migration before treating this as fully cleared.
5. The owner still needs to confirm published outcome claims, company-logo context, contact details and final business/privacy/refund terms before a commercial public launch. Copy cleanup is not legal approval. Do not deploy the development database containing test users and fixtures as a real customer database.
6. Free hosting may sleep and lose local files. Verify the provider's current limits and storage guarantees. A temporary sharing link works only while this computer and the local services remain online.

These results cover the exercised workflows; they are not a guarantee that every possible input, external integration or production load has been tested.

## Repeatable checks

```powershell
./run.ps1 -Mobile
node scripts/Verify-Backend.mjs
node scripts/Verify-Readiness.cjs api
./scripts/Verify-FreshDatabase.ps1
./scripts/Verify-Production.ps1
```

`Verify-FreshDatabase.ps1` also runs the new workflow suite in its own disposable database. Production verification creates and removes only its uniquely named test databases. Never point mutation tests at a real customer database. For native browser-driven review, the current site is `http://localhost:5102`.

See [DEPLOYMENT.md](DEPLOYMENT.md) for hosting configuration. Package security references: [MailKit maintainer advisory](https://github.com/jstedfast/MailKit/security/advisories/GHSA-9j88-vvj5-vhgr), [Microsoft runtime advisory](https://github.com/dotnet/runtime/security/advisories/GHSA-37gx-xxp4-5rgx). Dependency versions and remaining findings were checked against the official package registries.
