import { baseApi } from '@/api/baseApi'

const BASE = '/admin/catalog'

/** Back Office catalog management (needs catalog.manage). See backend/README "Managing the catalog". */
export const catalogApi = baseApi.enhanceEndpoints({ addTagTypes: ['Categories', 'Cities', 'WorkItems', 'RoomTemplates'] }).injectEndpoints({
  endpoints: (build) => ({
    getCategories: build.query({
      query: () => `${BASE}/categories`,
      providesTags: ['Categories'],
    }),
    createCategory: build.mutation({
      query: (body) => ({ url: `${BASE}/categories`, method: 'POST', body }),
      invalidatesTags: ['Categories'],
    }),
    updateCategory: build.mutation({
      query: ({ id, ...body }) => ({ url: `${BASE}/categories/${id}`, method: 'PUT', body }),
      invalidatesTags: ['Categories'],
    }),
    setCategoryActive: build.mutation({
      query: ({ id, isActive }) => ({
        url: `${BASE}/categories/${id}/${isActive ? 'activate' : 'deactivate'}`,
        method: 'POST',
      }),
      invalidatesTags: ['Categories'],
    }),
    deleteCategory: build.mutation({
      query: (id) => ({ url: `${BASE}/categories/${id}`, method: 'DELETE' }),
      invalidatesTags: ['Categories'],
    }),

    getWorkItems: build.query({
      query: () => `${BASE}/work-items`,
      providesTags: ['WorkItems'],
    }),
    createWorkItem: build.mutation({
      query: (body) => ({ url: `${BASE}/work-items`, method: 'POST', body }),
      invalidatesTags: ['WorkItems'],
    }),
    updateWorkItem: build.mutation({
      query: ({ id, ...body }) => ({ url: `${BASE}/work-items/${id}`, method: 'PUT', body }),
      invalidatesTags: ['WorkItems'],
    }),
    setWorkItemActive: build.mutation({
      query: ({ id, isActive }) => ({ url: `${BASE}/work-items/${id}/${isActive ? 'activate' : 'deactivate'}`, method: 'POST' }),
      invalidatesTags: ['WorkItems'],
    }),
    deleteWorkItem: build.mutation({
      query: (id) => ({ url: `${BASE}/work-items/${id}`, method: 'DELETE' }),
      invalidatesTags: ['WorkItems'],
    }),

    getRoomTemplates: build.query({
      query: () => `${BASE}/room-templates`,
      providesTags: ['RoomTemplates'],
    }),
    setRoomTemplate: build.mutation({
      query: ({ roomType, items }) => ({ url: `${BASE}/room-templates/${roomType}`, method: 'PUT', body: { items } }),
      invalidatesTags: ['RoomTemplates'],
    }),

    getRegions: build.query({
      query: () => `${BASE}/regions`,
    }),
    getCities: build.query({
      query: () => `${BASE}/cities`,
      providesTags: ['Cities'],
    }),
    createCity: build.mutation({
      query: (body) => ({ url: `${BASE}/cities`, method: 'POST', body }),
      invalidatesTags: ['Cities'],
    }),
    updateCity: build.mutation({
      query: ({ id, ...body }) => ({ url: `${BASE}/cities/${id}`, method: 'PUT', body }),
      invalidatesTags: ['Cities'],
    }),
    setCityActive: build.mutation({
      query: ({ id, isActive }) => ({ url: `${BASE}/cities/${id}/${isActive ? 'activate' : 'deactivate'}`, method: 'POST' }),
      invalidatesTags: ['Cities'],
    }),
    addDistrict: build.mutation({
      query: ({ cityId, ...body }) => ({ url: `${BASE}/cities/${cityId}/districts`, method: 'POST', body }),
      invalidatesTags: ['Cities'],
    }),
    updateDistrict: build.mutation({
      query: ({ cityId, id, ...body }) => ({ url: `${BASE}/cities/${cityId}/districts/${id}`, method: 'PUT', body }),
      invalidatesTags: ['Cities'],
    }),
    setDistrictActive: build.mutation({
      query: ({ cityId, id, isActive }) => ({
        url: `${BASE}/cities/${cityId}/districts/${id}/${isActive ? 'activate' : 'deactivate'}`,
        method: 'POST',
      }),
      invalidatesTags: ['Cities'],
    }),
  }),
})

export const {
  useGetCategoriesQuery,
  useCreateCategoryMutation,
  useUpdateCategoryMutation,
  useSetCategoryActiveMutation,
  useDeleteCategoryMutation,
  useGetWorkItemsQuery,
  useCreateWorkItemMutation,
  useUpdateWorkItemMutation,
  useSetWorkItemActiveMutation,
  useDeleteWorkItemMutation,
  useGetRoomTemplatesQuery,
  useSetRoomTemplateMutation,
  useGetRegionsQuery,
  useGetCitiesQuery,
  useCreateCityMutation,
  useUpdateCityMutation,
  useSetCityActiveMutation,
  useAddDistrictMutation,
  useUpdateDistrictMutation,
  useSetDistrictActiveMutation,
} = catalogApi
