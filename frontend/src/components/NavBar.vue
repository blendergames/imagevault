<template>
  <header class="nav">
    <div class="inner">
      <div class="brand">ImageVault</div>
      <div class="spacer"></div>
      <div class="right">
        <template v-if="user">
          <div class="user">
            <img v-if="user.picture" :src="user.picture" alt="avatar" />
            <span class="name">{{ user.name }}</span>
            <span class="email">{{ user.email }}</span>
          </div>
          <button class="btn small" @click="$emit('logout')">로그아웃</button>
          <button v-if="isDev" class="btn small dev" @click="$emit('dev-logout')">Dev 로그아웃</button>
        </template>
        <template v-else>
          <span v-if="configReady" class="hint">로그인 필요</span>
          <span v-else class="hint">초기 설정 필요</span>
        </template>
      </div>
    </div>
  </header>
</template>

<script setup>
defineProps({ user: Object, configReady: { type: Boolean, default: false } })
const isDev = import.meta.env.DEV
</script>

<style scoped>
.nav { position: sticky; top: 0; z-index: 10; background: var(--bg-elev); border-bottom: 1px solid var(--border); }
.inner { max-width: 1100px; margin: 0 auto; display: flex; align-items: center; padding: 12px 16px; gap: 12px; }
.brand { font-weight: 700; letter-spacing: 0.4px; }
.spacer { flex: 1; }
.right { display: flex; align-items: center; gap: 12px; color: var(--muted); }
.user { display: flex; align-items: center; gap: 8px; }
.user img { width: 24px; height: 24px; border-radius: 50%; border: 1px solid var(--border); }
.name { color: var(--text); }
.email { color: var(--muted); font-size: 12px; }
.btn.small { padding: 6px 10px; font-size: 12px; }
.btn.small.dev { background: #16a34a; }
.hint { font-size: 12px; }
</style>
