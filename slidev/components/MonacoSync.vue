<template>
  <span class="monaco-sync-activator" />
</template>

<script setup lang="ts">
import './monacosync.css'
import type { SharedState } from '@slidev/client'
import type { IDisposable, editor as MonacoEditorNamespace } from 'monaco-editor'
import { sharedState, useIsSlideActive, useSlideContext } from '@slidev/client'
import { computed, onBeforeUnmount, watch } from 'vue'

interface LiveCodeSyncPayload {
  editorKey: string
  content: string
  sourceId: string
  version: number
}

interface LiveCursorRange {
  startLineNumber: number
  startColumn: number
  endLineNumber: number
  endColumn: number
}

interface LiveCursorPayload {
  editorKey: string
  sourceId: string
  version: number
  visible: boolean
  cursor: { lineNumber: number, column: number } | null
  selection: LiveCursorRange | null
}

interface LiveScrollPayload {
  editorKey: string
  sourceId: string
  version: number
  scrollTop: number
  scrollLeft: number
}

type SharedStateWithLiveCode = SharedState & {
  liveCodeSync?: LiveCodeSyncPayload
  liveCursorSync?: LiveCursorPayload
  liveScrollSync?: LiveScrollPayload
  $patch?: (patch: Partial<SharedStateWithLiveCode>) => Promise<boolean>
}

type StandaloneCodeEditor = MonacoEditorNamespace.IStandaloneCodeEditor
type CodeEditor = MonacoEditorNamespace.ICodeEditor
type EditorSubscriptions = {
  onChange: IDisposable
  onCursorPosition: IDisposable
  onCursorSelection: IDisposable
  onScroll: IDisposable
  onFocus: IDisposable
  onBlur: IDisposable
  onDispose: IDisposable
}

const syncState = sharedState as SharedStateWithLiveCode
const sourceId = typeof crypto !== 'undefined' && 'randomUUID' in crypto
  ? crypto.randomUUID()
  : `live-code-sync-${Math.random().toString(36).slice(2)}`

const { $page, $renderContext } = useSlideContext()
const isSlideActive = useIsSlideActive()
const slideNo = computed(() => String($page.value))
const isMainRenderContext = computed(() => ['slide', 'presenter'].includes($renderContext.value))
const syncEnabled = computed(() => isMainRenderContext.value && isSlideActive.value)

let codeVersionCounter = 0
let cursorVersionCounter = 0
let scrollVersionCounter = 0
let monaco: typeof import('monaco-editor') | null = null
let createEditorListener: IDisposable | null = null

const trackedEditors = new Map<string, StandaloneCodeEditor>()
const editorSubscriptions = new Map<string, EditorSubscriptions>()
const publishTimers = new Map<string, ReturnType<typeof setTimeout>>()
const cursorPublishTimers = new Map<string, ReturnType<typeof setTimeout>>()
const scrollPublishTimers = new Map<string, ReturnType<typeof setTimeout>>()
const applyingRemoteByEditorId = new Set<string>()
const applyingRemoteScrollByEditorId = new Set<string>()

const latestRemoteVersionByEditorKey = new Map<string, number>()
const cachedPayloadByEditorKey = new Map<string, LiveCodeSyncPayload>()
const latestRemoteCursorVersionByEditorKey = new Map<string, number>()
const cachedCursorPayloadByEditorKey = new Map<string, LiveCursorPayload>()
const latestRemoteScrollVersionByEditorKey = new Map<string, number>()
const cachedScrollPayloadByEditorKey = new Map<string, LiveScrollPayload>()
const cursorDecorationIdsByEditorId = new Map<string, string[]>()

function isStandaloneCodeEditor(editor: CodeEditor): editor is StandaloneCodeEditor {
  return typeof (editor as StandaloneCodeEditor).onDidChangeModelContent === 'function'
}

function isConnectedEditor(editor: StandaloneCodeEditor): boolean {
  return editor.getContainerDomNode().isConnected
}

