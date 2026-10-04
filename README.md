# Home Services Platform

A marketplace for organizing construction, renovation and home services in Yerevan, Ejmiatsin, Abovyan, Ashtarak and Masis. Customers find specialists and companies, request and compare offers, and manage their orders. Staff run the platform from the Back Office.

## Repository layout

```text
home-services/
├─ backend/                 ASP.NET Core API (.NET 10, Clean Architecture)
│  ├─ src/                  Domain, Application, Infrastructure, Api
│  └─ tests/                Unit and integration tests
├─ frontend/
│  ├─ portal/               Public site + customer/partner portal (React, Redux, SCSS Modules)
│  ├─ backoffice/           Staff admin app (React, Redux, Ant Design)
│  └─ shared/               i18n keys, constants, utilities used by both apps
├─ docs/                    Architecture notes and decisions
└─ .github/workflows/       CI pipelines
```

## Tech stack

| Part | Stack |
| --- | --- |
| API | ASP.NET Core (.NET 10), EF Core, PostgreSQL, FluentValidation, Serilog, Hangfire, SignalR |
| Portal | React + Vite (JavaScript), Redux Toolkit + RTK Query, React Router, SCSS Modules |
| Back Office | React + Vite (JavaScript), Redux Toolkit + RTK Query, React Router, Ant Design |
| Local infrastructure | Docker Compose: PostgreSQL, Seq (logs), SeaweedFS (S3-compatible file storage) |

## Prerequisites

- [.NET 10 SDK](https://dotnet.microsoft.com/download)
- [Node.js 22 LTS](https://nodejs.org/) (includes npm)
- [Docker Desktop](https://www.docker.com/products/docker-desktop/)
- Git

## Getting started

One-time setup:

```powershell
copy .env.example .env        # then change the passwords in .env
cd frontend\portal; npm install; cd ..\..
cd frontend\backoffice; npm install; cd ..\..
```

Run everything (one terminal each):

```powershell
docker compose up -d                                   # PostgreSQL :5433, Seq :5341  (add --profile storage for file storage on :8333)
dotnet run --project backend/src/HomeServices.Api      # API  http://localhost:5080  (health: /health, docs: /swagger) — applies DB migrations on start
cd frontend/portal;     npm run dev                    # Portal      http://localhost:5173
cd frontend/backoffice; npm run dev                    # Back Office http://localhost:5174
```

Run the tests:

```powershell
dotnet test backend                                    # backend (Docker Desktop must be running)
cd frontend/portal;     npm run coverage               # Portal
cd frontend/backoffice; npm run coverage               # Back Office
```

## Staging

Staging runs on Railway (EU West). Setup, variables and everyday use: [docs/deployment.md](docs/deployment.md).

## Working agreement

See [docs/CONTRIBUTING.md](docs/CONTRIBUTING.md) for branching, commit messages, code review and the definition of done.
