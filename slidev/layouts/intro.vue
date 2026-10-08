<script setup lang="ts">
/**
 * Master: "Intro/Outro" – solid blue with the centred adesso logo. Content of the slide is not rendered.
 */
import logo from '../assets/adesso_Intro_White.svg'
</script>

<template>
  <div class="slidev-layout intro adesso-slide adesso--blue">
    <img class="intro__logo" :src="logo" alt="adesso">
  </div>
</template>
