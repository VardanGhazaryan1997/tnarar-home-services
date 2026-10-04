import { render, screen } from '@testing-library/react'
import hy from '@/i18n/locales/hy/common.json'
import App from './App'

describe('App', () => {
  it('boots with store, i18n and router and shows the home page', async () => {
    window.history.pushState({}, '', '/hy')
    render(<App />)
    expect(await screen.findByRole('heading', { level: 1, name: hy.home.title })).toBeInTheDocument()
  })
})
