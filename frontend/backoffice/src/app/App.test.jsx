import { render, screen } from '@testing-library/react'
import hy from '@/i18n/locales/hy/common.json'
import App from './App'

describe('App', () => {
  it('boots with store, theme, i18n and router and shows the dashboard', async () => {
    window.history.pushState({}, '', '/')
    render(<App />)
    expect(await screen.findByRole('heading', { level: 1, name: hy.dashboard.title })).toBeInTheDocument()
  })
})
