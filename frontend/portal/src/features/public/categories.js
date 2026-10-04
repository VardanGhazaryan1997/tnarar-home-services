/** Helpers over the category tree from /categories. */

/** Finds a category (or subcategory) by slug, with its parent: `{ category, parent }`. */
export function findCategory(categories = [], slug) {
  for (const parent of categories) {
    if (parent.slug === slug) return { category: parent, parent: null }
    const child = parent.children.find((item) => item.slug === slug)
    if (child) return { category: child, parent }
  }
  return { category: null, parent: null }
}

/** Every category id in the tree, parents first. */
export const allCategories = (categories = []) => categories.flatMap((parent) => [parent, ...parent.children])
