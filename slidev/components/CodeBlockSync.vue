<template>
  <span class="code-block-sync-activator" />
</template>

<script setup lang="ts">
import './codeblocksync.css'
import type { SharedState } from '@slidev/client'
import { sharedState, useIsSlideActive, useSlideContext } from '@slidev/client'
import { computed, onBeforeUnmount, watch } from 'vue'

interface LiveCodeBlockScrollPayload {
  codeBlockKey: string
  sourceId: string
  version: number
  scrollTop: number
  scrollLeft: number
}

interface LiveCodeBlockSelectionPayload {
  codeBlockKey: string
  sourceId: string
  version: number
  visible: boolean
  anchorOffset: number
  focusOffset: number
}

type SharedStateWithCodeBlockSync = SharedState & {
  liveCodeBlockScroll?: LiveCodeBlockScrollPayload
  liveCodeBlockSelection?: LiveCodeBlockSelectionPayload
  $patch?: (patch: Partial<SharedStateWithCodeBlockSync>) => Promise<boolean>
}

const syncState = sharedState as SharedStateWithCodeBlockSync
const sourceId = typeof crypto !== 'undefined' && 'randomUUID' in crypto
  ? crypto.randomUUID()
  : `code-block-sync-${Math.random().toString(36).slice(2)}`

const { $page, $renderContext } = useSlideContext()
const isSlideActive = useIsSlideActive()
const slideNo = computed(() => String($page.value))
const isPresenter = computed(() => $renderContext.value === 'presenter')

let scrollVersionCounter = 0
let selectionVersionCounter = 0
let pollTimer: ReturnType<typeof setInterval> | null = null
const applyingRemoteScroll = new Set<string>()
const latestRemoteScrollVersionByKey = new Map<string, number>()
const cachedScrollPayloadByKey = new Map<string, LiveCodeBlockScrollPayload>()
const latestRemoteSelectionVersionByKey = new Map<string, number>()
const cachedSelectionPayloadByKey = new Map<string, LiveCodeBlockSelectionPayload>()

/** Track element -> handler pairs for proper cleanup */
const scrollListeners = new Map<HTMLElement, () => void>()

function getWrappersOnSlide(): HTMLElement[] {
  const slideNode = document.querySelector(`.slidev-page[data-slidev-no="${slideNo.value}"]`)
  if (!slideNode)
    return []
  return Array.from(slideNode.querySelectorAll<HTMLElement>('.slidev-code-wrapper'))
}

function getCodeBlockKey(wrapper: HTMLElement): string {
  const wrappers = getWrappersOnSlide()
  const index = wrappers.indexOf(wrapper)
  return `${slideNo.value}:${index >= 0 ? index : 0}`
}

function getSlideNoFromKey(key: string): string {
  return key.split(':')[0] ?? ''
}

function nextScrollVersion(): number {
  scrollVersionCounter += 1
  return scrollVersionCounter
}

function nextSelectionVersion(): number {
  selectionVersionCounter += 1
  return selectionVersionCounter
}

function getScrollStateForWrapper(wrapper: HTMLElement): { scrollTop: number, scrollLeft: number } {
  const scrollTop = wrapper.scrollTop
  let scrollLeft = wrapper.scrollLeft

  const codeEls = wrapper.querySelectorAll<HTMLElement>('.slidev-code')
  for (const codeEl of codeEls) {
    if (codeEl.scrollLeft !== 0) {
      scrollLeft = codeEl.scrollLeft
      break
    }
  }

  return { scrollTop, scrollLeft }
}

function publishScrollPayload(payload: LiveCodeBlockScrollPayload): void {
  if (typeof syncState.$patch === 'function') {
    void syncState.$patch({ liveCodeBlockScroll: payload })
    return
  }
  syncState.lastUpdate = {
    id: sourceId,
    type: 'presenter',
    time: Date.now(),
  }
  const currentSlideNo = Number.parseInt(slideNo.value, 10)
  if (Number.isFinite(currentSlideNo))
    syncState.page = currentSlideNo
  syncState.liveCodeBlockScroll = payload
}

function publishSelectionPayload(payload: LiveCodeBlockSelectionPayload): void {
  if (typeof syncState.$patch === 'function') {
    void syncState.$patch({ liveCodeBlockSelection: payload })
    return
  }
  syncState.lastUpdate = {
    id: sourceId,
    type: 'presenter',
    time: Date.now(),
  }
  const currentSlideNo = Number.parseInt(slideNo.value, 10)
  if (Number.isFinite(currentSlideNo))
    syncState.page = currentSlideNo
  syncState.liveCodeBlockSelection = payload
}

