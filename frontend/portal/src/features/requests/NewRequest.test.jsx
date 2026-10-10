import { screen, waitFor } from '@testing-library/react'
import userEvent from '@testing-library/user-event'
import { http, HttpResponse } from 'msw'
import en from '@/i18n/locales/en/common.json'
import { CUSTOMER, problem, signedInAs } from '@/test/auth'
import { myRequest } from '@/test/fixtures'
import { renderRoute } from '@/test/renderWithProviders'
import { server } from '@/test/server'

function createApi(respond) {
  const calls = { created: [], uploads: [], stored: [], completed: [] }
  server.use(
    http.post('*/api/v1/files/uploads', async ({ request }) => {
      const body = await request.json()
      calls.uploads.push(body)
      const id = `file-${calls.uploads.length}`
      return HttpResponse.json({ fileId: id, uploadUrl: `https://storage.test/${id}`, method: 'PUT', headers: { 'Content-Type': body.contentType }, expiresAt: '2026-10-05T10:00:00Z' })
    }),
    http.put('https://storage.test/:id', ({ params }) => {
      calls.stored.push(params.id)
      return new HttpResponse(null, { status: 200 })
    }),
    http.post('*/api/v1/files/:id/complete', ({ params }) => {
      calls.completed.push(params.id)
      return HttpResponse.json({ id: params.id, kind: 'Image', status: 'Ready' })
    }),
    http.post('*/api/v1/requests', async ({ request }) => {
      const body = await request.json()
      calls.created.push(body)
      return respond ? respond(body) : HttpResponse.json(myRequest(), { status: 201 })
    }),
    http.get('*/api/v1/requests/req-1', () => HttpResponse.json(myRequest())),
    http.get('*/api/v1/requests/req-1/offers', () => HttpResponse.json([])),
  )
  return calls
}

async function fillWhatAndWhere(user) {
  await screen.findByRole('option', { name: 'Plumbing' })
  await user.selectOptions(screen.getByLabelText(en.requests.fields.category), 'cat-boilers')
  await user.selectOptions(screen.getByLabelText(en.requests.fields.city), 'city-yerevan')
  await user.selectOptions(screen.getByLabelText(/District/), 'dist-kentron')
  await user.click(screen.getByRole('button', { name: en.common.next }))
}

