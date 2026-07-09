<script setup lang="ts">
import { computed } from 'vue'
import type { ManifestStatus, SpecimenStatus } from '../api/types'

const props = defineProps<{
  status: SpecimenStatus | ManifestStatus
}>()

/** One colour per meaning. Nothing on this screen is coloured for decoration. */
const styles: Record<string, { label: string; tone: string }> = {
  Pending: { label: 'Pending', tone: 'pending' },
  Received: { label: 'Received', tone: 'received' },
  Flagged: { label: 'Missing', tone: 'flagged' },
  Open: { label: 'Open', tone: 'open' },
  Closed: { label: 'Closed', tone: 'closed' },
  ClosedWithDiscrepancy: { label: 'Closed · discrepancy', tone: 'flagged' }
}

const pill = computed(() => styles[props.status] ?? { label: props.status, tone: 'pending' })
</script>

<template>
  <span class="pill" :class="`pill--${pill.tone}`">{{ pill.label }}</span>
</template>

<style scoped>
.pill {
  display: inline-flex;
  align-items: center;
  gap: 6px;
  padding: 2px 8px;
  border-radius: 100px;
  font-size: 12px;
  font-weight: 600;
  white-space: nowrap;
}

.pill::before {
  content: '';
  width: 5px;
  height: 5px;
  border-radius: 50%;
  background: currentColor;
}

.pill--pending {
  color: var(--pending);
  background: var(--pending-wash);
}

.pill--received {
  color: var(--received);
  background: var(--received-wash);
}

.pill--flagged {
  color: var(--flagged);
  background: var(--flagged-wash);
}

.pill--open {
  color: var(--teal-deep);
  background: var(--teal-wash);
}

.pill--closed {
  color: var(--ink-2);
  background: var(--pending-wash);
}
</style>
