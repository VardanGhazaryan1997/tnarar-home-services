import { useTranslation } from 'react-i18next'
import { useLocation, useNavigate, useParams } from 'react-router'
import { useAvailableLanguages } from '@/i18n/hooks'
import { switchLanguagePath } from '@/i18n/languages'
import styles from './LanguageSwitcher.module.scss'

/** Opens the same page in another language. The choice is remembered for the next visit. Use under "/:lng". */
export default function LanguageSwitcher() {
  const { t } = useTranslation()
  const { lng } = useParams()
  const { languages } = useAvailableLanguages()
  const navigate = useNavigate()
  const location = useLocation()

  const handleChange = (event) => {
    navigate(`${switchLanguagePath(location.pathname, event.target.value)}${location.search}${location.hash}`)
  }

  return (
    <label className={styles['language-switcher']}>
      <span className={styles['language-switcher__label']}>{t('languageSwitcher.label')}</span>
      <select className={styles['language-switcher__select']} value={lng} onChange={handleChange}>
        {languages.map((language) => (
          <option key={language.code} value={language.code} lang={language.code}>
            {language.nativeName}
          </option>
        ))}
      </select>
    </label>
  )
}
