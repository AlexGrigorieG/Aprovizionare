import { useState } from 'react'
import { Link, useNavigate } from 'react-router-dom'
import { api, useApi } from '../api'
import { ErrorBox, PageHeader } from '../components/ui'
import { formatMoney, todayIso } from '../format'

const emptyLine = () => ({ productId: '', quantity: '', unitPrice: '' })

export default function OrderFormPage() {
  const navigate = useNavigate()
  const suppliers = useApi('/suppliers')
  const products = useApi('/products')

  const [supplierId, setSupplierId] = useState('')
  const [expectedDeliveryDate, setExpectedDeliveryDate] = useState('')
  const [currency, setCurrency] = useState('RON')
  const [lines, setLines] = useState([emptyLine()])
  const [error, setError] = useState(null)
  const [busy, setBusy] = useState(false)

  const total = lines.reduce((t, l) => t + (Number(l.quantity) || 0) * (Number(l.unitPrice) || 0), 0)

  function updateLine(index, field, value) {
    setLines((current) => current.map((line, i) => (i === index ? { ...line, [field]: value } : line)))
  }

  async function handleSubmit(event) {
    event.preventDefault()
    setBusy(true)
    setError(null)
    try {
      const order = await api('/purchase-orders', {
        method: 'POST',
        body: {
          supplierId: Number(supplierId),
          expectedDeliveryDate,
          currency,
          lines: lines
            .filter((l) => l.productId)
            .map((l) => ({ productId: Number(l.productId), quantity: Number(l.quantity), unitPrice: Number(l.unitPrice) })),
        },
      })
      navigate('/comenzi', { state: { flash: { text: `Comanda ${order.number} a fost creată.` } } })
    } catch (err) {
      setError(err)
      setBusy(false)
    }
  }

  return (
    <section className="page">
      <PageHeader title="Comandă nouă" subtitle="Comanda pleacă la furnizor cu statusul Trimisă." />

      <form className="card form" onSubmit={handleSubmit}>
        <div className="form-grid">
          <label className="field">
            <span>Furnizor</span>
            <select id="supplierId" value={supplierId} onChange={(e) => setSupplierId(e.target.value)} required>
              <option value="">Alege furnizorul</option>
              {suppliers.data?.map((s) => (
                <option key={s.id} value={s.id}>{s.name}</option>
              ))}
            </select>
          </label>
          <label className="field">
            <span>Livrare estimată</span>
            <input id="expectedDeliveryDate" type="date" min={todayIso()} value={expectedDeliveryDate} onChange={(e) => setExpectedDeliveryDate(e.target.value)} required />
          </label>
          <label className="field">
            <span>Monedă</span>
            <select id="currency" value={currency} onChange={(e) => setCurrency(e.target.value)}>
              <option value="RON">RON</option>
              <option value="EUR">EUR</option>
            </select>
          </label>
        </div>

        <h2 className="section-title">Linii</h2>
        <div className="lines">
          {lines.map((line, index) => (
            <div className="line" key={index}>
              <select id={`product-${index}`} value={line.productId} onChange={(e) => updateLine(index, 'productId', e.target.value)} required aria-label="Produs">
                <option value="">Alege produsul</option>
                {products.data?.map((p) => (
                  <option key={p.id} value={p.id}>{p.code} · {p.name} ({p.unit})</option>
                ))}
              </select>
              <input id={`quantity-${index}`} type="number" min="0.001" step="any" placeholder="Cantitate" value={line.quantity} onChange={(e) => updateLine(index, 'quantity', e.target.value)} required aria-label="Cantitate" />
              <input id={`price-${index}`} type="number" min="0.01" step="0.01" placeholder={`Preț (${currency})`} value={line.unitPrice} onChange={(e) => updateLine(index, 'unitPrice', e.target.value)} required aria-label="Preț unitar" />
              <button type="button" className="btn btn-ghost btn-small" onClick={() => setLines((c) => c.filter((_, i) => i !== index))} disabled={lines.length === 1}>
                Elimină
              </button>
            </div>
          ))}
        </div>
        <button type="button" className="btn btn-ghost" onClick={() => setLines((c) => [...c, emptyLine()])}>
          Adaugă linie
        </button>

        <ErrorBox error={error || suppliers.error || products.error} />

        <div className="form-footer">
          <span className="total">Total: <strong>{formatMoney(total, currency)}</strong></span>
          <div className="actions">
            <Link className="btn btn-ghost" to="/comenzi">Renunță</Link>
            <button type="submit" className="btn btn-primary" disabled={busy}>
              {busy ? 'Se salvează…' : 'Trimite comanda'}
            </button>
          </div>
        </div>
      </form>
    </section>
  )
}
