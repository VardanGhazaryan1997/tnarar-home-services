/** Data as /admin/translations and /admin/languages return it. */
export const ADMIN_LANGUAGES = [
  { code: 'en', name: 'English', nativeName: 'English', isActive: true, isDefault: false, sortOrder: 3 },
  { code: 'hy', name: 'Armenian', nativeName: 'Հայերեն', isActive: true, isDefault: true, sortOrder: 1 },
  { code: 'ru', name: 'Russian', nativeName: 'Русский', isActive: true, isDefault: false, sortOrder: 2 },
  { code: 'fr', name: 'French', nativeName: 'Français', isActive: false, isDefault: false, sortOrder: 4 },
]

export const NAMESPACES = [
  {
    namespace: 'portal',
    keyCount: 4,
    languages: [
      { language: 'hy', translated: 4, missing: 0 },
      { language: 'ru', translated: 3, missing: 1 },
      { language: 'en', translated: 2, missing: 2 },
      { language: 'fr', translated: 0, missing: 4 },
    ],
  },
]

export const textRow = (key, values) => ({ key, values })

export const TEXTS = [
  textRow('home.title', { hy: 'Գլխավոր', ru: 'Главная', en: 'Home' }),
  textRow('home.search', { hy: 'Որոնել', ru: 'Поиск' }),
]

export const textsPage = (items, overrides = {}) => ({ items, page: 1, pageSize: 50, totalCount: items.length, ...overrides })
