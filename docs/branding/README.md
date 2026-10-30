# Codlek brand assets

The approved identity is the owner's brand sheet **"Codlek — الهوية النهائية (8a)"**, palette **Navy Depth**, kept as
`source/codlek-logo-design.pdf`. It is the visual source of truth; nothing here was redrawn.

## Logo files
The sheet is a vector PDF: the mark is drawn as filled paths and the wordmark as glyph outlines. The SVGs below are those
exact shapes and colours, extracted by `source/extract_logo.py` (development tool; it uses PyMuPDF, which is AGPL and
is never shipped). Re-running it reproduces the files byte for byte.

| File | Use |
|---|---|
| `codlek-logo.svg` | Full logo (mark + "Codlek") on light backgrounds: documents, light screens. Aspect ratio 4.15 : 1. |
| `codlek-logo-reversed.svg` | Full logo on navy/dark backgrounds (sidebar, sign-in panel): the C and the wordmark white, the blue dash and squares unchanged, as the sheet's own app tile shows them. |
| `codlek-mark.svg`, `codlek-mark-reversed.svg` | The mark alone (collapsed sidebar). |
| `codlek-app-icon.svg`, `codlek-app-icon-512.png` | The sheet's navy app tile (favicon, app icons). |

Rules from the sheet: the dash and the two squares are `#0B63CE`; the C and the word "Codlek" are `#0B1B3F` (white in
the reversed variant). Size by height only; never stretch, recolour or outline. Keep clear space of at least half the
mark's height. Below about 90 px wide use the mark instead of the full logo.

**Where the copies live** (each repository manages its own; no absolute paths, no external hosting):
- API: `src/BuildingBlocks/WarehouseERP.BuildingBlocks.Infrastructure/Documents/Assets/codlek-logo.svg` (embedded resource,
  the default PDF logo; `Documents:LogoPath` can replace it with a company's own SVG).
- Web: `src/assets/brand/*.svg` (imported by `src/shared/ui/Brand.tsx`), `public/favicon.svg`, `public/favicon.ico`
  (16/32/48 px rendered from the tile), `public/apple-touch-icon.png` (180 px).

## Palette "Navy Depth" (exact values from the sheet)
| Role | HEX |
|---|---|
| Primary (logo) | `#0B63CE` |
| Navy (logo) | `#0B1B3F` |
| Accent 1 | `#0A2A55` |
| Accent 2 | `#1E4E8C` |
| Text | `#0B1424` |
| Background | `#F5F7FA` |
| Surface | `#FFFFFF` |
| Border | `#E2E7EE` |
| Muted text | `#5B6676` |

The sheet also says: no red and no loud colours in the site palette. Semantic colours (success, warning, info,
destructive) therefore keep their own hues and are not replaced by the brand blue.

### Derived values (not on the sheet; chosen for contrast)
| Token | Value | Why |
|---|---|---|
| `--secondary`, `--muted` | `#EEF2F7` | between Background and Border |
| `--accent` (hover/selection tint) | `#E8F0FB` | light primary tint; Accent 1 text on it is 12.4 : 1 |
| `--input` | `#CDD5E0` | fields read as fields against the Surface |
| `--sidebar-foreground` | `#D5DCE6` | 11 : 1 on navy |
| `--sidebar-primary`, `--sidebar-ring` | `#4D8FE0` | primary lightened to 5.1 : 1 on navy, for icons and focus |
| `--sidebar-border` | `#1B2C50` | hairline on navy |

### Contrast (WCAG 2.x)
White on Primary 5.69 : 1 · Primary on Surface 5.69 : 1 · Text on Background 17.2 : 1 · Muted text on Background 5.42 : 1
and on Surface 5.82 : 1 · white on Accent 2 (selected navigation) 8.32 : 1 · white on Accent 1 (hover) 14.2 : 1. All
body-text pairs pass AA. The logo's own blue on navy (2.97 : 1) is used only inside the mark, never for text.

The web app has no dark theme today (only light tokens exist); the reversed logo is ready for one.
