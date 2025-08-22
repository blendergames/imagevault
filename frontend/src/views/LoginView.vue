<template>
  <div class="card">
    <h2>로그인이 필요합니다</h2>
    <p class="muted">Google 계정으로 로그인해 주세요.</p>
    <div class="space"></div>
    <button class="btn" @click="login">Google로 로그인</button>
  </div>
</template>

<script setup>
function login() {
  window.location.href = '/api/auth/login'
}
</script>

<style scoped>
.muted { color: var(--muted); }
</style>

