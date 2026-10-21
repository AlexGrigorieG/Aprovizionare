const number = (min, max) =>
  new Intl.NumberFormat('ro-RO', { minimumFractionDigits: min, maximumFractionDigits: max })

const quantityFormat = number(0, 3)
const moneyFormat = number(2, 2)
const rateFormat = number(4, 4)

export const formatQuantity = (value) => quantityFormat.format(value ?? 0)

export const formatMoney = (value, currency = 'RON') => `${moneyFormat.format(value ?? 0)} ${currency}`

export const formatRate = (value) => rateFormat.format(value)

export function formatDate(iso) {
  if (!iso) return ''
  const [year, month, day] = iso.slice(0, 10).split('-')
  return `${day}.${month}.${year}`
}

export const formatDateTime = (iso) =>
  new Date(iso).toLocaleString('ro-RO', { dateStyle: 'short', timeStyle: 'short' })

export const todayIso = () => new Date().toLocaleDateString('sv-SE')

export const ORDER_STATUS = {
  Sent: { label: 'Trimisă', tone: 'info' },
  PartiallyReceived: { label: 'Recepționată parțial', tone: 'warning' },
  Received: { label: 'Recepționată', tone: 'success' },
  Cancelled: { label: 'Anulată', tone: 'muted' },
}

export const INVOICE_STATUS = {
  Registered: { label: 'Înregistrată', tone: 'info' },
  Validated: { label: 'Validată', tone: 'info' },
  Blocked: { label: 'Blocată', tone: 'danger' },
  ApprovedForPayment: { label: 'Aprobată la plată', tone: 'warning' },
  Paid: { label: 'Plătită', tone: 'success' },
  Rejected: { label: 'Respinsă', tone: 'muted' },
}

export const isOpenOrder = (order) => order.status === 'Sent' || order.status === 'PartiallyReceived'
