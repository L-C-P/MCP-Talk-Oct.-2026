<script setup lang="ts">
/**
 * Master: "Title and Content WHITE" (variant white, default) / "Title and Content DARK" (dark).
 *
 * Markdown: `# Title` followed by the content.
 */
import { computed } from 'vue'
import { isDarkVariant, resolveVariant } from '../layoutHelper'
import pillBlue from '../assets/Pill_Blue.svg'
import pillWhite from '../assets/Pill_White.svg'

const props = defineProps<{ variant?: string }>()

const variant = computed(() => resolveVariant('default', props.variant))
</script>

<template>
  <div class="slidev-layout default adesso-slide adesso-content" :class="`adesso--${variant}`">
    <img class="adesso-title-pill" :src="isDarkVariant(variant) ? pillWhite : pillBlue" alt="">
    <slot />
  </div>
</template>
