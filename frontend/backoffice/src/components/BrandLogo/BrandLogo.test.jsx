import { render, screen } from '@testing-library/react'
import { BRAND } from '@/theme/theme'
import BrandLogo from './BrandLogo'

describe('BrandLogo', () => {
  it('shows the mark and the Armenian wordmark with an accessible name', () => {
    render(<BrandLogo />)

    const logo = screen.getByRole('img', { name: 'Tnarar' })
    expect(logo.querySelector('svg')).toBeInTheDocument()
    expect(logo).toHaveTextContent('Tnarar')
    expect(screen.getByText('rar')).toHaveStyle({ color: BRAND.tuffStone })
  })

  it('uses light colors on dark backgrounds', () => {
    render(<BrandLogo tone="dark" />)

    expect(screen.getByText('Tna')).toHaveStyle({ color: '#ffffff' })
    expect(screen.getByText('rar')).toHaveStyle({ color: BRAND.tuffLight })
  })

  it('can show the mark alone', () => {
    render(<BrandLogo compact size={24} />)

    const logo = screen.getByRole('img', { name: 'Tnarar' })
    expect(logo).not.toHaveTextContent('Tna')
    expect(logo.querySelector('svg')).toHaveAttribute('width', '24')
  })
})