function isEditorInPresenterMainPane(editor: StandaloneCodeEditor): boolean {
  if ($renderContext.value !== 'presenter')
    return true

  const editorNode = editor.getContainerDomNode()
  const gridSection = editorNode.closest<HTMLElement>('.grid-section')
  if (!gridSection)
    return true

  return gridSection.classList.contains('main')
}

function parseEditorKey(editorKey: string): { slideNo: string, editorIndex: number } | null {
  const [slideNo, rawEditorIndex] = editorKey.split(':')
  if (!slideNo)
    return null

  const parsedEditorIndex = Number.parseInt(rawEditorIndex ?? '0', 10)
  return {
    slideNo,
    editorIndex: Number.isFinite(parsedEditorIndex) ? parsedEditorIndex : 0,
  }
}

function isVisibleEditor(editor: StandaloneCodeEditor): boolean {
  const { width, height } = editor.getContainerDomNode().getBoundingClientRect()
  return width > 0 && height > 0
}

function nextCodeVersion(): number {
  codeVersionCounter += 1
  return codeVersionCounter
}

function nextCursorVersion(): number {
  cursorVersionCounter += 1
  return cursorVersionCounter
}

function nextScrollVersion(): number {
  scrollVersionCounter += 1
  return scrollVersionCounter
}

function getEditorKey(editor: StandaloneCodeEditor): string | null {
  if (!isConnectedEditor(editor))
    return null
  if (!isEditorInPresenterMainPane(editor))
    return null

  const editorNode = editor.getContainerDomNode()
  const slideNode = editorNode.closest<HTMLElement>('.slidev-page[data-slidev-no]')
  if (!slideNode?.dataset.slidevNo)
    return null

  const monacoContainer = editorNode.closest<HTMLElement>('.slidev-monaco-container')
  const siblingEditors = Array.from(slideNode.querySelectorAll<HTMLElement>('.slidev-monaco-container'))
  const editorIndex = monacoContainer ? siblingEditors.indexOf(monacoContainer) : -1

  return `${slideNode.dataset.slidevNo}:${editorIndex >= 0 ? editorIndex : 0}`
}

function getSlideNoFromEditorKey(editorKey: string): string {
  return parseEditorKey(editorKey)?.slideNo ?? ''
}

function getEditorIndexFromEditorKey(editorKey: string): number {
  return parseEditorKey(editorKey)?.editorIndex ?? 0
}

function isEditorFromCurrentSlide(editorKey: string | null): editorKey is string {
  return Boolean(editorKey) && getSlideNoFromEditorKey(editorKey) === slideNo.value
}

function isCollapsedRange(range: LiveCursorRange): boolean {
  return range.startLineNumber === range.endLineNumber && range.startColumn === range.endColumn
}

function canPublishCursorState(editor: StandaloneCodeEditor): boolean {
  if ($renderContext.value !== 'presenter')
    return false
  if (!isConnectedEditor(editor))
    return false
  if (!isEditorInPresenterMainPane(editor))
    return false
  if (!editor.hasTextFocus())
    return false
  return true
}

function canPublishScrollState(editor: StandaloneCodeEditor): boolean {
  if ($renderContext.value !== 'presenter')
    return false
  if (!isConnectedEditor(editor))
    return false
  if (!isEditorInPresenterMainPane(editor))
    return false
  return true
}

function touchLocalSharedStateMeta(): void {
  syncState.lastUpdate = {
    id: sourceId,
    type: $renderContext.value === 'presenter' ? 'presenter' : 'viewer',
    time: Date.now(),
  }

  const currentSlideNo = Number.parseInt(slideNo.value, 10)
  if (Number.isFinite(currentSlideNo))
    syncState.page = currentSlideNo
}

