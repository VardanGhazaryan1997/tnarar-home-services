import { baseApi } from '@/api/baseApi'

/** Static pages and FAQs for the website (content.manage). Texts are per language: { hy: "…", ru: "…" }. */
export const contentApi = baseApi.enhanceEndpoints({ addTagTypes: ['Pages', 'Faqs'] }).injectEndpoints({
  endpoints: (build) => ({
    getPages: build.query({ query: () => '/admin/pages', providesTags: ['Pages'] }),
    getPage: build.query({ query: (id) => `/admin/pages/${id}`, providesTags: ['Pages'] }),
    createPage: build.mutation({
      query: (body) => ({ url: '/admin/pages', method: 'POST', body }),
      invalidatesTags: ['Pages'],
    }),
    updatePage: build.mutation({
      query: ({ id, ...body }) => ({ url: `/admin/pages/${id}`, method: 'PUT', body }),
      invalidatesTags: ['Pages'],
    }),
    setPagePublished: build.mutation({
      query: ({ id, published }) => ({ url: `/admin/pages/${id}/${published ? 'publish' : 'unpublish'}`, method: 'POST' }),
      invalidatesTags: ['Pages'],
    }),
    deletePage: build.mutation({
      query: (id) => ({ url: `/admin/pages/${id}`, method: 'DELETE' }),
      invalidatesTags: ['Pages'],
    }),

    getFaqs: build.query({ query: () => '/admin/faqs', providesTags: ['Faqs'] }),
    createFaq: build.mutation({
      query: (body) => ({ url: '/admin/faqs', method: 'POST', body }),
      invalidatesTags: ['Faqs'],
    }),
    updateFaq: build.mutation({
      query: ({ id, ...body }) => ({ url: `/admin/faqs/${id}`, method: 'PUT', body }),
      invalidatesTags: ['Faqs'],
    }),
    setFaqPublished: build.mutation({
      query: ({ id, published }) => ({ url: `/admin/faqs/${id}/${published ? 'publish' : 'unpublish'}`, method: 'POST' }),
      invalidatesTags: ['Faqs'],
    }),
    deleteFaq: build.mutation({
      query: (id) => ({ url: `/admin/faqs/${id}`, method: 'DELETE' }),
      invalidatesTags: ['Faqs'],
    }),
  }),
})

export const {
  useGetPagesQuery,
  useGetPageQuery,
  useCreatePageMutation,
  useUpdatePageMutation,
  useSetPagePublishedMutation,
  useDeletePageMutation,
  useGetFaqsQuery,
  useCreateFaqMutation,
  useUpdateFaqMutation,
  useSetFaqPublishedMutation,
  useDeleteFaqMutation,
} = contentApi
