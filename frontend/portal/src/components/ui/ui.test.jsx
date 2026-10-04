import { render, screen } from '@testing-library/react'
import userEvent from '@testing-library/user-event'
import { useState } from 'react'
import { createMemoryRouter, RouterProvider } from 'react-router'
import Alert from './Alert/Alert'
import Button from './Button/Button'
import Card from './Card/Card'
import EmptyState from './EmptyState/EmptyState'
import { SelectField, TextField } from './Field/Field'
import Icon from './Icon/Icon'
import { ICON_NAMES } from './Icon/icons'
import Modal from './Modal/Modal'
import Spinner from './Spinner/Spinner'
import Tag from './Tag/Tag'

const inRouter = (element) => render(<RouterProvider router={createMemoryRouter([{ path: '/', element }])} />)

describe('Button', () => {
  it('renders a button with variant classes and handles clicks', async () => {
    const onClick = vi.fn()
    render(
      <Button variant="accent" size="lg" block onClick={onClick}>
        Send
      </Button>,
    )

    const button = screen.getByRole('button', { name: 'Send' })
    expect(button).toHaveClass('button', 'button--accent', 'button--lg', 'button--block')
    expect(button).toHaveAttribute('type', 'button')
    await userEvent.click(button)
    expect(onClick).toHaveBeenCalledOnce()
  })

  it('is disabled and busy while loading', () => {
    render(<Button loading>Save</Button>)
    const button = screen.getByRole('button', { name: 'Save' })
    expect(button).toBeDisabled()
    expect(button).toHaveAttribute('aria-busy', 'true')
  })

  it('renders a link with `to` and an icon-only button with a label', () => {
    inRouter(
      <>
        <Button to="/hy/requests">Requests</Button>
        <Button icon={<Icon name="close" />} aria-label="Close" variant="ghost" />
      </>,
    )
    expect(screen.getByRole('link', { name: 'Requests' })).toHaveAttribute('href', '/hy/requests')
    expect(screen.getByRole('button', { name: 'Close' })).toHaveClass('button--icon-only')
  })
})

describe('Fields', () => {
  it('connects the label, hint and error to the input', () => {
    render(<TextField label="Name" hint="As on your ID" error="Required" optionalText="optional" />)
    const input = screen.getByLabelText(/Name/)
    expect(input).toHaveAttribute('aria-invalid', 'true')
    expect(input).toHaveAccessibleDescription('As on your ID Required')
    expect(screen.getByText('(optional)')).toBeInTheDocument()
  })

  it('renders a textarea and a select with a placeholder', async () => {
    const onChange = vi.fn()
    render(
      <>
        <TextField label="About" multiline required />
        <SelectField label="City" placeholder="Choose" options={[{ value: 'y', label: 'Yerevan' }]} onChange={onChange} />
      </>,
    )
    expect(screen.getByLabelText('About').tagName).toBe('TEXTAREA')
    expect(screen.getByLabelText('About')).toBeRequired()
    await userEvent.selectOptions(screen.getByLabelText('City'), 'y')
    expect(onChange).toHaveBeenCalled()
    expect(screen.getByRole('option', { name: 'Choose' })).toHaveValue('')
  })
})

describe('Modal', () => {
  function Example() {
    const [open, setOpen] = useState(false)
    return (
      <>
        <button type="button" onClick={() => setOpen(true)}>
          Open
        </button>
        <Modal open={open} title="Decline request" onClose={() => setOpen(false)} footer={<button type="button">OK</button>}>
          <p>Why?</p>
        </Modal>
      </>
    )
  }

  it('opens as a labelled dialog and closes with Escape, the close button or the backdrop', async () => {
    const user = userEvent.setup()
    render(<Example />)

    await user.click(screen.getByRole('button', { name: 'Open' }))
    const dialog = screen.getByRole('dialog', { name: 'Decline request' })
    expect(dialog).toHaveFocus()
    expect(screen.getByRole('button', { name: 'OK' })).toBeInTheDocument()
    await user.keyboard('{Escape}')
    expect(screen.queryByRole('dialog')).not.toBeInTheDocument()
    expect(screen.getByRole('button', { name: 'Open' })).toHaveFocus()

    await user.click(screen.getByRole('button', { name: 'Open' }))
    await user.click(screen.getByRole('button', { name: 'Փակել' }))
    expect(screen.queryByRole('dialog')).not.toBeInTheDocument()

    await user.click(screen.getByRole('button', { name: 'Open' }))
    await user.click(screen.getByRole('dialog').parentElement)
    expect(screen.queryByRole('dialog')).not.toBeInTheDocument()
  })
})

describe('Small pieces', () => {
  it('renders cards, tags, alerts, spinners and empty states', () => {
    render(
      <>
        <Card title="Offers" actions={<Button size="sm">Add</Button>}>
          body
        </Card>
        <Tag tone="success">Accepted</Tag>
        <Alert tone="danger" title="Failed">
          Try again
        </Alert>
        <Alert title="Saved" />
        <Spinner label="Loading" />
        <Spinner />
        <EmptyState title="No requests yet" description="Create one" action={<Button>New</Button>} />
      </>,
    )
    expect(screen.getByRole('heading', { name: 'Offers' })).toBeInTheDocument()
    expect(screen.getByText('Accepted')).toHaveClass('tag--success')
    expect(screen.getByRole('alert')).toHaveTextContent('FailedTry again')
    expect(screen.getByRole('status', { name: 'Loading' })).toBeInTheDocument()
    expect(screen.getAllByRole('status')).toHaveLength(2)
    expect(screen.getByText('No requests yet')).toBeInTheDocument()
  })

  it('draws every icon, labelled only when asked', () => {
    render(
      <>
        {ICON_NAMES.map((name) => (
          <Icon key={name} name={name} />
        ))}
        <Icon name="home" label="Home" />
      </>,
    )
    expect(screen.getByRole('img', { name: 'Home' })).toBeInTheDocument()
    expect(document.querySelectorAll('svg[aria-hidden="true"]')).toHaveLength(ICON_NAMES.length)
  })
})