function publishCodePayload(payload: LiveCodeSyncPayload): void {
  // Always cache locally so changes persist across slide unmount/remount
  latestRemoteVersionByEditorKey.set(payload.editorKey, payload.version)
  cachedPayloadByEditorKey.set(payload.editorKey, payload)

  if (typeof syncState.$patch === 'function') {
    void syncState.$patch({ liveCodeSync: payload })
    return
  }
  touchLocalSharedStateMeta()
  syncState.liveCodeSync = payload
}

function publishCursorPayload(payload: LiveCursorPayload): void {
  latestRemoteCursorVersionByEditorKey.set(payload.editorKey, payload.version)
  cachedCursorPayloadByEditorKey.set(payload.editorKey, payload)

  if (typeof syncState.$patch === 'function') {
    void syncState.$patch({ liveCursorSync: payload })
    return
  }
  touchLocalSharedStateMeta()
  syncState.liveCursorSync = payload
}

function publishScrollPayload(payload: LiveScrollPayload): void {
  latestRemoteScrollVersionByEditorKey.set(payload.editorKey, payload.version)
  cachedScrollPayloadByEditorKey.set(payload.editorKey, payload)

  if (typeof syncState.$patch === 'function') {
    void syncState.$patch({ liveScrollSync: payload })
    return
  }
  touchLocalSharedStateMeta()
  syncState.liveScrollSync = payload
}

function clearCursorDecorations(editor: StandaloneCodeEditor): void {
  const editorId = editor.getId()
  const currentDecorationIds = cursorDecorationIdsByEditorId.get(editorId) ?? []
  if (currentDecorationIds.length === 0)
    return

  const nextDecorationIds = editor.deltaDecorations(currentDecorationIds, [])
  cursorDecorationIdsByEditorId.set(editorId, nextDecorationIds)
}

function toLiveCursorRange(selection: MonacoEditorNamespace.ISelection | null): LiveCursorRange | null {
  if (!selection)
    return null

  return {
    startLineNumber: selection.startLineNumber,
    startColumn: selection.startColumn,
    endLineNumber: selection.endLineNumber,
    endColumn: selection.endColumn,
  }
}

function publishEditorCursorState(editor: StandaloneCodeEditor, visibleOverride?: boolean, immediate = false): void {
  if ($renderContext.value !== 'presenter')
    return
  if (!syncEnabled.value && visibleOverride !== false)
    return

  const editorId = editor.getId()
  const editorKey = getEditorKey(editor)
  if (!isEditorFromCurrentSlide(editorKey))
    return

  const existingTimer = cursorPublishTimers.get(editorId)
  if (existingTimer)
    clearTimeout(existingTimer)

  const delay = immediate ? 0 : 30
  const timer = setTimeout(() => {
    cursorPublishTimers.delete(editorId)
    const visible = visibleOverride ?? canPublishCursorState(editor)
    const position = visible ? editor.getPosition() : null
    const selection = visible ? toLiveCursorRange(editor.getSelection()) : null
    const payload: LiveCursorPayload = {
      editorKey,
      sourceId,
      version: nextCursorVersion(),
      visible,
      cursor: position ? { lineNumber: position.lineNumber, column: position.column } : null,
      selection,
    }
    publishCursorPayload(payload)
  }, delay)

  cursorPublishTimers.set(editorId, timer)
}

function publishEditorScrollState(editor: StandaloneCodeEditor, immediate = false): void {
  if (!syncEnabled.value)
    return
  if (!canPublishScrollState(editor))
    return

  const editorId = editor.getId()
  if (applyingRemoteScrollByEditorId.has(editorId))
    return

  const editorKey = getEditorKey(editor)
  if (!isEditorFromCurrentSlide(editorKey))
    return

  const existingTimer = scrollPublishTimers.get(editorId)
  if (existingTimer)
    clearTimeout(existingTimer)

  const delay = immediate ? 0 : 30
  const timer = setTimeout(() => {
    scrollPublishTimers.delete(editorId)
    const payload: LiveScrollPayload = {
      editorKey,
      sourceId,
      version: nextScrollVersion(),
      scrollTop: editor.getScrollTop(),
      scrollLeft: editor.getScrollLeft(),
    }
    publishScrollPayload(payload)
  }, delay)

  scrollPublishTimers.set(editorId, timer)
}

