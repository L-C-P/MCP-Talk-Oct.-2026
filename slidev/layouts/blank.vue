<script setup lang="ts">
/**
 * Master: "Blank WHITE" (variant white, default) / "Blank DARK" (dark) – free area above the footer.
 *
 * Used for code and terminal slides. Add `class: blank--center` to centre the content.
 */
import { computed } from 'vue'
import { resolveVariant } from '../layoutHelper'

const props = defineProps<{ variant?: string }>()

const variant = computed(() => resolveVariant('blank', props.variant))
</script>

<template>
  <div class="slidev-layout blank adesso-slide" :class="`adesso--${variant}`">
    <slot />
  </div>
</template>
