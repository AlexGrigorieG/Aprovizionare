import { NavLink, Outlet, useNavigate } from 'react-router-dom'
import { useAuth } from '../auth-context'
import { ROLE_LABELS, navFor } from '../roles'

export default function Layout() {
  const { user, logout } = useAuth()
  const navigate = useNavigate()

  async function handleLogout() {
    await logout()
    navigate('/login')
  }

  return (
    <div className="shell">
      <aside className="sidebar">
        <div className="brand">
          <span className="brand-mark" aria-hidden="true" />
          Aprovizionare
        </div>
        <nav className="nav">
          {navFor(user.roles).map((item) => (
            <NavLink key={item.to} to={item.to} className={({ isActive }) => (isActive ? 'nav-link active' : 'nav-link')}>
              {item.label}
            </NavLink>
          ))}
        </nav>
        <div className="user-box">
          <div className="user-name">{user.fullName}</div>
          <div className="user-role">{user.roles.map((role) => ROLE_LABELS[role] ?? role).join(', ')}</div>
          <button type="button" className="btn btn-ghost btn-small" onClick={handleLogout}>
            Ieșire
          </button>
        </div>
      </aside>
      <main className="content">
        <Outlet />
      </main>
    </div>
  )
}