function reapplyCachedCursorForEditor(editor: StandaloneCodeEditor): void {
  const editorKey = getEditorKey(editor)
  if (!isEditorFromCurrentSlide(editorKey))
    return

  const cursorPayload = cachedCursorPayloadByEditorKey.get(editorKey)
  if (cursorPayload)
    applyCursorPayloadToEditor(editor, cursorPayload)
}

function reapplyCachedScrollForEditor(editor: StandaloneCodeEditor): void {
  const editorKey = getEditorKey(editor)
  if (!isEditorFromCurrentSlide(editorKey))
    return

  const scrollPayload = cachedScrollPayloadByEditorKey.get(editorKey)
  if (scrollPayload)
    applyScrollPayloadToEditor(editor, scrollPayload)
}

function applyPayloadToEditor(editor: StandaloneCodeEditor, payload: LiveCodeSyncPayload): void {
  if (!isConnectedEditor(editor))
    return
  if (!isEditorInPresenterMainPane(editor))
    return

  const model = editor.getModel()
  if (!model)
    return
  if (model.getValue() === payload.content)
    return

  const editorId = editor.getId()
  applyingRemoteByEditorId.add(editorId)
  let didUpdateContent = false
  try {
    model.setValue(payload.content)
    didUpdateContent = true
  }
  catch {
    // Ignore transient editor lifecycle errors during slide transitions.
  }
  finally {
    applyingRemoteByEditorId.delete(editorId)
    if (didUpdateContent) {
      reapplyCachedCursorForEditor(editor)
      reapplyCachedScrollForEditor(editor)
    }
  }
}

function pickPreferredEditor(editors: StandaloneCodeEditor[]): StandaloneCodeEditor | null {
  if (editors.length === 0)
    return null

  const focusedEditor = editors.find(editor => editor.hasTextFocus())
  if (focusedEditor)
    return focusedEditor

  const visibleEditor = editors.find(editor => isVisibleEditor(editor))
  if (visibleEditor)
    return visibleEditor

  return editors[0] ?? null
}

function applyCursorPayloadToEditor(editor: StandaloneCodeEditor, payload: LiveCursorPayload): void {
  if (!isConnectedEditor(editor))
    return
  if (!isEditorInPresenterMainPane(editor))
    return

  if (!payload.visible || !payload.cursor || !monaco) {
    clearCursorDecorations(editor)
    return
  }

  const model = editor.getModel()
  if (!model) {
    clearCursorDecorations(editor)
    return
  }

  const cursorPosition = model.validatePosition(payload.cursor)
  const decorations: MonacoEditorNamespace.IModelDeltaDecoration[] = [
    {
      range: new monaco.Range(
        cursorPosition.lineNumber,
        cursorPosition.column,
        cursorPosition.lineNumber,
        cursorPosition.column,
      ),
      options: {
        afterContentClassName: 'live-host-cursor-caret',
        stickiness: monaco.editor.TrackedRangeStickiness.NeverGrowsWhenTypingAtEdges,
      },
    },
  ]

  if (payload.selection && !isCollapsedRange(payload.selection)) {
    const selectionRange = model.validateRange(payload.selection)
    decorations.push({
      range: selectionRange,
      options: {
        className: 'live-host-cursor-selection',
        stickiness: monaco.editor.TrackedRangeStickiness.NeverGrowsWhenTypingAtEdges,
      },
    })
  }

  const editorId = editor.getId()
  const currentDecorationIds = cursorDecorationIdsByEditorId.get(editorId) ?? []
  const nextDecorationIds = editor.deltaDecorations(currentDecorationIds, decorations)
  cursorDecorationIdsByEditorId.set(editorId, nextDecorationIds)
}

