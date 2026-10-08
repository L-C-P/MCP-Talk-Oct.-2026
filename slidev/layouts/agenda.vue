<script setup lang="ts">
/**
 * Master: "Agenda" – title plus a numbered list (18 pt bold, blue).
 *
 * Markdown: `# Agenda` followed by an ordered list or `<Toc />`.
 */
import pillBlue from '../assets/Pill_Blue.svg'
</script>

<template>
  <div class="slidev-layout agenda adesso-slide adesso-content adesso--white">
    <img class="adesso-title-pill" :src="pillBlue" alt="">
    <slot />
  </div>
</template>
