import { useState } from 'react'
import { Navigate, useNavigate } from 'react-router-dom'
import { useAuth } from '../auth-context'

const TEST_ACCOUNTS = [
  { email: 'andrei@firma.ro', role: 'Achiziții' },
  { email: 'maria@firma.ro', role: 'Depozit' },
  { email: 'elena@firma.ro', role: 'Contabilitate' },
  { email: 'admin@firma.ro', role: 'Administrator' },
]

export default function LoginPage() {
  const { user, login } = useAuth()
  const navigate = useNavigate()
  const [email, setEmail] = useState('')
  const [password, setPassword] = useState('')
  const [error, setError] = useState(null)
  const [busy, setBusy] = useState(false)

  if (user) return <Navigate to="/" replace />

  async function handleSubmit(event) {
    event.preventDefault()
    setBusy(true)
    setError(null)
    try {
      await login(email, password)
      navigate('/')
    } catch (err) {
      setError(err.message)
    } finally {
      setBusy(false)
    }
  }

  function fill(account) {
    setEmail(account.email)
    setPassword('Parola123!')
  }

  return (
    <div className="login">
      <form className="login-card" onSubmit={handleSubmit}>
        <div className="brand">
          <span className="brand-mark" aria-hidden="true" />
          Aprovizionare
        </div>
        <p className="muted">Comenzi, recepții și facturi într-un singur loc.</p>

        <label className="field">
          <span>Email</span>
          <input id="email" type="email" autoComplete="username" value={email} onChange={(e) => setEmail(e.target.value)} required />
        </label>
        <label className="field">
          <span>Parolă</span>
          <input id="password" type="password" autoComplete="current-password" value={password} onChange={(e) => setPassword(e.target.value)} required />
        </label>

        {error && <div className="notice notice-danger">{error}</div>}

        <button type="submit" className="btn btn-primary" disabled={busy}>
          {busy ? 'Se verifică…' : 'Intră în cont'}
        </button>

        <div className="test-accounts">
          <span className="muted">Conturi de test (parola Parola123!):</span>
          {TEST_ACCOUNTS.map((account) => (
            <button key={account.email} type="button" className="link-button" onClick={() => fill(account)}>
              {account.email} · {account.role}
            </button>
          ))}
        </div>
      </form>
    </div>
  )
}
