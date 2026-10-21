import { BrowserRouter, Navigate, Route, Routes } from 'react-router-dom'
import AuthProvider from './AuthProvider'
import { useAuth } from './auth-context'
import Layout from './components/Layout'
import { Loading } from './components/ui'
import { ROLES, canAccess, homeFor } from './roles'
import AlertsPage from './pages/AlertsPage'
import AuditPage from './pages/AuditPage'
import InvoiceFormPage from './pages/InvoiceFormPage'
import InvoicesPage from './pages/InvoicesPage'
import LoginPage from './pages/LoginPage'
import OrderFormPage from './pages/OrderFormPage'
import OrdersPage from './pages/OrdersPage'
import ReceiptFormPage from './pages/ReceiptFormPage'
import ReceiptsPage from './pages/ReceiptsPage'
import StockPage from './pages/StockPage'

const { Purchasing, Warehouse, Accounting, Admin } = ROLES

function RequireAuth({ children }) {
  const { user, loading } = useAuth()
  if (loading) return <div className="center"><Loading /></div>
  if (!user) return <Navigate to="/login" replace />
  return children
}

function RequireRole({ roles, children }) {
  const { user } = useAuth()
  if (!canAccess(user.roles, roles)) return <Navigate to="/" replace />
  return children
}

function HomeRedirect() {
  const { user } = useAuth()
  return <Navigate to={homeFor(user.roles)} replace />
}

export default function App() {
  return (
    <AuthProvider>
      <BrowserRouter>
        <Routes>
          <Route path="/login" element={<LoginPage />} />
          <Route
            element={
              <RequireAuth>
                <Layout />
              </RequireAuth>
            }
          >
            <Route index element={<HomeRedirect />} />
            <Route path="comenzi" element={<OrdersPage />} />
            <Route path="comenzi/noua" element={<RequireRole roles={[Purchasing, Admin]}><OrderFormPage /></RequireRole>} />
            <Route path="receptii" element={<ReceiptsPage />} />
            <Route path="receptii/noua" element={<RequireRole roles={[Warehouse, Admin]}><ReceiptFormPage /></RequireRole>} />
            <Route path="facturi" element={<RequireRole roles={[Purchasing, Accounting, Admin]}><InvoicesPage /></RequireRole>} />
            <Route path="facturi/noua" element={<RequireRole roles={[Accounting, Admin]}><InvoiceFormPage /></RequireRole>} />
            <Route path="stoc" element={<StockPage />} />
            <Route path="alerte" element={<RequireRole roles={[Admin]}><AlertsPage /></RequireRole>} />
            <Route path="audit" element={<RequireRole roles={[Admin]}><AuditPage /></RequireRole>} />
          </Route>
          <Route path="*" element={<Navigate to="/" replace />} />
        </Routes>
      </BrowserRouter>
    </AuthProvider>
  )
}
