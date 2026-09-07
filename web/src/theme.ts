/**
 * Griot web theme — MUI v6.
 *
 * Single source of visual style (web/.agents/skills/material-ui-theme/SKILL.md).
 * Token contract: project-kit/context/ui-tokens.md
 * Design system + inspo provenance: docs/design/MASTER-DESIGN-SYSTEM.md
 *
 * Every value traces to an inspo reference; no ad-hoc colors/spacing allowed
 * outside this module (ui-rules.md #1, web feature spec 02).
 */
import { createTheme } from '@mui/material/styles';
import type { Shadows } from '@mui/material/styles';

// ---------------------------------------------------------------------------
// Tokens — project-kit/context/ui-tokens.md (sources in comments = inspo #)
// ---------------------------------------------------------------------------

export const tokens = {
  color: {
    canvas: {
      base: '#F7F8FA', // 1,3,4,8 cool-gray app canvas
      raised: '#FFFFFF', // all — cards, menus, sheets
      subtle: '#FAFAFA', // 2 — row hover
      soft: '#EDEDED', // 2 — segmented track, empty tracks, pressed
    },
    chrome: {
      ink: '#1E2022', // 1,10 — dark topbar/dock, primary CTA, toasts
      inkHover: '#33363A', // derived lighten of chrome.ink
    },
    ink: {
      primary: '#282828', // 2
      secondary: '#757575', // 2
      soft: '#A1A1A1', // 2 — disabled, overlines, timestamps
      inverse: '#FFFFFF', // 1 — text on chrome
    },
    border: {
      base: '#ECECEC', // 2
      subtle: '#F7F7F7', // 2
      inactive: '#CCCCCC', // 2
    },
    accent: {
      primary: '#3572F6', // 2 — links, selection, focus (not the CTA)
      primaryHover: '#2B5FD9', // derived darken for hover states
      soft: '#CEDAF3', // 2 — selected rows, skeleton base
      wash: '#FFF7ED', // 2 — hover wash ("blue 50")
      secondary: '#7C6CF6', // 6,7,9,11 — violet series/mobile gradients
      secondaryHover: '#6A59E8', // derived darken
      secondarySoft: '#EDE9FE', // 6,7
    },
    state: {
      success: '#47BA39', // 2 == TaskStatus.Done
      warning: '#FF7F1C', // 2 == TaskStatus.InReview
      danger: '#F14C43', // 2 == Priority.Urgent, overdue
      info: '#0BC1E6', // 2 == TaskStatus.InProgress
    },
    chart: {
      orange: '#F59E0B', // 1,5 == Priority.High, series color
      pink: '#EC4899', // 6,7 — series, notification dot
    },
    heatmap: ['#EFF4FF', '#BFDBFE', '#93C5FD', '#60A5FA', '#3572F6', '#1D4ED8'], // 8
    series: ['#3572F6', '#7C6CF6', '#EC4899', '#0BC1E6', '#F59E0B', '#47BA39'], // fixed order, 1,3,5,6,7
  },
  dark: {
    canvas: { base: '#0E0F11', raised: '#17181B' }, // derived variant
    border: 'rgba(255,255,255,0.08)',
  },
  radius: { sm: 10, pill: 999, md: 16, lg: 20, xl: 24 }, // §4.5
  space: { xs: 4, sm: 8, md: 12, lg: 16, xl: 24, xl2: 32, xl3: 48 }, // 8-pt
  motion: {
    fast: 150, // app shell
    medium: 250, // overlays
    ease: 'cubic-bezier(0.2, 0, 0, 1)',
  },
  elevation: {
    e0: 'none',
    e1: '0 1px 2px rgba(16,24,32,0.04)', // hover lift
    e2: '0 8px 24px rgba(16,24,32,0.08)', // popover/toast (+1px border)
    e3: '0 24px 64px rgba(16,24,32,0.16)', // modal
  },
  font: {
    body: 'Inter, system-ui, -apple-system, "Segoe UI", sans-serif',
    display: '"Space Grotesk", Inter, system-ui, sans-serif',
    mono: '"IBM Plex Mono", ui-monospace, "SFMono-Regular", monospace',
  },
  layout: { sidebar: 248, iconRail: 56, rowHeight: 44, columnMin: 272, columnMax: 320 },
} as const;

