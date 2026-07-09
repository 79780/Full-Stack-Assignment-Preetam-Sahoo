<script setup lang="ts">
import { computed } from 'vue'
import type { ManifestDetail } from '../api/types'
import ReconciliationBar from './ReconciliationBar.vue'
import SpecimenTable from './SpecimenTable.vue'
import StatusPill from './StatusPill.vue'

const props = defineProps<{
  manifest: ManifestDetail | null
  loading: boolean
  busySpecimenId: number | null
  closing: boolean
}>()

const emit = defineEmits<{
  receive: [specimenId: number]
  flag: [specimenId: number]
  close: []
}>()

const locked = computed(() => props.manifest?.status !== 'Open')

const dateFormat = new Intl.DateTimeFormat(undefined, {
  weekday: 'short',
  day: 'numeric',
  month: 'short',
  hour: '2-digit',
  minute: '2-digit'
})
</script>

<template>
  <section class="detail card" aria-label="Manifest detail">
    <div v-if="loading && !manifest" class="detail__placeholder">Loading manifest…</div>

    <div v-else-if="!manifest" class="detail__placeholder">
      Pick a manifest from the worklist to start checking bottles in.
    </div>

    <template v-else>
      <header class="detail__head">
        <div class="detail__title">
          <span class="eyebrow">Manifest</span>
          <h1 class="mono detail__code">{{ manifest.code }}</h1>
          <StatusPill :status="manifest.status" />
        </div>

        <p class="detail__meta">
          {{ manifest.clinicName }} · sent {{ dateFormat.format(new Date(manifest.sentAt)) }}
        </p>
      </header>

      <div class="detail__progress">
        <ReconciliationBar :counts="manifest.counts" />

        <button
          type="button"
          class="close-btn"
          :disabled="!manifest.counts.canClose || closing"
          :title="
            manifest.counts.canClose
              ? 'Close this manifest'
              : 'Every bottle must be received or flagged first'
          "
          @click="emit('close')"
        >
          {{ closing ? 'Closing…' : 'Close manifest' }}
        </button>
      </div>

      <div v-if="locked" class="detail__locked">
        This manifest is closed. Bottles can no longer be received or flagged.
      </div>

      <div class="detail__table">
        <SpecimenTable
          :specimens="manifest.specimens"
          :busy-specimen-id="busySpecimenId"
          :locked="locked"
          @receive="emit('receive', $event)"
          @flag="emit('flag', $event)"
        />
      </div>
    </template>
  </section>
</template>

<style scoped>
.detail {
  display: flex;
  flex-direction: column;
  overflow: hidden;
}

.detail__placeholder {
  padding: 48px 24px;
  text-align: center;
  color: var(--ink-2);
}

.detail__head {
  padding: 18px 20px 14px;
  border-bottom: 1px solid var(--line);
}

.detail__title {
  display: flex;
  align-items: center;
  gap: 10px;
}

.detail__title .eyebrow {
  order: -1;
}

.detail__code {
  margin: 0;
  font-size: 20px;
  font-weight: 600;
  letter-spacing: -0.01em;
}

.detail__meta {
  margin: 4px 0 0;
  color: var(--ink-2);
}

.detail__progress {
  display: flex;
  align-items: flex-end;
  gap: 24px;
  padding: 16px 20px;
  background: var(--surface-sunk);
  border-bottom: 1px solid var(--line);
}

.detail__progress > :first-child {
  flex: 1;
}

.close-btn {
  padding: 8px 16px;
  border: 1px solid var(--teal);
  border-radius: var(--radius-sm);
  background: var(--teal);
  color: #fff;
  font-weight: 600;
  transition: background-color 120ms ease;
}

.close-btn:hover:not(:disabled) {
  background: var(--teal-deep);
}

.close-btn:disabled {
  background: transparent;
  border-color: var(--line-strong);
  color: var(--ink-3);
}

.detail__locked {
  padding: 10px 20px;
  background: var(--pending-wash);
  border-bottom: 1px solid var(--line);
  color: var(--ink-2);
  font-size: 13px;
}

.detail__table {
  padding: 16px 20px 20px;
  overflow-y: auto;
}

@media (max-width: 720px) {
  .detail__progress {
    flex-direction: column;
    align-items: stretch;
    gap: 14px;
  }
}
</style>
