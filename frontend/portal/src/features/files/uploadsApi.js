import { baseApi } from '@/api/baseApi'

export const uploadsApi = baseApi.injectEndpoints({
  endpoints: (build) => ({
    /** Asks for a signed upload URL: { fileId, uploadUrl, method, headers, expiresAt }. */
    requestUpload: build.mutation({
      query: (body) => ({ url: '/files/uploads', method: 'POST', body }),
    }),
    /** After the browser uploaded the bytes: checks them and returns the file. */
    completeUpload: build.mutation({
      query: (fileId) => ({ url: `/files/${fileId}/complete`, method: 'POST' }),
    }),
  }),
})

export const { useRequestUploadMutation, useCompleteUploadMutation } = uploadsApi
