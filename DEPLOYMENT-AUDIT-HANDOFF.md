# Continue NextGen work

Last updated: 20 October 2026.

The current source is in this checkout. Start with [DEPLOYMENT-AUDIT.md](DEPLOYMENT-AUDIT.md) and [DEPLOYMENT.md](DEPLOYMENT.md).

- Local website: http://localhost:5102. Same-Wi-Fi phone URL: http://192.168.1.12:5102 (recheck the computer's address if it changes).
- API: http://localhost:5101. Isolated PostgreSQL: 127.0.0.1:55432. Keep `.local/backend.json`, access notes, backups and resume files private.
- Restart with `./run.ps1 -Mobile`; stop only the owned development apps using `./scripts/Stop-Local.ps1`. Do not delete the database directory. No new public tunnel was started for this audit.
- The migration concern is resolved: `Core/Migrations/20260627185511_InitialCreate.cs` creates the schema; `Core/Persistence/Migrations/20261007183642_Initial.cs` is an empty follow-up. The production test confirmed both migration records in a new disposable database. Preserve existing migration history.
- The pending instructor attendance fix now compiles and has regression coverage. The isolated workflow suite passed 61 checks. Final production verification passed 11 checks. Current public responsive review passed 288 cases.
- Existing approved logo/artwork, translations and no-price enrollment behavior are preserved. Enrollment requests are admissions leads. The mobile browser test was confirmed under Admin → Leads with its selected course and message.
- Latest database backup: `.local/backups/nextgen-local-20261020-004741.dump`. Other private test logs and earlier screenshots remain under `.local/` and `../audit/`.
- Remaining work is hosting setup/container execution, real SMTP delivery, optional Tailwind 4 migration to address the remaining build-only advisories, and owner approval of business/legal content before commercial launch. See the audit for exact limits.
- Translation JSON is tracked in `src/NextGenCoaching.Web/Resources/Translations.json`. The existing authoring TSVs and builder remain in `D:/NG website/audit/`; do not rerun the builder without preserving those TSVs. New audit strings are in `translations-readiness.tsv`.

No live payment or external notification was sent during verification. Mutation tests use local QA records or disposable databases and must not be run against a real customer database.
