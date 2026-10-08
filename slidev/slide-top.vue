<script setup lang="ts">
/**
 * Slide footer modelled on the adesso PowerPoint master (slideMaster1).
 *
 * Rendered on top of every slide – in the presentation, the presenter view and the export.
 *
 * Central configuration in the headmatter of slides.md:
 *   footer:
 *     text: "Title of the presentation"   # defaults to `title`
 *     date: "26.06.2026"
 *     pageNumber: true                    # default
 *     logo: true                          # default
 *
 * Per-slide frontmatter (not on the first slide – its frontmatter is the headmatter):
 *   footer: false                         # hide on this slide
 *   footer: true                          # force on layouts that hide it by default
 *   footer: { text: "...", variant: dark } # override single fields; variant follows the layout otherwise
 */
import { computed } from 'vue'
import { useSlideContext } from '@slidev/client'
import logoBlue from './assets/adesso_basic_Blue.svg'
import logoWhite from './assets/adesso_basic_White.svg'
import pillBlue from './assets/Pill_Blue.svg'
import pillWhite from './assets/Pill_White.svg'
import { isDarkVariant, resolveVariant } from './layoutHelper'

interface FooterOptions {
  text?: string
  date?: string
  pageNumber?: boolean
  logo?: boolean
  /** `light` or `dark` forces the colour; by default it follows the slide's layout variant. */
  variant?: 'light' | 'dark'
}

// Titleslide and Intro/Outro hide the footer in the master (showMasterSp="0").
const HIDDEN_LAYOUTS = ['cover', 'intro']

const { $page, $route, $frontmatter, $slidev } = useSlideContext()

const layout = computed<string>(() => $route?.meta.layout ?? $frontmatter.layout ?? 'default')

// The first slide's frontmatter is the headmatter, so its `footer` is the central configuration, not a per-slide override.
const slideSetting = computed(() => ($page.value === 1 ? undefined : $frontmatter.footer) as FooterOptions | boolean | undefined)

const options = computed<FooterOptions>(() => ({
  text: $slidev.configs.title,
  pageNumber: true,
  logo: true,
  // Slides on a blue, gradient or dark background (BLUE / GRADIENT / DARK layouts) use the white footer.
  variant: isDarkVariant(resolveVariant(layout.value, $frontmatter.variant)) ? 'dark' : 'light',
  ...($slidev.configs.footer as FooterOptions | undefined),
  ...(typeof slideSetting.value === 'object' ? slideSetting.value : {}),
}))

const visible = computed(() => {
  if (slideSetting.value === false)
    return false
  if (slideSetting.value !== undefined)
    return true

  return $slidev.configs.footer !== false && !HIDDEN_LAYOUTS.includes(layout.value)
})

const isDark = computed(() => options.value.variant === 'dark')

// 9 pt on a 960 pt wide PowerPoint slide, scaled to the Slidev canvas width; independent of the slide's `zoom`.
const fontSize = computed(() => `calc(${($slidev.configs.canvasWidth ?? 980) * 9 / 960}px / var(--slidev-slide-zoom-scale, 1))`)
</script>

<template>
  <footer v-if="visible" class="adesso-footer" :class="{ 'adesso-footer--dark': isDark }" :style="{ fontSize }">
    <span v-if="options.pageNumber" class="adesso-footer__number">{{ $page }}</span>
    <span v-if="options.text" class="adesso-footer__text">{{ options.text }}</span>
    <img class="adesso-footer__pill" :src="isDark ? pillWhite : pillBlue" alt="">
    <span v-if="options.date" class="adesso-footer__date">{{ options.date }}</span>
    <img v-if="options.logo" class="adesso-footer__logo" :src="isDark ? logoWhite : logoBlue" alt="adesso">
  </footer>
</template>

<style scoped>
/* Positions and sizes in % of the 16:9 slide, taken from the PowerPoint master (EMU / 12192000 × 6858000). */
.adesso-footer {
  position: absolute;
  inset: 0;
  z-index: 10;
  pointer-events: none;
  /* tx2 / dk2 of the adesso theme */
  color: #006ec7;
  font-family: 'Fira Sans', sans-serif;
  font-weight: 400;
  line-height: 1;
}

.adesso-footer--dark {
  color: #ffffff;
}

.adesso-footer > * {
  position: absolute;
}

.adesso-footer > span {
  top: 94.09%;
  height: 2.1%;
  display: flex;
  align-items: center;
  white-space: nowrap;
}

.adesso-footer__number {
  left: 3.35%;
  width: 4.13%;
}

.adesso-footer__text {
  left: 8.66%;
  width: 40.75%;
  overflow: hidden;
  text-overflow: ellipsis;
}

.adesso-footer__pill {
  left: 50.48%;
  top: 94.42%;
  width: 1.77%;
  height: 1.44%;
}

.adesso-footer__date {
  left: 53.25%;
  width: 5.91%;
}

.adesso-footer__logo {
  left: 92.87%;
  top: 93.53%;
  width: 4.37%;
  height: 3.06%;
}
</style>
