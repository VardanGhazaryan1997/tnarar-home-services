import { App as AntApp, ConfigProvider } from 'antd'
import { I18nextProvider, useTranslation } from 'react-i18next'
import i18n, { getAntdLocale } from '@/i18n'
import { theme } from '@/theme/theme'

function ThemedApp({ children }) {
  const { i18n: current } = useTranslation()
  return (
    <ConfigProvider theme={theme} locale={getAntdLocale(current.resolvedLanguage)}>
      <AntApp>{children}</AntApp>
    </ConfigProvider>
  )
}

/** i18n + Ant Design theme and locale. The store is provided separately. */
export default function AppProviders({ children }) {
  return (
    <I18nextProvider i18n={i18n}>
      <ThemedApp>{children}</ThemedApp>
    </I18nextProvider>
  )
}