function applyScrollPayloadToEditor(editor: StandaloneCodeEditor, payload: LiveScrollPayload): void {
  if (!isConnectedEditor(editor))
    return
  if (!isEditorInPresenterMainPane(editor))
    return

  const currentScrollTop = editor.getScrollTop()
  const currentScrollLeft = editor.getScrollLeft()
  if (currentScrollTop === payload.scrollTop && currentScrollLeft === payload.scrollLeft)
    return

  const editorId = editor.getId()
  applyingRemoteScrollByEditorId.add(editorId)
  try {
    editor.setScrollTop(payload.scrollTop)
    editor.setScrollLeft(payload.scrollLeft)
  }
  catch {
    // Ignore transient editor lifecycle errors during slide transitions.
  }
  finally {
    applyingRemoteScrollByEditorId.delete(editorId)
  }
}

function applyPayloadToMatchingEditors(payload: LiveCodeSyncPayload): void {
  if (!monaco)
    return

  const payloadSlideNo = getSlideNoFromEditorKey(payload.editorKey)
  if (payloadSlideNo !== slideNo.value)
    return

  const payloadEditorIndex = getEditorIndexFromEditorKey(payload.editorKey)
  const standaloneEditors = monaco.editor.getEditors().filter(isStandaloneCodeEditor)
  const currentSlideEditors: { editor: StandaloneCodeEditor, editorKey: string }[] = []
  for (const editor of standaloneEditors) {
    const editorKey = getEditorKey(editor)
    if (!isEditorFromCurrentSlide(editorKey))
      continue
    currentSlideEditors.push({ editor, editorKey })
  }

  const exactMatches = currentSlideEditors
    .filter(({ editorKey }) => editorKey === payload.editorKey)
    .map(({ editor }) => editor)
  const exactMatch = pickPreferredEditor(exactMatches)
  if (exactMatch) {
    applyPayloadToEditor(exactMatch, payload)
    return
  }

  const indexMatches = currentSlideEditors
    .filter(({ editorKey }) => getEditorIndexFromEditorKey(editorKey) === payloadEditorIndex)
    .map(({ editor }) => editor)
  const indexMatch = pickPreferredEditor(indexMatches)
  if (indexMatch) {
    applyPayloadToEditor(indexMatch, payload)
    return
  }

  if (currentSlideEditors.length === 1)
    applyPayloadToEditor(currentSlideEditors[0].editor, payload)
}

function applyCursorPayloadToMatchingEditors(payload: LiveCursorPayload): void {
  if (!monaco)
    return

  const payloadSlideNo = getSlideNoFromEditorKey(payload.editorKey)
  if (payloadSlideNo !== slideNo.value)
    return

  const payloadEditorIndex = getEditorIndexFromEditorKey(payload.editorKey)
  const standaloneEditors = monaco.editor.getEditors().filter(isStandaloneCodeEditor)
  const currentSlideEditors: { editor: StandaloneCodeEditor, editorKey: string }[] = []
  for (const editor of standaloneEditors) {
    const editorKey = getEditorKey(editor)
    if (!isEditorFromCurrentSlide(editorKey))
      continue
    currentSlideEditors.push({ editor, editorKey })
  }

  const exactMatches = currentSlideEditors
    .filter(({ editorKey }) => editorKey === payload.editorKey)
    .map(({ editor }) => editor)
  const exactMatch = pickPreferredEditor(exactMatches)
  if (exactMatch) {
    applyCursorPayloadToEditor(exactMatch, payload)
    return
  }

  const indexMatches = currentSlideEditors
    .filter(({ editorKey }) => getEditorIndexFromEditorKey(editorKey) === payloadEditorIndex)
    .map(({ editor }) => editor)
  const indexMatch = pickPreferredEditor(indexMatches)
  if (indexMatch) {
    applyCursorPayloadToEditor(indexMatch, payload)
    return
  }

  if (currentSlideEditors.length === 1)
    applyCursorPayloadToEditor(currentSlideEditors[0].editor, payload)
}

