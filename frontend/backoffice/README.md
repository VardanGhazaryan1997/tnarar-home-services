# Back Office

Staff admin app. React + Vite (JavaScript), Redux Toolkit + RTK Query, React Router, react-i18next and **Ant Design only** (no custom SCSS — styling goes through the theme in `src/theme/theme.js`).

## Scripts

| Command | What it does |
| --- | --- |
| `npm run dev` | Dev server on http://localhost:5174 (proxies `/api` to http://localhost:5080) |
| `npm test` | Vitest in watch mode — use this while doing TDD |
| `npm run coverage` | All tests once with coverage; fails below 90% |
| `npm run lint` | JavaScript lint (oxlint) |
| `npm run build` | Production build into `dist/` |
| `npm run check` | Everything CI runs: lint, coverage, build |

## Folder structure

```text
src/
├─ app/          App shell: providers, store, routes, navigation menu config
├─ components/   Shared components (LanguageSwitcher)
├─ features/     One folder per feature: slice, RTK Query endpoints, pages, tests (auth, catalog)
├─ api/          RTK Query base API; admin endpoints use /admin/... paths
├─ i18n/         i18next setup, bundled locales (hy, ru, en), Ant Design locale mapping
├─ layouts/      AdminLayout: side menu on desktop, drawer on phones
├─ pages/        One folder per page, with its tests
├─ theme/        Ant Design theme tokens
└─ test/         Test setup, MSW server, matchMedia mock (setViewportWidth), render helpers
```

## Conventions

- **TDD:** write the failing test next to the code, then make it pass.
- **Responsive tests:** call `setViewportWidth(375)` to test the phone layout; the default is 1280 px.
- **New menu section:** add it to `src/app/navigation.jsx`, a route in `src/app/routes.jsx` and labels in all three locale files.
- **Pages load lazily:** add page routes with `lazy: page(() => import(...))` in `routes.jsx`, so each page brings only the Ant Design components it uses and the build has no oversized chunk. Because navigation then waits for the page to load, assert on the URL with `await waitFor(...)` in tests.

## Languages

- The Back Office starts in the staff member's last choice (`localStorage` key `hs_admin_lang`), else the browser's language, else Armenian. URLs don't carry the language.
- The language switcher in the header changes everything at once: our texts, Ant Design's own texts (`getAntdLocale`) and date formats (dayjs, `getDayjsLocale`).
- Armenian, Russian and English are bundled. Languages activated later come from `GET /api/v1/languages`; their text is fetched from `GET /api/v1/translations/{lng}/common` (task T36a) and falls back to Armenian until then. Ant Design and dates fall back to Armenian for those languages.
- Tests start in Armenian (see `src/test/setup.js`).

## Signing in

- `/login` (email + password) → `/login/2fa` (authenticator code). On the first sign-in the second page shows a QR code and key to add the account to an authenticator app.
- The access token is kept in memory only (Redux `auth` slice). The refresh token is an httpOnly cookie, so a page reload restores the session through `POST /admin/auth/refresh` (`SessionGate`).
- Every request sends the access token. When it has expired (401) the session is refreshed once and the request retried; if that fails, the staff member is signed out and the sign-in page says why (`src/api/baseQuery.js`).
- Pages for signed-in staff sit under `RequireAuth`; after signing in, staff return to the page they wanted.
- **Permissions:** menu entries in `src/app/navigation.jsx` have a `permission` and are hidden from staff without it; their routes are wrapped in `RequirePermission`, which shows a no-access page. Super Admins see everything. Use `hasPermission(staff, code)` for buttons and actions.
- **Errors:** show API errors with `errorMessage(t, error)` and put validation errors on form fields with `fieldErrors(t, error)` (`src/api/errors.js`). Messages are translated from the error code under `errors.*` in the locale files.
- **Tests:** by default the browser holds a Super Admin's session. Use `signedOutBrowser()` or `signedInAs(staffMember([...permissions]))` from `src/test/auth.js`.
- **Dev sign-in:** `admin@homeservices.local` / `Dev-Admin-Password-1` (created by the API in development); set up the authenticator on first sign-in.

## Catalog (`/catalog`, needs `catalog.manage`)

- **Categories tab** (`/catalog/categories`): the two-level category tree. Add, edit, add subcategory, hide/show and delete (with confirmation). Names are entered per active language; the default language is required.
- **Cities and districts tab** (`/catalog/cities`): expand a city to see and add its districts. Cities and districts are hidden, never deleted.
- While adding, the slug is suggested from the English name until edited by hand. Editing keeps translations for languages that aren't active right now.
- API errors land on the form field they belong to (`applyApiError` in `features/catalog/formErrors.js`); anything else shows as a message.
- **Tests:** default catalog data is in `src/test/catalog.js`. In big Ant Design tables, find icon buttons with `findByLabelText(...)` rather than `findByRole('button', { name })`, which can become extremely slow.

