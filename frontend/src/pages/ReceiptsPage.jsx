import { Fragment, useState } from 'react'
import { Link } from 'react-router-dom'
import { useApi } from '../api'
import { useAuth } from '../auth-context'
import { Badge, Empty, ErrorBox, Flash, Loading, PageHeader, Segmented } from '../components/ui'
import { formatDate, formatQuantity } from '../format'
import { ROLES, canAccess } from '../roles'

// Un produs primit peste comandă e împărțit în două linii (pe comandă + surplus),
// așa că lista numără produsele distincte, iar detaliul arată liniile.
const countProducts = (receipt) => new Set(receipt.lines.map((line) => line.productId)).size

export default function ReceiptsPage() {
  const { user } = useAuth()
  const { data: receipts, error, loading } = useApi('/goods-receipts')
  const [filter, setFilter] = useState('all')
  const [expanded, setExpanded] = useState(null)

  const canEdit = canAccess(user.roles, [ROLES.Warehouse, ROLES.Admin])
  const all = receipts ?? []
  const toClarify = all.filter((r) => r.hasUnorderedItems)
  const visible = filter === 'clarify' ? toClarify : all

  return (
    <section className="page">
      <PageHeader
        title="Recepții"
        subtitle="Ce a intrat efectiv în depozit, pe tranșe."
        action={canEdit && <Link className="btn btn-primary" to="/receptii/noua">Recepție nouă</Link>}
      />
      <Flash />

      <div className="toolbar">
        <Segmented
          value={filter}
          onChange={setFilter}
          options={[
            { value: 'all', label: 'Toate', count: all.length },
            { value: 'clarify', label: 'De clarificat', count: toClarify.length },
          ]}
        />
      </div>

      <ErrorBox error={error} />

      {loading ? (
        <Loading />
      ) : visible.length === 0 ? (
        <Empty>Nu există recepții în această listă.</Empty>
      ) : (
        <div className="table-wrap">
          <table>
            <thead>
              <tr>
                <th>Nr.</th>
                <th>Data</th>
                <th>Furnizor</th>
                <th>Comandă</th>
                <th>Produse</th>
                <th>Observații</th>
              </tr>
            </thead>
            <tbody>
              {visible.map((receipt) => {
                const isExpanded = expanded === receipt.id
                return (
                  <Fragment key={receipt.id}>
                    <tr className="clickable" onClick={() => setExpanded(isExpanded ? null : receipt.id)}>
                      <td className="strong">R-{receipt.id}</td>
                      <td>{formatDate(receipt.receivedDate)}</td>
                      <td>{receipt.supplierName}</td>
                      <td>{receipt.purchaseOrderNumber ?? <span className="muted">fără comandă</span>}</td>
                      <td>
                        {countProducts(receipt)} {countProducts(receipt) === 1 ? 'produs' : 'produse'}
                        {receipt.hasUnorderedItems && <> <Badge tone="warning">De clarificat</Badge></>}
                      </td>
                      <td className="muted">{receipt.notes}</td>
                    </tr>
                    {isExpanded && (
                      <tr className="detail-row">
                        <td colSpan={6}>
                          <table className="inner">
                            <thead>
                              <tr>
                                <th>Produs</th>
                                <th className="num">Cantitate</th>
                                <th>Încadrare</th>
                              </tr>
                            </thead>
                            <tbody>
                              {receipt.lines.map((line) => (
                                <tr key={line.id}>
                                  <td>{line.productName}</td>
                                  <td className="num">{formatQuantity(line.quantityReceived)} {line.unit}</td>
                                  <td>
                                    {line.isUnordered ? (
                                      <Badge tone="warning">Necomandată sau peste comandă</Badge>
                                    ) : (
                                      <span className="muted">pe linia de comandă</span>
                                    )}
                                  </td>
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
