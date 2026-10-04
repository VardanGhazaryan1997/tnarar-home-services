import { NavLink } from 'react-router'
import { bem } from '@/shared/bem'
import styles from './SectionTabs.module.scss'

const b = bem(styles)

/** Links between sibling pages of a section, shown as tabs. `items`: [{ to, label, end }]. */
export default function SectionTabs({ label, items }) {
  return (
    <nav className={styles['section-tabs']} aria-label={label}>
      {items.map((item) => (
        <NavLink key={item.to} to={item.to} end={item.end} className={({ isActive }) => b('section-tabs__link', { active: isActive })}>
          {item.label}
        </NavLink>
      ))}
    </nav>
  )
}
