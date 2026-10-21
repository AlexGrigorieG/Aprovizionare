import { useState } from 'react'
import { api, useApi } from '../api'
import { Empty, ErrorBox, Loading, PageHeader } from '../components/ui'

export default function AlertsPage() {
  const { data: messages, error, loading, reload } = useApi('/alerts/preview')
  const [result, setResult] = useState(null)
  const [sendError, setSendError] = useState(null)
  const [busy, setBusy] = useState(false)

  async function sendNow() {
    setBusy(true)
    setSendError(null)
    try {
      const { sent } = await api('/alerts/send-now', { method: 'POST' })
      setResult(sent)
      reload()
    } catch (err) {
      setSendError(err)
    } finally {
      setBusy(false)
    }
  }

  return (
    <section className="page">
      <PageHeader
        title="Alerte pe email"
        subtitle="Pleacă automat în fiecare dimineață. Aici vezi ce ar pleca acum și le poți trimite imediat."
        action={
          <button type="button" className="btn btn-primary" onClick={sendNow} disabled={busy || !messages?.length}>
            {busy ? 'Se trimit…' : 'Trimite acum'}
          </button>
        }
      />

      {result !== null && (
        <div className="notice notice-success">
          Am trimis {result} {result === 1 ? 'email' : 'emailuri'}. Fără server SMTP configurat, le găsești ca fișiere .eml în folderul Aprovizionare.Api/emails.
        </div>
      )}
      <ErrorBox error={error || sendError} />

      {loading ? (
        <Loading />
      ) : !messages?.length ? (
        <Empty>Nu e nimic de anunțat: nicio comandă întârziată, nicio factură blocată, nimic de clarificat.</Empty>
      ) : (
        <div className="stack">
          {messages.map((message, index) => (
            <article className="card email" key={index}>
              <div className="email-meta">
                <span className="muted">Către</span> {message.to.join(', ')}
              </div>
              <div className="email-subject">{message.subject}</div>
              <pre className="email-body">{message.body}</pre>
            </article>
          ))}
        </div>
      )}
    </section>
  )
}
