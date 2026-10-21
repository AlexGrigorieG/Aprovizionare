import { Fragment, useState } from 'react'
import { Link } from 'react-router-dom'
import { api, useApi } from '../api'
import { useAuth } from '../auth-context'
import { Badge, Empty, ErrorBox, Flash, Loading, PageHeader, Progress, Segmented, StatusBadge } from '../components/ui'
import { ORDER_STATUS, formatDate, formatMoney, formatQuantity, isOpenOrder } from '../format'
import { ROLES, canAccess } from '../roles'

const sum = (lines, pick) => lines.reduce((total, line) => total + pick(line), 0)

export default function OrdersPage() {
  const { user } = useAuth()
  const { data: orders, error, loading, reload } = useApi('/purchase-orders')
  const [filter, setFilter] = useState('open')
  const [expanded, setExpanded] = useState(null)
  const [actionError, setActionError] = useState(null)

  const canEdit = canAccess(user.roles, [ROLES.Purchasing, ROLES.Admin])
  const showPrices = !user.roles.includes(ROLES.Warehouse)

  const all = orders ?? []
  const filters = {
    open: all.filter(isOpenOrder),
    late: all.filter((o) => o.isLate),
    all,
  }
  const visible = filters[filter]

  async function cancel(order) {
    setActionError(null)
    try {
      await api(`/purchase-orders/${order.id}/cancel`, { method: 'POST' })
      reload()
    } catch (err) {
      setActionError(err)
    }
  }

  return (
    <section className="page">
      <PageHeader
        title="Comenzi de aprovizionare"
        subtitle={showPrices ? 'Ce s-a comandat, de la cine și cât a sosit până acum.' : 'Marfa care urmează să sosească în depozit.'}
        action={canEdit && <Link className="btn btn-primary" to="/comenzi/noua">Comandă nouă</Link>}
      />
      <Flash />

      <div className="toolbar">
        <Segmented
          value={filter}
          onChange={setFilter}
          options={[
            { value: 'open', label: 'Deschise', count: filters.open.length },
            { value: 'late', label: 'Întârziate', count: filters.late.length },
            { value: 'all', label: 'Toate', count: filters.all.length },
          ]}
        />
      </div>

      <ErrorBox error={error || actionError} />

      {loading ? (
        <Loading />
      ) : visible.length === 0 ? (
        <Empty>Nu există comenzi în această listă.</Empty>
      ) : (
        <div className="table-wrap">
          <table>
            <thead>
              <tr>
                <th>Număr</th>
                <th>Furnizor</th>
                <th>Comandată</th>
                <th>Livrare estimată</th>
                <th>Status</th>
                <th className="num">Recepționat</th>
                {showPrices && <th className="num">Valoare</th>}
                <th />
              </tr>
            </thead>
            <tbody>
              {visible.map((order) => {
                const ordered = sum(order.lines, (l) => l.quantityOrdered)
                const received = sum(order.lines, (l) => Math.min(l.quantityReceived, l.quantityOrdered))
                const isExpanded = expanded === order.id
                return (
                  <Fragment key={order.id}>
                    <tr className="clickable" onClick={() => setExpanded(isExpanded ? null : order.id)}>
                      <td className="strong">{order.number}</td>
                      <td>{order.supplierName}</td>
                      <td>{formatDate(order.orderDate)}</td>
                      <td>
                        {formatDate(order.expectedDeliveryDate)}
                        {order.isLate && <> <Badge tone="danger">Întârziată</Badge></>}
                      </td>
                      <td><StatusBadge status={order.status} map={ORDER_STATUS} /></td>
                      <td className="num">
                        <Progress value={received} max={ordered} />
                      </td>
                      {showPrices && (
                        <td className="num">{formatMoney(sum(order.lines, (l) => l.quantityOrdered * l.unitPrice), order.currency)}</td>
                      )}
                      <td className="num">
                        {canEdit && order.status === 'Sent' && (
                          <button
                            type="button"
                            className="btn btn-ghost btn-small"
                            onClick={(e) => {
                              e.stopPropagation()
                              cancel(order)
                            }}
                          >
                            Anulează
                          </button>
                        )}
                      </td>
                    </tr>
                    {isExpanded && (
                      <tr className="detail-row">
                        <td colSpan={showPrices ? 8 : 7}>
                          <table className="inner">
                            <thead>
                              <tr>
                                <th>Produs</th>
                                <th className="num">Comandat</th>
                                <th className="num">Recepționat</th>
                                <th className="num">Rămas</th>
                                {showPrices && <th className="num">Preț unitar</th>}
                              </tr>
                            </thead>
                            <tbody>
                              {order.lines.map((line) => (
                                <tr key={line.id}>
                                  <td>{line.productName}</td>
                                  <td className="num">{formatQuantity(line.quantityOrdered)} {line.unit}</td>
                                  <td className="num">{formatQuantity(line.quantityReceived)} {line.unit}</td>
                                  <td className="num">{formatQuantity(Math.max(0, line.quantityOrdered - line.quantityReceived))} {line.unit}</td>
                                  {showPrices && <td className="num">{formatMoney(line.unitPrice, order.currency)}</td>}
                                </tr>
                              ))}
                            </tbody>
                          </table>
                        </td>
                      </tr>
                    )}
                  </Fragment>
                )
              })}
            </tbody>
          </table>
        </div>
      )}
    </section>
  )
}
