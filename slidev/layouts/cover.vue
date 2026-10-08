<script setup lang="ts">
/**
 * Master: "Titleslide" (variant white), "Titleslide BLUE" (blue), "Titleslide GRADIENT" (gradient, default).
 *
 * Markdown: `# Title` and a paragraph as subtitle.
 * Frontmatter: `place` – shown as "Place | Date" at the bottom; the date comes from `footer.date` in the headmatter.
 */
import { computed } from 'vue'
import { configs } from '@slidev/client'
import { isDarkVariant, resolveVariant } from '../layoutHelper'
import logoBlue from '../assets/adesso_basic_Blue.svg'
import logoWhite from '../assets/adesso_basic_White.svg'
import arrowBlue from '../assets/Arrow_Blue.svg'
import arrowWhite from '../assets/Arrow_White.svg'
import pillBlue from '../assets/Pill_Blue.svg'
import pillWhite from '../assets/Pill_White.svg'

const props = defineProps<{ variant?: string, place?: string }>()

const variant = computed(() => resolveVariant('cover', props.variant))
const dark = computed(() => isDarkVariant(variant.value))
const placeDate = computed(() => [props.place, (configs.footer as { date?: string } | undefined)?.date].filter(Boolean).join(' | '))
</script>

<template>
  <div class="slidev-layout cover adesso-slide" :class="`adesso--${variant}`">
    <img class="cover__logo" :src="dark ? logoWhite : logoBlue" alt="adesso">
    <div class="cover__content">
      <slot />
    </div>
    <p v-if="placeDate" class="cover__place-date">{{ placeDate }}</p>
    <img class="cover__arrow" :src="dark ? arrowWhite : arrowBlue" alt="">
    <img class="cover__pill" :src="dark ? pillWhite : pillBlue" alt="">
  </div>
</template>
