import { defineAppSetup } from '@slidev/types'
import { slides } from '#slidev/slides'

export default defineAppSetup(({ router }) => {
  const normalizePresenterPath = (path: string) => {
    return path
      .replace(/^\/presenter\.\//, '/presenter/')
      .replace(/^\/presenter\/(?:presenter\/)+/, '/presenter/')
  }

  const getPresenterSlideId = (path: string) => {
    const match = path.match(/\/presenter\/([^/?#]+)/)
    return match?.[1]
  }
  const getPresenterSlideNo = (path: string) => {
    const slideId = getPresenterSlideId(path)
    if (!slideId)
      return null

    const slideNo = Number(slideId)
    if (!Number.isInteger(slideNo))
      return null

    return slideNo
  }
  const getLastPresenterSlideNo = () => {
    return slides.value.reduce((maxSlideNo, slide) => {
      if (typeof slide.no !== 'number')
        return maxSlideNo

      return Math.max(maxSlideNo, slide.no)
    }, 0)
  }
  const clickPresenterTimerPlay = () => {
    if (typeof document === 'undefined')
      return false

    const playIcon = document.querySelector<HTMLElement>('.slidev-presenter .grid-section.bottom .i-carbon\\:play')
    const toggleButton = playIcon?.parentElement as HTMLElement | null
    if (!toggleButton)
      return false

    toggleButton.click()
    return true
  }
  const clickPresenterTimerPause = () => {
    if (typeof document === 'undefined')
      return false

    const pauseIcon = document.querySelector<HTMLElement>('.slidev-presenter .grid-section.bottom .i-carbon\\:pause')
    const toggleButton = pauseIcon?.parentElement as HTMLElement | null
    if (!toggleButton)
      return false

    toggleButton.click()
    return true
  }

  let timerControlRunId = 0

  const schedulePresenterTimerControl = (action: 'start' | 'stop') => {
    timerControlRunId += 1
    const runId = timerControlRunId
    const maxAttempts = 6
    const retryDelayMs = 250

    const attemptTimerControl = (attempt: number) => {
      if (runId !== timerControlRunId)
        return

      const currentPresenterSlide = getPresenterSlideNo(router.currentRoute.value.path)
      if (currentPresenterSlide === null)
        return

      const lastPresenterSlideNo = getLastPresenterSlideNo()

      if (action === 'start' && currentPresenterSlide === lastPresenterSlideNo)
        return

      if (action === 'stop' && currentPresenterSlide !== lastPresenterSlideNo)
        return

      if (action === 'start')
        clickPresenterTimerPlay()
      else
        clickPresenterTimerPause()

      if (attempt >= maxAttempts)
        return

      setTimeout(() => {
        requestAnimationFrame(() => {
          attemptTimerControl(attempt + 1)
        })
      }, retryDelayMs)
    }

    requestAnimationFrame(() => {
      if (runId !== timerControlRunId)
        return
      attemptTimerControl(1)
    })
  }
  const schedulePresenterTimerAutoStart = () => {
    schedulePresenterTimerControl('start')
  }
  const schedulePresenterTimerStop = () => {
    schedulePresenterTimerControl('stop')
  }
  router.beforeEach((to) => {
    const normalizedPath = normalizePresenterPath(to.path)
    if (normalizedPath === to.path)
      return true

    return {
      path: normalizedPath,
      query: to.query,
      hash: to.hash,
      replace: true,
    }
  })

  router.afterEach((to, from) => {
    const toSlide = getPresenterSlideNo(to.path)
    const fromSlide = getPresenterSlideNo(from.path)
    if (toSlide === null || toSlide === fromSlide)
      return

    if (toSlide === getLastPresenterSlideNo()) {
      schedulePresenterTimerStop()
      return
    }

    if (fromSlide === null)
      return

    schedulePresenterTimerAutoStart()
  })
})