describe('New request', () => {
  it('walks through the steps, uploads a photo and sends the request', async () => {
    const user = userEvent.setup()
    signedInAs(CUSTOMER)
    const calls = createApi()
    const { router } = renderRoute('/en/requests/new')

    expect(await screen.findByText(/Step 1 of 3/)).toBeInTheDocument()
    expect(await screen.findByRole('option', { name: '— Boilers' })).toBeInTheDocument()
    await fillWhatAndWhere(user)

    expect(screen.getByText(/Step 2 of 3/)).toBeInTheDocument()
    await user.type(screen.getByLabelText(en.requests.fields.description), 'The boiler makes a loud noise at night.')
    await user.upload(screen.getByLabelText(en.requests.fields.photos), new File(['x'], 'boiler.jpg', { type: 'image/jpeg' }))
    await waitFor(() => expect(calls.completed).toEqual(['file-1']))
    expect(calls.stored).toEqual(['file-1'])
    expect(calls.uploads[0]).toEqual({ fileName: 'boiler.jpg', contentType: 'image/jpeg', size: 1 })
    await user.click(screen.getByRole('button', { name: en.common.next }))

    await user.type(screen.getByLabelText(/Convenient time/), 'mornings')
    await user.type(screen.getByLabelText(/Budget up to/), '50000')
    await user.click(screen.getByRole('button', { name: en.requests.submit }))

    await waitFor(() => expect(router.state.location.pathname).toBe('/en/requests/req-1'))
    expect(calls.created).toEqual([
      {
        kind: 'Open',
        partnerId: null,
        categoryId: 'cat-boilers',
        cityId: 'city-yerevan',
        districtId: 'dist-kentron',
        description: 'The boiler makes a loud noise at night.',
        preferredDate: null,
        timeNote: 'mornings',
        budgetMin: null,
        budgetMax: 50000,
        mediaFileIds: ['file-1'],
        estimateId: null,
      },
    ])
    expect(await screen.findByText(en.requests.created)).toBeInTheDocument()
  })

  it('checks each step before moving on, and goes back', async () => {
    const user = userEvent.setup()
    signedInAs(CUSTOMER)
    createApi()
    renderRoute('/en/requests/new')

    await user.click(await screen.findByRole('button', { name: en.common.next }))
    expect(screen.getByText(en.requests.errors.categoryRequired)).toBeInTheDocument()
    expect(screen.getByText(en.requests.errors.cityRequired)).toBeInTheDocument()

    await fillWhatAndWhere(user)
    await user.type(screen.getByLabelText(en.requests.fields.description), 'Too short')
    await user.click(screen.getByRole('button', { name: en.common.next }))
    expect(screen.getByText('Describe the job in at least 20 characters.')).toBeInTheDocument()

    await user.click(screen.getByRole('button', { name: en.common.back }))
    expect(screen.getByLabelText(en.requests.fields.category)).toHaveValue('cat-boilers')
  })

  it('checks the budget range', async () => {
    const user = userEvent.setup()
    signedInAs(CUSTOMER)
    const calls = createApi()
    renderRoute('/en/requests/new')

    await fillWhatAndWhere(user)
    await user.type(screen.getByLabelText(en.requests.fields.description), 'The boiler makes a loud noise at night.')
    await user.click(screen.getByRole('button', { name: en.common.next }))
    await user.type(screen.getByLabelText(/Budget from/), '50000')
    await user.type(screen.getByLabelText(/Budget up to/), '1000')
    await user.click(screen.getByRole('button', { name: en.requests.submit }))

    expect(screen.getByText(en.errors.budget.range_invalid)).toBeInTheDocument()
    expect(calls.created).toHaveLength(0)
  })

  it('sends a direct request to one partner and shows API errors on the right step', async () => {
    const user = userEvent.setup()
    signedInAs(CUSTOMER)
    const calls = createApi(() => problem(400, 'validation_failed', { errors: { description: ['description.length'] } }))
    renderRoute('/en/requests/new?partner=partner-1&name=Aram%20Plumbing&category=cat-plumbing')

    expect(await screen.findByText('Only Aram Plumbing receives this request.')).toBeInTheDocument()
    await screen.findByRole('option', { name: 'Masis' })
    expect(screen.getByLabelText(en.requests.fields.category)).toHaveValue('cat-plumbing')
    await user.selectOptions(screen.getByLabelText(en.requests.fields.city), 'city-masis')
    expect(screen.getByLabelText(/District/)).toBeDisabled()
    await user.click(screen.getByRole('button', { name: en.common.next }))
    await user.type(screen.getByLabelText(en.requests.fields.description), 'The kitchen tap is leaking, please help.')
    await user.click(screen.getByRole('button', { name: en.common.next }))
    await user.click(screen.getByRole('button', { name: en.requests.submit }))

    expect(await screen.findByText(en.errors.description.length)).toBeInTheDocument()
    expect(screen.getByText(/Step 2 of 3/)).toBeInTheDocument()
    expect(calls.created[0]).toMatchObject({ kind: 'Direct', partnerId: 'partner-1', cityId: 'city-masis', districtId: null })
  })

  it('shows rule errors such as an unavailable partner', async () => {
    const user = userEvent.setup()
    signedInAs(CUSTOMER)
    createApi(() => problem(422, 'request.partner_unavailable'))
    renderRoute('/en/requests/new?partner=partner-1')

    await fillWhatAndWhere(user)
    await user.type(screen.getByLabelText(en.requests.fields.description), 'The kitchen tap is leaking, please help.')
    await user.click(screen.getByRole('button', { name: en.common.next }))
    await user.click(screen.getByRole('button', { name: en.requests.submit }))

    expect(await screen.findByText(en.errors.request.partner_unavailable)).toBeInTheDocument()
  })
})