function applyScrollPayloadToMatchingEditors(payload: LiveScrollPayload): void {
  if (!monaco)
    return

  const payloadSlideNo = getSlideNoFromEditorKey(payload.editorKey)
  if (payloadSlideNo !== slideNo.value)
    return

  const payloadEditorIndex = getEditorIndexFromEditorKey(payload.editorKey)
  const standaloneEditors = monaco.editor.getEditors().filter(isStandaloneCodeEditor)
  const currentSlideEditors: { editor: StandaloneCodeEditor, editorKey: string }[] = []
  for (const editor of standaloneEditors) {
    const editorKey = getEditorKey(editor)
    if (!isEditorFromCurrentSlide(editorKey))
      continue
    currentSlideEditors.push({ editor, editorKey })
  }

  const exactMatches = currentSlideEditors
    .filter(({ editorKey }) => editorKey === payload.editorKey)
    .map(({ editor }) => editor)
  const exactMatch = pickPreferredEditor(exactMatches)
  if (exactMatch) {
    applyScrollPayloadToEditor(exactMatch, payload)
    return
  }

  const indexMatches = currentSlideEditors
    .filter(({ editorKey }) => getEditorIndexFromEditorKey(editorKey) === payloadEditorIndex)
    .map(({ editor }) => editor)
  const indexMatch = pickPreferredEditor(indexMatches)
  if (indexMatch) {
    applyScrollPayloadToEditor(indexMatch, payload)
    return
  }

  if (currentSlideEditors.length === 1)
    applyScrollPayloadToEditor(currentSlideEditors[0].editor, payload)
}

function canPublishEditorChanges(editor: StandaloneCodeEditor): boolean {
  if (!isConnectedEditor(editor))
    return false
  if (!isEditorInPresenterMainPane(editor))
    return false
  if (!editor.hasTextFocus())
    return false
  return true
}

function publishEditorChanges(editor: StandaloneCodeEditor): void {
  if (!syncEnabled.value)
    return
  if (!canPublishEditorChanges(editor))
    return

  const editorId = editor.getId()
  if (applyingRemoteByEditorId.has(editorId))
    return

  const editorKey = getEditorKey(editor)
  if (!isEditorFromCurrentSlide(editorKey))
    return

  const existingTimer = publishTimers.get(editorId)
  if (existingTimer)
    clearTimeout(existingTimer)

  const timer = setTimeout(() => {
    publishTimers.delete(editorId)
    const payload: LiveCodeSyncPayload = {
      editorKey,
      content: editor.getValue(),
      sourceId,
      version: nextCodeVersion(),
    }
    publishCodePayload(payload)
  }, 100)

  publishTimers.set(editorId, timer)
}

function untrackEditor(editorId: string): void {
  const editor = trackedEditors.get(editorId)
  if (editor)
    clearCursorDecorations(editor)

  const subscriptions = editorSubscriptions.get(editorId)
  subscriptions?.onChange.dispose()
  subscriptions?.onCursorPosition.dispose()
  subscriptions?.onCursorSelection.dispose()
  subscriptions?.onScroll.dispose()
  subscriptions?.onFocus.dispose()
  subscriptions?.onBlur.dispose()
  subscriptions?.onDispose.dispose()
  editorSubscriptions.delete(editorId)
  trackedEditors.delete(editorId)

  const timer = publishTimers.get(editorId)
  if (timer)
    clearTimeout(timer)
  publishTimers.delete(editorId)

  const cursorTimer = cursorPublishTimers.get(editorId)
  if (cursorTimer)
    clearTimeout(cursorTimer)
  cursorPublishTimers.delete(editorId)

  const scrollTimer = scrollPublishTimers.get(editorId)
  if (scrollTimer)
    clearTimeout(scrollTimer)
  scrollPublishTimers.delete(editorId)
  applyingRemoteByEditorId.delete(editorId)
  applyingRemoteScrollByEditorId.delete(editorId)
  cursorDecorationIdsByEditorId.delete(editorId)
}

