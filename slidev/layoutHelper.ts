/**
 * Colour variants of the adesso PowerPoint master layouts.
 *
 * white    – WHITE layouts: white background, blue text
 * blue     – BLUE layouts: solid tx2 background, white text
 * gradient – GRADIENT layouts: turquoise → blue gradient, white text
 * dark     – DARK layouts: dark blue gradient, white text
 */
export type AdessoVariant = 'white' | 'blue' | 'gradient' | 'dark'

/** Default variant per layout, chosen to match the master layout it replicates. */
const DEFAULT_VARIANTS: Record<string, AdessoVariant> = {
  cover: 'gradient', // Titleslide GRADIENT
  intro: 'blue', // Intro/Outro
  section: 'gradient', // Section Header GRADIENT
  default: 'white', // Title and Content WHITE
  agenda: 'white', // Agenda
  blank: 'white', // Blank WHITE
  end: 'blue', // Contact slide
}

export function resolveVariant(layout: string, variant?: string): AdessoVariant {
  return (variant as AdessoVariant | undefined) ?? DEFAULT_VARIANTS[layout] ?? 'white'
}

export function isDarkVariant(variant: AdessoVariant): boolean {
  return variant !== 'white'
}
