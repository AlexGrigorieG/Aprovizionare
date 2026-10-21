import { useLocation } from 'react-router-dom'

export function PageHeader({ title, subtitle, action }) {
  return (
    <header className="page-header">
      <div>
        <h1>{title}</h1>
        {subtitle && <p className="page-subtitle">{subtitle}</p>}
      </div>
      {action}
    </header>
  )
}

export function Badge({ tone = 'muted', children }) {
  return <span className={`badge badge-${tone}`}>{children}</span>
}

export function StatusBadge({ status, map }) {
  const info = map[status] ?? { label: status, tone: 'muted' }
  return <Badge tone={info.tone}>{info.label}</Badge>
}

export function ErrorBox({ error }) {
  if (!error) return null
  return (
    <div className="notice notice-danger" role="alert">
      {error.message ?? String(error)}
    </div>
  )
}

// Mesajul trimis de pagina anterioară prin navigate(..., { state: { flash } }).
export function Flash() {
  const location = useLocation()
  const flash = location.state?.flash
  if (!flash) return null
  return <div className={`notice notice-${flash.tone ?? 'success'}`}>{flash.text}</div>
}

export function Loading() {
  return <p className="muted">Se încarcă…</p>
}

export function Empty({ children }) {
  return <div className="empty">{children}</div>
}

export function Segmented({ value, onChange, options }) {
  return (
    <div className="segmented" role="group">
      {options.map((option) => (
        <button
          key={option.value}
          type="button"
          className={option.value === value ? 'active' : ''}
          onClick={() => onChange(option.value)}
        >
          {option.label}
          {option.count !== undefined && <span className="count">{option.count}</span>}
        </button>
      ))}
    </div>
  )
}

export function Progress({ value, max }) {
  const percent = max > 0 ? Math.min(100, Math.round((value / max) * 100)) : 0
  return (
    <div className="progress" title={`${percent}%`}>
      <div className="progress-bar" style={{ width: `${percent}%` }} />
    </div>
  )
}
