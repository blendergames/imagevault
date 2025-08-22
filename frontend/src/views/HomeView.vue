<template>
  <div class="card">
    <h2>로그인 완료</h2>
    <p class="muted">{{ user.name }}님, 로그인된 상태입니다. 앞으로 페이지 내용을 구현할 예정입니다.</p>

    <div class="space"></div>
    <div class="row">
      <input class="input" style="flex:1" placeholder="이미지 검색" />
      <button class="btn">이미지 업로드</button>
    </div>

    <div class="space"></div>
    <div class="placeholder">
      빈 영역 — 추후 콘텐츠가 여기 표시됩니다.
    </div>
  </div>
</template>

<script setup>
defineProps({ user: { type: Object, required: true } })
</script>

<style scoped>
.muted { color: var(--muted); }
.placeholder { border: 2px dashed var(--border); border-radius: 10px; height: 360px; display: grid; place-items: center; color: var(--muted); }
</style>

