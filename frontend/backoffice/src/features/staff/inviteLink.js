/** The link an invited staff member opens to choose a password. */
export const inviteLink = (token, origin = window.location.origin) => `${origin}/accept-invite?token=${encodeURIComponent(token)}`
