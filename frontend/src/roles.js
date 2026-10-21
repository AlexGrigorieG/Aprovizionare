export const ROLES = {
  Purchasing: 'Purchasing',
  Warehouse: 'Warehouse',
  Accounting: 'Accounting',
  Admin: 'Admin',
}

export const ROLE_LABELS = {
  Purchasing: 'Achiziții',
  Warehouse: 'Depozit',
  Accounting: 'Contabilitate',
  Admin: 'Administrator',
}

const { Purchasing, Warehouse, Accounting, Admin } = ROLES
const ALL = [Purchasing, Warehouse, Accounting, Admin]

// Meniul repetă regulile din backend; serverul rămâne cel care le impune.
const NAV = [
  { to: '/comenzi', label: 'Comenzi', roles: ALL },
  { to: '/receptii', label: 'Recepții', roles: ALL },
  { to: '/facturi', label: 'Facturi', roles: [Purchasing, Accounting, Admin] },
  { to: '/stoc', label: 'Stoc', roles: ALL },
  { to: '/alerte', label: 'Alerte pe email', roles: [Admin] },
  { to: '/audit', label: 'Jurnal de audit', roles: [Admin] },
]

const HOME = {
  [Purchasing]: '/comenzi',
  [Warehouse]: '/receptii',
  [Accounting]: '/facturi',
  [Admin]: '/comenzi',
}

export const canAccess = (userRoles, allowed) => allowed.some((role) => userRoles.includes(role))

export const navFor = (userRoles) => NAV.filter((item) => canAccess(userRoles, item.roles))

export const homeFor = (userRoles) => HOME[userRoles[0]] ?? '/comenzi'
