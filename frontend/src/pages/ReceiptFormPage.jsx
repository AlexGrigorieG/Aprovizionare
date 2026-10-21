import { useState } from 'react'
import { Link, useNavigate } from 'react-router-dom'
import { api, useApi } from '../api'
import { ErrorBox, PageHeader } from '../components/ui'
import { formatDate, formatQuantity, isOpenOrder, todayIso } from '../format'

const emptyLine = () => ({ productId: '', quantity: '' })

export default function ReceiptFormPage() {
  const navigate = useNavigate()
  const suppliers = useApi('/suppliers')
  const products = useApi('/products')
  const orders = useApi('/purchase-orders')

  const [supplierId, setSupplierId] = useState('')
  const [purchaseOrderId, setPurchaseOrderId] = useState('')
  const [receivedDate, setReceivedDate] = useState(todayIso())
  const [notes, setNotes] = useState('')
  const [lines, setLines] = useState([emptyLine()])
  const [error, setError] = useState(null)
  const [busy, setBusy] = useState(false)

  const openOrders = (orders.data ?? []).filter((o) => isOpenOrder(o) && String(o.supplierId) === supplierId)
  const selectedOrder = openOrders.find((o) => String(o.id) === purchaseOrderId)

  function chooseSupplier(value) {
    setSupplierId(value)
    setPurchaseOrderId('')
    setLines([emptyLine()])
  }

  // Precompletăm cu ce mai e de primit; depozitul corectează cu ce a numărat efectiv.
  function chooseOrder(value) {
    setPurchaseOrderId(value)
    const order = openOrders.find((o) => String(o.id) === value)
    if (!order) {
      setLines([emptyLine()])
      return
    }
    const remaining = order.lines
      .filter((l) => l.quantityOrdered > l.quantityReceived)
      .map((l) => ({ productId: String(l.productId), quantity: String(l.quantityOrdered - l.quantityReceived) }))
    setLines(remaining.length > 0 ? remaining : [emptyLine()])
  }

  function updateLine(index, field, value) {
    setLines((current) => current.map((line, i) => (i === index ? { ...line, [field]: value } : line)))
  }

  function expectedFor(productId) {
    const orderLines = selectedOrder?.lines.filter((l) => String(l.productId) === productId) ?? []
    if (orderLines.length === 0) return selectedOrder ? 'nu este pe comandă' : null
    const ordered = orderLines.reduce((t, l) => t + l.quantityOrdered, 0)
    const received = orderLines.reduce((t, l) => t + l.quantityReceived, 0)
    return `comandat ${formatQuantity(ordered)}, primit până acum ${formatQuantity(received)}`
  }

  async function handleSubmit(event) {
    event.preventDefault()
    setBusy(true)
    setError(null)
    try {
      const receipt = await api('/goods-receipts', {
        method: 'POST',
        body: {
          supplierId: Number(supplierId),
          purchaseOrderId: purchaseOrderId ? Number(purchaseOrderId) : null,
          receivedDate,
          notes: notes.trim() || null,
          lines: lines
            .filter((l) => l.productId && Number(l.quantity) > 0)
            .map((l) => ({ productId: Number(l.productId), quantity: Number(l.quantity) })),
        },
      })
      const unordered = receipt.lines.filter((l) => l.isUnordered).length
      navigate('/receptii', {
        state: {
          flash: unordered > 0
            ? { tone: 'warning', text: `Recepția R-${receipt.id} a fost salvată. ${unordered} ${unordered === 1 ? 'poziție a fost marcată' : 'poziții au fost marcate'} de clarificat, iar Achizițiile vor fi anunțate.` }
            : { text: `Recepția R-${receipt.id} a fost salvată.` },
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
        title="Recepție nouă"
        subtitle="Introdu ce ai numărat. Aplicația împarte singură marfa pe comandă și marchează ce e în plus."
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
            <select id="purchaseOrderId" value={purchaseOrderId} onChange={(e) => chooseOrder(e.target.value)} disabled={!supplierId}>
              <option value="">Fără comandă (marfă necomandată)</option>
              {openOrders.map((o) => (
                <option key={o.id} value={o.id}>
                  {o.number} · livrare {formatDate(o.expectedDeliveryDate)}
                </option>
              ))}
            </select>
          </label>
          <label className="field">
            <span>Data recepției</span>
            <input id="receivedDate" type="date" max={todayIso()} value={receivedDate} onChange={(e) => setReceivedDate(e.target.value)} required />
          </label>
        </div>

        <h2 className="section-title">Ce a sosit</h2>
        <div className="lines">
          {lines.map((line, index) => {
            const hint = line.productId ? expectedFor(line.productId) : null
            return (
              <div className="line" key={index}>
                <select id={`product-${index}`} value={line.productId} onChange={(e) => updateLine(index, 'productId', e.target.value)} required aria-label="Produs">
                  <option value="">Alege produsul</option>
                  {products.data?.map((p) => (
                    <option key={p.id} value={p.id}>{p.code} · {p.name} ({p.unit})</option>
                  ))}
                </select>
                <input id={`quantity-${index}`} type="number" min="0.001" step="any" placeholder="Cantitate numărată" value={line.quantity} onChange={(e) => updateLine(index, 'quantity', e.target.value)} required aria-label="Cantitate" />
                <span className="line-hint">{hint}</span>
                <button type="button" className="btn btn-ghost btn-small" onClick={() => setLines((c) => c.filter((_, i) => i !== index))} disabled={lines.length === 1}>
                  Elimină
                </button>
              </div>
            )
          })}
        </div>
        <button type="button" className="btn btn-ghost" onClick={() => setLines((c) => [...c, emptyLine()])}>
          Adaugă produs
        </button>

        <label className="field">
          <span>Observații</span>
          <textarea id="notes" rows={2} value={notes} onChange={(e) => setNotes(e.target.value)} placeholder="ex. Tranșa 1, ambalaj deteriorat la 2 bucăți" />
        </label>

        <ErrorBox error={error || suppliers.error || products.error || orders.error} />

        <div className="form-footer">
          <span />
          <div className="actions">
            <Link className="btn btn-ghost" to="/receptii">Renunță</Link>
            <button type="submit" className="btn btn-primary" disabled={busy}>
              {busy ? 'Se salvează…' : 'Salvează recepția'}
            </button>
          </div>
        </div>
      </form>
    </section>
  )
}
