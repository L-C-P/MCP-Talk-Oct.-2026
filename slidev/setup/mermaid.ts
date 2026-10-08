// setup/mermaid.ts
import {defineMermaidSetup} from '@slidev/types'

export default defineMermaidSetup(() => {
  return {
    theme: 'neutral',
    flowchart: {
      curve: 'rounded',
      rankSpacing: 100,
    },
  }
})