// Soft chip background: base at ~14% alpha over white (text on chip = base).
export const softFill = (hex: string): string => `${hex}24`;

// Severity maps — visual == EF Core enum (ui-tokens.md; web feature spec 02).
export const taskStatusColor: Record<string, string> = {
  Backlog: tokens.color.ink.soft,
  Todo: tokens.color.accent.primary,
  InProgress: tokens.color.state.info,
  InReview: tokens.color.state.warning,
  Done: tokens.color.state.success,
};
export const priorityColor: Record<string, string> = {
  Low: tokens.color.state.info,
  Medium: tokens.color.state.warning,
  High: tokens.color.chart.orange,
  Urgent: tokens.color.state.danger,
};

// ---------------------------------------------------------------------------
// Theme
// ---------------------------------------------------------------------------

declare module '@mui/material/styles' {
  interface Theme {
    griot: typeof tokens;
  }
  interface ThemeOptions {
    griot: typeof tokens;
  }
}

// Primary CTA = dark chrome (inspo 1 topbar CTA, inspo 10 "Stop Charging").
const chromeButton = {
  background: tokens.color.chrome.ink,
  color: tokens.color.ink.inverse,
  '&:hover': { background: tokens.color.chrome.inkHover },
};

export const theme = createTheme({
  griot: tokens,
  palette: {
    mode: 'light',
    primary: {
      main: tokens.color.accent.primary, // 2 — links/selection/focus
      light: tokens.color.accent.soft,
      dark: tokens.color.accent.primaryHover,
      contrastText: '#FFFFFF',
    },
    secondary: {
      main: tokens.color.accent.secondary, // 6,7,9,11
      light: tokens.color.accent.secondarySoft,
      dark: tokens.color.accent.secondaryHover,
      contrastText: '#FFFFFF',
    },
    success: { main: tokens.color.state.success },
    warning: { main: tokens.color.state.warning },
    error: { main: tokens.color.state.danger },
    info: { main: tokens.color.state.info },
    background: {
      default: tokens.color.canvas.base, // 1,3,4,8
      paper: tokens.color.canvas.raised, // all
    },
    text: {
      primary: tokens.color.ink.primary, // 2
      secondary: tokens.color.ink.secondary, // 2
      disabled: tokens.color.ink.soft,
    },
    divider: tokens.color.border.base,
  },
  shape: { borderRadius: tokens.radius.md },
  spacing: tokens.space.sm, // 8-pt grid base
  typography: {
    fontFamily: tokens.font.body,
    h1: { fontFamily: tokens.font.display, fontSize: '2rem', lineHeight: 34 / 28, fontWeight: 700, letterSpacing: '-0.5px' }, // 1
    h2: { fontFamily: tokens.font.display, fontSize: '1.375rem', lineHeight: 28 / 22, fontWeight: 700, letterSpacing: '-0.3px' }, // 1,8
    h3: { fontFamily: tokens.font.body, fontSize: '1rem', lineHeight: 22 / 16, fontWeight: 600 }, // 3,5
    h4: { fontFamily: tokens.font.body, fontSize: '0.9375rem', lineHeight: 20 / 15, fontWeight: 600 },
    subtitle1: { fontSize: '0.875rem', lineHeight: 20 / 14, fontWeight: 500 },
    body1: { fontSize: '0.875rem', lineHeight: 20 / 14 }, // 14/20 default
    body2: { fontSize: '0.8125rem', lineHeight: 18 / 13 }, // 13/18 meta
    caption: { fontSize: '0.75rem', lineHeight: 16 / 12, color: tokens.color.ink.secondary },
    overline: { fontSize: '0.6875rem', fontWeight: 600, letterSpacing: '0.6em', textTransform: 'uppercase', color: tokens.color.ink.soft }, // 4,8
    button: { textTransform: 'none', fontWeight: 600 },
  },
  transitions: {
    easing: { easeInOut: tokens.motion.ease, easeOut: tokens.motion.ease },
    duration: { shorter: tokens.motion.fast, standard: tokens.motion.medium },
  },
  shadows: [
    'none',
    tokens.elevation.e1, // E1 hover lift
    tokens.elevation.e2, // E2 popover/toast
    tokens.elevation.e3, // E3 modal
    ...new Array<string>(20).fill('none'),
  ] as unknown as Shadows,
  components: {
    MuiButton: {
      defaultProps: { disableElevation: true },
      styleOverrides: {
        root: { borderRadius: tokens.radius.sm, boxShadow: 'none', paddingInline: tokens.space.lg },
        containedPrimary: chromeButton, // 1,10 — primary CTA is chrome ink, not blue
        containedSecondary: { background: tokens.color.accent.primary, '&:hover': { background: tokens.color.accent.primaryHover } },
        outlined: { borderColor: tokens.color.border.base, color: tokens.color.ink.primary, '&:hover': { borderColor: tokens.color.border.inactive, background: tokens.color.canvas.subtle } },
        sizeSmall: { paddingInline: tokens.space.md },
      },
    },
    MuiChip: {
      defaultProps: { size: 'small' },
      styleOverrides: {
        root: { borderRadius: tokens.radius.pill, fontWeight: 500 }, // pills everywhere (1,3,4,8)
        filledPrimary: { background: tokens.color.accent.soft, color: tokens.color.accent.primaryHover },
        filledSecondary: { background: tokens.color.accent.secondarySoft, color: tokens.color.accent.secondary },
        outlined: { borderColor: tokens.color.border.base },
      },
    },
    MuiCard: {
      styleOverrides: {
        root: {
          borderRadius: tokens.radius.md,
          border: `1px solid ${tokens.color.border.base}`, // flat-first hairline, no shadow (4,8)
          backgroundImage: 'none',
          boxShadow: 'none',
        },
      },
    },
    MuiPaper: {
      styleOverrides: {
        root: { backgroundImage: 'none' },
        rounded: { borderRadius: tokens.radius.md },
        menuPaper: { borderRadius: tokens.radius.sm, border: `1px solid ${tokens.color.border.base}`, boxShadow: tokens.elevation.e2 },
      },
    },
    MuiDialog: { styleOverrides: { paper: { borderRadius: tokens.radius.lg, boxShadow: tokens.elevation.e3 } } },
    MuiDrawer: { styleOverrides: { paper: { backgroundColor: tokens.color.canvas.base, borderRight: `1px solid ${tokens.color.border.base}` } } },
    MuiTableCell: { styleOverrides: { root: { height: tokens.layout.rowHeight, borderColor: tokens.color.border.subtle, fontVariantNumeric: 'tabular-nums' } } }, // 4,6
    MuiLinearProgress: { styleOverrides: { root: { height: 8, borderRadius: tokens.radius.pill, backgroundColor: tokens.color.canvas.soft }, bar: { borderRadius: tokens.radius.pill } } }, // 7,8,9,11
    MuiSkeleton: { defaultProps: { animation: 'wave' } },
    MuiAlert: { styleOverrides: { standard: { borderRadius: tokens.radius.sm } } },
    MuiTooltip: { styleOverrides: { tooltip: { backgroundColor: tokens.color.chrome.ink, fontSize: '0.75rem', borderRadius: tokens.radius.sm } } }, // 1,7
    MuiOutlinedInput: { styleOverrides: { root: { borderRadius: tokens.radius.sm, '& fieldset': { borderColor: tokens.color.border.base } } } },
    MuiListItemButton: { styleOverrides: { root: { borderRadius: tokens.radius.sm, '&.Mui-selected': { backgroundColor: tokens.color.canvas.raised, boxShadow: tokens.elevation.e1, '&:hover': { backgroundColor: tokens.color.canvas.raised } } } } }, // 4,5 sidebar active = white card + E1
    MuiBadge: { styleOverrides: { badge: { fontWeight: 600 } } },
  },
});

export default theme;