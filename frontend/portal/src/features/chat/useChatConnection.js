import { HubConnectionBuilder, LogLevel } from '@microsoft/signalr'
import { useEffect } from 'react'
import { useDispatch, useStore } from 'react-redux'
import { API_BASE_URL, resolveBaseUrl } from '@/api/config'
import { NOTIFICATION_TAGS } from '@/features/notifications/notificationsApi'
import { addMessage, chatApi } from './chatApi'

/** The chat hub next to the API: "/api/v1" → "/hubs/chat" on the same host. */
export const hubUrl = () => new URL('/hubs/chat', resolveBaseUrl(API_BASE_URL)).href

/**
 * While a user is signed in, listens to the chat hub: new messages go straight into open threads, and
 * conversation lists and unread badges refresh. New notifications refresh the bell and the pages they are
 * about. Reconnects on its own; the token is read fresh each time.
 */
export function useChatConnection(enabled) {
  const dispatch = useDispatch()
  const store = useStore()

  useEffect(() => {
    if (!enabled) return undefined

    const connection = new HubConnectionBuilder()
      .withUrl(hubUrl(), { accessTokenFactory: () => store.getState().auth.accessToken ?? '' })
      .withAutomaticReconnect()
      .configureLogging(LogLevel.Warning)
      .build()

    connection.on('messageReceived', (message) => {
      dispatch(addMessage(message))
      dispatch(chatApi.util.invalidateTags(['Conversations']))
    })
    connection.on('conversationRead', ({ conversationId }) => {
      dispatch(chatApi.util.invalidateTags([{ type: 'Conversations', id: conversationId }]))
    })
    // Something happened on a request, offer, order or the profile: refresh the bell and what's on screen.
    connection.on('notificationReceived', () => dispatch(chatApi.util.invalidateTags(NOTIFICATION_TAGS)))
    // A reconnect may have missed events: refresh what's on screen.
    connection.onreconnected(() => dispatch(chatApi.util.invalidateTags(['Conversations', 'Messages', 'Notifications'])))

    connection.start().catch(() => {
      // Offline or the server is restarting: withAutomaticReconnect only covers dropped connections,
      // so the lists still refresh on their own when the user opens them.
    })

    return () => {
      connection.stop()
    }
  }, [enabled, dispatch, store])
}
