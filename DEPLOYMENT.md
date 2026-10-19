# Deploying NextGen

The complete application has three parts: Blazor website, ASP.NET Core API and PostgreSQL. Publishing only the website shows fallback course content but cannot save registrations. Prices remain absent from the public enrollment journey; an enrollment request creates an admin lead, not a paid student enrollment.

## Local and friend testing

Use `./run.ps1 -Mobile` from this folder. The current source runs on port 5102; a published copy on another port may be older. Keep the API and database running. Temporary Cloudflare links require the PC to stay awake and connected and change when restarted. They are testing links, not permanent hosting.

## Containers on a server

1. Copy `.env.example` to `.env`. Set independent, randomly generated database, JWT and proxy secrets and an admin password. Do not reuse development accounts or import the development database containing verification fixtures.
2. Run `docker compose -f docker-compose.production.yml up -d --build`.
3. Put a TLS reverse proxy in front of the website's loopback port 8080. Enable WebSockets. Configure `ReverseProxy__KnownProxies__0` with its address when forwarding headers; do not trust arbitrary forwarded headers. HTTPS redirect is disabled in this setup because TLS terminates at that proxy.
4. The API and database have no exposed host ports. Keep the state volumes: they contain database records, uploaded resumes and authentication encryption keys. Restrict host filesystem access to these volumes.
5. Verify `/health/live` on the website and `/health/ready` on the API, then submit an enrollment request, confirm it under Admin → Leads, create a student account and test sign-in after a restart.

The web container builds Tailwind during its build. Both containers run as a non-root user. Production API startup refuses default secrets, short admin passwords and enabled demo accounts. Keys must remain available across restarts and across replicas; a single-instance deployment is the supported baseline.

## Render preview blueprint

`render.yaml` defines separate web and API services. Supply a persistent PostgreSQL connection string (Npgsql format), admin credentials and the **same** `Api__ProxyKey` on both services. Set the website's `Api__BaseUrl` to the deployed API's HTTPS origin. The database is deliberately not provisioned as an expiring free instance.

Render free services may sleep and have ephemeral filesystems. Free Render PostgreSQL expires after 30 days. A durable deployment needs persistent database storage, key storage and resume storage with verified backups. Use persistent disks/volumes or a storage service before accepting real student documents. See [Render free instance limitations](https://render.com/docs/free). These files prepare deployment; they do not create an account, select a paid plan, configure a domain or publish changes automatically.

## Backups and external services

Schedule encrypted PostgreSQL backups and test a restore into a separate database before launch. Back up private resume files and encryption keys securely. Keep these outside Git and public upload paths. Do not delete the production volumes during an update.

SMTP must be configured and verified before password recovery email delivery is advertised. SMS is currently a logging implementation; WhatsApp uses a contact link. Payments are not part of the public enrollment form. No deployment check should charge money or message real students.

Indexing is disabled by default for testing. Set `Site__Indexable=true` and `Site__BaseUrl=https://your-domain` only for a verified public site. Review contact details, marketing outcome claims and privacy/terms with the owner before a real launch.
