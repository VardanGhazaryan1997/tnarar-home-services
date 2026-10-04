import { GlobalOutlined } from '@ant-design/icons'
import { Select } from 'antd'
import { useTranslation } from 'react-i18next'
import { useGetLanguagesQuery } from '@/api/languagesApi'
import { BUNDLED_LANGUAGES, NATIVE_NAMES } from '@/i18n/languages'

const BUNDLED_OPTIONS = BUNDLED_LANGUAGES.map((code) => ({ value: code, label: NATIVE_NAMES[code] }))

/**
 * Changes the Back Office language. Lists the active languages from the API, or the
 * bundled ones if it can't be reached. The choice is remembered on this device.
 */
export default function LanguageSwitcher() {
  const { t, i18n } = useTranslation()
  const { data } = useGetLanguagesQuery()
  const options = data?.length ? data.map((l) => ({ value: l.code, label: l.nativeName })) : BUNDLED_OPTIONS

  return (
    <Select
      aria-label={t('languageSwitcher.label')}
      prefix={<GlobalOutlined />}
      value={i18n.resolvedLanguage}
      options={options}
      onChange={(lng) => i18n.changeLanguage(lng)}
      popupMatchSelectWidth={false}
      variant="borderless"
    />
  )
}
