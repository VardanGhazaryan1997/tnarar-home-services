# Staging on Railway

Staging runs on [Railway](https://railway.com) in the **EU West (Amsterdam)** region: one project with five services.

```text
                    HTTPS (Railway edge)
   browser ───────────────┬───────────────────────┐
                          ▼                       ▼
                 portal (Caddy)          backoffice (Caddy)
                 site + /api, /hubs      site + /api
                          │   private network     │
                          └─────────► api ◄───────┘
                                      │  .NET 10, port 8080, one instance
                          ┌───────────┴───────────┐
                          ▼                       ▼
                   Postgres (Railway)      Bucket (Railway, S3)
                                             ▲
   browser ── uploads/downloads with signed links ┘
```

- **portal** and **backoffice** serve their built sites and forward `/api/*` (and `/hubs/*` for live chat) to the
  API over Railway's private network. The browser only talks to its own site's domain, so there is no CORS and the
  refresh cookie keeps working (`SameSite=Strict`).
- **api** has no public domain. It applies database migrations when it starts, and runs the background jobs
  (request follow-up, order auto-complete, commissions, notifications). **Keep it at one instance**: two would run
  every job twice.
- **Files** go straight between the browser and the bucket with signed links; the API sets the bucket's CORS rules
  for the two sites on start.
- **SMS** are not sent on staging: every Portal sign-in code is **`111111`** (`Auth:FixedOtpCode` in
  `appsettings.Staging.json`), and the texts are written to the API log (look for `SMS to`). The API refuses to
  start in Production if a fixed code is set.

Each service's intended build and deploy settings are in its `railway.json` (Dockerfile, health check, one replica,
watch paths). Railway no longer applies these files to services created after 2026-08-28, so enter the same values
in each service's **Settings** (see step 2); the files stay as the record of what each service should have.

## One-time setup

### 1. Project, database and bucket

1. In Railway: **New Project → Empty project**. Name it `tnarar-staging`. In **Settings**, set the region to
   **EU West (Amsterdam)**.
2. **Create → Database → PostgreSQL**. Keep the name `Postgres`. In its **Backups** tab, turn on **daily** backups.
3. **Create → Bucket**, region EU West. Name it `files`.

### 2. The three app services

Connect Railway to the GitHub repository (**Create → GitHub Repo**), three times, and set each service up like this
(**Settings** tab):

| Service name (exactly) | Root directory | Config file path |
| --- | --- | --- |
| `api` | *(empty: the repository root)* | `/backend/railway.json` |
| `portal` | `/frontend/portal` | `/frontend/portal/railway.json` |
| `backoffice` | `/frontend/backoffice` | `/frontend/backoffice/railway.json` |

The name `api` matters: the two sites find it at `api.railway.internal`.

Then, because Railway ignores the config files for new services, set these by hand:

| Service | Setting | Value |
| --- | --- | --- |
| `api` | **Variables** | `RAILWAY_DOCKERFILE_PATH=backend/Dockerfile` (without it Railway tries Railpack and the build fails) |
| `api` | **Settings → Deploy** | Healthcheck path `/health`, timeout `300`; keep **1** replica |
| `portal` / `backoffice` | **Settings → Build** | Watch paths `/frontend/portal/**` / `/frontend/backoffice/**` |
| `portal` / `backoffice` | **Settings → Deploy** | Healthcheck path `/` |
| all four (and Postgres) | **Settings → Deploy → Regions** | **EU West (Amsterdam)** (new services default to US West) |

For **portal** and **backoffice**, after their first deploy (before that Railway shows "Could not load public
networking"): **Settings → Networking → Generate Domain** (or add a custom domain such as
`staging.tnarar.am` and `admin.staging.tnarar.am`, then the DNS record Railway shows). Don't give **api** a public
domain.

### 3. Variables

In each service's **Variables** tab (**Raw Editor**). `${{ ... }}` are Railway references: they fill in the other
service's values. Replace `<...>` with your values.

**api**

```env
ASPNETCORE_ENVIRONMENT=Staging
PORT=8080
ASPNETCORE_FORWARDEDHEADERS_ENABLED=true
ConnectionStrings__Database=Host=${{Postgres.PGHOST}};Port=${{Postgres.PGPORT}};Database=${{Postgres.PGDATABASE}};Username=${{Postgres.PGUSER}};Password=${{Postgres.PGPASSWORD}}
Jwt__SigningKey=<random, at least 32 characters>
Auth__SecretHashKey=<another random value, at least 32 characters>
Bootstrap__SuperAdmin__Email=<your email>
Bootstrap__SuperAdmin__FullName=<your name>
Bootstrap__SuperAdmin__Password=<a strong password, at least 12 characters>
Storage__ServiceUrl=${{files.ENDPOINT}}
Storage__Bucket=${{files.BUCKET}}
Storage__AccessKey=${{files.ACCESS_KEY_ID}}
Storage__SecretKey=${{files.SECRET_ACCESS_KEY}}
Storage__CorsOrigins__0=https://${{portal.RAILWAY_PUBLIC_DOMAIN}}
Storage__CorsOrigins__1=https://${{backoffice.RAILWAY_PUBLIC_DOMAIN}}
Notifications__PortalUrl=https://${{portal.RAILWAY_PUBLIC_DOMAIN}}
```

Make the two random keys in PowerShell (run it twice; works in Windows PowerShell 5 and PowerShell 7):

```powershell
$b = New-Object byte[] 48; [Security.Cryptography.RandomNumberGenerator]::Create().GetBytes($b); [Convert]::ToBase64String($b)
```

The three `RAILWAY_PUBLIC_DOMAIN` lines only get a value once the sites have domains; add them (or redeploy the
API) after generating the domains.

Never reuse the development keys from `appsettings.Development.json`. Changing `Jwt__SigningKey` later signs
everyone out; changing `Auth__SecretHashKey` invalidates all sessions and pending codes.

The Super Admin is created only when there are no staff yet; after the first start you can delete the three
`Bootstrap__SuperAdmin__*` variables.

**portal** and **backoffice**

```env
API_UPSTREAM=${{api.RAILWAY_PRIVATE_DOMAIN}}:8080
```

Click **Deploy** (Railway shows the staged changes). The first API start creates the database tables.

### 4. Check it works

1. **api** logs end with `Application started` and no `File storage could not be prepared` warning.
2. `https://<portal domain>/api/v1/languages` returns a list of languages.
3. The Back Office signs in with the Super Admin email and password, then asks you to set up two-factor
   authentication.
4. In the Portal, sign in with any phone number and the code `111111`.
5. Upload a photo (partner profile or a new request). If it fails with a CORS error in the browser console, check
   the `Storage__CorsOrigins__*` values and the api log.

## Everyday use

- **Deploys:** every push to `main` redeploys the apps whose files changed. A failed health check keeps the previous
  version running.
- **Rollback:** a service's **Deployments** tab → an earlier deployment → **Redeploy**. Database migrations are not
  rolled back; a migration that must be undone needs a new migration.
- **Logs:** each service's **Logs** tab (Seq is for local development only).
- **Database:** **Postgres → Data** to browse; **Backups** to restore. Connect from your machine with the
  `DATABASE_PUBLIC_URL` shown under **Postgres → Variables** (it goes over the internet; prefer read-only work).
- **Cost:** usage is billed per second. Staging should stay around the Hobby plan's included usage plus a few
  dollars; check **Usage** in the workspace settings.

## Local check of the images (optional)

With Docker Desktop, from the repository root:

```powershell
docker build -f backend/Dockerfile -t hs-api .
docker build -t hs-portal frontend/portal
docker build -t hs-backoffice frontend/backoffice
```
