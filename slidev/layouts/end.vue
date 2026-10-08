<script setup lang="ts">
/**
 * Master: "Contact slide" – blue background, large white title, contact details bottom right.
 *
 * Markdown: `# Thank you` (or any content) in the title area; use the `contact` slot for the details:
 *   ::contact::
 *   adesso SE …
 */
import logoWhite from '../assets/adesso_basic_White.svg'
import pillWhite from '../assets/Pill_White.svg'
</script>

<template>
  <div class="slidev-layout end adesso-slide adesso--blue">
    <img class="end__logo" :src="logoWhite" alt="adesso">
    <img class="adesso-title-pill" :src="pillWhite" alt="">
    <div class="end__title">
      <slot />
    </div>
    <div v-if="$slots.contact" class="end__contact">
      <slot name="contact" />
    </div>
  </div>
</template>
