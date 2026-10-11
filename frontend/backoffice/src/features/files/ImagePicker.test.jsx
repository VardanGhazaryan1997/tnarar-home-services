import { render, screen, waitFor } from '@testing-library/react'
import userEvent from '@testing-library/user-event'
import { http, HttpResponse } from 'msw'
import { useState } from 'react'
import { Provider } from 'react-redux'
import AppProviders from '@/app/AppProviders'
import { makeStore } from '@/app/store'
import hy from '@/i18n/locales/hy/common.json'
import { problem } from '@/test/auth'
import { server } from '@/test/server'
import ImagePicker from './ImagePicker'

const SLOW = 60_000
const f = hy.files

function Harness({ initial = null, onValue }) {
  const [value, setValue] = useState(initial)
  return (
    <Provider store={makeStore()}>
      <AppProviders>
        <ImagePicker
          label="Logo"
          value={value}
          onChange={(next) => {
            setValue(next)
            onValue?.(next)
          }}
        />
      </AppProviders>
    </Provider>
  )
}

const picture = (type = 'image/png', size = 100) => {
  const file = new File(['x'], 'logo.png', { type })
  Object.defineProperty(file, 'size', { value: size })
  return file
}

function mockUpload({ putStatus = 200, complete = true } = {}) {
  server.use(
    http.post('*/api/v1/files/uploads', () => HttpResponse.json({ fileId: 'file-1', uploadUrl: 'https://storage.test/put', method: 'PUT', headers: { 'Content-Type': 'image/png' } })),
    http.put('https://storage.test/put', () => new HttpResponse(null, { status: putStatus })),
    http.post('*/api/v1/files/file-1/complete', () => (complete ? HttpResponse.json({ id: 'file-1', url: 'https://storage.test/file-1', thumbnailUrl: 'https://storage.test/file-1/thumb' }) : problem(422, 'file.invalid_content'))),
  )
}

describe('ImagePicker', () => {
  it('uploads a picture, shows it, and lets it go', async () => {
    mockUpload()
    const values = []
    const user = userEvent.setup()
    render(<Harness onValue={(value) => values.push(value)} />)

    await user.upload(screen.getByLabelText('Logo'), picture())

    expect(await screen.findByAltText(f.preview)).toHaveAttribute('src', 'https://storage.test/file-1/thumb')
    expect(values[0]).toEqual({ uploading: true })
    expect(values.at(-1)).toEqual({ fileId: 'file-1', url: 'https://storage.test/file-1/thumb' })

    await user.click(screen.getByRole('button', { name: f.remove }))
    await waitFor(() => expect(screen.queryByAltText(f.preview)).not.toBeInTheDocument())
    expect(values.at(-1)).toBeNull()
  }, SLOW)

  it('offers to replace a picture it already has', () => {
    render(<Harness initial={{ fileId: 'old', url: 'https://storage.test/old' }} />)
    expect(screen.getByRole('button', { name: new RegExp(f.replace) })).toBeInTheDocument()
  })

  it.each([
    ['a type that is not a picture', picture('application/pdf'), f.typeNotAllowed],
    ['a picture that is too large', picture('image/png', 11 * 1024 * 1024), f.tooLarge],
  ])('refuses %s without uploading', async (_, file, message) => {
    const user = userEvent.setup({ applyAccept: false })
    render(<Harness />)

    await user.upload(screen.getByLabelText('Logo'), file)
    expect(await screen.findByText(message)).toBeInTheDocument()
  }, SLOW)

  it.each([
    ['storage refuses the bytes', { putStatus: 500 }],
    ['the API rejects the file', { complete: false }],
  ])('keeps the old picture when %s', async (_, options) => {
    mockUpload(options)
    const user = userEvent.setup()
    render(<Harness initial={{ fileId: 'old', url: 'https://storage.test/old' }} />)

    await user.upload(screen.getByLabelText('Logo'), picture())
    expect(await screen.findByText(f.uploadFailed)).toBeInTheDocument()
    expect(screen.getByAltText(f.preview)).toHaveAttribute('src', 'https://storage.test/old')
  }, SLOW)

  it('shows an error when no upload link can be had', async () => {
    server.use(http.post('*/api/v1/files/uploads', () => problem(500, 'internal')))
    const user = userEvent.setup()
    render(<Harness />)

    await user.upload(screen.getByLabelText('Logo'), picture())
    expect(await screen.findByText(f.uploadFailed)).toBeInTheDocument()
  }, SLOW)
})
