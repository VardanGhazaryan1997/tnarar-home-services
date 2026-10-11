import { baseApi } from '@/api/baseApi'
import { withoutEmpty } from '@/shared/useUrlFilters'

const BASE = '/admin/stores'

/**
 * Back Office stores, their branches and products (needs materials.pricing). See backend/README "Stores and material
 * prices". Saving a product changes the material prices worked out from store prices, so those lists reload too.
 */
export const storesApi = baseApi
  .enhanceEndpoints({ addTagTypes: ['Stores', 'Store', 'StoreProducts', 'Materials', 'MaterialOffers'] })
  .injectEndpoints({
    endpoints: (build) => ({
      getStores: build.query({
        query: ({ search, isActive } = {}) => ({ url: BASE, params: withoutEmpty({ search: search?.trim(), isActive }) }),
        providesTags: ['Stores'],
      }),
      getStore: build.query({
        query: (id) => `${BASE}/${id}`,
        providesTags: (_result, _error, id) => [{ type: 'Store', id }],
      }),
      createStore: build.mutation({
        query: (body) => ({ url: BASE, method: 'POST', body }),
        invalidatesTags: ['Stores'],
      }),
      updateStore: build.mutation({
        query: ({ id, ...body }) => ({ url: `${BASE}/${id}`, method: 'PUT', body }),
        invalidatesTags: (_result, _error, { id }) => ['Stores', { type: 'Store', id }, 'Materials', 'MaterialOffers'],
      }),
      deleteStore: build.mutation({
        query: (id) => ({ url: `${BASE}/${id}`, method: 'DELETE' }),
        invalidatesTags: ['Stores'],
      }),
      setStoreBranches: build.mutation({
        query: ({ id, branches }) => ({ url: `${BASE}/${id}/branches`, method: 'PUT', body: { branches } }),
        invalidatesTags: (_result, _error, { id }) => ['Stores', { type: 'Store', id }],
      }),
      /** Materials a product can be linked to (the admin material list, for staff without catalog.manage). */
      getLinkableMaterials: build.query({
        query: () => `${BASE}/materials`,
        providesTags: ['Materials'],
      }),
      getStoreProducts: build.query({
        query: ({ storeId, search, materialId, linked, stale, page = 1, pageSize = 25 }) => ({
          url: `${BASE}/${storeId}/products`,
          params: withoutEmpty({ search: search?.trim(), materialId, linked, stale, page, pageSize }),
        }),
        providesTags: ['StoreProducts'],
      }),
      createStoreProduct: build.mutation({
        query: ({ storeId, ...body }) => ({ url: `${BASE}/${storeId}/products`, method: 'POST', body }),
        invalidatesTags: ['StoreProducts', 'Stores', 'Materials', 'MaterialOffers'],
      }),
      updateStoreProduct: build.mutation({
        query: ({ storeId, productId, ...body }) => ({ url: `${BASE}/${storeId}/products/${productId}`, method: 'PUT', body }),
        invalidatesTags: ['StoreProducts', 'Stores', 'Materials', 'MaterialOffers'],
      }),
      deleteStoreProduct: build.mutation({
        query: ({ storeId, productId }) => ({ url: `${BASE}/${storeId}/products/${productId}`, method: 'DELETE' }),
        invalidatesTags: ['StoreProducts', 'Stores', 'Materials', 'MaterialOffers'],
      }),
      /** The store's price is still right: "price updated" becomes now. */
      confirmStoreProductPrice: build.mutation({
        query: ({ storeId, productId }) => ({ url: `${BASE}/${storeId}/products/${productId}/confirm`, method: 'POST' }),
        invalidatesTags: ['StoreProducts', 'Stores', 'Materials', 'MaterialOffers'],
      }),
      /** Every product linked to a material across stores. */
      getMaterialStoreProducts: build.query({
        query: (materialId) => `/admin/catalog/materials/${materialId}/store-products`,
        providesTags: ['MaterialOffers'],
      }),
    }),
  })

export const {
  useGetStoresQuery,
  useGetStoreQuery,
  useCreateStoreMutation,
  useUpdateStoreMutation,
  useDeleteStoreMutation,
  useSetStoreBranchesMutation,
  useGetLinkableMaterialsQuery,
  useGetStoreProductsQuery,
  useCreateStoreProductMutation,
  useUpdateStoreProductMutation,
  useDeleteStoreProductMutation,
  useConfirmStoreProductPriceMutation,
  useGetMaterialStoreProductsQuery,
} = storesApi
