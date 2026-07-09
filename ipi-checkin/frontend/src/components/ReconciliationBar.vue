<script setup lang="ts">
import { computed } from 'vue'
import type { ManifestCounts } from '../api/types'

const props = defineProps<{ counts: ManifestCounts }>()

/**
 * The bar is the manifest, one segment per bottle-state, drawn to scale. A technician can
 * read progress without reading a single number, and the bar cannot disagree with the
 * counts beside it because both come from the same server payload.
 */
const segments = computed(() => {
  const { total, received, flagged, pending } = props.counts
  if (total === 0) return []

  return [
    { tone: 'received', count: received },
    { tone: 'flagged', count: flagged },
    { tone: 'pending', count: pending }
  ]
    .filter((s) => s.count > 0)
    .map((s) => ({ ...s, width: `${(s.count / total) * 100}%` }))
})

const actioned = computed(() => props.counts.received + props.counts.flagged)
</script>

<template>
  <div class="rec">
    <div class="rec__head">
      <span class="rec__count mono">{{ actioned }}<span class="rec__of">/{{ counts.total }}</span></span>
      <span class="rec__label">bottles accounted for</span>

      <span v-if="counts.pending > 0" class="rec__state rec__state--waiting">
        {{ counts.pending }} still pending
      </span>
      <span v-else-if="counts.openDiscrepancies > 0" class="rec__state rec__state--flagged">
        Reconciled with {{ counts.openDiscrepancies }} missing
      </span>
      <span v-else class="rec__state rec__state--ready">Reconciled</span>
    </div>

    <div class="rec__track" role="img" :aria-label="`${actioned} of ${counts.total} bottles accounted for`">
      <span
        v-for="segment in segments"
        :key="segment.tone"
        class="rec__seg"
        :class="`rec__seg--${segment.tone}`"
        :style="{ width: segment.width }"
      />
    </div>
  </div>
</template>

<style scoped>
.rec {
  display: flex;
  flex-direction: column;
  gap: 10px;
}

.rec__head {
  display: flex;
  align-items: baseline;
  gap: 8px;
}

.rec__count {
  font-size: 22px;
  font-weight: 600;
  letter-spacing: -0.02em;
}

.rec__of {
  color: var(--ink-3);
  font-weight: 500;
}

.rec__label {
  color: var(--ink-2);
}

.rec__state {
  margin-left: auto;
  font-size: 12px;
  font-weight: 600;
}

.rec__state--waiting {
  color: var(--ink-2);
}

.rec__state--ready {
  color: var(--received);
}

.rec__state--flagged {
  color: var(--flagged);
}

.rec__track {
  display: flex;
  height: 6px;
  gap: 2px;
  border-radius: 100px;
  background: var(--pending-wash);
  overflow: hidden;
}

.rec__seg {
  transition: width 180ms ease-out;
}

.rec__seg--received {
  background: var(--received);
}

.rec__seg--flagged {
  background: var(--flagged);
}

.rec__seg--pending {
  background: var(--line-strong);
}
</style>
