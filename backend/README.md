# Backend

ASP.NET Core API on .NET 10, Clean Architecture. Dependencies point inward only, and an architecture test enforces it:

```text
Domain  ←  Application  ←  Infrastructure
                       ←  Api (references Application + Infrastructure)
```

| Project | Holds |
| --- | --- |
| `src/HomeServices.Domain` | Entities, value objects, status rules. No framework references. |
| `src/HomeServices.Application` | Use cases (commands/queries), validators, interfaces for external services |
| `src/HomeServices.Infrastructure` | EF Core + PostgreSQL, Identity, SMS, storage, payments, jobs |
| `src/HomeServices.Api` | HTTP endpoints, auth, Swagger, health checks |
| `tests/HomeServices.Domain.Tests` | Unit tests for entities and business rules |
| `tests/HomeServices.Architecture.Tests` | Layer dependency rules (NetArchTest) |
| `tests/HomeServices.Application.Tests` | Unit tests for use cases (xUnit, Shouldly, NSubstitute) |
| `tests/HomeServices.Infrastructure.IntegrationTests` | EF Core against a real PostgreSQL (Testcontainers) |
| `tests/HomeServices.Api.IntegrationTests` | HTTP-level tests with WebApplicationFactory + PostgreSQL |

## Prerequisites

- .NET 10 SDK
- **Docker Desktop running**: integration tests start their own PostgreSQL container, and the API needs `docker compose up -d` (from the repo root).

## Commands (run from `backend/`)

```powershell
dotnet build                     # build everything (warnings and style violations fail the build)
dotnet test                      # run all tests
dotnet test --collect:"XPlat Code Coverage"   # tests + coverage report
dotnet run --project src/HomeServices.Api     # API on http://localhost:5080
```

Check it's running: http://localhost:5080/health should return `Healthy`.

## Conventions

- **TDD:** write the failing test first, then the code.
- **Package versions** are set once in `Directory.Packages.props`; projects reference packages without a version.
- **Code style** comes from the repo's `.editorconfig` and is enforced at build time (`TreatWarningsAsErrors`).
- Test names describe behaviour: `Health_returns_200_Healthy`.

## Use cases (commands and queries)

No MediatR: each use case is a command or query plus one handler, and endpoints inject the handler directly.

```csharp
public sealed record CreateRequest(Guid CategoryId, string Description) : ICommand<Guid>;

public sealed class CreateRequestHandler(IAppDbContext db) : ICommandHandler<CreateRequest, Guid>
{
    public async Task<Guid> HandleAsync(CreateRequest command, CancellationToken ct) { /* ... */ }
}

public sealed class CreateRequestValidator : AbstractValidator<CreateRequest>
{
    public CreateRequestValidator() =>
        RuleFor(x => x.Description).NotEmpty().WithErrorCode("description.required");
}
```

`AddApplication()` finds every handler and validator automatically and wraps each handler so its validators run first.

## Errors

Every error response is RFC 7807 ProblemDetails (`application/problem+json`) with a stable `code` that the frontend translates, and a `traceId` for support:

| Thrown | Status | `code` |
| --- | --- | --- |
| FluentValidation `ValidationException` | 400 | `validation_failed`, plus `errors: { field: [codes] }` |
| `ForbiddenException` | 403 | `forbidden` or a specific code |
| `NotFoundException` | 404 | `not_found` or a specific code |
| `ConflictException` | 409 | `conflict` or a specific code |
| `DomainException` (business rule) | 422 | the rule's code, e.g. `order.not_in_progress` |
| anything else | 500 | `internal_error` (details are logged, never returned) |

## Logs and API docs

- Logs go to the console, and to Seq at http://localhost:5341 in development (`docker compose up -d`).
- Swagger UI: http://localhost:5080/swagger (development only). OpenAPI JSON: `/openapi/v1.json`.

## Database

