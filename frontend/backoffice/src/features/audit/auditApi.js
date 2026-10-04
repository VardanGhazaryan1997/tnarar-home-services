import { baseApi } from '@/api/baseApi'

/** Who changed what and when (audit.view). Entries are written by the server; this is read-only. */
export const auditApi = baseApi.enhanceEndpoints({ addTagTypes: ['AuditLog'] }).injectEndpoints({
  endpoints: (build) => ({
    getAuditLog: build.query({
      query: ({ entityType, entityId, action, from, to, page = 1, pageSize = 50 } = {}) => ({
        url: '/admin/audit-log',
        params: Object.fromEntries(
          Object.entries({ entityType, entityId: entityId?.trim(), action, from, to, page, pageSize }).filter(([, v]) => v !== undefined && v !== ''),
        ),
      }),
      providesTags: ['AuditLog'],
    }),
  }),
})

export const { useGetAuditLogQuery } = auditApi
