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

export async function devLogin() {
  const res = await fetch('/api/auth/dev-login', { method: 'GET', credentials: 'include' })
  if (!res.ok) throw new Error('dev-login failed')
  return res.json()
}

export async function devLogout() {
  const res = await fetch('/api/auth/dev-logout', { method: 'POST', credentials: 'include' })
  if (!res.ok) throw new Error('dev-logout failed')
  return res.json()
}

export async function uploadImage(file, description = '') {
  const fd = new FormData()
  fd.append('file', file)
  fd.append('description', description || '')
  const res = await fetch('/api/images', {
    method: 'POST',
    body: fd,
    credentials: 'include'
  })
  if (!res.ok) throw new Error('upload failed')
  return res.json()
}

export async function searchImages(q) {
  const url = q ? `/api/images/search?q=${encodeURIComponent(q)}` : '/api/images/search'
  const res = await fetch(url, { credentials: 'include' })
  if (!res.ok) throw new Error('search failed')
  return res.json()
}