- PostgreSQL via EF Core (Npgsql). Tables and columns are snake_case automatically (`PartnerProfileId` → `partner_profile_id`).
- Entities inherit from `Entity` (UUID v7 id), `AuditableEntity` (created/updated at and by, filled in on save) or `SoftDeletableEntity` (delete hides the row; normal queries skip it).
- In development the API applies pending migrations on startup.
- The dev connection string is in `src/HomeServices.Api/appsettings.Development.json` and matches `.env.example`. If you changed the password in `.env`, change it there too.

Migrations (run from `backend/`):

```powershell
dotnet tool restore                                   # once: installs dotnet-ef from .config/dotnet-tools.json
dotnet ef migrations add <Name> --project src/HomeServices.Infrastructure --startup-project src/HomeServices.Api --output-dir Persistence/Migrations
```

A test fails if the model has changes without a migration, so add one whenever you add or change an entity.

## Languages

- The `languages` table holds the languages the platform can show. Armenian (default), Russian and English are seeded by the first migration.
- `GET /api/v1/languages` lists active languages for the language switcher.
- Every request gets a language from its `Accept-Language` header, picked among the active languages and falling back to the default. Use cases read it through `ICurrentLanguage`, and the response carries it in `Content-Language`.
- Translated content uses `LocalizedText` (stored as `jsonb`, e.g. `{"hy": "…", "ru": "…"}`): `title.Get(currentLanguage.Code, fallbackLanguage)`.
- Adding a language is data, not code: insert and activate it, and it appears in the switcher and in negotiation within 5 minutes (catalog cache).

### Interface texts and languages

Interface texts live in the `ui_translations` table: namespace (`portal`, `backoffice`), dotted key (`catalog.tabs.cities`), language, text. The default language's keys are the full set; other languages fall back to it key by key.

- **Apps:** `GET /api/v1/i18n/{lng}/{namespace}` (no sign-in) returns nested i18next JSON with fallback texts filled in, an `ETag` (send `If-None-Match` → 304) and `Cache-Control: max-age=300`. Unknown or inactive languages → 404 `language.not_found`. The Portal and Back Office load it for languages they don't bundle (hy/ru/en ship in the apps as an offline fallback).
- **First load:** import the bundled files once, e.g. `POST /api/v1/admin/translations/portal/import/hy` with the body of `frontend/portal/src/i18n/locales/hy/common.json` (then `ru`, `en`, and the same for `backoffice`).

| Endpoint (`translations.manage`) | What it does |
| --- | --- |
| `GET /api/v1/admin/translations` | Namespaces with key counts and each language's translated / missing counts |
| `GET /api/v1/admin/translations/missing` | Missing keys per namespace for every active language |
| `GET /api/v1/admin/translations/{ns}?search=&missingIn=&page=&pageSize=` | Keys with their text in every language |
| `PUT /api/v1/admin/translations/{ns}/keys/{key}` `{ values: { "hy": "…", "ru": "…" } }` | Creates a key (needs the default-language text) or changes texts; `""` removes a language's text |
| `DELETE /api/v1/admin/translations/{ns}/keys/{key}` | Removes the key in every language |
| `GET /api/v1/admin/translations/{ns}/export/{lng}?withFallback=` | Downloads `{ns}.{lng}.json` |
| `POST /api/v1/admin/translations/{ns}/import/{lng}?replace=` | Loads an i18next JSON file (nested or dotted keys). The default language creates keys; others only fill existing ones (unknown keys are reported as `skipped`). `replace=true` removes what the file doesn't have |
| `GET /api/v1/admin/languages` | All languages |
| `POST /api/v1/admin/languages` `{ code, name, nativeName, sortOrder }` | Adds a language, inactive (201) |
| `PUT /api/v1/admin/languages/{code}` | Names and switcher order |
| `POST /api/v1/admin/languages/{code}/activate`, `…/deactivate` | Shows / hides it; the default language can't be hidden |

- A key can't be both a text and a group (`home` and `home.title`): 409 `translation.key_conflict`. Changes reach other API instances within 5 minutes (cache).
- Errors: `namespace.invalid`, `key.invalid`, `values.required`, `values.too_long`, `values.language_invalid`, `language.invalid`, `content.invalid`, `code.invalid`, `name.required`, `native_name.required`, `sort_order.invalid` (400); `language.not_found`, `translation.not_found`, `translation.namespace_not_found` (404); `language.code_taken`, `translation.key_conflict` (409); `translation.default_required`, `language.default_cannot_be_deactivated` (422).

