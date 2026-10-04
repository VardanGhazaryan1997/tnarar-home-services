# Decisions log

Newest first. One line per decision; link the discussion if there is one.

| Date | Decision |
| --- | --- |
| 2026-10-04 | Staging on Railway (EU West, Amsterdam): API, Postgres, S3 bucket, and the two sites served by Caddy, which forwards /api and /hubs to the API over the private network (same origin, no CORS). One API instance because the background jobs run inside it. See docs/deployment.md. |
| 2026-10-03 | Test-driven development; coverage gates 100% Domain/Application, 90% elsewhere. Shouldly instead of FluentAssertions (commercial since v8). |
| 2026-10-03 | Portal CSS uses BEM naming inside SCSS Modules, enforced by Stylelint. |
| 2026-10-03 | Languages: hy (default), ru, en at launch; new languages added as data via the Back Office (react-i18next on the frontend). |
| 2026-10-03 | Frontends use plain JavaScript (JSX), not TypeScript. |
| 2026-10-03 | Backend targets .NET 10 (LTS). |
| 2026-10-03 | No MediatR or AutoMapper (commercial licences); use own handler interfaces and manual mapping. |
| 2026-10-03 | Portal styling: SCSS Modules. Back Office styling: Ant Design only. |
| 2026-10-03 | Release 1 covers services; materials and projects follow in phase 3. |
