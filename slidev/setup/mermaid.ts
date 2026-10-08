// setup/mermaid.ts
import {defineMermaidSetup} from '@slidev/types'
import * as theme from '../styles/theme'

export default defineMermaidSetup(() => {
  return {
    theme: 'base',
    themeVariables: {
      titleColor: theme.BLACK,
      lineColor: theme.PRIMARY,
      primaryBorderColor: theme.BLACK,
      primaryColor: theme.WHITE,
      primaryTextColor: theme.BLACK,
      secondaryColor: theme.LIGHT_GRAY,
      tertiaryColor: theme.LIGHT_GRAY,
      background: theme.WHITE,
      clusterBkg: theme.WHITE,
      clusterBorder: theme.GRAY,
      edgeLabelBackground: theme.WHITE,

      // Sequence Diagram
      actorBkg: theme.WHITE,
      actorBorder: theme.BLACK,
      actorTextColor: theme.BLACK,
      actorLineColor: theme.BLACK,
      signalColor: theme.PRIMARY,
      signalTextColor: theme.BLACK,
      labelBoxBkgColor: theme.WHITE,
      labelBoxBorderColor: theme.PRIMARY,
      loopTextColor: theme.BLACK,
      noteBkgColor: theme.WHITE,
      noteBorderColor: theme.PRIMARY,
      noteTextColor: theme.BLACK,
    },

    flowchart: {
      curve: 'rounded',
      rankSpacing: 100,
    },
  }
})
