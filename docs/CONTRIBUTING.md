# Contributing

## Branches

- `main` is always deployable. Nobody pushes to it directly.
- Create a short-lived branch per task, named after its ID in the task list:
  - `feature/T07-portal-auth-api`
  - `fix/T17-refresh-token-loop`
- Open a pull request into `main`. At least one other developer reviews it.

## Commit messages

Use [Conventional Commits](https://www.conventionalcommits.org/):

```text
feat(api): add SMS code verification endpoint
fix(portal): keep bottom tab bar above the keyboard on iOS
chore(ci): cache npm dependencies
```

Common types: `feat`, `fix`, `refactor`, `test`, `docs`, `chore`, `style`.
Scopes: `api`, `portal`, `backoffice`, `shared`, `ci`, `infra`.

## Test-driven development

Every change is written test-first:

1. **Red** — write a test for the next acceptance criterion and watch it fail.
2. **Green** — write the least code that makes it pass.
3. **Refactor** — clean up with all tests green.

Coverage gates (CI fails below these): Domain 100%, Application 100%, Infrastructure + Api 90%, Portal and Back Office 90%. EF migrations, `Program.cs`, `main.jsx` and SCSS are excluded.

- Backend: xUnit, Shouldly, NSubstitute, NetArchTest; integration tests with WebApplicationFactory + Testcontainers PostgreSQL.
- Frontend: Vitest, React Testing Library, user-event, MSW. End-to-end: Playwright.
- Test names describe behaviour: `Confirm_completion_fails_when_order_is_not_in_progress`.
- A bug fix starts with a failing test that reproduces it.

## Styling (Portal): BEM

- One block per component: `OfferCard.jsx` + `OfferCard.module.scss` → `.offer-card`.
- Elements `.offer-card__title`, modifiers `.offer-card--accepted`. Kebab-case, no element of an element.
- Nest only with `&__` and `&--`; no tag or ID selectors, no `!important`.
- A block never styles another block's internals; the parent positions children with a mix class.
- Stylelint's `selector-class-pattern` enforces the naming. The Back Office uses Ant Design only.

## Translations

- Never hard-code user-facing text: use `t('namespace:key')` (react-i18next).
- Add every new key in hy, ru and en. CI checks that all three are complete.
- The API returns error codes (e.g. `phone.invalid`); the frontend translates them.

## Code review checklist

- Does it meet the task's acceptance criteria?
- Are permissions checked in the API (not only hidden in the UI)?
- Are new texts added as translation keys (hy / ru / en), not hard-coded?
- Does the UI work at 375 px and 1280 px wide?
- Were tests written first, and do they assert outcomes (not just run the code)?
- Do class names follow BEM?

## Definition of done

1. Acceptance criteria met and demoed.
2. Tests written first, CI green, coverage gates met.
3. Responsive check done at 375 px and 1280 px.
4. Translation keys added.
5. Swagger updated for any API change.
6. Reviewed and merged into `main`.
