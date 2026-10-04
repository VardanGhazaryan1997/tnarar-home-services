import { screen, waitFor } from '@testing-library/react'
import userEvent from '@testing-library/user-event'
import { http, HttpResponse } from 'msw'
import en from '@/i18n/locales/en/common.json'
import { CUSTOMER, problem, signedInAs } from '@/test/auth'
import { renderRoute } from '@/test/renderWithProviders'
import { server } from '@/test/server'

const file = (name, type, size = 1) => {
  const f = new File(['x'], name, { type })
  Object.defineProperty(f, 'size', { value: size })
  return f
}

async function openPhotoStep(user) {
  await screen.findByRole('option', { name: 'Plumbing' })
  await user.selectOptions(screen.getByLabelText(en.requests.fields.category), 'cat-plumbing')
  await user.selectOptions(screen.getByLabelText(en.requests.fields.city), 'city-yerevan')
  await user.click(screen.getByRole('button', { name: en.common.next }))
  return screen.getByLabelText(en.requests.fields.photos)
}

describe('Photo uploads', () => {
  it('refuses wrong types and large files, and reports failed uploads', async () => {
    const user = userEvent.setup({ applyAccept: false })
    signedInAs(CUSTOMER)
    let tickets = 0
    server.use(
      http.post('*/api/v1/files/uploads', () => {
        tickets += 1
        return tickets === 1
          ? HttpResponse.json({ fileId: 'f1', uploadUrl: 'https://storage.test/f1', method: 'PUT', headers: {} })
          : tickets === 2
            ? HttpResponse.json({ fileId: 'f2', uploadUrl: 'https://storage.test/f2', method: 'PUT', headers: {} })
            : problem(400, 'validation_failed')
      }),
      http.put('https://storage.test/f1', () => new HttpResponse(null, { status: 403 })),
      http.put('https://storage.test/f2', () => new HttpResponse(null, { status: 200 })),
      http.post('*/api/v1/files/f2/complete', () => problem(422, 'file.not_uploaded')),
    )
    renderRoute('/en/requests/new')
    const input = await openPhotoStep(user)

    await user.upload(input, [file('notes.pdf', 'application/pdf'), file('big.mp4', 'video/mp4', 300 * 1024 * 1024)])
    expect(screen.getByText(en.files.typeNotAllowed)).toBeInTheDocument()
    expect(screen.getByText(en.files.tooLarge)).toBeInTheDocument()

    await user.upload(input, [file('a.jpg', 'image/jpeg'), file('b.mov', 'video/quicktime'), file('c.png', 'image/png')])
    await waitFor(() => expect(screen.getAllByText(en.files.uploadFailed)).toHaveLength(3))

    await user.click(screen.getByRole('button', { name: 'Remove notes.pdf' }))
    expect(screen.queryByText(en.files.typeNotAllowed)).not.toBeInTheDocument()
  })

  it('takes at most ten files and hides the add button when full', async () => {
    const user = userEvent.setup()
    signedInAs(CUSTOMER)
    server.use(http.post('*/api/v1/files/uploads', () => new Promise(() => {})))
    renderRoute('/en/requests/new')
    const input = await openPhotoStep(user)

    await user.upload(input, Array.from({ length: 12 }, (_, i) => file(`p${i}.jpg`, 'image/jpeg')))

    expect(screen.getByText('2 file(s) not added: at most 10.')).toBeInTheDocument()
    expect(screen.queryByRole('button', { name: en.files.add })).not.toBeInTheDocument()
    expect(screen.getAllByRole('status', { name: /Uploading/ })).toHaveLength(10)
    expect(screen.getByRole('button', { name: en.common.next })).toBeEnabled()
  })
})
