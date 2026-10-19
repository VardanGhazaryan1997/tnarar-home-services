import { createSlice } from '@reduxjs/toolkit'
import { chatApi } from '../chatApi'
import { isReading } from './focus'
import { playMessageSound } from './sound'

/** At most this many alerts show at once; older ones make way. */
export const MAX_ALERTS = 3

/** New-message alerts on screen (T2): who wrote, a preview, and where to read it. */
const alertsSlice = createSlice({
  name: 'messageAlerts',
  initialState: { items: [] },
  reducers: {
    alertAdded(state, { payload }) {
      if (state.items.some((item) => item.id === payload.id)) return
      state.items.push(payload)
      if (state.items.length > MAX_ALERTS) state.items.shift()
    },
    alertDismissed(state, { payload: id }) {
      state.items = state.items.filter((item) => item.id !== id)
    },
    /** The user opened the conversation: its alerts are done. */
    conversationAlertsCleared(state, { payload: conversationId }) {
      state.items = state.items.filter((item) => item.conversationId !== conversationId)
    },
  },
})

export const { alertAdded, alertDismissed, conversationAlertsCleared } = alertsSlice.actions
export const messageAlertsReducer = alertsSlice.reducer
export const selectMessageAlerts = (state) => state.messageAlerts.items

/**
 * A message arrived live: alert the user with a visible notice and the chime, unless they are reading that
 * conversation right now (in a visible, focused tab) or wrote it themselves (on another device, say).
 */
export const alertForMessage = (message) => async (dispatch) => {
  if (isReading(message.conversationId)) return
  // Usually cached already (the conversation list or thread); fetched once otherwise.
  const request = dispatch(chatApi.endpoints.getConversation.initiate(message.conversationId))
  const conversation = (await request).data
  request.unsubscribe()
  if (!conversation || message.senderRole === conversation.myRole) return

  dispatch(
    alertAdded({
      id: message.id,
      conversationId: message.conversationId,
      name: conversation.otherParty.name,
      senderRole: message.senderRole,
      body: message.body,
      files: message.attachments?.length ?? 0,
    }),
  )
  playMessageSound(message.id)
}
