import { useState } from 'react'
import { Link, useNavigate } from 'react-router-dom'
import { api, useApi } from '../api'
import { ErrorBox, PageHeader } from '../components/ui'
import { formatDate, formatMoney, formatQuantity, todayIso } from '../format'

export default function InvoiceFormPage() {
  const navigate = useNavigate()
  const suppliers = useApi('/suppliers')
  const orders = useApi('/purchase-orders')

  const [supplierId, setSupplierId] = useState('')
  const [purchaseOrderId, setPurchaseOrderId] = useState('')
  const [number, setNumber] = useState('')
  const [invoiceDate, setInvoiceDate] = useState(todayIso())
  const [lines, setLines] = useState([])
  const [error, setError] = useState(null)
  const [busy, setBusy] = useState(false)

  const supplierOrders = (orders.data ?? []).filter((o) => o.status !== 'Cancelled' && String(o.supplierId) === supplierId)
  const order = supplierOrders.find((o) => String(o.id) === purchaseOrderId)
  const included = lines.filter((l) => l.include)
  const total = included.reduce((t, l) => t + (Number(l.quantity) || 0) * (Number(l.unitPrice) || 0), 0)

  function chooseSupplier(value) {
    setSupplierId(value)
    setPurchaseOrderId('')
    setLines([])
  }

  // Liniile facturii se scriu după documentul primit de la furnizor; le precompletăm din comandă.
  function chooseOrder(value) {
    setPurchaseOrderId(value)
    const selected = supplierOrders.find((o) => String(o.id) === value)
    setLines(
      (selected?.lines ?? []).map((l) => ({
        purchaseOrderLineId: l.id,
        productName: l.productName,
        unit: l.unit,
        ordered: l.quantityOrdered,
        received: l.quantityReceived,
        include: l.quantityReceived > 0,
        quantity: String(l.quantityReceived || l.quantityOrdered),
        unitPrice: String(l.unitPrice ?? ''),
      })),
    )
  }

  function updateLine(index, field, value) {
    setLines((current) => current.map((line, i) => (i === index ? { ...line, [field]: value } : line)))
  }

  async function handleSubmit(event) {
    event.preventDefault()
    setBusy(true)
    setError(null)
    try {
      const invoice = await api('/invoices', {
        method: 'POST',
        body: {
          number,
          supplierId: Number(supplierId),
          purchaseOrderId: Number(purchaseOrderId),
          invoiceDate,
          currency: order.currency,
          lines: included.map((l) => ({
            purchaseOrderLineId: l.purchaseOrderLineId,
            quantity: Number(l.quantity),
            unitPrice: Number(l.unitPrice),
          })),
        },
      })
      navigate('/facturi', {
        state: {
          flash: invoice.status === 'Blocked'
            ? { tone: 'danger', text: `Factura ${invoice.number} a fost înregistrată, dar este blocată: ${invoice.blockReason}` }
            : { text: `Factura ${invoice.number} a trecut verificarea și așteaptă aprobarea la plată.` },
        },
      })
    } catch (err) {
      setError(err)
      setBusy(false)
    }
  }

  return (
    <section className="page">
      <PageHeader
        title="Înregistrează factură"
        subtitle="Introdu cantitățile și prețurile exact cum apar pe factura furnizorului. Verificarea se face la salvare."
      />

      <form className="card form" onSubmit={handleSubmit}>
        <div className="form-grid">
          <label className="field">
            <span>Furnizor</span>
            <select id="supplierId" value={supplierId} onChange={(e) => chooseSupplier(e.target.value)} required>
              <option value="">Alege furnizorul</option>
              {suppliers.data?.map((s) => (
                <option key={s.id} value={s.id}>{s.name}</option>
              ))}
            </select>
          </label>
          <label className="field">
            <span>Comandă</span>
            <select id="purchaseOrderId" value={purchaseOrderId} onChange={(e) => chooseOrder(e.target.value)} disabled={!supplierId} required>
              <option value="">Alege comanda</option>
              {supplierOrders.map((o) => (
                <option key={o.id} value={o.id}>
                  {o.number} · {formatDate(o.orderDate)} · {o.currency}
                </option>
              ))}
            </select>
          </label>
          <label className="field">
            <span>Număr factură</span>
            <input id="number" value={number} onChange={(e) => setNumber(e.target.value)} placeholder="ex. F-778" maxLength={50} required />
          </label>
          <label className="field">
            <span>Data facturii</span>
            <input id="invoiceDate" type="date" max={todayIso()} value={invoiceDate} onChange={(e) => setInvoiceDate(e.target.value)} required />
          </label>
        </div>

        {order && (
          <>
            <h2 className="section-title">Linii facturate · {order.currency}</h2>
            <div className="table-wrap">
              <table className="inner">
                <thead>
                  <tr>
                    <th />
                    <th>Produs</th>
                    <th className="num">Comandat</th>
                    <th className="num">Recepționat</th>
                    <th className="num">Cantitate facturată</th>
                    <th className="num">Preț facturat</th>
                  </tr>
                </thead>
                <tbody>
                  {lines.map((line, index) => (
                    <tr key={line.purchaseOrderLineId} className={line.include ? '' : 'disabled'}>
                      <td>
                        <input id={`include-${index}`} type="checkbox" checked={line.include} onChange={(e) => updateLine(index, 'include', e.target.checked)} aria-label={`Include ${line.productName}`} />
                      </td>
                      <td>{line.productName}</td>
                      <td className="num">{formatQuantity(line.ordered)} {line.unit}</td>
                      <td className="num">{formatQuantity(line.received)} {line.unit}</td>
                      <td className="num">
                        <input id={`quantity-${index}`} className="cell-input" type="number" min="0.001" step="any" value={line.quantity} onChange={(e) => updateLine(index, 'quantity', e.target.value)} disabled={!line.include} required={line.include} aria-label="Cantitate facturată" />
                      </td>
                      <td className="num">
                        <input id={`price-${index}`} className="cell-input" type="number" min="0.01" step="0.0001" value={line.unitPrice} onChange={(e) => updateLine(index, 'unitPrice', e.target.value)} disabled={!line.include} required={line.include} aria-label="Preț facturat" />
                      </td>
                    </tr>
                  ))}
                </tbody>
              </table>
            </div>
            {order.currency !== 'RON' && (
              <p className="hint">Valoarea în RON se calculează automat, la cursul BNR din data facturii.</p>
            )}
          </>
        )}

        <ErrorBox error={error || suppliers.error || orders.error} />

        <div className="form-footer">
          <span className="total">{order && <>Total: <strong>{formatMoney(total, order.currency)}</strong></>}</span>
          <div className="actions">
            <Link className="btn btn-ghost" to="/facturi">Renunță</Link>
            <button type="submit" className="btn btn-primary" disabled={busy || !order || included.length === 0}>
              {busy ? 'Se verifică…' : 'Înregistrează și verifică'}
            </button>
          </div>
        </div>
      </form>
    </section>
  )
}
