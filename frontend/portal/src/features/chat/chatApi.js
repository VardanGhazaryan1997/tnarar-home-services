import { baseApi } from '@/api/baseApi'

/** Conversations between customers and partners, and their messages. */
export const chatApi = baseApi.injectEndpoints({
  endpoints: (build) => ({
    /** Opens (or returns) the conversation about a request: partners pass no partnerId, customers name the partner. */
    openConversation: build.mutation({
      query: ({ requestId, partnerId }) => ({ url: `/requests/${requestId}/conversation`, method: 'POST', body: { partnerId: partnerId ?? null } }),
      invalidatesTags: ['Conversations'],
    }),
    getConversations: build.query({
      query: ({ page = 1 } = {}) => ({ url: '/conversations', params: { page, pageSize: 50 } }),
      providesTags: ['Conversations'],
    }),
    getConversation: build.query({
      query: (id) => `/conversations/${id}`,
      providesTags: (_result, _error, id) => [{ type: 'Conversations', id }],
    }),
    getUnread: build.query({
      query: () => '/conversations/unread',
      providesTags: ['Conversations'],
    }),
    /** The newest messages; older pages are loaded with getOlderMessages and kept in the page's state. */
    getMessages: build.query({
      query: (id) => ({ url: `/conversations/${id}/messages`, params: { limit: 50 } }),
      providesTags: (_result, _error, id) => [{ type: 'Messages', id }],
    }),
    getOlderMessages: build.mutation({
      query: ({ id, before }) => ({ url: `/conversations/${id}/messages`, params: { before, limit: 50 } }),
    }),
    sendMessage: build.mutation({
      query: ({ id, body, fileIds }) => ({ url: `/conversations/${id}/messages`, method: 'POST', body: { body: body || null, fileIds } }),
      // The new message goes into the thread at once (the live event may arrive before or after).
      onQueryStarted: async ({ id }, { dispatch, queryFulfilled }) => {
        try {
          const { data } = await queryFulfilled
          dispatch(addMessage(data, id))
        } catch {
          // The composer shows the error.
        }
      },
      invalidatesTags: ['Conversations'],
    }),
    markRead: build.mutation({
      query: (id) => ({ url: `/conversations/${id}/read`, method: 'POST' }),
      invalidatesTags: ['Conversations'],
    }),
  }),
})

/** Adds a message to its conversation's cached thread, once (by id). */
export function addMessage(message, conversationId = message.conversationId) {
  return chatApi.util.updateQueryData('getMessages', conversationId, (draft) => {
    if (!draft.items.some((item) => item.id === message.id)) draft.items.push(message)
  })
}

export const {
  useOpenConversationMutation,
  useGetConversationsQuery,
  useGetConversationQuery,
  useGetUnreadQuery,
  useGetMessagesQuery,
  useGetOlderMessagesMutation,
  useSendMessageMutation,
  useMarkReadMutation,
} = chatApi
