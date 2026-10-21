import { useCallback, useEffect, useState } from 'react'

export class ApiError extends Error {
  constructor(status, message) {
    super(message)
    this.status = status
  }
}

export async function api(path, { method = 'GET', body } = {}) {
  const response = await fetch(`/api${path}`, {
    method,
    headers: body === undefined ? {} : { 'Content-Type': 'application/json' },
    body: body === undefined ? undefined : JSON.stringify(body),
  })

  const text = await response.text()
  let data
  try {
    data = text ? JSON.parse(text) : null
  } catch {
    data = null
  }

  if (!response.ok) throw new ApiError(response.status, errorMessage(response.status, data))
  return data
}

function errorMessage(status, data) {
  if (data?.message) return data.message
  if (data?.errors) return Object.values(data.errors).flat().join(' ')
  if (status === 401) return 'Sesiunea a expirat. Autentifică-te din nou.'
  if (status === 403) return 'Nu ai drepturi pentru această acțiune.'
  if (status === 404) return 'Înregistrarea nu a fost găsită.'
  return 'A apărut o eroare neașteptată. Încearcă din nou.'
}

export function useApi(path) {
  const [state, setState] = useState({ data: null, error: null, loading: true })
  const [version, setVersion] = useState(0)

  useEffect(() => {
    let cancelled = false
    api(path).then(
      (data) => !cancelled && setState({ data, error: null, loading: false }),
      (error) => !cancelled && setState({ data: null, error, loading: false }),
    )
    return () => {
      cancelled = true
    }
  }, [path, version])

  const reload = useCallback(() => setVersion((v) => v + 1), [])
  return { ...state, reload }
}
