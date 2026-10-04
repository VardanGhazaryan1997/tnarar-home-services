import { Card, Flex, Typography } from 'antd'
import { useTranslation } from 'react-i18next'
import BrandLogo from '@/components/BrandLogo/BrandLogo'
import LanguageSwitcher from '@/components/LanguageSwitcher/LanguageSwitcher'

/** Centered card used by the sign-in pages, with the language switcher above it. */
export default function AuthCard({ title, children }) {
  const { t } = useTranslation()

  return (
    <Flex vertical align="center" justify="center" gap="middle" style={{ minHeight: '100vh', padding: 16 }}>
      <Flex justify="flex-end" style={{ width: '100%', maxWidth: 420 }}>
        <LanguageSwitcher />
      </Flex>
      <Card style={{ width: '100%', maxWidth: 420 }}>
        <Flex vertical gap={8}>
          <BrandLogo size={36} />
          <Typography.Text type="secondary">{t('app.name')}</Typography.Text>
        </Flex>
        <Typography.Title level={1} style={{ marginTop: 4 }}>
          {title}
        </Typography.Title>
        {children}
      </Card>
    </Flex>
  )
}
