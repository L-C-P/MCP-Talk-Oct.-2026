<template>
  <RenderWhen context="main">
    <div ref="playerContainer"/>
  </RenderWhen>
</template>

<script setup>
import {ref, computed, watch, onMounted, onBeforeUnmount} from "vue";
import RenderWhen from "@slidev/client/builtin/RenderWhen.vue";
import {sharedState, useIsSlideActive, useSlideContext} from "@slidev/client";
import * as AsciinemaPlayer from "asciinema-player";

/**
 * Payload shape for the shared-state sync channel.
 * Mirrors the CodeBlockSync pattern: versioned, source-tagged, slide-scoped.
 */
const props = defineProps({
  src: {type: String, required: true},
  props: {type: Object, default: () => ({})},
  markers: {type: Array, default: () => []},
});

const defaultProps = {
  autoPlay: true,
  fit: false,
  idleTimeLimit: 5,
  terminalFontFamily: 'Hack Nerd Font',
  terminalFontSize: '13px',
  controls: false,
  pauseOnMarkers: false,
};

const mergedPlayerProps = computed(() => ({
  ...defaultProps,
  ...props.props,
  ...(props.markers.length > 0 && {markers: props.markers})
}));

const playerContainer = ref(null);
const player = ref(null);
const visibilityObserver = ref(null);
const wasVisible = ref(false);
const playDelayHandler = ref(null);

// --- Presenter ↔ client sync state ---
const { $page, $renderContext } = useSlideContext()
const isSlideActive = useIsSlideActive()
const slideNo = computed(() => String($page.value))
const isPresenter = computed(() => $renderContext.value === 'presenter')

const sourceId = typeof crypto !== 'undefined' && 'randomUUID' in crypto
  ? crypto.randomUUID()
  : `asciinema-sync-${Math.random().toString(36).slice(2)}`

let syncVersionCounter = 0
let applyingRemote = false
let latestRemoteVersion = 0
let cachedRemotePayload = null
let lastPublishedPosition = -1
let positionPollTimer = null

function nextSyncVersion() {
  syncVersionCounter += 1
  return syncVersionCounter
}

function publishPlayerState(playing, currentTime) {
  const payload = {
    src: props.src,
    sourceId,
    version: nextSyncVersion(),
    playing,
    currentTime,
  }

  if (typeof sharedState.$patch === 'function') {
    void sharedState.$patch({ liveAsciinemaPlayer: payload })
    return
  }

  sharedState.lastUpdate = {
    id: sourceId,
    type: 'presenter',
    time: Date.now(),
  }
  const currentSlideNo = Number.parseInt(slideNo.value, 10)
  if (Number.isFinite(currentSlideNo))
    sharedState.page = currentSlideNo
  sharedState.liveAsciinemaPlayer = payload
}

// --- Presenter: react to local player events ---
const POLL_INTERVAL = 250
const SEEK_THRESHOLD = 0.5 // seconds – unexpected position change = seek

let isCurrentlyPlaying = false
let lastPollTime = 0

function onPlayerPlay() {
  if (!isPresenter.value || !player.value) return
  isCurrentlyPlaying = true
  player.value.getCurrentTime().then(t => {
    lastPublishedPosition = t
    lastPollTime = Date.now()
    publishPlayerState(true, t)
    startPositionPolling()
  })
}

function onPlayerPause() {
  if (!isPresenter.value || !player.value) return
  isCurrentlyPlaying = false
  player.value.getCurrentTime().then(t => {
    lastPublishedPosition = t
    lastPollTime = Date.now()
    publishPlayerState(false, t)
    // Polling continues – detects seeks in both paused and playing state
  })
}

/**
 * Poll position continuously. Detects:
 * - Paused + position changed → manual scrub
 * - Playing + position jumped unexpectedly → seek during playback
 */
function startPositionPolling() {
  stopPositionPolling()
  if (!isPresenter.value) return
  lastPollTime = Date.now()
  positionPollTimer = setInterval(() => {
    if (!isPresenter.value || !player.value) return
    const now = Date.now()
    const elapsed = (now - lastPollTime) / 1000
    lastPollTime = now

    player.value.getCurrentTime().then(t => {
      const expectedDelta = isCurrentlyPlaying ? elapsed : 0
      const actualDelta = t - lastPublishedPosition
      const jumpDelta = Math.abs(actualDelta - expectedDelta)

      if (jumpDelta > SEEK_THRESHOLD) {
        lastPublishedPosition = t
        publishPlayerState(isCurrentlyPlaying, t)
      }
    })
  }, POLL_INTERVAL)
}

function stopPositionPolling() {
  if (positionPollTimer) {
    clearInterval(positionPollTimer)
    positionPollTimer = null
  }
}