function applySelectionFromOffsets(wrapper: HTMLElement, anchorOffset: number, focusOffset: number): void {
  const codeEl = wrapper.querySelector('.slidev-code') as HTMLElement | null
  if (!codeEl)
    return

  const startOffset = Math.min(anchorOffset, focusOffset)
  const endOffset = Math.max(anchorOffset, focusOffset)

  if (startOffset === 0 && endOffset === 0)
    return

  let currentOffset = 0
  let startNode: Node | null = null
  let startNodeOffset = 0
  let endNode: Node | null = null
  let endNodeOffset = 0

  const walker = document.createTreeWalker(codeEl, NodeFilter.SHOW_TEXT)
  let node: Node | null = walker.currentNode

  while (node) {
    const nodeLength = node.textContent?.length || 0

    if (!startNode && currentOffset + nodeLength >= startOffset) {
      startNode = node
      startNodeOffset = startOffset - currentOffset
    }
    if (!endNode && currentOffset + nodeLength >= endOffset) {
      endNode = node
      endNodeOffset = endOffset - currentOffset
      break
    }

    currentOffset += nodeLength
    node = walker.nextNode()
  }

  if (!startNode || !endNode)
    return

  try {
    const range = document.createRange()
    range.setStart(startNode, startNodeOffset)
    range.setEnd(endNode, endNodeOffset)

    const selection = window.getSelection()
    if (selection) {
      selection.removeAllRanges()
      selection.addRange(range)
    }
  }
  catch {
    // Ignore range errors during transitions
  }
}

function onWrapperScroll(wrapper: HTMLElement, key: string): void {
  if (applyingRemoteScroll.has(key))
    return
  if (!isPresenter.value)
    return

  const scrollState = getScrollStateForWrapper(wrapper)
  const payload: LiveCodeBlockScrollPayload = {
    codeBlockKey: key,
    sourceId,
    version: nextScrollVersion(),
    scrollTop: scrollState.scrollTop,
    scrollLeft: scrollState.scrollLeft,
  }
  publishScrollPayload(payload)
}

function applyRemoteScroll(wrapper: HTMLElement, payload: LiveCodeBlockScrollPayload): void {
  if (isPresenter.value)
    return

  const key = getCodeBlockKey(wrapper)
  if (key !== payload.codeBlockKey)
    return
  if (applyingRemoteScroll.has(key))
    return

  applyingRemoteScroll.add(key)
  wrapper.scrollTop = payload.scrollTop
  wrapper.scrollLeft = payload.scrollLeft

  const codeEls = wrapper.querySelectorAll<HTMLElement>('.slidev-code')
  for (const codeEl of codeEls) {
    if (codeEl.scrollLeft !== payload.scrollLeft)
      codeEl.scrollLeft = payload.scrollLeft
  }

  setTimeout(() => applyingRemoteScroll.delete(key), 10)
}

/** Synchronous selection change handler. */
function onSelectionChange(): void {
  if (!isPresenter.value || !isSlideActive.value)
    return

  const selection = window.getSelection()
  if (!selection)
    return

  const isCollapsed = selection.isCollapsed
  const rangeCount = selection.rangeCount
  const anchorNode = selection.anchorNode
  const anchorOffset = selection.anchorOffset
  const focusNode = selection.focusNode
  const focusOffset = selection.focusOffset

  if (isCollapsed) {
    const key = `${slideNo.value}:0`
    publishSelectionPayload({
      codeBlockKey: key,
      sourceId,
      version: nextSelectionVersion(),
      visible: false,
      anchorOffset: 0,
      focusOffset: 0,
    })
    return
  }

  if (rangeCount === 0)
    return

  const wrappers = getWrappersOnSlide()
  for (const wrapper of wrappers) {
    const codeEl = wrapper.querySelector('.slidev-code')
    if (!codeEl)
      continue

    if (!codeEl.contains(anchorNode) || !codeEl.contains(focusNode))
      continue

    let anchorTextOffset = -1
    let focusTextOffset = -1
    let currentOffset = 0

    const walker = document.createTreeWalker(codeEl, NodeFilter.SHOW_TEXT)
    let node: Node | null = walker.currentNode

    while (node) {
      const nodeLength = node.textContent?.length || 0
      if (anchorTextOffset === -1 && node === anchorNode) {
        anchorTextOffset = currentOffset + anchorOffset
      }
      if (focusTextOffset === -1 && node === focusNode) {
        focusTextOffset = currentOffset + focusOffset
      }
      if (anchorTextOffset !== -1 && focusTextOffset !== -1)
        break
      currentOffset += nodeLength
      node = walker.nextNode()
    }

    if (anchorTextOffset === -1 || focusTextOffset === -1)
      continue

    const key = getCodeBlockKey(wrapper)
    publishSelectionPayload({
      codeBlockKey: key,
      sourceId,
      version: nextSelectionVersion(),
      visible: true,
      anchorOffset: anchorTextOffset,
      focusOffset: focusTextOffset,
    })
    break
  }
}

