// Ant Design theme for the Back Office, from the TnaShen brand (logo & colors).
// All styling goes through these tokens; the Back Office has no custom SCSS.

/** Brand colors. */
export const BRAND = {
  /** Primary dark: text, buttons, the side bar. */
  araratNight: '#1D3346',
  /** Accent on light backgrounds (logo, highlights). */
  tuffStone: '#C8643B',
  /** Accent on dark backgrounds. */
  tuffLight: '#E07A4F',
  /** Page background. */
  cleanMist: '#F4F6F7',
  /**
   * Tuff Stone deepened for white text on it (4.98:1, WCAG AA); plain Tuff Stone is 3.9:1.
   * Used for the selected menu item.
   */
  tuffStoneDeep: '#B4532C',
}

export const theme = {
  token: {
    colorPrimary: BRAND.araratNight,
    colorLink: BRAND.araratNight,
    colorInfo: BRAND.araratNight,
    colorTextHeading: BRAND.araratNight,
    colorBgLayout: BRAND.cleanMist,
    borderRadius: 8,
    fontFamily: "Outfit, 'Noto Sans Armenian', system-ui, -apple-system, 'Segoe UI', Roboto, sans-serif",
  },
  components: {
    Layout: {
      headerBg: '#ffffff',
      siderBg: BRAND.araratNight,
      headerPadding: '0 16px',
    },
    Menu: {
      darkItemBg: BRAND.araratNight,
      darkSubMenuItemBg: BRAND.araratNight,
      darkItemSelectedBg: BRAND.tuffStoneDeep,
      darkItemColor: 'rgba(255, 255, 255, 0.78)',
      darkItemHoverColor: '#ffffff',
    },
  },
}
