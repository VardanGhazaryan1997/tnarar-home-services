import { baseApi } from '@/api/baseApi'

const STAFF = '/admin/staff'
const ROLES = '/admin/roles'

const withoutEmpty = (params) => Object.fromEntries(Object.entries(params).filter(([, value]) => value !== undefined && value !== ''))

// Account actions on the staff page and the request each one sends.
export const STAFF_ACTIONS = {
  suspend: { url: (id) => `${STAFF}/${id}/suspend`, method: 'POST' },
  activate: { url: (id) => `${STAFF}/${id}/activate`, method: 'POST' },
  resetTwoFactor: { url: (id) => `${STAFF}/${id}/reset-two-factor`, method: 'POST' },
  grantSuperAdmin: { url: (id) => `${STAFF}/${id}/super-admin`, method: 'POST' },
  revokeSuperAdmin: { url: (id) => `${STAFF}/${id}/super-admin`, method: 'DELETE' },
}

/**
 * Staff accounts (staff.view to read, staff.manage to change) and roles (roles.manage to change,
 * Super Admins only). See backend/README "Staff and roles".
 */
export const staffApi = baseApi.enhanceEndpoints({ addTagTypes: ['Staff', 'StaffMember', 'Roles'] }).injectEndpoints({
  endpoints: (build) => ({
    getStaff: build.query({
      query: ({ search, status, roleId, page = 1, pageSize = 50 } = {}) => ({
        url: STAFF,
        params: withoutEmpty({ search: search?.trim(), status, roleId, page, pageSize }),
      }),
      providesTags: ['Staff'],
    }),
    getStaffMember: build.query({
      query: (id) => `${STAFF}/${id}`,
      providesTags: (result, error, id) => [{ type: 'StaffMember', id }],
    }),
    inviteStaff: build.mutation({
      query: (body) => ({ url: STAFF, method: 'POST', body }),
      invalidatesTags: ['Staff'],
    }),
    renewInvite: build.mutation({
      query: (id) => ({ url: `${STAFF}/${id}/invite`, method: 'POST' }),
      invalidatesTags: (result, error, id) => ['Staff', { type: 'StaffMember', id }],
    }),
    updateStaffMember: build.mutation({
      query: ({ id, ...body }) => ({ url: `${STAFF}/${id}`, method: 'PUT', body }),
      invalidatesTags: (result, error, { id }) => ['Staff', { type: 'StaffMember', id }],
    }),
    staffAction: build.mutation({
      query: ({ id, action }) => ({ url: STAFF_ACTIONS[action].url(id), method: STAFF_ACTIONS[action].method }),
      invalidatesTags: (result, error, { id }) => ['Staff', { type: 'StaffMember', id }],
    }),

    getPermissions: build.query({
      query: () => '/admin/permissions',
    }),
    getRoles: build.query({
      query: () => ROLES,
      providesTags: ['Roles'],
    }),
    createRole: build.mutation({
      query: (body) => ({ url: ROLES, method: 'POST', body }),
      invalidatesTags: ['Roles'],
    }),
    updateRole: build.mutation({
      query: ({ id, ...body }) => ({ url: `${ROLES}/${id}`, method: 'PUT', body }),
      // Role names show on staff rows and permissions on staff pages.
      invalidatesTags: ['Roles', 'Staff', 'StaffMember'],
    }),
    deleteRole: build.mutation({
      query: (id) => ({ url: `${ROLES}/${id}`, method: 'DELETE' }),
      invalidatesTags: ['Roles'],
    }),
  }),
})

export const {
  useGetStaffQuery,
  useGetStaffMemberQuery,
  useInviteStaffMutation,
  useRenewInviteMutation,
  useUpdateStaffMemberMutation,
  useStaffActionMutation,
  useGetPermissionsQuery,
  useGetRolesQuery,
  useCreateRoleMutation,
  useUpdateRoleMutation,
  useDeleteRoleMutation,
} = staffApi
