<template>
  <div class="card">
    <h2>로그인이 필요합니다</h2>
    <p class="muted">Google 계정으로 로그인해 주세요.</p>
    <div class="space"></div>
    <button class="btn" @click="login">Google로 로그인</button>
    <template v-if="isDev">
      <span style="display:inline-block; width: 8px;"></span>
      <button class="btn" style="background:#16a34a" @click="devLoginClick">Dev 로그인</button>
    </template>
  </div>
</template>

<script setup>
import { devLogin } from '../api'

const emit = defineEmits(['logged-in'])

function login() {
  window.location.href = '/api/auth/login'
}

const isDev = import.meta.env.DEV

async function devLoginClick() {
  try {
    await devLogin()
    emit('logged-in')
  } catch (e) {
    alert('Dev 로그인 실패: ' + (e?.message || e))
  }
}
</script>

<style scoped>
.muted { color: var(--muted); }
</style>
