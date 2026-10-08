# AGENTS.md

Guidance for agents working on the Slidev deck in this directory.

## Scope
- Applies to `slidev/` content.
- Inherits repository defaults from `../AGENTS.md`.
- Always read `../AGENTS.md` at the start of each new session before making changes in `slidev/`.

## Source files
- Deck: `slides.md`
- Layouts (adesso PowerPoint master): `layouts/*.vue`
- Styling: `styles/style.css` (imported via `style.css`)
- Layout variants: `layoutHelper.ts`
- Footer: `slide-top.vue`
- Mermaid colours: `styles/theme.ts` (used by `setup/mermaid.ts`)
- App setup hooks: `setup/main.ts`

## Design system (adesso PowerPoint master)
The layouts replicate `adesso-PPT-Master.potx`. Positions are % of the 16:9 slide as in the master, font sizes are
master points via `var(--pt)`, colours are the master theme colours (`--color-primary` = `#006ec7`, tx2).

| Layout | Master layout | `variant:` (default first) |
|---|---|---|
| `cover` | Titleslide | `gradient`, `blue`, `white` |
| `intro` | Intro/Outro (logo only) | – |
| `section` | Section Header | `gradient`, `white` |
| `default` | Title and Content (pill top left) | `white`, `dark` |
| `agenda` | Agenda (numbered, for `<Toc />`) | – |
| `blank` | Blank (code and terminal slides) | `white`, `dark` |
| `end` | Contact slide | – |

- `class: blank--center` centres the content of a `blank` slide (used for terminal casts and images).
- `cover` shows "Place | Date" at the bottom: `place:` in the slide frontmatter plus `footer.date` from the headmatter.
- Graphics from the master live in `assets/` (logos, arrows, pills, gradients).

## Footer (`slide-top.vue`)
- Rendered on every slide (presentation, presenter view, overview, export) at the master positions:
  slide number, presentation title, pill + date, adesso logo.
- Central configuration in the headmatter: `footer: { text, date, pageNumber, logo }`.
- Per slide: `footer: false` hides it, `footer: { … }` overrides single fields. The first slide's frontmatter is the
  headmatter, so it has no per-slide override.
- Hidden on `cover` and `intro`; white on `blue`, `gradient` and `dark` variants, blue otherwise.

## Fitting slides (`slidev-addon-autofit`)
- Overflowing slides are scaled automatically by the addon `slidev-addon-autofit` (npm, registered in the headmatter).
  It sets Slidev's `--slidev-slide-zoom-scale` before the slide is painted.
- Never change content sizes (code block heights, images, fonts) to make a slide fit. Use a manual `zoom:` only for a
  deliberate override (it takes precedence over autofit); `autofit: false` disables the addon for a slide.
- Layouts keep frame, title and footer at master size under zoom by dividing by `var(--slidev-slide-zoom-scale)`;
  keep that compensation when changing paddings or title sizes.

## Editing guidance for style changes
- Prefer changing `styles/style.css` or a layout over inline styles in `slides.md`.
- Choose a layout and `variant:` in the frontmatter instead of adding background images or colour classes.
- Rules that must beat the default theme's layout styles (e.g. `.slidev-layout.section h1`) need the `.slidev-layout`
  prefix for specificity.
- For quotes in `slides.md`, use Markdown quote syntax (`> ...`).
- Keep Mermaid labels compatible: use `<br/>`, never literal `\n`.

## Presenter countdown behavior (`setup/main.ts`)
- The deck contains a router hook that auto-starts the presenter countdown when the active presenter slide changes.
- Implementation details:
  - Parses presenter route slide ids from paths like `/presenter/<id>`.
  - Runs in `router.afterEach`.
  - Only triggers when both old and new presenter slide ids exist and differ.
  - Tries to click the presenter timer play toggle via DOM query:
    - `.slidev-presenter .grid-section.bottom .i-carbon\:play`
  - Uses a double `requestAnimationFrame` retry to handle delayed DOM rendering.
- If presenter UI structure or icon classes change in Slidev updates, this selector logic may need to be adjusted.

## Quick checks before handoff
- `npm run build` succeeds.
- Title, section and content slides match the master (pill, title position, footer).
- No slide content runs into the footer (autofit active, no manual `zoom:` left over unintentionally).
- Footer shows on all slides except `cover` and `intro`, in the right colour.