function trackEditor(editor: StandaloneCodeEditor): void {
  const editorId = editor.getId()
  if (trackedEditors.has(editorId))
    return
  if (!isConnectedEditor(editor))
    return
  if (!isEditorInPresenterMainPane(editor))
    return

  const editorKey = getEditorKey(editor)
  if (!isEditorFromCurrentSlide(editorKey))
    return

  trackedEditors.set(editorId, editor)

  const onChange = editor.onDidChangeModelContent(() => publishEditorChanges(editor))
  const onCursorPosition = editor.onDidChangeCursorPosition(() => publishEditorCursorState(editor))
  const onCursorSelection = editor.onDidChangeCursorSelection(() => publishEditorCursorState(editor))
  const onScroll = editor.onDidScrollChange(() => publishEditorScrollState(editor))
  const onFocus = editor.onDidFocusEditorText(() => publishEditorCursorState(editor, true, true))
  const onBlur = editor.onDidBlurEditorText(() => publishEditorCursorState(editor, false, true))
  const onDispose = editor.onDidDispose(() => untrackEditor(editorId))
  editorSubscriptions.set(editorId, { onChange, onCursorPosition, onCursorSelection, onScroll, onFocus, onBlur, onDispose })

  // Restore from cache: local cache first, then sharedState
  const cachedPayload = cachedPayloadByEditorKey.get(editorKey)
  const sharedPayload = syncState.liveCodeSync
  let payloadToApply: LiveCodeSyncPayload | null = null
  if (cachedPayload) {
    payloadToApply = cachedPayload
  }
  else if (sharedPayload && getSlideNoFromEditorKey(sharedPayload.editorKey) === slideNo.value) {
    // Restore from shared state and seed local cache
    payloadToApply = sharedPayload
    latestRemoteVersionByEditorKey.set(sharedPayload.editorKey, sharedPayload.version)
    cachedPayloadByEditorKey.set(sharedPayload.editorKey, sharedPayload)
  }
  if (payloadToApply)
    applyPayloadToEditor(editor, payloadToApply)

  // Restore cursor state
  const cachedCursor = cachedCursorPayloadByEditorKey.get(editorKey)
  const sharedCursor = syncState.liveCursorSync
  let cursorPayloadToApply: LiveCursorPayload | null = null
  if (cachedCursor) {
    cursorPayloadToApply = cachedCursor
  }
  else if (sharedCursor && getSlideNoFromEditorKey(sharedCursor.editorKey) === slideNo.value) {
    cursorPayloadToApply = sharedCursor
    latestRemoteCursorVersionByEditorKey.set(sharedCursor.editorKey, sharedCursor.version)
    cachedCursorPayloadByEditorKey.set(sharedCursor.editorKey, sharedCursor)
  }
  if (cursorPayloadToApply)
    applyCursorPayloadToEditor(editor, cursorPayloadToApply)

  // Restore scroll state
  const cachedScroll = cachedScrollPayloadByEditorKey.get(editorKey)
  const sharedScroll = syncState.liveScrollSync
  let scrollPayloadToApply: LiveScrollPayload | null = null
  if (cachedScroll) {
    scrollPayloadToApply = cachedScroll
  }
  else if (sharedScroll && getSlideNoFromEditorKey(sharedScroll.editorKey) === slideNo.value) {
    scrollPayloadToApply = sharedScroll
    latestRemoteScrollVersionByEditorKey.set(sharedScroll.editorKey, sharedScroll.version)
    cachedScrollPayloadByEditorKey.set(sharedScroll.editorKey, sharedScroll)
  }
  if (scrollPayloadToApply)
    applyScrollPayloadToEditor(editor, scrollPayloadToApply)
}

