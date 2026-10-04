import { InputNumber } from 'antd'
import { MAX_PERCENT } from './commissionParts'

/** The percent field used for every rate: 0–50 with up to two decimals. */
export default function PercentInput(props) {
  return <InputNumber min={0} max={MAX_PERCENT} step={0.5} precision={2} suffix="%" style={{ width: '100%', maxWidth: 200 }} {...props} />
}
