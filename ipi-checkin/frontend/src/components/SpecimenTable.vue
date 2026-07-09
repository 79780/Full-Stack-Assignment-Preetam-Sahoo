<script setup lang="ts">
import type { Specimen } from '../api/types'
import StatusPill from './StatusPill.vue'

defineProps<{
  specimens: Specimen[]
  busySpecimenId: number | null
  locked: boolean
}>()

const emit = defineEmits<{
  receive: [specimenId: number]
  flag: [specimenId: number]
}>()

const timeFormat = new Intl.DateTimeFormat(undefined, { hour: '2-digit', minute: '2-digit' })
</script>

<template>
  <table class="specimens">
    <caption class="sr-only">Bottles listed on this manifest</caption>
    <thead>
      <tr>
        <th scope="col" class="eyebrow">Specimen</th>
        <th scope="col" class="eyebrow">Patient</th>
        <th scope="col" class="eyebrow">Status</th>
        <th scope="col" class="eyebrow specimens__actions-head">
          <span v-if="!locked">Action</span>
        </th>
      </tr>
    </thead>

    <tbody>
      <tr v-for="specimen in specimens" :key="specimen.id">
        <td class="mono specimens__code">{{ specimen.code }}</td>
        <td>{{ specimen.patient }}</td>
        <td>
          <StatusPill :status="specimen.status" />
          <span v-if="specimen.receivedAt" class="specimens__time mono">
            {{ timeFormat.format(new Date(specimen.receivedAt)) }}
          </span>
        </td>
        <td class="specimens__actions">
          <template v-if="!locked">
            <button
              type="button"
              class="btn btn--primary"
              :disabled="busySpecimenId === specimen.id || specimen.status === 'Received'"
              @click="emit('receive', specimen.id)"
            >
              Receive
            </button>
            <button
              type="button"
              class="btn btn--ghost"
              :disabled="
                busySpecimenId === specimen.id ||
                specimen.status === 'Received' ||
                specimen.status === 'Flagged'
              "
              @click="emit('flag', specimen.id)"
            >
              Flag missing
            </button>
          </template>
        </td>
      </tr>
    </tbody>
  </table>
</template>

<style scoped>
.specimens {
  width: 100%;
  border-collapse: collapse;
}

.specimens th,
.specimens td {
  text-align: left;
  padding: 10px 12px;
  border-bottom: 1px solid var(--line);
  vertical-align: middle;
}

.specimens th {
  padding-top: 0;
  padding-bottom: 8px;
  border-bottom: 1px solid var(--line-strong);
}

.specimens tbody tr:last-child td {
  border-bottom: 0;
}

.specimens__code {
  font-weight: 600;
  font-size: 13px;
}

.specimens__time {
  margin-left: 8px;
  font-size: 12px;
  color: var(--ink-3);
}

.specimens__actions-head,
.specimens__actions {
  text-align: right;
  white-space: nowrap;
}

.specimens__actions {
  display: flex;
  gap: 6px;
  justify-content: flex-end;
}

.btn {
  padding: 5px 11px;
  border-radius: var(--radius-sm);
  font-size: 13px;
  font-weight: 600;
  border: 1px solid transparent;
  transition: background-color 120ms ease, border-color 120ms ease;
}

.btn--primary {
  background: var(--teal);
  color: #fff;
}

.btn--primary:hover:not(:disabled) {
  background: var(--teal-deep);
}

.btn--ghost {
  background: transparent;
  border-color: var(--line-strong);
  color: var(--ink-2);
}

.btn--ghost:hover:not(:disabled) {
  border-color: var(--flagged);
  color: var(--flagged);
}

.btn:disabled {
  opacity: 0.4;
}

.sr-only {
  position: absolute;
  width: 1px;
  height: 1px;
  overflow: hidden;
  clip: rect(0 0 0 0);
  white-space: nowrap;
}
</style>
