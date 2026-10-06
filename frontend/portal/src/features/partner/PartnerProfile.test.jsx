import { screen, waitFor, within } from '@testing-library/react'
import userEvent from '@testing-library/user-event'
import { http, HttpResponse } from 'msw'
import en from '@/i18n/locales/en/common.json'
import { CUSTOMER, PARTNER_USER, problem, signedInAs } from '@/test/auth'
import { partnerMedia, partnerProfile, priceList } from '@/test/fixtures'
import { renderRoute } from '@/test/renderWithProviders'
import { server } from '@/test/server'

const t = (text, values) => Object.entries(values).reduce((out, [key, value]) => out.replace(`{{${key}}}`, value), text)

/**
 * A fake partner-profile API holding one profile (or none). Records PUT bodies and media calls.
 * `missing` is recomputed like the API does.
 */
function profileApi(initial = null) {
  const state = { profile: initial, saves: [], added: [], removed: [], submitted: 0 }
  const missing = (p) =>
    [p.about.trim().length < 50 && 'about', !p.categoryIds.length && 'services', !p.areas.length && 'areas', !p.workExamples.length && 'work_examples'].filter(Boolean)
  const respond = () => {
    const p = state.profile
    const left = missing(p)
    return HttpResponse.json({ ...p, missingForSubmit: left, canSubmit: ['Draft', 'NeedsChanges'].includes(p.status) && left.length === 0 })
  }
  let nextFile = 0
  server.use(
    http.get('*/api/v1/me/partner-profile', () => (state.profile ? respond() : problem(404, 'partner.not_found'))),
    http.put('*/api/v1/me/partner-profile', async ({ request }) => {
      const body = await request.json()
      state.saves.push(body)
      const avatar = body.avatarFileId ? { ...partnerMedia(body.avatarFileId).file } : null
      state.profile = { ...partnerProfile({ workExamples: [], documents: [] }), ...state.profile, ...body, avatar }
      return respond()
    }),
    http.post('*/api/v1/me/partner-profile/media', async ({ request }) => {
      const body = await request.json()
      state.added.push(body)
      const media = partnerMedia(body.fileId)
      const key = body.kind === 'WorkExample' ? 'workExamples' : 'documents'
      state.profile = { ...state.profile, [key]: [...state.profile[key], media] }
      return respond()
    }),
    http.delete('*/api/v1/me/partner-profile/media/:id', ({ params }) => {
      state.removed.push(params.id)
      const drop = (list) => list.filter((media) => media.id !== params.id)
      state.profile = { ...state.profile, workExamples: drop(state.profile.workExamples), documents: drop(state.profile.documents) }
      return respond()
    }),
    http.post('*/api/v1/me/partner-profile/submit', () => {
      state.submitted += 1
      state.profile = { ...state.profile, status: 'UnderReview', submittedAt: '2026-10-05T10:00:00Z' }
      return respond()
    }),
    http.post('*/api/v1/files/uploads', () => {
      nextFile += 1
      return HttpResponse.json({ fileId: `upload-${nextFile}`, uploadUrl: `https://storage.test/upload-${nextFile}`, method: 'PUT', headers: {} })
    }),
    http.put('https://storage.test/:id', () => new HttpResponse(null, { status: 200 })),
    http.post('*/api/v1/files/:id/complete', ({ params }) => HttpResponse.json(partnerMedia(params.id).file)),
  )
  return state
}

const continueButton = () => screen.getByRole('button', { name: en.partner.saveContinue })
const stepTitle = (name) => screen.findByRole('heading', { level: 2, name: en.partner.steps[name] })

