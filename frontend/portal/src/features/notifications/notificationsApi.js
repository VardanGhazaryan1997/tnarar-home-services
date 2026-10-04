import { baseApi } from '@/api/baseApi'

const TAG = 'Notifications'

/** The signed-in user's notifications (T53). New ones arrive live over the chat hub and refresh these. */
export const notificationsApi = baseApi.injectEndpoints({
  endpoints: (build) => ({
    getNotifications: build.query({
      query: ({ page = 1, unreadOnly = false } = {}) => ({ url: '/notifications', params: { page, pageSize: 20, unreadOnly: unreadOnly || undefined } }),
      providesTags: [TAG],
    }),
    getUnreadNotifications: build.query({
      query: () => '/notifications/unread-count',
      providesTags: [TAG],
    }),
    markNotificationRead: build.mutation({
      query: (id) => ({ url: `/notifications/${id}/read`, method: 'POST' }),
      invalidatesTags: [TAG],
    }),
    markAllNotificationsRead: build.mutation({
      query: () => ({ url: '/notifications/read-all', method: 'POST' }),
      invalidatesTags: [TAG],
    }),
  }),
})

export const { useGetNotificationsQuery, useGetUnreadNotificationsQuery, useMarkNotificationReadMutation, useMarkAllNotificationsReadMutation } =
  notificationsApi

/**
 * What a new notification may have changed on screen: the notification list itself, and the requests, offers
 * and orders it is about.
 */
export const NOTIFICATION_TAGS = [TAG, 'Orders', 'MyRequests', 'Inbox', 'Offers', 'MyOffers', 'PartnerProfile']
