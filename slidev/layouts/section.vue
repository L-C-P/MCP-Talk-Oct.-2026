<script setup lang="ts">
/**
 * Master: "Section Header WHITE" (variant white), "Section Header GRADIENT" (gradient, default).
 *
 * Markdown: `# Section name` and an optional paragraph as short description.
 */
import { computed } from 'vue'
import { isDarkVariant, resolveVariant } from '../layoutHelper'
import arrowBlue from '../assets/Arrow_Blue.svg'
import arrowWhite from '../assets/Arrow_White.svg'

const props = defineProps<{ variant?: string }>()

const variant = computed(() => resolveVariant('section', props.variant))
const dark = computed(() => isDarkVariant(variant.value))
</script>

<template>
  <div class="slidev-layout section adesso-slide" :class="`adesso--${variant}`">
    <div class="section__content">
      <slot />
    </div>
    <img class="section__arrow" :src="dark ? arrowWhite : arrowBlue" alt="">
  </div>
</template>
