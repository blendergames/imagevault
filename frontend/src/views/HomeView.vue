<template>
  <div class="card">
    <h2>이미지 검색 / 업로드</h2>
    <p class="muted">{{ user.name }}님, 이미지를 업로드하고 설명으로 검색할 수 있습니다.</p>

    <div class="space"></div>
    <div class="row">
      <input class="input" style="flex:1" placeholder="이미지 설명으로 검색" v-model="q" @input="onSearchInput" />
      <button class="btn" @click="pickFile">이미지 업로드</button>
      <input ref="fileInput" type="file" accept="image/*" style="display:none" @change="onFilePicked" />
    </div>

    <div class="space"></div>
    <div v-if="results.length === 0" class="placeholder">검색 결과가 없습니다.</div>
    <div v-else class="grid">
      <div class="thumb" v-for="item in results" :key="item.id" @click="openOriginal(item.id)" title="원본 보기">
        <img :src="item.thumbUrl" :alt="item.description" />
        <div class="cap">{{ item.description }}</div>
      </div>
    </div>
  </div>
</template>

<script setup>
import { ref, onMounted } from 'vue'
import { searchImages, uploadImage } from '../api'

defineProps({ user: { type: Object, required: true } })

const q = ref('')
const results = ref([])
const fileInput = ref(null)
let searchTimer = null

function onSearchInput() {
  clearTimeout(searchTimer)
  searchTimer = setTimeout(runSearch, 250)
}

async function runSearch() {
  try {
    results.value = await searchImages(q.value || '')
  } catch (e) {
    console.error(e)
  }
}

function pickFile() {
  fileInput.value?.click()
}

async function onFilePicked(e) {
  const file = e.target.files?.[0]
  if (!file) return
  const desc = window.prompt('파일 설명을 입력하세요 (검색에 사용됩니다):', '') || ''
  try {
    await uploadImage(file, desc)
    q.value = desc
    await runSearch()
  } catch (err) {
    alert('업로드 실패: ' + (err?.message || err))
  } finally {
    e.target.value = ''
  }
}

function openOriginal(id) {
  window.open(`/api/images/${id}/original`, '_blank')
}

onMounted(runSearch)
</script>

<style scoped>
.muted { color: var(--muted); }
.placeholder { border: 2px dashed var(--border); border-radius: 10px; height: 180px; display: grid; place-items: center; color: var(--muted); }
.grid { display: grid; grid-template-columns: repeat(auto-fill, minmax(120px, 1fr)); gap: 12px; }
.thumb { background: #0b0d11; border: 1px solid var(--border); border-radius: 8px; padding: 6px; cursor: pointer; }
.thumb img { width: 100%; height: auto; display: block; border-radius: 6px; }
.cap { font-size: 12px; color: var(--muted); margin-top: 4px; overflow: hidden; text-overflow: ellipsis; white-space: nowrap; }
</style>
