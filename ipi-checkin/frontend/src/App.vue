<script setup lang="ts">
import { onMounted } from 'vue'
import ErrorBanner from './components/ErrorBanner.vue'
import ManifestDetail from './components/ManifestDetail.vue'
import ManifestList from './components/ManifestList.vue'
import { useCheckIn } from './composables/useCheckIn'

const {
  lab,
  labId,
  manifests,
  detail,
  selectedId,
  loadingWorklist,
  loadingDetail,
  busySpecimenId,
  closing,
  error,
  loadWorklist,
  selectManifest,
  receive,
  flag,
  close,
  switchLab,
  dismissError
} = useCheckIn()

// Two seeded labs. The switcher exists so tenant isolation is visible, not just asserted.
const labs = [
  { id: 1, name: 'Northgate Pathology' },
  { id: 2, name: 'Ridgeview Diagnostics' }
]

onMounted(loadWorklist)
</script>

<template>
  <div class="shell">
    <header class="topbar">
      <div class="topbar__brand">
        <span class="topbar__mark" aria-hidden="true"></span>
        <span class="topbar__name">IPI Pro</span>
        <span class="topbar__screen">Specimen check-in</span>
      </div>

      <label class="topbar__lab">
        <span class="eyebrow">Signed in as</span>
        <select :value="labId" @change="switchLab(Number(($event.target as HTMLSelectElement).value))">
          <option v-for="option in labs" :key="option.id" :value="option.id">
            {{ option.name }}
          </option>
        </select>
      </label>
    </header>

    <main class="layout">
      <ErrorBanner v-if="error" class="layout__error" :error="error" @dismiss="dismissError" />

      <ManifestList
        class="layout__list"
        :manifests="manifests"
        :selected-id="selectedId"
        :loading="loadingWorklist"
        @select="selectManifest"
      />

      <ManifestDetail
        class="layout__detail"
        :manifest="detail"
        :loading="loadingDetail"
        :busy-specimen-id="busySpecimenId"
        :closing="closing"
        @receive="receive"
        @flag="flag"
        @close="close"
      />
    </main>

    <p class="footnote">
      Scoped to {{ lab?.name ?? 'this lab' }} by the server. Manifests from other labs are not
      hidden in the client — they are never sent.
    </p>
  </div>
</template>

<style scoped>
.shell {
  display: flex;
  flex-direction: column;
  min-height: 100%;
  max-width: 1180px;
  margin: 0 auto;
  padding: 0 20px 24px;
}

.topbar {
  display: flex;
  align-items: center;
  justify-content: space-between;
  gap: 16px;
  padding: 18px 0 16px;
}

.topbar__brand {
  display: flex;
  align-items: baseline;
  gap: 10px;
}

.topbar__mark {
  align-self: center;
  width: 10px;
  height: 10px;
  border-radius: 2px;
  background: var(--teal);
}

.topbar__name {
  font-weight: 700;
  letter-spacing: -0.01em;
}

.topbar__screen {
  color: var(--ink-2);
}

.topbar__lab {
  display: flex;
  align-items: center;
  gap: 8px;
}

.topbar__lab select {
  padding: 5px 8px;
  border: 1px solid var(--line-strong);
  border-radius: var(--radius-sm);
  background: var(--surface);
  color: var(--ink);
  font: inherit;
}

.layout {
  display: grid;
  grid-template-columns: 320px 1fr;
  gap: 16px;
  align-items: start;
  flex: 1;
}

.layout__error {
  grid-column: 1 / -1;
}

.layout__list {
  max-height: calc(100vh - 160px);
}

.layout__detail {
  min-height: 320px;
  max-height: calc(100vh - 160px);
}

.footnote {
  margin: 16px 0 0;
  font-size: 12px;
  color: var(--ink-3);
}

@media (max-width: 860px) {
  .layout {
    grid-template-columns: 1fr;
  }

  .layout__list,
  .layout__detail {
    max-height: none;
  }
}
</style>