### Pages and FAQs

Information pages (About, Terms, Privacy…) and FAQs are written per language; the default language is required for titles, questions and answers. Bodies and answers are Markdown (the Portal renders them without raw HTML).

| Endpoint | Who | What it does |
| --- | --- | --- |
| `GET /api/v1/pages` | anyone | Published pages (slug, title, `showInFooter`), by position |
| `GET /api/v1/pages/{slug}` | anyone | A published page in the request language; otherwise 404 `page.not_found` |
| `GET /api/v1/faqs?audience=` | anyone | Published questions by audience (`General`, `Customers`, `Partners`) and position |
| `GET` / `POST /api/v1/admin/pages`, `GET` / `PUT` / `DELETE …/{id}` | `content.manage` | Pages with every translation. New pages are drafts; deleting keeps history and frees the slug |
| `POST /api/v1/admin/pages/{id}/publish`, `…/unpublish` | `content.manage` | A page needs a body before it can be published |
| `GET` / `POST /api/v1/admin/faqs`, `PUT` / `DELETE …/{id}`, `…/publish`, `…/unpublish` | `content.manage` | Questions; new ones are unpublished |

Errors: `slug.invalid`, `title.*`, `body.*`, `question.*`, `answer.*` (`required`, `language_invalid`, `too_long`, `default_language_required`), `audience.invalid`, `sort_order.invalid` (400); `page.not_found`, `faq.not_found` (404); `page.slug_taken` (409); `page.body_required` (422).

## Reference data

- `GET /api/v1/categories`: active service categories as a tree.
- `GET /api/v1/cities`: active cities with their districts. Names are returned in the request language.
- Seeded by migrations (`Persistence/Configurations/CatalogSeed.cs`): 7 launch categories (construction, renovation, plumbing, heating, electrical, exterior cladding, cleaning), 5 cities (Yerevan, Ejmiatsin, Abovyan, Ashtarak, Masis) and Yerevan's 12 districts.

### Managing the catalog (Back Office, needs `catalog.manage`)

All under `/api/v1/admin/catalog`. Names are sent and returned per language, e.g. `{ "hy": "Սանտեխնիկա", "ru": "Сантехника", "en": "Plumbing" }`; the default language is required, others are optional.

| Endpoint | What it does |
| --- | --- |
| `GET categories` | All categories as a tree, inactive ones included |
| `POST categories`, `PUT categories/{id}` | Create / edit: `slug`, `name`, `icon`, `parentId`, `sortOrder` |
| `POST categories/{id}/activate`, `…/deactivate` | Show / hide on the Portal (hiding a category hides its subcategories) |
| `DELETE categories/{id}` | Soft delete; only without subcategories. The slug can then be reused |
| `GET cities` | All cities with their districts, inactive ones included |
| `POST cities`, `PUT cities/{id}`, `…/activate`, `…/deactivate` | Cities aren't deleted, because partners and orders refer to them |
| `POST cities/{id}/districts`, `PUT cities/{id}/districts/{districtId}`, `…/activate`, `…/deactivate` | Districts of a city |

Rules: categories have two levels (a subcategory can't have subcategories, and a category with subcategories can't move under another one). Slugs are unique (districts: within their city). Errors: `slug.invalid`, `name.default_language_required`, `name.language_invalid`, `name.too_long`, `icon.too_long`, `sort_order.invalid` (400); `category.not_found`, `city.not_found`, `district.not_found` (404); `category.slug_taken`, `city.slug_taken`, `district.slug_taken` (409); `category.parent_not_found`, `category.too_deep`, `category.parent_invalid`, `category.has_subcategories` (422). Every change is in the audit log.

## Portal sign-in

Phone number + SMS code. No passwords.

