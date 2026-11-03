---
trigger: glob
globs: "frontend/**"
description: "Rules for the Angular frontend."
---
# Frontend Rules (Angular)
*   **State:** Keep one SignalStore file per scope (`<name>.store.ts`). Components read store signals and call store methods. They never write state or call `HttpClient`. Never use `@ngrx/store` or a `BehaviorSubject` state service.
*   **Folders:** `shared/` groups by intent (`storage/`, `i18n/`). A feature has a module-like name, its feature-wide files at the root, and one flat folder per use case (`machines/dashboard/`). Never create technical folders (`services/`, `models/`, `components/`, `utils/`).
    *   **Aliases:** Use `@app/`, `@shared/`, and one `@<feature>/` per feature folder directly under `app/` (no `features/` folder). Add the alias to `tsconfig.json` and the `@app/<feature>/*` ban to ESLint with the feature. ESLint bans `../` imports.
*   **Scope by Provider:** Global: `providedIn: 'root'`, only in `shared/`. Feature: `providers` on the lazy feature route. Local: `providers` on the component. Choose the narrowest scope that all readers share.
*   **Persistence:** Use only `withStorageSync` and `BROWSER_STORAGE`. Never use `localStorage` directly.
*   **Runtime Config:** `config.json` is public. Put only values that every visitor may see in it. Never add a secret, a key, or an internal host name.
*   **DI:** Inject with `inject()` only. Import each class or token that DI reads at run time as a value. Use `import type` only for pure types (ESLint `prefer-inject`, `consistent-type-imports`).
*   **Tests:** Test only logic with branching or processing (services, stores, store features, serializers, pipes, component methods). Never test rendering, templates, or the DOM. Test a component method through the class. `ng generate component` skips the spec (`angular.json`); add one by hand only for such logic.
*   **Server Data:** Use `httpResource` or `rxMethod` inside the store.
*   **Styling:** Define colors and tokens only in `healthyfac-preset.ts`. Templates use Tailwind token utilities (`bg-surface-*`, `text-primary`). Never write a hex color in a component.
    *   **Plain CSS:** Use plain CSS, no SCSS. A component gets no style file by default. Add one (`--style css`) only when utilities and tokens are not enough. Tailwind setup lives only in `src/styles.css`.
    *   **`@apply`:** Use it only for more than 16 utilities on one element (component CSS with `@reference` to `styles.css`), or for a set that appears in 3 or more places or must look the same (`@layer components` in `styles.css`, `hf-` prefix). Never use it for a short set such as `flex flex-col`. No tool checks it.
*   **Text:** Write every visible string as a translation key in all files under `public/i18n/`. Write each key segment in capitalized snake case (`ERRORS.HTTP.NOT_FOUND`). The keys inside `PRIMENG` keep the PrimeNG names.
*   **Time:** Use Temporal from `@shared/time/temporal` for every date and time value. Use `Instant` for past moments, `PlainDateTime` plus an IANA zone or `ZonedDateTime` for planned local times, and `PlainDate` for dates. Read the current time only through `CLOCK`. `Date` appears only inside `shared/time/` (the PrimeNG boundary).
*   **Forms:** Use Signal Forms only (`form()`, `[formField]`). A PrimeNG control that needs another value type gets a `FormValueControl` wrapper in the `shared/` folder of its intent. Use `ngModel` only inside such a wrapper, or for one standalone control outside a form (e.g., the shell language select).
*   **Zoneless and `OnPush`:** The view updates only from signals, event bindings, and the `async` pipe. Never set a plain field from `setTimeout` or a raw `subscribe` for the view. Use a signal (`toSignal`, a store). ESLint blocks `zone.js`, `NgZone`, `provideZoneChangeDetection`, and `ChangeDetectionStrategy.Eager`.
*   **Generation:** Use `ng generate` for components, services, guards, and pipes.
*   **Enforcement:** Prettier and ESLint, run by the Stop hook. `translation-files.spec.ts` checks the translation keys.
