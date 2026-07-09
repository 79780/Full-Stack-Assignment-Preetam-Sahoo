<script setup lang="ts">
import type { ApiError } from '../api/types'

defineProps<{ error: ApiError }>()
const emit = defineEmits<{ dismiss: [] }>()
</script>

<template>
  <!--
    Errors say what happened and what to do about it. The server's stable `code` decides the
    wording; its `detail` is the fallback, already written for a human.
  -->
  <div class="banner" role="alert">
    <span class="banner__text">
      <template v-if="error.code === 'manifest_not_reconciled'">
        Receive or flag every bottle before closing this manifest.
      </template>
      <template v-else-if="error.code === 'network_error'">
        Cannot reach the check-in service. Check that the API is running, then try again.
      </template>
      <template v-else>{{ error.message }}</template>
    </span>

    <button type="button" class="banner__dismiss" @click="emit('dismiss')">Dismiss</button>
  </div>
</template>

<style scoped>
.banner {
  display: flex;
  align-items: center;
  gap: 16px;
  padding: 10px 16px;
  border: 1px solid var(--flagged);
  border-radius: var(--radius);
  background: var(--flagged-wash);
  color: var(--flagged);
}

.banner__text {
  flex: 1;
  font-weight: 500;
}

.banner__dismiss {
  background: transparent;
  border: 0;
  color: inherit;
  font-weight: 600;
  font-size: 13px;
  text-decoration: underline;
  padding: 2px 4px;
}
</style>
