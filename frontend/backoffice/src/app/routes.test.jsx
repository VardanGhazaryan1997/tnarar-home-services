import { screen } from '@testing-library/react'
import userEvent from '@testing-library/user-event'
import hy from '@/i18n/locales/hy/common.json'
import { renderRoute } from '@/test/renderWithProviders'

describe('routes', () => {
  it('shows the dashboard at /', async () => {
    renderRoute('/')
    expect(await screen.findByRole('heading', { level: 1, name: hy.dashboard.title })).toBeInTheDocument()
  })

  it('shows a 404 page for unknown URLs with a way back to the dashboard', async () => {
    const user = userEvent.setup()
    const { router } = renderRoute('/no-such-page')

    expect(await screen.findByText(hy.notFound.title)).toBeInTheDocument()
    await user.click(screen.getByRole('button', { name: hy.notFound.backHome }))

    expect(router.state.location.pathname).toBe('/')
  })
})
