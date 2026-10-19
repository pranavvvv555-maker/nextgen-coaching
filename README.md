# NextGen IT Coaching

.NET 10 Blazor website, ASP.NET Core API and PostgreSQL. Public programs, masterclasses and enrollment requests connect to role-based admin, student, instructor and HR workspaces.

## Run locally

From this folder, run `./run.ps1 -Mobile`. It initializes the project's isolated development database, applies migrations, generates local secrets and starts the API and current website. Keep the existing PostgreSQL installation and saved database files intact.

- Website: http://localhost:5102
- Admin: http://localhost:5102/admin
- Development access: private `.local/DEVELOPMENT-ACCESS.txt`
- Setup and service controls: [BACKEND-SETUP.md](BACKEND-SETUP.md)

The public enrollment form creates an admissions lead without prices or payment UI. An enrollment request does not automatically create an active paid enrollment.

## Deploy

Follow [DEPLOYMENT.md](DEPLOYMENT.md). The website, API and database must all be configured. Docker images build styles and publish each application; runtime state belongs in persistent volumes. `render.yaml` describes two services and requires database and secret configuration. It is not a complete one-click permanent free database deployment.

## Verification

- `node scripts/Verify-Backend.mjs`: API authentication, authorization, validation, leads, registrations and capacity checks.
- `node scripts/Verify-Masterclass.cjs`: masterclass layouts and interactions.
- `./scripts/Verify-FreshDatabase.ps1`: disposable-database migration and seed verification.
- `./scripts/Verify-Production.ps1`: release startup, safe configuration and backup/restore verification in disposable databases.
- Reports and screenshots are kept privately under `.local/`; the deployment audit describes coverage and remaining external dependencies.

See [DEPLOYMENT-AUDIT.md](DEPLOYMENT-AUDIT.md) for verified results and remaining launch work.

Use development verification fixtures only on a testing database. Never publish `.local`, `.env`, local credentials, backups or student documents in source archives.
