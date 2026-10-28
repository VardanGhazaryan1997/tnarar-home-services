import { PRICE_LIMIT, productPriceProblem } from './stores'

/**
 * Quick price editing (R7): a draft is what a row's inputs hold, `{ price, salePrice, saleEndsOn, inStock, confirm }`,
 * with `saleEndsOn` as "YYYY-MM-DD" or null and `confirm` true when staff ticked "price checked".
 */

/** A product's current values as a draft. */
export const toDraft = (product) => ({
  price: product.price,
  salePrice: product.salePrice ?? null,
  saleEndsOn: product.saleEndsOn ?? null,
  inStock: product.inStock,
  confirm: false,
})

/** True when the draft changes the product's price, sale or stock. */
export const isChanged = (product, draft) =>
  draft.price !== product.price ||
  (draft.salePrice ?? null) !== (product.salePrice ?? null) ||
  (draft.saleEndsOn ?? null) !== (product.saleEndsOn ?? null) ||
  draft.inStock !== product.inStock

/** True when the row will be sent: it changed, or its unchanged price was ticked as checked. */
export const isPending = (product, draft) => Boolean(draft) && (isChanged(product, draft) || draft.confirm)

/** What's wrong with a draft (a text key), by the server's rules, or null. */
export function draftProblem(draft) {
  if (draft.price == null) return 'stores.product.priceRequired'
  if (draft.price < 1 || draft.price > PRICE_LIMIT) return 'stores.quickEdit.priceInvalid'
  return productPriceProblem(draft)
}

/** The rows to save, as the API takes them: only changed or checked ones. */
export function toItems(products, drafts) {
  return products
    .filter((product) => isPending(product, drafts[product.id]))
    .map((product) => {
      const draft = drafts[product.id]
      return {
        productId: product.id,
        price: draft.price,
        salePrice: draft.salePrice ?? null,
        saleEndsOn: draft.saleEndsOn ?? null,
        inStock: draft.inStock,
        confirm: draft.confirm && !isChanged(product, draft),
      }
    })
}
