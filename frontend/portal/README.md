# Portal

Public site and customer/partner portal. React + Vite (JavaScript), Redux Toolkit + RTK Query, React Router, react-i18next, SCSS Modules with BEM.

## Scripts

| Command | What it does |
| --- | --- |
| `npm run dev` | Dev server on http://localhost:5173 (proxies `/api` to http://localhost:5080) |
| `npm test` | Vitest in watch mode — use this while doing TDD |
| `npm run coverage` | All tests once with coverage; fails below 90% |
| `npm run lint` | JavaScript lint (oxlint) |
| `npm run lint:styles` | Stylelint, including the BEM class-name rule |
| `npm run build` | Production build into `dist/` |
| `npm run check` | Everything CI runs: lint, styles, coverage, build |

## Folder structure

```text
src/
├─ app/          App shell: providers, store, routes
├─ api/          RTK Query base API; features inject their endpoints
├─ components/   Shared components (LanguageSwitcher; base components arrive in T14)
├─ i18n/         i18next setup, bundled locales (hy, ru, en), language routing
├─ layouts/      Page layouts (AppLayout)
├─ pages/        One folder per page: Page.jsx + Page.module.scss + tests
├─ styles/       Global styles (tokens and mixins arrive in T13)
└─ test/         Test setup, MSW server, render helpers
```

## Conventions

- **TDD:** write the failing test next to the code (`Thing.test.jsx`), then make it pass.
- **API mocks:** use MSW (`server.use(http.get(...))`) — never mock `fetch` directly.
- **Text:** never hard-code UI text; add keys to all three files in `src/i18n/locales/`. A test fails if a key is missing in any language.
- **Styles:** one BEM block per component, e.g. `.offer-card`, `.offer-card__title`, `.offer-card--accepted`.

## Languages

- **URLs carry the language:** every page lives under `/:lng`, e.g. `/hy`, `/ru/partners/42`. Build links with `useLocalizedPath()` so they keep the current language.
- **`/` and URLs without a language** redirect to the visitor's language: their last choice (`localStorage` key `hs_lang`), else the browser's preferred languages, else Armenian.
- **Bundled vs added languages:** Armenian, Russian and English ship with the app and render instantly. Languages activated later in the Back Office come from `GET /api/v1/languages`, and their text is fetched from `GET /api/v1/translations/{lng}/common` (built in T36a; until then they fall back to Armenian).
- **Language switcher:** `components/LanguageSwitcher` opens the same page in another language and remembers the choice.

