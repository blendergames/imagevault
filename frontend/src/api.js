export async function getConfigStatus() {
  const res = await fetch('/api/config/status', { credentials: 'include' })
  if (!res.ok) throw new Error('config status failed')
  return res.json()
}

export async function saveConfig(payload) {
  const res = await fetch('/api/config', {
    method: 'POST',
    headers: { 'Content-Type': 'application/json' },
    body: JSON.stringify(payload),
    credentials: 'include'
  })
  if (!res.ok) throw new Error('config save failed')
  return res.json()
}

export async function getMe() {
  const res = await fetch('/api/auth/me', { credentials: 'include' })
  if (res.status === 401) return null
  if (!res.ok) throw new Error('me failed')
  return res.json()
}

export async function logout() {
  const res = await fetch('/api/auth/logout', { method: 'POST', credentials: 'include' })
  if (!res.ok) throw new Error('logout failed')
  return res.json()
}

