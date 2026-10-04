import { baseApi } from '@/api/baseApi'

const TEXTS = '/admin/translations'
const LANGUAGES = '/admin/languages'
const enc = encodeURIComponent

/**
 * Interface texts per namespace and language, and the languages themselves (translations.manage).
 * Language changes also refresh the public language list the switchers use ('Languages').
 */
export const translationsApi = baseApi.enhanceEndpoints({ addTagTypes: ['TextNamespaces', 'Texts', 'AdminLanguages'] }).injectEndpoints({
  endpoints: (build) => ({
    getNamespaces: build.query({ query: () => TEXTS, providesTags: ['TextNamespaces'] }),
    getTexts: build.query({
      query: ({ ns, search, missingIn, page = 1, pageSize = 50 }) => ({
        url: `${TEXTS}/${enc(ns)}`,
        params: Object.fromEntries(Object.entries({ search: search?.trim(), missingIn, page, pageSize }).filter(([, v]) => v !== undefined && v !== '')),
      }),
      providesTags: ['Texts'],
    }),
    saveText: build.mutation({
      query: ({ ns, key, values }) => ({ url: `${TEXTS}/${enc(ns)}/keys/${enc(key)}`, method: 'PUT', body: { values } }),
      invalidatesTags: ['Texts', 'TextNamespaces'],
    }),
    deleteText: build.mutation({
      query: ({ ns, key }) => ({ url: `${TEXTS}/${enc(ns)}/keys/${enc(key)}`, method: 'DELETE' }),
      invalidatesTags: ['Texts', 'TextNamespaces'],
    }),
    exportTexts: build.mutation({
      query: ({ ns, language, withFallback }) => ({
        url: `${TEXTS}/${enc(ns)}/export/${enc(language)}`,
        params: withFallback ? { withFallback: true } : undefined,
        // The file comes back as text; errors are still ProblemDetails JSON.
        responseHandler: (response) => (response.ok ? response.text() : response.json()),
      }),
    }),
    importTexts: build.mutation({
      query: ({ ns, language, content, replace }) => ({
        url: `${TEXTS}/${enc(ns)}/import/${enc(language)}`,
        method: 'POST',
        params: replace ? { replace: true } : undefined,
        body: content,
      }),
      invalidatesTags: ['Texts', 'TextNamespaces'],
    }),

    getAdminLanguages: build.query({ query: () => LANGUAGES, providesTags: ['AdminLanguages'] }),
    createLanguage: build.mutation({
      query: (body) => ({ url: LANGUAGES, method: 'POST', body }),
      invalidatesTags: ['AdminLanguages', 'Languages', 'TextNamespaces'],
    }),
    updateLanguage: build.mutation({
      query: ({ code, ...body }) => ({ url: `${LANGUAGES}/${enc(code)}`, method: 'PUT', body }),
      invalidatesTags: ['AdminLanguages', 'Languages'],
    }),
    setLanguageActive: build.mutation({
      query: ({ code, active }) => ({ url: `${LANGUAGES}/${enc(code)}/${active ? 'activate' : 'deactivate'}`, method: 'POST' }),
      invalidatesTags: ['AdminLanguages', 'Languages', 'TextNamespaces'],
    }),
  }),
})

export const {
  useGetNamespacesQuery,
  useGetTextsQuery,
  useSaveTextMutation,
  useDeleteTextMutation,
  useExportTextsMutation,
  useImportTextsMutation,
  useGetAdminLanguagesQuery,
  useCreateLanguageMutation,
  useUpdateLanguageMutation,
  useSetLanguageActiveMutation,
} = translationsApi
