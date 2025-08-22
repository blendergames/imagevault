<template>
  <div class="app dark">
    <NavBar :user="user" @logout="handleLogout" />
    <main class="container">
      <SetupView v-if="!configReady" @saved="refreshAll" />
      <LoginView v-else-if="!user" />
      <HomeView v-else :user="user" />
    </main>
  </div>
  <footer class="footer">ImageVault • Dark Theme • v0.1</footer>
</template>

<script setup>
import { ref, onMounted } from 'vue'
import { getConfigStatus, getMe, logout } from './api'
import NavBar from './components/NavBar.vue'
import LoginView from './views/LoginView.vue'
import SetupView from './views/SetupView.vue'
import HomeView from './views/HomeView.vue'

const user = ref(null)
const configReady = ref(false)

async function refreshAll() {
  const cfg = await getConfigStatus()
  configReady.value = !!cfg.complete
  if (configReady.value) {
    user.value = await getMe()
  } else {
    user.value = null
  }
}

async function handleLogout() {
  await logout()
  await refreshAll()
}

onMounted(refreshAll)
</script>

<style>
:root {
  --bg: #0f1115;
  --bg-elev: #161a22;
  --text: #e5e7eb;
  --muted: #9aa4b2;
  --primary: #4f46e5;
  --primary-2: #6366f1;
  --border: #232a36;
}

.dark { background: var(--bg); color: var(--text); min-height: 100vh; }
.container { max-width: 1100px; margin: 0 auto; padding: 24px; }
.card { background: var(--bg-elev); border: 1px solid var(--border); border-radius: 10px; padding: 20px; }
.btn { background: var(--primary); color: white; border: none; padding: 10px 14px; border-radius: 8px; cursor: pointer; }
.btn:hover { background: var(--primary-2); }
.input { background: #0b0d11; border: 1px solid var(--border); color: var(--text); padding: 10px 12px; border-radius: 8px; }
.row { display: flex; gap: 12px; align-items: center; }
.space { height: 16px; }
.footer { text-align: center; color: var(--muted); padding: 16px 0 40px; }
</style>