| Endpoint | What it does |
| --- | --- |
| `POST /api/v1/auth/otp/send` `{ phone }` | Texts a 6-digit code (valid 5 min; resend after 60 s; max 5 per hour) |
| `POST /api/v1/auth/otp/verify` `{ phone, code }` | Checks the code (max 5 tries), creates the account on first sign-in, returns `{ accessToken, accessTokenExpiresAt, user, isNewUser }` and sets the `hs_refresh` cookie |
| `POST /api/v1/auth/refresh` | Uses the cookie to issue a new access token and a new cookie (rotation; reusing an old token ends every session of that user) |
| `POST /api/v1/auth/logout` | Ends the session and clears the cookie |
| `GET` / `PUT /api/v1/me` | The signed-in user's profile (`Authorization: Bearer <accessToken>`) |

- Access tokens are JWTs that last 15 minutes. Keep them in memory in the frontend, not in localStorage.
- The refresh token lives only in an httpOnly, SameSite=Strict cookie scoped to `/api/v1/auth`, so page scripts can't read it.
- SMS codes and refresh tokens are stored as HMAC hashes only.
- **Development:** SMS messages aren't sent. They're written to the log as `SMS to +374…: …` (console and Seq). A real SMS gateway comes in T70.
- **Configuration:** `Jwt:SigningKey` and `Auth:SecretHashKey` (32+ characters each) are required. Development values are in `appsettings.Development.json`; production values must come from environment variables or a secret store. The API won't start without them.

## Back Office sign-in (staff)

Email + password, then an authenticator-app code. 2FA is mandatory for every staff account.

| Endpoint | What it does |
| --- | --- |
| `POST /api/v1/admin/auth/login` `{ email, password }` | Returns `{ status, challengeToken, setupSecret?, setupUri? }`. `status` is `two_factor_setup_required` on first sign-in (show `setupUri` as a QR code) or `two_factor_required` |
| `POST /api/v1/admin/auth/2fa/setup` `{ challengeToken, code }` | First sign-in: confirms the authenticator app, enables 2FA, starts a session |
| `POST /api/v1/admin/auth/2fa/verify` `{ challengeToken, code }` | Later sign-ins: checks the code, starts a session |
| `POST /api/v1/admin/auth/refresh`, `/logout` | Same rotation and cookie rules as the Portal (cookie `hs_staff_refresh`, path `/api/v1/admin/auth`, 12-hour sessions) |
| `GET /api/v1/admin/me` | The signed-in staff member |

- **Separate audiences:** staff tokens use their own audience, so they're rejected by Portal endpoints, and Portal tokens are rejected by `/api/v1/admin/*`. Admin controllers use `[Authorize(Policy = AuthPolicies.Staff)]`.
- **Protections:**
  - 5 wrong passwords lock the account for 15 minutes.
  - Unknown emails and wrong passwords get the same answer.
  - An authenticator code can't be used twice.
  - The challenge token expires after 5 minutes.
- **First Super Admin:** created at startup from `Bootstrap:SuperAdmin` (`Email`, `FullName`, `Password`, minimum 12 characters) when no staff account exists. Development uses `admin@homeservices.local` / `Dev-Admin-Password-1`. For the first production deployment, set these as environment variables, then remove them.
- Passwords use ASP.NET Core Identity's PBKDF2 hasher. TOTP follows RFC 6238 and works with Google/Microsoft Authenticator, 1Password, etc.

## Roles and permissions (Back Office)

