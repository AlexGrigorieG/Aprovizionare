import { Fragment, useState } from 'react'
import { Link } from 'react-router-dom'
import { api, useApi } from '../api'
import { useAuth } from '../auth-context'
import { Empty, ErrorBox, Flash, Loading, PageHeader, Segmented, StatusBadge } from '../components/ui'
import { INVOICE_STATUS, formatDate, formatMoney, formatQuantity, formatRate } from '../format'
import { ROLES, canAccess } from '../roles'

const FILTERS = {
  todo: (i) => i.status === 'Blocked' || i.status === 'Validated' || i.status === 'ApprovedForPayment',
  blocked: (i) => i.status === 'Blocked',
  all: () => true,
}

export default function InvoicesPage() {
  const { user } = useAuth()
  const { data: invoices, error, loading, reload } = useApi('/invoices')
  const [filter, setFilter] = useState('todo')
  const [expanded, setExpanded] = useState(null)
  const [panel, setPanel] = useState(null)
  const [note, setNote] = useState('')
  const [actionError, setActionError] = useState(null)
  const [busy, setBusy] = useState(false)

  const canEdit = canAccess(user.roles, [ROLES.Accounting, ROLES.Admin])
  const all = invoices ?? []
  const visible = all.filter(FILTERS[filter])

  async function act(invoice, action, body) {
    setBusy(true)
    setActionError(null)
    try {
      await api(`/invoices/${invoice.id}/${action}`, { method: 'POST', body })
      setPanel(null)
      setNote('')
      reload()
    } catch (err) {
      setActionError(err)
    } finally {
      setBusy(false)
    }
  }

  function openPanel(invoice, kind) {
    setPanel({ id: invoice.id, kind })
    setNote('')
    setActionError(null)
  }

  return (
    <section className="page">
      <PageHeader
        title="Facturi furnizori"
        subtitle="Fiecare factură e comparată cu comanda (prețul) și cu recepțiile (cantitatea)."
        action={canEdit && <Link className="btn btn-primary" to="/facturi/noua">Înregistrează factură</Link>}
      />
      <Flash />

      <div className="toolbar">
        <Segmented
          value={filter}
          onChange={setFilter}
          options={[
            { value: 'todo', label: 'De rezolvat', count: all.filter(FILTERS.todo).length },
            { value: 'blocked', label: 'Blocate', count: all.filter(FILTERS.blocked).length },
            { value: 'all', label: 'Toate', count: all.length },
          ]}
        />
      </div>

      <ErrorBox error={error || actionError} />

      {loading ? (
        <Loading />
      ) : visible.length === 0 ? (
        <Empty>Nu există facturi în această listă.</Empty>
      ) : (
        <div className="table-wrap">
          <table>
            <thead>
              <tr>
                <th>Număr</th>
                <th>Furnizor</th>
                <th>Comandă</th>
                <th>Data</th>
                <th className="num">Total</th>
                <th className="num">În RON</th>
                <th>Status</th>
                {canEdit && <th />}
              </tr>
            </thead>
            <tbody>
              {visible.map((invoice) => {
                const isExpanded = expanded === invoice.id
                const columns = canEdit ? 8 : 7
                return (
                  <Fragment key={invoice.id}>
                    <tr className="clickable" onClick={() => setExpanded(isExpanded ? null : invoice.id)}>
                      <td className="strong">{invoice.number}</td>
                      <td>{invoice.supplierName}</td>
                      <td>{invoice.purchaseOrderNumber}</td>
                      <td>{formatDate(invoice.invoiceDate)}</td>
                      <td className="num">{formatMoney(invoice.totalAmount, invoice.currency)}</td>
                      <td className="num">
                        {formatMoney(invoice.totalRon)}
                        {invoice.currency !== 'RON' && <div className="hint">curs BNR {formatRate(invoice.exchangeRate)}</div>}
                      </td>
                      <td><StatusBadge status={invoice.status} map={INVOICE_STATUS} /></td>
                      {canEdit && (
                        <td className="num actions-cell" onClick={(e) => e.stopPropagation()}>
                          <InvoiceActions invoice={invoice} busy={busy} onAct={act} onPanel={openPanel} />
                        </td>
                      )}
                    </tr>

                    {(invoice.blockReason || invoice.decisionNote) && (
                      <tr className="note-row">
                        <td colSpan={columns}>
                          {invoice.blockReason && <div className="reason reason-danger">{invoice.blockReason}</div>}
                          {invoice.decisionNote && <div className="reason">Decizie: {invoice.decisionNote}</div>}
                        </td>
                      </tr>
                    )}

                    {panel?.id === invoice.id && (
                      <tr className="detail-row">
                        <td colSpan={columns}>
                          <div className="panel">
                            <label className="field">
                              <span>{panel.kind === 'approve' ? 'Justificare pentru aprobarea unei facturi blocate' : 'Motivul respingerii'}</span>
                              <textarea id={`note-${invoice.id}`} rows={2} value={note} onChange={(e) => setNote(e.target.value)} autoFocus />
                            </label>
                            <div className="actions">
                              <button type="button" className="btn btn-ghost" onClick={() => setPanel(null)}>Renunță</button>
                              <button
                                type="button"
                                className={panel.kind === 'approve' ? 'btn btn-primary' : 'btn btn-danger'}
                                disabled={busy || !note.trim()}
                                onClick={() =>
                                  panel.kind === 'approve'
                                    ? act(invoice, 'approve', { justification: note })
                                    : act(invoice, 'reject', { reason: note })
                                }
                              >
                                {panel.kind === 'approve' ? 'Aprobă cu justificare' : 'Respinge factura'}
                              </button>
                            </div>
                          </div>
                        </td>
                      </tr>
                    )}

                    {isExpanded && (
                      <tr className="detail-row">
                        <td colSpan={columns}>
                          <table className="inner">
                            <thead>
                              <tr>
                                <th>Produs</th>
                                <th className="num">Cantitate</th>
                                <th className="num">Preț unitar</th>
                                <th className="num">Valoare</th>
                              </tr>
                            </thead>
                            <tbody>
                              {invoice.lines.map((line) => (
                                <tr key={line.id}>
                                  <td>{line.productName}</td>
                                  <td className="num">{formatQuantity(line.quantity)}</td>
                                  <td className="num">{formatMoney(line.unitPrice, invoice.currency)}</td>
                                  <td className="num">{formatMoney(line.lineTotal, invoice.currency)}</td>
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

function InvoiceActions({ invoice, busy, onAct, onPanel }) {
  switch (invoice.status) {
    case 'Validated':
      return (
        <div className="row-actions">
          <button type="button" className="btn btn-primary btn-small" disabled={busy} onClick={() => onAct(invoice, 'approve', {})}>Aprobă la plată</button>
          <button type="button" className="btn btn-ghost btn-small" onClick={() => onPanel(invoice, 'reject')}>Respinge</button>
        </div>
      )
    case 'Blocked':
      return (
        <div className="row-actions">
          <button type="button" className="btn btn-ghost btn-small" disabled={busy} onClick={() => onAct(invoice, 'recheck')}>Reverifică</button>
          <button type="button" className="btn btn-ghost btn-small" onClick={() => onPanel(invoice, 'approve')}>Aprobă totuși</button>
          <button type="button" className="btn btn-ghost btn-small" onClick={() => onPanel(invoice, 'reject')}>Respinge</button>
        </div>
      )
    case 'ApprovedForPayment':
      return (
        <div className="row-actions">
          <button type="button" className="btn btn-primary btn-small" disabled={busy} onClick={() => onAct(invoice, 'pay')}>Marchează plătită</button>
        </div>
      )
    default:
      return null
  }
}
