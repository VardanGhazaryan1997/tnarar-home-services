import { useTranslation } from 'react-i18next'
import Card from '@/components/ui/Card/Card'
import Button from '@/components/ui/Button/Button'
import { SelectField, TextField } from '@/components/ui/Field/Field'
import Icon from '@/components/ui/Icon/Icon'
import Segmented from '@/components/ui/Segmented/Segmented'
import { intlLocale } from '@/i18n/languages'
import { formatMoney } from '@/shared/format'
import { LIMITS, floorArea, lineQuantity, newOpening, roomProblems } from './draft'
import { ROOM_TYPES } from './estimatorApi'
import styles from './estimator.module.scss'

/**
 * One room of the estimate: its name and type, size (length × width or the floor area, plus the height), doors and
 * windows (they're taken off the wall area), and the work wanted with each line's quantity and price range.
 * `measured` is the room as the server priced it (null while the sizes aren't complete).
 */
export default function RoomCard({ room, measured, workItems, onChange, onRemove, onMove, onAddWork, first, last }) {
  const { t, i18n } = useTranslation()
  const lng = i18n.language
  const problems = roomProblems(room)
  const number = (value) => new Intl.NumberFormat(intlLocale(lng), { maximumFractionDigits: 2 }).format(value)
  const set = (patch) => onChange({ ...room, ...patch })
  const setOpening = (index, patch) => set({ openings: room.openings.map((opening, i) => (i === index ? { ...opening, ...patch } : opening)) })
  const setLine = (index, patch) => set({ lines: room.lines.map((line, i) => (i === index ? { ...line, ...patch } : line)) })
  const area = floorArea(room)
  const error = (key) => (key ? t(key, LIMITS) : undefined)
  const hasPrice = measured && measured.totalTypical > 0

  return (
    <Card
      className={styles.room}
      aria-label={room.name || t(`estimator.roomTypes.${room.type}`)}
      title={room.name || t(`estimator.roomTypes.${room.type}`)}
      actions={
        <div className={styles['room__tools']}>
          <Button variant="ghost" size="sm" icon={<Icon name="chevronUp" />} aria-label={t('estimator.room.moveUp')} disabled={first} onClick={() => onMove(-1)} />
          <Button variant="ghost" size="sm" icon={<Icon name="chevronDown" />} aria-label={t('estimator.room.moveDown')} disabled={last} onClick={() => onMove(1)} />
          <Button variant="ghost" size="sm" icon={<Icon name="trash" />} aria-label={t('estimator.room.remove')} onClick={onRemove} />
        </div>
      }
    >
      <div className={styles['room__body']}>
        <div className={styles['room__grid']}>
          <TextField label={t('estimator.room.name')} value={room.name} maxLength={LIMITS.name} onChange={(event) => set({ name: event.target.value })} />
          <SelectField
            label={t('estimator.roomType')}
            value={room.type}
            onChange={(event) => set({ type: event.target.value })}
            options={ROOM_TYPES.map((type) => ({ value: type, label: t(`estimator.roomTypes.${type}`) }))}
          />
        </div>

        <fieldset className={styles['room__group']}>
          <legend className={styles['room__legend']}>{t('estimator.room.size')}</legend>
          <Segmented
            label={t('estimator.room.sizeMode')}
            value={room.sizeMode}
            onChange={(sizeMode) => set({ sizeMode })}
            options={[
              { value: 'sides', label: t('estimator.room.bySides') },
              { value: 'area', label: t('estimator.room.byArea') },
            ]}
          />
          <div className={styles['room__sizes']}>
            {room.sizeMode === 'sides' ? (
              <>
                <TextField label={t('estimator.room.length')} inputMode="decimal" autoComplete="off" value={room.length} error={error(problems.length)} onChange={(event) => set({ length: event.target.value })} />
                <TextField label={t('estimator.room.width')} inputMode="decimal" autoComplete="off" value={room.width} error={error(problems.width)} onChange={(event) => set({ width: event.target.value })} />
              </>
            ) : (
              <TextField label={t('estimator.area')} inputMode="decimal" autoComplete="off" value={room.area} error={error(problems.area)} onChange={(event) => set({ area: event.target.value })} />
            )}
            <TextField
              label={t('estimator.room.height')}
              inputMode="decimal"
              autoComplete="off"
              placeholder={number(LIMITS.defaultHeight)}
              value={room.height}
              error={error(problems.height)}
              onChange={(event) => set({ height: event.target.value })}
            />
          </div>
          {measured && (
            <p className={styles['room__geometry']}>
              {t('estimator.room.geometry', {
                floor: number(measured.floorArea),
                walls: number(measured.wallArea),
                perimeter: number(measured.perimeter),
              })}
            </p>
          )}
        </fieldset>

        <fieldset className={styles['room__group']}>
          <legend className={styles['room__legend']}>{t('estimator.room.openings')}</legend>
          {room.openings.length === 0 && <p className={styles['room__muted']}>{t('estimator.room.noOpenings')}</p>}
          {room.openings.map((opening, index) => {
            const found = problems.openings?.[index] ?? {}
            const kind = t(`estimator.openings.${opening.kind}`)
            return (
              <div key={opening.key} className={styles['room__opening']} role="group" aria-label={`${kind} ${index + 1}`}>
                <span className={styles['room__opening-kind']}>
                  <Icon name={opening.kind === 'Door' ? 'door' : 'window'} size={18} />
                  {kind}
                </span>
                <TextField label={t('estimator.room.openingWidth')} inputMode="decimal" autoComplete="off" value={opening.width} error={error(found.width)} onChange={(event) => setOpening(index, { width: event.target.value })} />
                <TextField label={t('estimator.room.openingHeight')} inputMode="decimal" autoComplete="off" value={opening.height} error={error(found.height)} onChange={(event) => setOpening(index, { height: event.target.value })} />
                <TextField label={t('estimator.room.openingCount')} inputMode="numeric" autoComplete="off" value={opening.count} error={error(found.count)} onChange={(event) => setOpening(index, { count: event.target.value })} />
                <Button
                  variant="ghost"
                  size="sm"
                  icon={<Icon name="close" />}
                  aria-label={t('estimator.room.removeOpening', { kind })}
                  onClick={() => set({ openings: room.openings.filter((_, i) => i !== index) })}
                />
              </div>
            )
          })}
          <div className={styles['room__add']}>
            {['Door', 'Window'].map((kind) => (
              <Button
                key={kind}
                variant="secondary"
                size="sm"
                icon={<Icon name="plus" />}
                disabled={room.openings.length >= LIMITS.openings}
                onClick={() => set({ openings: [...room.openings, newOpening(kind)] })}
              >
                {t(`estimator.room.add${kind}`)}
              </Button>
            ))}
          </div>
        </fieldset>

        <fieldset className={styles['room__group']}>
          <legend className={styles['room__legend']}>{t('estimator.room.work')}</legend>
          {room.lines.length === 0 && <p className={styles['room__muted']}>{t('estimator.room.noWork')}</p>}
          <ul className={styles['room__lines']}>
            {room.lines.map((line, index) => {
              const item = workItems.get(line.workItemId)
              const result = measured?.lines.find((measuredLine) => measuredLine.workItemId === line.workItemId)
              const name = result?.name ?? item?.name ?? t('estimator.room.unknownWork')
              const auto = lineQuantity({ ...line, quantity: '' }, room) ?? result?.measuredQuantity
              const unit = item?.unit ?? result?.unit
              return (
                <li key={line.workItemId} className={styles['room__line']}>
                  <div className={styles['room__line-name']}>
                    <span>{name}</span>
                    {!item && <span className={styles['room__warning']}>{t('estimator.room.gone')}</span>}
                  </div>
                  <TextField
                    className={styles['room__quantity']}
                    label={t('estimator.room.quantity', { unit: unit ? t(`estimator.units.${unit}`) : '' })}
                    inputMode="decimal"
                    autoComplete="off"
                    placeholder={auto != null ? number(auto) : t('estimator.room.enterQuantity')}
                    value={line.quantity}
                    error={error(problems.lines?.[index])}
                    onChange={(event) => setLine(index, { quantity: event.target.value })}
                  />
                  <span className={styles['room__line-price']}>
                    {result?.priceTypical != null
                      ? `${formatMoney(result.priceMin, lng)} – ${formatMoney(result.priceMax, lng)}`
                      : result?.needsQuantity
                        ? t('estimator.room.needsQuantity')
                        : result
                          ? t('estimator.room.noPrice')
                          : ''}
                  </span>
                  <Button
                    variant="ghost"
                    size="sm"
                    icon={<Icon name="close" />}
                    aria-label={t('estimator.room.removeWork', { name })}
                    onClick={() => set({ lines: room.lines.filter((_, i) => i !== index) })}
                  />
                </li>
              )
            })}
          </ul>
          <div className={styles['room__add']}>
            <Button variant="secondary" size="sm" icon={<Icon name="plus" />} disabled={room.lines.length >= LIMITS.lines} onClick={onAddWork}>
              {t('estimator.room.addWork')}
            </Button>
          </div>
        </fieldset>

        <p className={styles['room__total']}>
          {hasPrice
            ? t('estimator.room.total', { range: `${formatMoney(measured.totalMin, lng)} – ${formatMoney(measured.totalMax, lng)}` })
            : area == null
              ? t('estimator.room.needsSize')
              : ''}
        </p>
      </div>
    </Card>
  )
}