- **Permissions** are a fixed list in code (`Domain/Staff/Permissions.cs`), e.g. `partners.approve`, `commissions.manage`, `audit.view`. `roles.manage` and `settings.payments` are reserved for Super Admins and can't be put in a role.
- **Roles** are data, so Super Admins can change them. The `AddRoles` migration inserts the built-in roles: Administrator, Operator, Finance, Support, Content Manager. A staff member can have several roles.
  - Seed data with list values (like a role's permissions) goes in the migration, not `HasData`: EF compares lists by reference and would always report pending model changes.
- **Super Admins** have every permission. `SuperAdminGuard` blocks suspending or demoting the last active Super Admin.
- **Protecting an endpoint:** `[HasPermission(Permissions.PartnersApprove)]`. Effective permissions travel in the staff access token (`perm` claims), so role changes take effect at the next token refresh, within 15 minutes.
- `GET /api/v1/admin/permissions` returns the catalog. `GET /api/v1/admin/roles` returns roles with their permissions (needs `staff.view`). `GET /api/v1/admin/me` includes the signed-in staff member's permissions.

## Staff management (Back Office)

New staff are **invited**: a manager creates the account, copies the one-time link and sends it. The person chooses a password at `POST /api/v1/admin/auth/accept-invite` `{ token, password }` (no sign-in; links last 7 days, `StaffAuth:InviteLifetimeDays`), then signs in and sets up 2FA as usual. Invited accounts can't sign in. Email delivery of invitations comes later.

| Endpoint | Permission | What it does |
| --- | --- | --- |
| `GET /api/v1/admin/staff?search=&status=&roleId=&page=&pageSize=` | `staff.view` | Staff by name; `status` is `Active`, `Suspended` or `Invited` |
| `GET /api/v1/admin/staff/{id}` | `staff.view` | A member with their effective permissions |
| `POST /api/v1/admin/staff` `{ email, fullName, roleIds, isSuperAdmin }` | `staff.manage` | Invites; returns `inviteToken` once (put it in the link, e.g. `/accept-invite?token=…`) |
| `POST …/{id}/invite` | `staff.manage` | A new link for someone who hasn't joined; the old one stops working |
| `PUT …/{id}` `{ fullName, roleIds }` | `staff.manage` | Name and roles (roles are replaced) |
| `POST …/{id}/suspend`, `…/activate` | `staff.manage` | Suspending ends their sessions (within 15 minutes) and cancels an open invitation |
| `POST …/{id}/reset-two-factor` | `staff.manage` | Lost phone: forgets the authenticator, ends sessions; a new one is set up at the next sign-in |
| `POST` / `DELETE …/{id}/super-admin` | Super Admin | Grants / revokes Super Admin |
| `POST /api/v1/admin/roles` `{ name, description, permissions }` | Super Admin | Creates a role (201) |
| `PUT /api/v1/admin/roles/{id}` | Super Admin | Changes a role; built-in roles keep their name |
| `DELETE /api/v1/admin/roles/{id}` | Super Admin | Deletes an unused custom role (204) |

- Super Admin accounts can only be changed by Super Admins; nobody can suspend themselves; the last active Super Admin can't be suspended or demoted.
- Role and permission changes reach staff at their next token refresh (within 15 minutes). Role names are unique ignoring case.
- Errors: `email.invalid`, `name.required`, `name.too_long`, `roles.required`, `roles.invalid`, `description.too_long`, `permissions.required`, `permissions.unknown`, `permissions.super_admin_only`, `token.required`, `password.too_short`, `password.too_long` (400); `staff.super_admin_only` (403); `staff.not_found`, `role.not_found`, `staff.invite_invalid` (404); `staff.email_taken`, `role.name_taken`, `role.in_use` (409); `staff.invite_expired`, `staff.not_invited`, `staff.cannot_suspend_self`, `staff.last_super_admin`, `role.system_cannot_be_renamed`, `role.system_cannot_be_deleted` (422).

## Portal users (Back Office)

| Endpoint | Permission | What it does |
| --- | --- | --- |
| `GET /api/v1/admin/users?search=&role=&status=&page=&pageSize=` | `users.view` | Users newest first. `search` matches the phone (any format), name or email; `role` is `Customer` or `Partner`; `status` is `Active` or `Blocked`. Each row shows the partner profile's status, if any |
| `GET /api/v1/admin/users/{id}` | `users.view` | A user with their partner profile and block reason |
| `POST /api/v1/admin/users/{id}/block` `{ reason }` | `users.block` | No sign-in, sessions end within 15 minutes, the partner profile disappears from public pages |
| `POST /api/v1/admin/users/{id}/unblock` | `users.block` | Lifts the block and clears the reason |

Errors: `reason.required`, `reason.too_long`, `role.invalid`, `status.invalid`, `search.too_long`, `page.invalid`, `page_size.invalid` (400); `user.not_found` (404). Blocks are in the audit log.

## Audit log

- Every create, change and delete of an entity marked `IAudited` is written to the `audit_log` table in the same transaction: who (staff, user or system), when, the entity, and each changed property's old and new value. `AuditLogInterceptor` does this; nothing needs to be called by hand.
- Audited: languages, categories, cities, districts, users, staff, roles and role assignments, partner profiles with their services, areas and media. Not audited: OTP codes, refresh tokens, and the log itself.
- `[AuditRedacted]` records that a property changed but hides the values (password hashes, authenticator secrets). `[NotAudited]` ignores a property entirely (sign-in timestamps, failed-attempt counters); changing only such properties writes no entry.
- Soft deletes are logged as `Deleted`. Entries are never edited.
- `GET /api/v1/admin/audit-log` (needs `audit.view`) lists entries newest first, with optional `entityType`, `entityId`, `actorId`, `action`, `from`, `to`, `page` and `pageSize` (at most 200).

## Files (photos, videos, documents)

Files live in S3-compatible storage: SeaweedFS in development (`docker compose --profile storage up -d`, S3 API on :8333), any S3 service in production. The browser uploads straight to storage; the API never streams large files.

| Endpoint | What it does |
| --- | --- |
| `POST /api/v1/files/uploads` `{ fileName, contentType, size }` | Records a pending file and returns `{ fileId, uploadUrl, method: "PUT", headers, expiresAt }` (link valid 15 min) |
| *(browser)* `PUT uploadUrl` | Sends the bytes with exactly the returned `headers` (`Content-Type`) |
| `POST /api/v1/files/{id}/complete` | Checks the stored file and, for images, makes a thumbnail. Safe to call again |
| `GET /api/v1/files/{id}` | The file with fresh signed `url` and `thumbnailUrl` (valid 60 min, so don't store them) |

- Works with Portal and staff tokens (policy `AuthPolicies.SignedIn`). Only the owner can complete an upload; the owner and staff can read it.
- Allowed: JPEG, PNG, WebP up to 10 MB; MP4 and MOV up to 200 MB; PDF up to 10 MB. 60 uploads per person per hour.
- Completing checks the real size and the file's first bytes (a renamed program is refused), then for images reads the size and makes a JPEG thumbnail (480 px longest side, EXIF orientation applied) with SkiaSharp. A refused file is deleted from storage.
- Errors: `file_name.required`, `content_type.not_allowed`, `size.invalid`, `size.too_large` (400); `file.not_found` (404); `file.too_many_uploads` (429); `file.not_uploaded`, `file.too_large`, `file.invalid_content` (422).
- Storage keys look like `files/2026/10/{fileId}/original.jpg` and `…/thumbnail.jpg`.
- **Configuration:** `Storage:ServiceUrl`, `Storage:AccessKey`, `Storage:SecretKey`, `Storage:Bucket` (default `home-services`). Set `Storage:PublicServiceUrl` when browsers reach storage at a different address than the API (signed links use it). `Storage:CreateBucket` creates the bucket at startup (on in development). `Files:*` sets link lifetimes and the hourly limit. Development values match `infra/seaweedfs/s3.json`. If storage isn't running, the API still starts and logs a warning; only uploads fail.

## Partner profiles

A Portal user becomes a partner by creating a profile; staff review it before it's public. Statuses: Draft → Under review → Needs changes / Approved / Rejected; Approved ↔ Suspended. Every status change is kept in `partner_status_changes` (who, when, comment).

| Endpoint (Portal token) | What it does |
| --- | --- |
| `GET /api/v1/me/partner-profile` | The profile, or 404 `partner.not_found` |
| `PUT /api/v1/me/partner-profile` `{ type, displayName, about, yearsOfExperience, avatarFileId, categoryIds, areas: [{ cityId, districtId? }] }` | Creates (and gives the user the Partner role) or updates. Services and areas are replaced by the lists sent |
| `POST /api/v1/me/partner-profile/media` `{ kind: "WorkExample" \| "Document", fileId, caption }` | Attaches a file uploaded with `/api/v1/files` |
| `DELETE /api/v1/me/partner-profile/media/{id}` | Removes it |
| `POST /api/v1/me/partner-profile/submit` | Sends it for review |

- `type`: `Specialist`, `Company` or `Supplier` (fixed after the first approval). Enums are sent and returned by name.
- Editing is allowed in Draft, Needs changes and Approved (an approved profile stays public while edited; changes are in the audit log). Not while under review, rejected or suspended: 422 `partner.not_editable`.
- Submitting needs about text of 50+ characters, at least one service, one area and one work example; the response's `missingForSubmit` lists what's missing (`about`, `services`, `areas`, `work_examples`) and `canSubmit` says whether it's ready.
- A whole city (no `districtId`) covers its districts, so districts of that city are dropped.
- Work examples are photos or videos (at most 30); documents are photos or PDFs (at most 10) and are never public. Files must be the user's own and completed. The avatar must be a photo.
- `reviewComment` shows staff's comment while the profile needs changes, is rejected or suspended.
- Errors: `type.invalid`, `display_name.required`, `display_name.length`, `about.too_long`, `years_of_experience.invalid`, `categories.required|too_many|invalid`, `areas.required|too_many|invalid`, `avatar.invalid`, `kind.invalid`, `caption.too_long`, `file.invalid` (400); `partner.not_found` (404); `partner.not_editable`, `partner.incomplete`, `partner.cannot_submit`, `partner.type_locked`, `partner.media_duplicate`, `partner.too_many_media`, `partner.media_not_found` (422).

### Reviewing partners (Back Office)

| Endpoint | Permission | What it does |
| --- | --- | --- |
| `GET /api/v1/admin/partners?status=&type=&search=&page=&pageSize=` | `partners.view` | Profiles. `status=UnderReview` is the review queue, oldest submission first; otherwise newest first. `search` matches the name, or the owner's phone in any format |
| `GET /api/v1/admin/partners/{id}` | `partners.view` | `{ profile, owner, history }`: the profile with documents, the owner's contact details, and status changes newest first with who made them |
| `POST …/{id}/approve` | `partners.approve` | Under review → Approved (public) |
| `POST …/{id}/request-changes` `{ comment }` | `partners.approve` | Under review → Needs changes; the partner sees the comment |
| `POST …/{id}/reject` `{ comment }` | `partners.approve` | Under review → Rejected (final) |
| `POST …/{id}/suspend` `{ comment }` | `partners.approve` | Approved → Suspended (hidden) |
| `POST …/{id}/reinstate` | `partners.approve` | Suspended → Approved |

Errors: `comment.required`, `comment.too_long`, `status.invalid`, `type.invalid`, `page.invalid`, `page_size.invalid`, `search.too_long` (400); `partner.not_found` (404); `partner.not_under_review`, `partner.not_approved`, `partner.not_suspended` (422). Decisions are in the audit log and the profile's history.

### Public partner pages (no sign-in)

| Endpoint | What it does |
| --- | --- |
| `GET /api/v1/partners?category=&city=&district=&type=&search=&page=&pageSize=` | Approved partners whose account isn't blocked, newest approval first (`pageSize` at most 50). Filters are slugs: a category includes its subcategories; partners serving a whole city match each of its districts; `district` needs `city`. An unknown or hidden category/place gives an empty list. `search` matches the name or about text |
| `GET /api/v1/partners/{slug}` | The public profile: about, categories, areas, avatar, work examples, `memberSince`. Not approved → 404 `partner.not_found` |

- Each profile gets a slug when it's created, e.g. `aram-santekhnik-3f9a2c`: the name in Latin letters (Armenian and Russian are transliterated) plus 6 characters of the id. Renaming keeps the slug. The owner's `GET /me/partner-profile` returns it as `slug`.
- Visitors see only the avatar and work examples, never documents or the owner's phone. File links are signed and expire after an hour, so pages fetch them fresh.
- Names are in the request language (`Accept-Language`).
- Errors: `district.needs_city`, `type.invalid`, `search.too_long`, `page.invalid`, `page_size.invalid` (400).

### Unreadable requests

Broken JSON, an unknown enum name or a missing required field return the usual 400 shape: `code: "validation_failed"` with `errors: { field: ["value.invalid" | "value.required"] }` (`InvalidRequestProblems`).

## Renovation estimates

| Endpoint | What it does |
| --- | --- |
| `POST /api/v1/estimates/measure` | Measures and prices rooms without saving (anyone) |
| `GET /api/v1/estimates/templates` | The usual work per room type, with market prices and default counts (anyone) |
| `POST /api/v1/estimates/quick` | One room of a type and floor area with its usual work, priced (anyone; the home page widget) |
| `GET /api/v1/estimates/shared/{token}` | A shared estimate by its link, read-only (anyone); 404 `estimate.not_found` once sharing stops |
| `GET /api/v1/me/estimates`, `GET …/{id}` | The signed-in user's estimates (up to 50) with totals; one estimate with rooms and prices |
| `POST /api/v1/me/estimates`, `PUT …/{id}`, `DELETE …/{id}` | Save a new estimate (201), replace one (title, `cityId`, `oldBuilding`, all rooms), delete one |
| `POST …/{id}/share`, `DELETE …/{id}/share` | Turn the share link on (a random 22-character `shareToken`) or off |
| `GET /api/v1/admin/catalog/room-templates`, `PUT …/room-templates/{roomType}` | Back Office (`catalog.manage`): every room type's usual work; replace one type's list |

- Prices are labour only: quantity × the work item's market range, +15% in an old building, +10% on wall and ceiling work under ceilings above 3 m. Amounts are rounded (10 under 1,000, 100 under 100,000, else 1,000).
- Visitors keep their estimate in the browser; it's saved on the server once they sign in. A saved estimate keeps lines for work hidden since; its prices leave them out (the line is missing from `measurement`).
- A template line is measured from the room, or has a fixed `quantity`, or a `quantityPerSquareMeter` of floor (rounded up, at least 1). Work that can't be measured (pieces, points, hours, m³) needs one of the two: 422 `room_template.quantity_required`.
- Requests from an estimate: `POST /api/v1/requests` with `estimateId` copies the estimate's work into the request (`lines`: room, work, unit, amount; the estimated range only in the customer's view, like the budget) and also sends it to partners offering the services of those lines. Partners can price a work offer line by line: each offer line may carry `requestLineId`, `quantity` and `unitPrice` (its `amount` is worked out); when any line is priced, every included line must be, and the amounts must add up to the price (422 `offer.lines_unpriced`, `offer.lines_sum_mismatch`; `offer.line_invalid` for a line of another request). Up to 200 lines per request and per offer.
- Errors: `title.invalid`, `rooms.required`, `rooms.count_invalid`, `room.name_invalid`, `room.type_invalid`, `room.duplicate_lines`, `line.quantity_invalid` (400); `estimate.size_invalid`, `estimate.opening_invalid`, `estimate.work_item_not_found`, `estimate.city_not_found`, `estimate.too_many` (422); `estimate.not_found` (404).

## Code coverage

CI fails when backend coverage drops below 90% of lines or 85% of branches (migrations excluded). To see the report locally (one-time: `dotnet tool install --global dotnet-reportgenerator-globaltool`):

```powershell
dotnet test --settings coverlet.runsettings --collect "XPlat Code Coverage" --results-directory TestResults
reportgenerator -reports:"TestResults/**/coverage.cobertura.xml" -targetdir:TestResults/coverage -reporttypes:"Html;TextSummary"
type TestResults\coverage\Summary.txt          # open TestResults\coverage\index.html for line-by-line detail
```

Delete `TestResults` before re-running, or old results are counted again.

The coverage tool can't read attributes that take ASP.NET Core shared-framework types, such as `[LoggerMessage(Level = LogLevel.Information)]`, in Domain, Application or Infrastructure; it then silently skips the whole project (CI fails with "Only 3 of 4 projects were measured"). Use `LoggerMessage.Define` there instead (see `FakeSmsSender`). The Api project is not affected.

