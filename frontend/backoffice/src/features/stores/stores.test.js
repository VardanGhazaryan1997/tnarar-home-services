import dayjs from 'dayjs'
import {
  blankBranch,
  isWebAddress,
  productPriceProblem,
  toBranchInputs,
  toBranchRows,
  toProductBody,
  toProductValues,
  toStoreBody,
  toStoreValues,
  townOptions,
} from './stores'

describe('isWebAddress', () => {
  it('accepts http and https addresses only', () => {
    expect(isWebAddress('https://shop.example')).toBe(true)
    expect(isWebAddress(' http://shop.example/a ')).toBe(true)
    expect(isWebAddress('ftp://shop.example')).toBe(false)
    expect(isWebAddress('shop.example')).toBe(false)
  })
})

describe('store form values', () => {
  it('turns a blank form into a body with empty fields as null', () => {
    const values = { ...toStoreValues(null), slug: ' demo ', name: ' Demo ', website: '  ', phone: '' }
    expect(toStoreBody(values)).toEqual({
      slug: 'demo',
      name: 'Demo',
      description: {},
      website: null,
      phone: null,
      logoFileId: null,
      sortOrder: 0,
      isActive: true,
    })
  })

  it('round-trips a saved store, with its logo', () => {
    const saved = { slug: 'd', name: 'D', description: { en: 'x' }, website: 'https://d.example', phone: '1', logoFileId: 'f1', logoUrl: 'u', sortOrder: 3, isActive: false }
    const values = toStoreValues(saved)
    expect(values.logo).toEqual({ fileId: 'f1', url: 'u' })
    expect(toStoreBody(values)).toEqual({ ...saved, logoFileId: 'f1', logoUrl: undefined })
  })
})

describe('branches', () => {
  it('maps rows to inputs and back, trimming and nulling empty fields', () => {
    const inputs = toBranchInputs([{ ...blankBranch(), name: ' A ', cityId: 'c1', address: ' 1 St ' }])
    expect(inputs).toEqual([{ name: 'A', cityId: 'c1', districtId: null, address: '1 St', phone: null, openingHours: null }])
    const rows = toBranchRows([{ name: 'A', cityId: 'c1', districtId: null, address: '1 St', phone: null, openingHours: null }])
    expect(rows).toEqual([{ name: 'A', cityId: 'c1', districtId: undefined, address: '1 St', phone: '', openingHours: '' }])
    expect(toBranchRows()).toEqual([])
    expect(toBranchInputs()).toEqual([])
  })

  it('groups towns by region, with towns of no region last', () => {
    const cities = [
      { id: 'a', name: 'A', regionId: 'r1' },
      { id: 'b', name: 'B', regionId: 'r2' },
      { id: 'c', name: 'C', regionId: null },
    ]
    const regions = [
      { id: 'r1', name: 'R1' },
      { id: 'r2', name: 'R2' },
      { id: 'r3', name: 'Empty' },
    ]
    expect(townOptions(cities, regions, 'Other')).toEqual([
      { label: 'R1', options: [{ value: 'a', label: 'A' }] },
      { label: 'R2', options: [{ value: 'b', label: 'B' }] },
      { label: 'Other', options: [{ value: 'c', label: 'C' }] },
    ])
    expect(townOptions(cities.slice(0, 1), regions, 'Other')).toHaveLength(1)
    expect(townOptions()).toEqual([])
  })
})

describe('productPriceProblem', () => {
  it('asks for both sale fields or none, and a sale price below the price', () => {
    expect(productPriceProblem({ price: 100 })).toBeNull()
    expect(productPriceProblem({ price: 100, salePrice: 80, saleEndsOn: dayjs('2026-11-01') })).toBeNull()
    expect(productPriceProblem({ price: 100, salePrice: 80 })).toBe('stores.product.saleNeedsBoth')
    expect(productPriceProblem({ price: 100, saleEndsOn: dayjs('2026-11-01') })).toBe('stores.product.saleNeedsBoth')
    expect(productPriceProblem({ price: 100, salePrice: 100, saleEndsOn: dayjs('2026-11-01') })).toBe('stores.product.saleTooHigh')
  })
})

describe('product form values', () => {
  it('sends a grade only with a material, and the sale end as a date', () => {
    const base = { ...toProductValues(null), name: ' Putty ', packSize: 25, price: 5000, grade: 'Premium' }
    expect(toProductBody(base)).toMatchObject({ name: 'Putty', materialId: null, grade: null, saleEndsOn: null, salePrice: null, brand: null, code: null, inStock: true })
    expect(toProductBody({ ...base, materialId: 'm1', salePrice: 4000, saleEndsOn: dayjs('2026-11-01') })).toMatchObject({
      materialId: 'm1',
      grade: 'Premium',
      salePrice: 4000,
      saleEndsOn: '2026-11-01',
    })
  })

  it('shows a saved product in the form', () => {
    const values = toProductValues({
      name: 'P', brand: null, code: 'c', packSize: 2, packLabel: null, price: 10, salePrice: 5, saleEndsOn: '2026-11-01',
      inStock: false, photoFileId: 'f', photoUrl: 'u', materialId: 'm', grade: 'Budget', isActive: true,
    })
    expect(values.saleEndsOn.format('YYYY-MM-DD')).toBe('2026-11-01')
    expect(values).toMatchObject({ brand: '', packLabel: '', photo: { fileId: 'f', url: 'u' }, materialId: 'm', grade: 'Budget', inStock: false })
    expect(toProductValues({ ...{ name: 'P', packSize: 1, price: 1, inStock: true, isActive: true }, salePrice: null })).toMatchObject({ salePrice: undefined, saleEndsOn: null, photo: null })
  })
})
