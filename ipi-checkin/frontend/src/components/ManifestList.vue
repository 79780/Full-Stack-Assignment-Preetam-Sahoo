<script setup lang="ts">
import type { ManifestSummary } from '../api/types'
import StatusPill from './StatusPill.vue'

defineProps<{
  manifests: ManifestSummary[]
  selectedId: number | null
  loading: boolean
}>()

const emit = defineEmits<{ select: [id: number] }>()

const dateFormat = new Intl.DateTimeFormat(undefined, {
  day: 'numeric',
  month: 'short',
  hour: '2-digit',
  minute: '2-digit'
})

function sent(iso: string) {
  return dateFormat.format(new Date(iso))
}
</script>

<template>
  <section class="worklist card" aria-label="Manifest worklist">
    <header class="worklist__head">
      <span class="eyebrow">Worklist</span>
      <span v-if="!loading" class="worklist__tally mono">{{ manifests.length }}</span>
    </header>

    <ol v-if="loading" class="worklist__items" aria-busy="true">
      <li v-for="n in 3" :key="n" class="skeleton" />
    </ol>

    <p v-else-if="manifests.length === 0" class="worklist__empty">
      No manifests have arrived for this lab yet.
    </p>

    <ol v-else class="worklist__items">
      <li v-for="manifest in manifests" :key="manifest.id">
        <button
          type="button"
          class="row"
          :class="{ 'row--active': manifest.id === selectedId }"
          :aria-current="manifest.id === selectedId"
          @click="emit('select', manifest.id)"
        >
          <span class="row__top">
            <span class="row__code mono">{{ manifest.code }}</span>
            <StatusPill :status="manifest.status" />
          </span>

          <span class="row__clinic">{{ manifest.clinicName }}</span>

          <span class="row__meta">
            <span>{{ sent(manifest.sentAt) }}</span>
            <span aria-hidden="true">·</span>
            <span class="mono">
              {{ manifest.counts.received }}/{{ manifest.counts.total }} received
            </span>
            <span v-if="manifest.counts.openDiscrepancies > 0" class="row__flag">
              {{ manifest.counts.openDiscrepancies }} missing
            </span>
          </span>
        </button>
      </li>
    </ol>
  </section>
</template>

<style scoped>
.worklist {
  display: flex;
  flex-direction: column;
  overflow: hidden;
}

.worklist__head {
  display: flex;
  align-items: center;
  justify-content: space-between;
  padding: 14px 16px;
  border-bottom: 1px solid var(--line);
}

.worklist__tally {
  font-size: 12px;
  color: var(--ink-3);
}

.worklist__empty {
  margin: 0;
  padding: 24px 16px;
  color: var(--ink-2);
}

.worklist__items {
  list-style: none;
  margin: 0;
  padding: 6px;
  overflow-y: auto;
  display: flex;
  flex-direction: column;
  gap: 2px;
}

.row {
  display: flex;
  flex-direction: column;
  gap: 4px;
  width: 100%;
  padding: 10px 10px 11px;
  text-align: left;
  background: transparent;
  border: 0;
  border-radius: var(--radius-sm);
  color: inherit;
}

.row:hover {
  background: var(--surface-sunk);
}

/* The selected manifest is the one on the bench; a left rule says so without shouting. */
.row--active,
.row--active:hover {
  background: var(--teal-wash);
  box-shadow: inset 2px 0 0 var(--teal);
}

.row__top {
  display: flex;
  align-items: center;
  justify-content: space-between;
  gap: 8px;
}

.row__code {
  font-size: 13px;
  font-weight: 600;
}

.row__clinic {
  color: var(--ink-2);
  font-size: 13px;
}

.row__meta {
  display: flex;
  align-items: center;
  flex-wrap: wrap;
  gap: 6px;
  font-size: 12px;
  color: var(--ink-3);
}

.row__flag {
  color: var(--flagged);
  font-weight: 600;
}

.skeleton {
  height: 64px;
  margin: 2px 4px;
  border-radius: var(--radius-sm);
  background: linear-gradient(90deg, var(--surface-sunk), var(--pending-wash), var(--surface-sunk));
  background-size: 200% 100%;
  animation: shimmer 1.2s linear infinite;
}

@keyframes shimmer {
  to {
    background-position: -200% 0;
  }
}
</style>