function stopSync(): void {
  createEditorListener?.dispose()
  createEditorListener = null
  for (const editor of trackedEditors.values())
    publishEditorCursorState(editor, false, true)

  for (const editorId of Array.from(trackedEditors.keys()))
    untrackEditor(editorId)
}

async function startSync(): Promise<void> {
  if (typeof window === 'undefined')
    return

  if (!monaco)
    monaco = await import('monaco-editor')

  for (const editor of monaco.editor.getEditors()) {
    if (isStandaloneCodeEditor(editor))
      trackEditor(editor)
  }

  createEditorListener = monaco.editor.onDidCreateEditor((editor) => {
    if (isStandaloneCodeEditor(editor))
      trackEditor(editor)
  })
}

function handleRemotePayload(payload?: LiveCodeSyncPayload): void {
  if (!payload)
    return
  if (payload.sourceId === sourceId)
    return
  if (getSlideNoFromEditorKey(payload.editorKey) !== slideNo.value)
    return

  const knownVersion = latestRemoteVersionByEditorKey.get(payload.editorKey) ?? 0
  if (payload.version <= knownVersion)
    return

  latestRemoteVersionByEditorKey.set(payload.editorKey, payload.version)
  cachedPayloadByEditorKey.set(payload.editorKey, payload)

  if (syncEnabled.value)
    applyPayloadToMatchingEditors(payload)
}

function handleRemoteCursorPayload(payload?: LiveCursorPayload): void {
  if (!payload)
    return
  if (payload.sourceId === sourceId)
    return
  if (getSlideNoFromEditorKey(payload.editorKey) !== slideNo.value)
    return

  const knownVersion = latestRemoteCursorVersionByEditorKey.get(payload.editorKey) ?? 0
  if (payload.version <= knownVersion)
    return

  latestRemoteCursorVersionByEditorKey.set(payload.editorKey, payload.version)
  cachedCursorPayloadByEditorKey.set(payload.editorKey, payload)

  if (syncEnabled.value)
    applyCursorPayloadToMatchingEditors(payload)
}

function handleRemoteScrollPayload(payload?: LiveScrollPayload): void {
  if (!payload)
    return
  if (payload.sourceId === sourceId)
    return
  if (getSlideNoFromEditorKey(payload.editorKey) !== slideNo.value)
    return

  const knownVersion = latestRemoteScrollVersionByEditorKey.get(payload.editorKey) ?? 0
  if (payload.version <= knownVersion)
    return

  latestRemoteScrollVersionByEditorKey.set(payload.editorKey, payload.version)
  cachedScrollPayloadByEditorKey.set(payload.editorKey, payload)

  if (syncEnabled.value)
    applyScrollPayloadToMatchingEditors(payload)
}

const stopWatchingPayload = watch(
  () => syncState.liveCodeSync,
  payload => handleRemotePayload(payload),
  { immediate: true, deep: true },
)

const stopWatchingCursorPayload = watch(
  () => syncState.liveCursorSync,
  payload => handleRemoteCursorPayload(payload),
  { immediate: true, deep: true },
)

const stopWatchingScrollPayload = watch(
  () => syncState.liveScrollSync,
  payload => handleRemoteScrollPayload(payload),
  { immediate: true, deep: true },
)

const stopWatchingSyncEnabled = watch(
  syncEnabled,
  (enabled) => {
    if (enabled)
      void startSync()
    else
      stopSync()
  },
  { immediate: true },
)

onBeforeUnmount(() => {
  stopWatchingPayload()
  stopWatchingCursorPayload()
  stopWatchingScrollPayload()
  stopWatchingSyncEnabled()
  stopSync()
})
</script>

<style scoped>
.monaco-sync-activator {
  display: none;
}
</style>
