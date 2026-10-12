import { useWhereToBuy } from '@/features/stores/storesApi'
import { whereToBuyRequest } from './storeOffers'

/**
 * Where to buy the materials of a measurement (E2): one request for the cheapest offer of each material and each
 * store's total. It is extra information, so while loading and when it fails this is undefined.
 */
export default function useWhereToBuyFor(measurement) {
  const request = whereToBuyRequest(measurement)
  const result = useWhereToBuy(request, { skip: !request })
  return request && result.isSuccess ? result.data : undefined
}