function addScrollListener(el: HTMLElement, handler: () => void): void {
  if (scrollListeners.has(el))
    return
  el.addEventListener('scroll', handler, { passive: true })
  scrollListeners.set(el, handler)
}

function removeScrollListeners(): void {
  for (const [el, handler] of scrollListeners.entries()) {
    el.removeEventListener('scroll', handler)
  }
  scrollListeners.clear()
}

function setupScrollSync(wrapper: HTMLElement): void {
  const key = getCodeBlockKey(wrapper)
  const handler = () => onWrapperScroll(wrapper, key)

  addScrollListener(wrapper, handler)

  const codeEls = wrapper.querySelectorAll<HTMLElement>('.slidev-code')
  for (const codeEl of codeEls)
    addScrollListener(codeEl, handler)

  const cached = cachedScrollPayloadByKey.get(key)
  if (cached && !isPresenter.value)
    applyRemoteScroll(wrapper, cached)
}

function setupSelectionSync(wrapper: HTMLElement): void {
  const key = getCodeBlockKey(wrapper)
  const cached = cachedSelectionPayloadByKey.get(key)
  if (cached && !isPresenter.value && cached.visible) {
    applySelectionFromOffsets(wrapper, cached.anchorOffset, cached.focusOffset)
  }
}

function trackAllWrappers(): void {
  const wrappers = getWrappersOnSlide()
  for (const wrapper of wrappers) {
    setupScrollSync(wrapper)
    setupSelectionSync(wrapper)
  }
}

function startSync(): void {
  removeScrollListeners()
  if (pollTimer) {
    clearInterval(pollTimer)
    pollTimer = null
  }

  trackAllWrappers()
  pollTimer = setInterval(() => {
    if (isSlideActive.value)
      trackAllWrappers()
  }, 100)
  document.addEventListener('selectionchange', onSelectionChange)
}

function stopSync(): void {
  if (pollTimer) {
    clearInterval(pollTimer)
    pollTimer = null
  }
  document.removeEventListener('selectionchange', onSelectionChange)
  removeScrollListeners()
}

function handleRemoteScrollPayload(payload?: LiveCodeBlockScrollPayload): void {
  if (!payload || payload.sourceId === sourceId)
    return
  if (getSlideNoFromKey(payload.codeBlockKey) !== slideNo.value)
    return

  const knownVersion = latestRemoteScrollVersionByKey.get(payload.codeBlockKey) ?? 0
  if (payload.version <= knownVersion)
    return

  latestRemoteScrollVersionByKey.set(payload.codeBlockKey, payload.version)
  cachedScrollPayloadByKey.set(payload.codeBlockKey, payload)

  if (isPresenter.value)
    return

  const wrappers = getWrappersOnSlide()
  for (const wrapper of wrappers) {
    const key = getCodeBlockKey(wrapper)
    if (key === payload.codeBlockKey) {
      applyRemoteScroll(wrapper, payload)
      return
    }
  }
}

function handleRemoteSelectionPayload(payload?: LiveCodeBlockSelectionPayload): void {
  if (!payload || payload.sourceId === sourceId)
    return
  if (getSlideNoFromKey(payload.codeBlockKey) !== slideNo.value)
    return

  const knownVersion = latestRemoteSelectionVersionByKey.get(payload.codeBlockKey) ?? 0
  if (payload.version <= knownVersion)
    return

  latestRemoteSelectionVersionByKey.set(payload.codeBlockKey, payload.version)
  cachedSelectionPayloadByKey.set(payload.codeBlockKey, payload)

  if (isPresenter.value)
    return

  const wrappers = getWrappersOnSlide()
  for (const wrapper of wrappers) {
    const key = getCodeBlockKey(wrapper)
    if (key === payload.codeBlockKey) {
      if (payload.visible)
        applySelectionFromOffsets(wrapper, payload.anchorOffset, payload.focusOffset)
      else
        window.getSelection()?.removeAllRanges()
      return
    }
  }
}

const stopWatchingScroll = watch(
  () => syncState.liveCodeBlockScroll,
  payload => handleRemoteScrollPayload(payload),
  { immediate: true, deep: true },
)

const stopWatchingSelection = watch(
  () => syncState.liveCodeBlockSelection,
  payload => handleRemoteSelectionPayload(payload),
  { immediate: true, deep: true },
)

const stopWatchingSlideActive = watch(
  isSlideActive,
  (active) => {
    if (active)
      startSync()
    else
      stopSync()
  },
  { immediate: true },
)

onBeforeUnmount(() => {
  stopWatchingScroll()
  stopWatchingSelection()
  stopWatchingSlideActive()
  stopSync()
})
</script>

<style scoped>
.code-block-sync-activator {
  display: none;
}
</style>