// --- Client: apply remote state ---
function applyRemoteState(payload) {
  if (!payload || payload.sourceId === sourceId) return
  if (payload.version <= latestRemoteVersion) return

  latestRemoteVersion = payload.version
  cachedRemotePayload = payload

  if (isPresenter.value || !player.value) return
  if (!isSlideActive.value) return

  applyingRemote = true
  const rawSeekTarget = payload.currentTime ?? 0

  Promise.all([
    player.value.getCurrentTime(),
    player.value.getDuration(),
  ]).then(([currentPosition, duration]) => {
    // Clamp seek position to just before end to avoid empty screen
    const maxPos = duration != null ? Math.max(0, duration - 0.05) : rawSeekTarget
    const seekTarget = Math.min(Math.max(0, rawSeekTarget), maxPos)
    const positionDelta = Math.abs(currentPosition - seekTarget)

    const applyPlayPause = () => {
      if (payload.playing)
        player.value.play()
      else
        player.value.pause()
    }

    // Seek when: position changed significantly (>0.3s) OR when paused and position differs
    const shouldSeek = positionDelta > 0.3 || (!payload.playing && positionDelta > 0.01)

    if (shouldSeek) {
      player.value.seek(seekTarget).then(() => {
        applyPlayPause()
      }).finally(() => {
        setTimeout(() => { applyingRemote = false }, 80)
      })
    } else {
      // Only play/pause transition at roughly the same position → no seek
      applyPlayPause()
      setTimeout(() => { applyingRemote = false }, 80)
    }
  })
}

// TEMPORARY: built-in sync disabled.
// const stopWatchingSync = watch(
//   () => sharedState.liveAsciinemaPlayer,
//   payload => applyRemoteState(payload),
//   { immediate: true, deep: true },
// )
const stopWatchingSync = () => {}

// --- Focus / blur helpers (unchanged) ---
const blurFocusedPlayerElement = () => {
  const activeElement = document.activeElement;
  const target = playerContainer.value;

  if (
    activeElement instanceof HTMLElement &&
    target &&
    target.contains(activeElement)
  ) {
    // activeElement.blur();
  }
};

const onPointerUp = () => {
  requestAnimationFrame(blurFocusedPlayerElement);
};

const onFocusIn = () => {
  requestAnimationFrame(blurFocusedPlayerElement);
};

onMounted(() => {
  const target = playerContainer.value;
  if (!(target instanceof HTMLElement)) return;

  player.value = AsciinemaPlayer.create(
    props.src,
    target,
    mergedPlayerProps.value
  );

  // TEMPORARY: built-in sync disabled.
  // Register presenter-side event listeners
  // player.value.addEventListener('play', onPlayerPlay);
  // player.value.addEventListener('playing', onPlayerPlay);
  // player.value.addEventListener('pause', onPlayerPause);

  target.addEventListener("pointerup", onPointerUp);
  target.addEventListener("focusin", onFocusIn);

  visibilityObserver.value = new IntersectionObserver(
    ([entry]) => {
      const isVisible = entry.isIntersecting && entry.intersectionRatio > 0;

      if (isVisible && !wasVisible.value && player.value) {
        player.value.pause();

        if (mergedPlayerProps.value?.numberStartAt)
          player.value.seek(mergedPlayerProps.value.numberStartAt);
        else
          player.value.seek(0);

        if (mergedPlayerProps.value?.autoPlay) {
          if (mergedPlayerProps.value?.delayStart) {
            playDelayHandler.value = setTimeout(
              () => player.value.play(),
              mergedPlayerProps.value.delayStart * 1000
            );
          } else {
            player.value.play();
          }
        }
      } else if (!isVisible && wasVisible.value && player.value) {
        player.value.pause();
        player.value.seek(0);
      }

      wasVisible.value = isVisible;
    },
    {threshold: 0.1}
  );

  visibilityObserver.value.observe(target);

  // TEMPORARY: built-in sync disabled.
  // If a remote state was cached before the player was ready, apply it now
  // if (cachedRemotePayload && !isPresenter.value) {
  //   applyRemoteState(cachedRemotePayload);
  // }
});

onBeforeUnmount(() => {
  if (playDelayHandler.value) {
    clearTimeout(playDelayHandler.value);
    playDelayHandler.value = null;
  }

  const target = playerContainer.value;
  if (target instanceof HTMLElement) {
    target.removeEventListener("pointerup", onPointerUp);
    target.removeEventListener("focusin", onFocusIn);
  }

  visibilityObserver.value?.disconnect();
  visibilityObserver.value = null;

  stopPositionPolling()
  stopWatchingSync();
});
</script>

<style>
@import 'asciinema-player/dist/bundle/asciinema-player.css';

.slidev-presenter .ap-player:focus-within {
  outline: 2px solid var(--brand-primary, #4c9aff);
  outline-offset: 3px;
  border-radius: 4px;
}
</style>
