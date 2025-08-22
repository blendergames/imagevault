<template>
  <div class="card">
    <h2>초기 설정: MariaDB 연결</h2>
    <p class="muted">DB 정보가 없어 설정 파일을 생성합니다. 제출 시 파일시스템에 저장되고 Git에서 무시됩니다.</p>
    <div class="space"></div>
    <form @submit.prevent="save">
      <div class="row">
        <label class="w">Host</label>
        <input class="input flex" v-model="form.dbHost" placeholder="localhost" required />
      </div>
      <div class="row">
        <label class="w">Port</label>
        <input class="input flex" type="number" v-model.number="form.dbPort" placeholder="3306" required />
      </div>
      <div class="row">
        <label class="w">Database</label>
        <input class="input flex" v-model="form.dbName" placeholder="imagevault" required />
      </div>
      <div class="row">
        <label class="w">User</label>
        <input class="input flex" v-model="form.dbUser" placeholder="root" required />
      </div>
      <div class="row">
        <label class="w">Password</label>
        <input class="input flex" v-model="form.dbPassword" type="password" placeholder="••••••" />
      </div>
      <div class="space"></div>
      <div class="row">
        <button class="btn" :disabled="saving">저장</button>
        <span class="muted" v-if="saved">저장 완료. 페이지가 갱신됩니다.</span>
      </div>
    </form>
  </div>
</template>

<script setup>
import { reactive, ref } from 'vue'
import { saveConfig } from '../api'

const emit = defineEmits(['saved'])
const form = reactive({ dbHost: '', dbPort: 3306, dbName: '', dbUser: '', dbPassword: '' })
const saving = ref(false)
const saved = ref(false)

async function save() {
  saving.value = true
  try {
    await saveConfig(form)
    saved.value = true
    emit('saved')
  } finally {
    saving.value = false
  }
}
</script>

<style scoped>
.muted { color: var(--muted); }
.w { display: inline-block; width: 120px; color: var(--muted); }
.flex { flex: 1; }
</style>

