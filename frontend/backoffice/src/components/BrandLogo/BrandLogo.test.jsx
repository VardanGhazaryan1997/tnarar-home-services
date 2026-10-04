import { render, screen } from '@testing-library/react'
import { BRAND } from '@/theme/theme'
import BrandLogo from './BrandLogo'

describe('BrandLogo', () => {
  it('shows the mark and the Armenian wordmark with an accessible name', () => {
    render(<BrandLogo />)

    const logo = screen.getByRole('img', { name: 'ՏնաՇեն' })
    expect(logo.querySelector('svg')).toBeInTheDocument()
    expect(logo).toHaveTextContent('ՏնաՇեն')
    expect(screen.getByText('Շեն')).toHaveStyle({ color: BRAND.tuffStone })
  })

  it('uses light colors on dark backgrounds', () => {
    render(<BrandLogo tone="dark" />)

    expect(screen.getByText('Տնա')).toHaveStyle({ color: '#ffffff' })
    expect(screen.getByText('Շեն')).toHaveStyle({ color: BRAND.tuffLight })
  })

  it('can show the mark alone', () => {
    render(<BrandLogo compact size={24} />)

    const logo = screen.getByRole('img', { name: 'ՏնաՇեն' })
    expect(logo).not.toHaveTextContent('Տնա')
    expect(logo.querySelector('svg')).toHaveAttribute('width', '24')
  })
})