describe('Partner onboarding', () => {
  it('creates a profile step by step, saving each step', async () => {
    const user = userEvent.setup()
    signedInAs(CUSTOMER)
    const api = profileApi()
    const { store } = renderRoute('/en/partner')

    await stepTitle('type')
    expect(screen.getByText(t(en.partner.stepOf, { step: 1, total: 7 }))).toBeInTheDocument()
    await user.click(screen.getByRole('radio', { name: new RegExp(en.public.partnerType.Company) }))
    expect(screen.getByLabelText(en.partner.fields.nameCompany)).toBeInTheDocument()
    await user.click(continueButton())
    expect(screen.getByText(en.partner.errors.name)).toBeInTheDocument()
    expect(api.saves).toHaveLength(0)

    await user.type(screen.getByLabelText(en.partner.fields.nameCompany), 'Best Builders')
    await user.type(screen.getByLabelText(new RegExp(en.partner.fields.years)), '99')
    await user.click(continueButton())
    expect(screen.getByText(en.partner.errors.years)).toBeInTheDocument()
    await user.clear(screen.getByLabelText(new RegExp(en.partner.fields.years)))
    await user.type(screen.getByLabelText(new RegExp(en.partner.fields.years)), '12')
    await user.click(continueButton())

    await stepTitle('services')
    expect(api.saves[0]).toEqual({ type: 'Company', displayName: 'Best Builders', yearsOfExperience: 12, about: '', avatarFileId: null, categoryIds: [], areas: [] })
    expect(store.getState().auth.user.roles).toContain('Partner')
    await user.click(continueButton())
    expect(screen.getByText(en.partner.errors.services)).toBeInTheDocument()
    await user.click(screen.getByRole('checkbox', { name: 'Plumbing' }))
    await user.click(screen.getByRole('checkbox', { name: 'Boilers' }))
    await user.click(screen.getByRole('checkbox', { name: 'Plumbing' }))
    expect(screen.getByText(t(en.partner.selected, { n: 1, max: 20 }))).toBeInTheDocument()
    await user.click(continueButton())

    await stepTitle('areas')
    expect(api.saves[1].categoryIds).toEqual(['cat-boilers'])
    await user.click(continueButton())
    expect(screen.getByText(en.partner.errors.areas)).toBeInTheDocument()
    await user.click(screen.getByRole('checkbox', { name: 'Kentron' }))
    // Ararat's places are behind a toggle; a whole region replaces the places chosen inside it.
    await user.click(screen.getByRole('button', { name: t(en.partner.showPlaces, { count: 2 }) }))
    await user.click(screen.getByRole('checkbox', { name: 'Masis' }))
    await user.click(screen.getByRole('checkbox', { name: t(en.partner.wholeRegion, { region: 'Ararat' }) }))
    expect(screen.queryByRole('checkbox', { name: 'Masis' })).not.toBeInTheDocument()
    expect(screen.getByText(t(en.partner.coversRegion, { region: 'Ararat' }))).toBeInTheDocument()
    await user.click(screen.getByRole('checkbox', { name: t(en.partner.wholeCity, { city: 'Yerevan' }) }))
    expect(screen.queryByRole('checkbox', { name: 'Kentron' })).not.toBeInTheDocument()
    // Search lists matching places across regions.
    await user.type(screen.getByLabelText(en.partner.searchPlaces), 'dal')
    expect(screen.getByRole('checkbox', { name: 'Dalar · Ararat' })).toBeInTheDocument()
    await user.clear(screen.getByLabelText(en.partner.searchPlaces))
    await user.click(continueButton())

    await stepTitle('about')
    expect(api.saves[2].areas).toEqual([
      { regionId: 'region-ararat', cityId: null, districtId: null },
      { regionId: null, cityId: 'city-yerevan', districtId: null },
    ])
    await user.type(screen.getByLabelText(en.partner.fields.about), 'We build.')
    await user.click(continueButton())
    expect(screen.getByText(en.partner.errors.about)).toBeInTheDocument()
    await user.type(screen.getByLabelText(en.partner.fields.about), ' Houses, flats and offices across Yerevan since 2010.')
    await user.click(continueButton())

    await stepTitle('work')
    expect(api.saves[3].about).toBe('We build. Houses, flats and offices across Yerevan since 2010.')
    await user.click(screen.getByRole('button', { name: en.common.back }))
    await stepTitle('about')
    expect(screen.getByLabelText(en.partner.fields.about)).toHaveValue('We build. Houses, flats and offices across Yerevan since 2010.')
  })

  it('attaches and removes work examples and documents as they upload', async () => {
    const user = userEvent.setup({ applyAccept: false })
    signedInAs(PARTNER_USER)
    const api = profileApi(partnerProfile({ workExamples: [] }))
    renderRoute('/en/partner?step=work')

    await stepTitle('work')
    await user.click(screen.getByRole('button', { name: en.common.next }))
    expect(screen.getByText(en.partner.errors.work)).toBeInTheDocument()

    await user.upload(screen.getByLabelText(en.partner.fields.work), new File(['x'], 'kitchen.jpg', { type: 'image/jpeg' }))
    expect(await screen.findByRole('img', { name: 'upload-1.jpg' })).toBeInTheDocument()
    expect(api.added).toEqual([{ kind: 'WorkExample', fileId: 'upload-1', caption: null }])

    await user.upload(screen.getByLabelText(en.partner.fields.documents), new File(['x'], 'license.pdf', { type: 'application/pdf' }))
    await waitFor(() => expect(api.added).toHaveLength(2))
    expect(api.added[1]).toMatchObject({ kind: 'Document', fileId: 'upload-2' })
    await user.upload(screen.getByLabelText(en.partner.fields.documents), new File(['x'], 'notes.txt', { type: 'text/plain' }))
    expect(screen.getByText(en.files.typeNotAllowed)).toBeInTheDocument()
    await user.click(screen.getByRole('button', { name: t(en.files.remove, { name: 'notes.txt' }) }))

    await user.click(screen.getByRole('button', { name: t(en.files.remove, { name: 'upload-1.jpg' }) }))
    await waitFor(() => expect(api.removed).toEqual(['media-upload-1']))
    await waitFor(() => expect(screen.queryByRole('img', { name: 'upload-1.jpg' })).not.toBeInTheDocument())
  })

  it('reports media the API refuses', async () => {
    const user = userEvent.setup()
    signedInAs(PARTNER_USER)
    profileApi(partnerProfile())
    server.use(
      http.post('*/api/v1/me/partner-profile/media', () => problem(422, 'partner.too_many_media')),
      http.delete('*/api/v1/me/partner-profile/media/:id', () => problem(422, 'partner.not_editable')),
    )
    renderRoute('/en/partner?step=work')

    await user.upload(await screen.findByLabelText(en.partner.fields.work), new File(['x'], 'kitchen.jpg', { type: 'image/jpeg' }))
    expect(await screen.findByText(en.errors.partner.too_many_media)).toBeInTheDocument()
    await user.click(screen.getByRole('button', { name: t(en.files.remove, { name: 'work-1.jpg' }) }))
    expect(await screen.findByText(en.errors.partner.not_editable)).toBeInTheDocument()
  })

  it('adds and removes a profile photo', async () => {
    const user = userEvent.setup()
    signedInAs(PARTNER_USER)
    const api = profileApi(partnerProfile())
    renderRoute('/en/partner?step=about')

    await stepTitle('about')
    await user.upload(screen.getByLabelText(en.partner.fields.avatar), new File(['x'], 'me.jpg', { type: 'image/jpeg' }))
    expect(await screen.findByRole('button', { name: en.partner.avatarRemove })).toBeInTheDocument()
    await user.click(continueButton())
    await stepTitle('work')
    expect(api.saves[0].avatarFileId).toBe('upload-1')

    await user.click(screen.getByRole('button', { name: en.common.back }))
    await user.click(await screen.findByRole('button', { name: en.partner.avatarRemove }))
    expect(screen.getByRole('button', { name: en.partner.avatarAdd })).toBeInTheDocument()
    await user.click(continueButton())
    await waitFor(() => expect(api.saves[1].avatarFileId).toBeNull())
  })

  it('takes optional prices after the work examples and saves them when continuing', async () => {
    const user = userEvent.setup()
    signedInAs(PARTNER_USER)
    profileApi(partnerProfile())
    const saves = []
    server.use(
      http.get('*/api/v1/me/partner-profile/prices', () => HttpResponse.json(priceList())),
      http.put('*/api/v1/me/partner-profile/prices', async ({ request }) => {
        const body = await request.json()
        saves.push(body)
        return HttpResponse.json(priceList())
      }),
    )
    renderRoute('/en/partner?step=work')

    await stepTitle('work')
    await user.click(screen.getByRole('button', { name: en.common.next }))
    await stepTitle('prices')
    const faucet = await screen.findByRole('listitem', { name: 'Faucet installation' })
    await user.type(within(faucet).getByLabelText(en.partner.prices.from), '6500')
    await user.click(screen.getByRole('button', { name: en.common.next }))

    await stepTitle('review')
    expect(saves[0].prices).toContainEqual({ workItemId: 'wi-faucet', priceFrom: 6500, priceTo: null, includesMaterials: false })
  })

  it('lets partners skip prices, but not continue with a wrong one', async () => {
    const user = userEvent.setup()
    signedInAs(PARTNER_USER)
    profileApi(partnerProfile())
    server.use(http.get('*/api/v1/me/partner-profile/prices', () => HttpResponse.json(priceList())))
    renderRoute('/en/partner?step=prices')

    const faucet = await screen.findByRole('listitem', { name: 'Faucet installation' })
    await user.type(within(faucet).getAllByRole('textbox')[1], '100')
    await user.click(screen.getByRole('button', { name: en.common.next }))
    expect(await screen.findByText(en.partner.prices.errors.fix)).toBeInTheDocument()
    expect(screen.getByRole('heading', { level: 2, name: en.partner.steps.prices })).toBeInTheDocument()

    await user.clear(within(faucet).getAllByRole('textbox')[1])
    await user.click(screen.getByRole('button', { name: en.common.next }))
    await stepTitle('review')
  })

  it('lists what is missing on the last step and links to it', async () => {
    const user = userEvent.setup()
    signedInAs(PARTNER_USER)
    profileApi(partnerProfile({ about: 'Short', workExamples: [] }))
    renderRoute('/en/partner?step=review')

    const missing = await screen.findByRole('alert')
    expect(within(missing).getByText(en.partner.missingTitle)).toBeInTheDocument()
    expect(screen.getByRole('button', { name: en.partner.submit })).toBeDisabled()
    await user.click(within(missing).getByRole('button', { name: en.partner.missing.work_examples }))
    await stepTitle('work')
    await user.click(screen.getByRole('button', { name: new RegExp(en.partner.steps.review) }))
    await user.click(within(await screen.findByRole('alert')).getByRole('button', { name: en.partner.missing.about }))
    await stepTitle('about')
  })

  it('sends a complete profile for review and shows where it stands', async () => {
    const user = userEvent.setup()
    signedInAs(PARTNER_USER)
    const api = profileApi(partnerProfile())
    renderRoute('/en/partner?step=review')

    expect(await screen.findByText(en.partner.readyText)).toBeInTheDocument()
    expect(screen.getByText('Plumbing')).toBeInTheDocument()
    expect(screen.getByText('Yerevan')).toBeInTheDocument()
    expect(screen.getByText(t(en.partner.mediaSummary, { work: 1, documents: 0 }))).toBeInTheDocument()
    await user.click(screen.getByRole('button', { name: t(en.partner.editSection, { section: en.partner.steps.services }) }))
    await stepTitle('services')
    await user.click(screen.getByRole('button', { name: new RegExp(en.partner.steps.review) }))

    await user.click(await screen.findByRole('button', { name: en.partner.submit }))
    expect(await screen.findByRole('heading', { name: en.partner.status.UnderReview.title })).toBeInTheDocument()
    expect(screen.getByText(en.partner.notice.submitted)).toBeInTheDocument()
    expect(screen.getByText(/Sent on Oct 5, 2026/)).toBeInTheDocument()
    expect(api.submitted).toBe(1)
  })

  it('shows what the team asked to change, and sends it again', async () => {
    const user = userEvent.setup()
    signedInAs(PARTNER_USER)
    profileApi(partnerProfile({ status: 'NeedsChanges', reviewComment: 'Please add a photo of your license.' }))
    server.use(http.post('*/api/v1/me/partner-profile/submit', () => problem(422, 'partner.incomplete')))
    renderRoute('/en/partner?step=review')

    expect(await screen.findByText('Please add a photo of your license.')).toBeInTheDocument()
    expect(screen.getByText(en.partner.needsChangesTitle)).toBeInTheDocument()
    await user.click(screen.getByRole('button', { name: en.partner.resubmit }))
    expect(await screen.findByText(en.errors.partner.incomplete)).toBeInTheDocument()
  })

  it('takes API field errors back to the step they belong to', async () => {
    const user = userEvent.setup()
    signedInAs(PARTNER_USER)
    profileApi(partnerProfile())
    server.use(http.put('*/api/v1/me/partner-profile', () => problem(400, 'validation_failed', { errors: { categoryIds: ['categories.invalid'] } })))
    renderRoute('/en/partner?step=about')

    await stepTitle('about')
    await user.click(continueButton())
    await stepTitle('services')
    expect(await screen.findByText(en.errors.categories.invalid)).toBeInTheDocument()
  })

  it('shows other save failures', async () => {
    const user = userEvent.setup()
    signedInAs(PARTNER_USER)
    profileApi(partnerProfile())
    server.use(http.put('*/api/v1/me/partner-profile', () => problem(422, 'partner.not_editable')))
    renderRoute('/en/partner')

    await stepTitle('type')
    await user.click(continueButton())
    expect(await screen.findByText(en.errors.partner.not_editable)).toBeInTheDocument()
    expect(screen.getByRole('heading', { level: 2, name: en.partner.steps.type })).toBeInTheDocument()
  })

  it('opens an approved profile as live, with editing that keeps the type', async () => {
    const user = userEvent.setup()
    signedInAs(PARTNER_USER)
    profileApi(partnerProfile({ status: 'Approved', slug: 'aram-plumbing', canSubmit: false }))
    renderRoute('/en/partner')

    expect(await screen.findByRole('heading', { name: en.partner.status.Approved.title })).toBeInTheDocument()
    expect(screen.getByRole('link', { name: en.partner.viewPublic })).toHaveAttribute('href', '/en/partners/aram-plumbing')
    expect(screen.getByRole('link', { name: en.partner.openInbox })).toHaveAttribute('href', '/en/inbox')

    await user.click(screen.getByRole('button', { name: en.partner.edit }))
    await stepTitle('type')
    expect(screen.getByText(en.partner.typeLocked)).toBeInTheDocument()
    expect(screen.getByRole('radio', { name: new RegExp(en.public.partnerType.Company) })).toBeDisabled()
    await user.click(screen.getByRole('button', { name: new RegExp(en.partner.steps.review) }))
    expect(await screen.findByText(en.partner.liveEditText)).toBeInTheDocument()
    await user.click(screen.getByRole('button', { name: en.partner.done }))
    expect(await screen.findByText(en.partner.notice.saved)).toBeInTheDocument()
  })

  it('explains rejected and paused profiles with the team comment', async () => {
    signedInAs(PARTNER_USER)
    profileApi(partnerProfile({ status: 'Rejected', reviewComment: 'We could not verify your documents.' }))
    const { unmount } = renderRoute('/en/partner')
    expect(await screen.findByRole('heading', { name: en.partner.status.Rejected.title })).toBeInTheDocument()
    expect(screen.getByText('We could not verify your documents.')).toBeInTheDocument()
    unmount()

    profileApi(partnerProfile({ status: 'Suspended', reviewComment: null }))
    renderRoute('/en/partner')
    expect(await screen.findByRole('heading', { name: en.partner.status.Suspended.title })).toBeInTheDocument()
  })

  it('shows load failures', async () => {
    signedInAs(PARTNER_USER)
    server.use(http.get('*/api/v1/me/partner-profile', () => problem(500, 'server')))
    renderRoute('/en/partner')
    expect(await screen.findByRole('button', { name: en.common.retry })).toBeInTheDocument()
  })
})

describe('Account page', () => {
  it('invites customers to become partners and takes partners to their profile', async () => {
    signedInAs(CUSTOMER)
    const { unmount } = renderRoute('/en/account')
    expect(await screen.findByRole('link', { name: en.account.partner.join })).toHaveAttribute('href', '/en/partner')
    unmount()

    signedInAs(PARTNER_USER)
    renderRoute('/en/account')
    expect(await screen.findByRole('link', { name: en.account.partner.open })).toHaveAttribute('href', '/en/partner')
  })
})
