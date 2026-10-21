import { useState } from 'react'
import { useApi } from '../api'
import { Badge, Empty, ErrorBox, Loading, PageHeader } from '../components/ui'
import { formatDateTime } from '../format'

const ENTITIES = {
  PurchaseOrder: 'Comandă',
  PurchaseOrderLine: 'Linie comandă',
  GoodsReceipt: 'Recepție',
  GoodsReceiptLine: 'Linie recepție',
  Invoice: 'Factură',
  InvoiceLine: 'Linie factură',
  Supplier: 'Furnizor',
  Product: 'Produs',
  ExchangeRate: 'Curs valutar',
}

const ACTIONS = {
  Added: { label: 'Creat', tone: 'success' },
  Modified: { label: 'Modificat', tone: 'info' },
  Deleted: { label: 'Șters', tone: 'danger' },
}

function describe(changes) {
  try {
    return Object.entries(JSON.parse(changes)).map(([field, { Old, New }]) =>
      Old === null ? `${field}: ${New}` : New === null ? `${field}: ${Old} (șters)` : `${field}: ${Old} → ${New}`,
    )
  } catch {
    return [changes]
  }
}

export default function AuditPage() {
  const [form, setForm] = useState({ entityName: '', entityId: '', userName: '' })
  const [query, setQuery] = useState('')
  const { data: logs, error, loading } = useApi(`/audit${query}`)

  function applyFilters(event) {
    event.preventDefault()
    const params = new URLSearchParams(Object.entries(form).filter(([, value]) => value.trim()))
    setQuery(params.size ? `?${params}` : '')
  }

  return (
    <section className="page">
      <PageHeader title="Jurnal de audit" subtitle="Cine a făcut ce și când. Ultimele 200 de modificări." />

      <form className="toolbar filters" onSubmit={applyFilters}>
        <select id="entityName" value={form.entityName} onChange={(e) => setForm({ ...form, entityName: e.target.value })} aria-label="Tip de înregistrare">
          <option value="">Toate tipurile</option>
          {Object.entries(ENTITIES).map(([value, label]) => (
            <option key={value} value={value}>{label}</option>
          ))}
        </select>
        <input id="entityId" placeholder="Id" value={form.entityId} onChange={(e) => setForm({ ...form, entityId: e.target.value })} aria-label="Id" />
        <input id="userName" placeholder="Utilizator" value={form.userName} onChange={(e) => setForm({ ...form, userName: e.target.value })} aria-label="Utilizator" />
        <button type="submit" className="btn btn-ghost">Filtrează</button>
      </form>

      <ErrorBox error={error} />

      {loading ? (
        <Loading />
      ) : !logs?.length ? (
        <Empty>Nicio modificare găsită.</Empty>
      ) : (
        <div className="table-wrap">
          <table>
            <thead>
              <tr>
                <th>Când</th>
                <th>Cine</th>
                <th>Acțiune</th>
                <th>Ce</th>
                <th>Modificări</th>
              </tr>
            </thead>
            <tbody>
              {logs.map((log) => {
                const action = ACTIONS[log.action] ?? { label: log.action, tone: 'muted' }
                return (
                  <tr key={log.id}>
                    <td className="nowrap">{formatDateTime(log.timestamp)}</td>
                    <td>{log.userName}</td>
                    <td><Badge tone={action.tone}>{action.label}</Badge></td>
                    <td className="nowrap">{ENTITIES[log.entityName] ?? log.entityName} #{log.entityId}</td>
                    <td className="changes">
                      {describe(log.changes).map((line, i) => (
                        <div key={i}>{line}</div>
                      ))}
                    </td>
                  </tr>
                )
              })}
            </tbody>
          </table>
        </div>
      )}
    </section>
  )
}
