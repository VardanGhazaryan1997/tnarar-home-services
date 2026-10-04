/** Pages and FAQs as /admin/pages and /admin/faqs return them. */
export const pageRow = (overrides = {}) => ({
  id: 'page-terms',
  slug: 'terms',
  title: { hy: 'Օգտագործման պայմաններ', en: 'Terms of use' },
  body: { hy: '## Պայմաններ', en: '## Terms' },
  showInFooter: true,
  sortOrder: 1,
  isPublished: true,
  publishedAt: '2026-09-20T10:00:00Z',
  updatedAt: '2026-09-21T10:00:00Z',
  ...overrides,
})

export const PAGES = [
  pageRow(),
  pageRow({ id: 'page-about', slug: 'about', title: { hy: 'Մեր մասին' }, body: {}, showInFooter: false, sortOrder: 2, isPublished: false, publishedAt: null }),
]

export const faqRow = (overrides = {}) => ({
  id: 'faq-pay',
  question: { hy: 'Ինչպե՞ս վճարել', en: 'How do I pay?' },
  answer: { hy: 'Քարտով կամ կանխիկ', en: 'By card or cash' },
  audience: 'Customers',
  sortOrder: 1,
  isPublished: true,
  ...overrides,
})

export const FAQS = [
  faqRow(),
  faqRow({ id: 'faq-join', question: { hy: 'Ինչպե՞ս միանալ' }, answer: { hy: 'Լրացրեք էջը' }, audience: 'Partners', sortOrder: 2, isPublished: false }),
]
