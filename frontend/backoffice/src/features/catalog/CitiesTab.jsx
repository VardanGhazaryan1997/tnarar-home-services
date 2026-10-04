import { EditOutlined, EyeInvisibleOutlined, EyeOutlined, PlusOutlined } from '@ant-design/icons'
import { Alert, App, Button, Flex, Input, Select, Space, Table, Tag, Tooltip, Typography } from 'antd'
import { useMemo, useState } from 'react'
import { useTranslation } from 'react-i18next'
import { errorMessage } from '@/api/errors'
import {
  useAddDistrictMutation,
  useCreateCityMutation,
  useGetCitiesQuery,
  useGetRegionsQuery,
  useSetCityActiveMutation,
  useSetDistrictActiveMutation,
  useUpdateCityMutation,
  useUpdateDistrictMutation,
} from './catalogApi'
import { localizedName } from './names'
import PlaceFormModal from './PlaceFormModal'
import { useCatalogLanguages } from './useCatalogLanguages'

/**
 * Towns and villages, by region, with their districts. Filter by region or search by name; places and districts are
 * hidden, not deleted.
 */
export default function CitiesTab() {
  const { t, i18n } = useTranslation()
  const { message } = App.useApp()
  const { defaultCode } = useCatalogLanguages()
  const { data = [], isLoading, error } = useGetCitiesQuery()
  const { data: regions = [] } = useGetRegionsQuery()
  const [regionFilter, setRegionFilter] = useState(null)
  const [search, setSearch] = useState('')
  const [createCity] = useCreateCityMutation()
  const [updateCity] = useUpdateCityMutation()
  const [setCityActive] = useSetCityActiveMutation()
  const [addDistrict] = useAddDistrictMutation()
  const [updateDistrict] = useUpdateDistrictMutation()
  const [setDistrictActive] = useSetDistrictActiveMutation()
  // { kind: 'city' | 'district', city, place } while a dialog is open.
  const [dialog, setDialog] = useState(null)

  const nameOf = (place) => localizedName(place.name, i18n.resolvedLanguage, defaultCode)
  const regionById = useMemo(() => new Map(regions.map((region) => [region.id, region])), [regions])

  const rows = useMemo(() => {
    const term = search.trim().toLocaleLowerCase()
    return data.filter((city) => {
      if (regionFilter === 'none' ? city.regionId : regionFilter && city.regionId !== regionFilter) return false
      if (!term) return true
      return city.slug.includes(term) || Object.values(city.name).some((name) => name.toLocaleLowerCase().includes(term))
    })
  }, [data, regionFilter, search])

  const toggle = async (action, isActive) => {
    try {
      await action().unwrap()
      message.success(t(isActive ? 'catalog.shownDone' : 'catalog.hiddenDone'))
    } catch (failure) {
      message.error(errorMessage(t, failure))
    }
  }

  const save = (body) => {
    const { kind, city, place } = dialog
    if (kind === 'city') {
      return (place ? updateCity({ id: place.id, ...body }) : createCity(body)).unwrap()
    }
    return (place ? updateDistrict({ cityId: city.id, id: place.id, ...body }) : addDistrict({ cityId: city.id, ...body })).unwrap()
  }

  const dialogTitle = () => {
    if (!dialog) return ''
    if (dialog.kind === 'city') return t(dialog.place ? 'catalog.cities.edit' : 'catalog.cities.add')
    return t(dialog.place ? 'catalog.districts.edit' : 'catalog.districts.addTo', { city: nameOf(dialog.city) })
  }

  const nameColumn = {
    title: t('catalog.columns.name'),
    key: 'name',
    render: (_, place) => (
      <Flex vertical>
        <Typography.Text>{nameOf(place)}</Typography.Text>
        <Typography.Text type="secondary" style={{ fontSize: 12 }}>
          {place.slug}
        </Typography.Text>
      </Flex>
    ),
  }
  const sortColumn = { title: t('catalog.columns.sortOrder'), dataIndex: 'sortOrder', key: 'sortOrder', width: 90, responsive: ['sm'] }
  const statusColumn = {
    title: t('catalog.columns.status'),
    key: 'status',
    width: 110,
    render: (_, place) =>
      place.isActive ? <Tag color="green">{t('catalog.status.active')}</Tag> : <Tag>{t('catalog.status.hidden')}</Tag>,
  }

  const actionButtons = (place, { onEdit, onToggle }) => {
    const name = nameOf(place)
    const toggleLabel = t(place.isActive ? 'catalog.actions.hide' : 'catalog.actions.show')
    return (
      <Space size={0}>
        <Tooltip title={t('catalog.actions.edit')}>
          <Button type="text" icon={<EditOutlined />} aria-label={`${t('catalog.actions.edit')}: ${name}`} onClick={onEdit} />
        </Tooltip>
        <Tooltip title={toggleLabel}>
          <Button
            type="text"
            icon={place.isActive ? <EyeInvisibleOutlined /> : <EyeOutlined />}
            aria-label={`${toggleLabel}: ${name}`}
            onClick={onToggle}
          />
        </Tooltip>
      </Space>
    )
  }

  const cityColumns = [
    nameColumn,
    {
      title: t('catalog.cities.region'),
      key: 'region',
      responsive: ['sm'],
      render: (_, city) => (regionById.get(city.regionId) ? nameOf(regionById.get(city.regionId)) : '—'),
    },
    {
      title: t('catalog.cities.kind'),
      key: 'kind',
      width: 100,
      render: (_, city) => <Tag color={city.kind === 'Village' ? 'gold' : 'blue'}>{t(`catalog.cities.kinds.${city.kind ?? 'City'}`)}</Tag>,
    },
    {
      title: t('catalog.cities.districtCount'),
      key: 'districts',
      width: 110,
      responsive: ['md'],
      render: (_, city) => city.districts.length,
    },
    sortColumn,
    statusColumn,
    {
      title: t('catalog.columns.actions'),
      key: 'actions',
      align: 'right',
      render: (_, city) =>
        actionButtons(city, {
          onEdit: () => setDialog({ kind: 'city', city, place: city }),
          onToggle: () => toggle(() => setCityActive({ id: city.id, isActive: !city.isActive }), !city.isActive),
        }),
    },
  ]

  const districtTable = (city) => (
    <Flex vertical gap="small">
      <Table
        rowKey="id"
        size="small"
        pagination={false}
        dataSource={city.districts}
        locale={{ emptyText: t('catalog.districts.empty') }}
        scroll={{ x: 'max-content' }}
        columns={[
          nameColumn,
          sortColumn,
          statusColumn,
          {
            title: t('catalog.columns.actions'),
            key: 'actions',
            align: 'right',
            render: (_, district) =>
              actionButtons(district, {
                onEdit: () => setDialog({ kind: 'district', city, place: district }),
                onToggle: () =>
                  toggle(
                    () => setDistrictActive({ cityId: city.id, id: district.id, isActive: !district.isActive }),
                    !district.isActive,
                  ),
              }),
          },
        ]}
      />
      <div>
        <Button icon={<PlusOutlined />} onClick={() => setDialog({ kind: 'district', city, place: null })}>
          {t('catalog.districts.addTo', { city: nameOf(city) })}
        </Button>
      </div>
    </Flex>
  )

  return (
    <>
      <Flex justify="space-between" align="center" gap="middle" wrap style={{ marginBottom: 16 }}>
        <Typography.Text type="secondary">{t('catalog.cities.help')}</Typography.Text>
        <Button type="primary" icon={<PlusOutlined />} onClick={() => setDialog({ kind: 'city', city: null, place: null })}>
          {t('catalog.cities.add')}
        </Button>
      </Flex>
      <Flex gap="middle" wrap style={{ marginBottom: 16 }}>
        <Select
          aria-label={t('catalog.cities.region')}
          style={{ minWidth: 220 }}
          value={regionFilter}
          onChange={setRegionFilter}
          options={[
            { value: null, label: t('catalog.cities.allRegions') },
            ...regions.map((region) => ({ value: region.id, label: nameOf(region) })),
            { value: 'none', label: t('catalog.cities.noRegion') },
          ]}
        />
        <Input.Search
          allowClear
          aria-label={t('catalog.cities.search')}
          placeholder={t('catalog.cities.search')}
          style={{ maxWidth: 280 }}
          value={search}
          onChange={(event) => setSearch(event.target.value)}
        />
      </Flex>
      {error && <Alert type="error" showIcon title={errorMessage(t, error)} style={{ marginBottom: 16 }} />}
      <Table
        rowKey="id"
        columns={cityColumns}
        dataSource={rows}
        loading={isLoading}
        pagination={{ pageSize: 50, hideOnSinglePage: true, showSizeChanger: false }}
        expandable={{ expandedRowRender: districtTable }}
        scroll={{ x: 'max-content' }}
        locale={{ emptyText: t('catalog.cities.empty') }}
      />
      <PlaceFormModal
        open={Boolean(dialog)}
        title={dialogTitle()}
        place={dialog?.place ?? null}
        regions={dialog?.kind === 'city' ? regions : undefined}
        regionName={nameOf}
        onSave={save}
        onClose={() => setDialog(null)}
      />
    </>
  )
}
