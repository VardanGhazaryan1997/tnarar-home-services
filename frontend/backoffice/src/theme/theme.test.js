import { BRAND, theme } from './theme'

describe('Back Office theme', () => {
  it('defines brand tokens for Ant Design', () => {
    expect(theme.token).toEqual(
      expect.objectContaining({ colorPrimary: expect.any(String), borderRadius: expect.any(Number), fontFamily: expect.stringContaining('Noto Sans Armenian') }),
    )
  })

  it('uses the TnaShen brand colors', () => {
    expect(theme.token.colorPrimary).toBe(BRAND.araratNight)
    expect(theme.token.colorBgLayout).toBe(BRAND.cleanMist)
    expect(theme.components.Layout.siderBg).toBe(BRAND.araratNight)
    expect(theme.components.Menu.darkItemSelectedBg).toBe(BRAND.tuffStoneDeep)
  })
})
