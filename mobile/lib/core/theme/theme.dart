// Griot mobile theme — Flutter 3.19+ / Dart 3.
//
// Token contract: project-kit/context/ui-tokens.md
// Design system + inspo provenance: docs/design/MASTER-DESIGN-SYSTEM.md
//
// Flutter's ColorScheme can't carry every token, so the full set ships in a
// ThemeExtension (GriotColors) — access via context.griotColors.
// Status/priority colors mirror the EF Core enums exactly (ui-rules.md #4).
import 'package:flutter/material.dart';

// ---------------------------------------------------------------------------
// Tokens — project-kit/context/ui-tokens.md (comments = inspo #)
// ---------------------------------------------------------------------------

/// Full Griot token set (colors). Use [GriotColors.of].
@immutable
class GriotColors extends ThemeExtension<GriotColors> {
  const GriotColors({
    required this.canvasBase,
    required this.canvasRaised,
    required this.canvasSubtle,
    required this.canvasSoft,
    required this.chromeInk,
    required this.inkPrimary,
    required this.inkSecondary,
    required this.inkSoft,
    required this.borderBase,
    required this.borderSubtle,
    required this.accentPrimary,
    required this.accentSoft,
    required this.accentWash,
    required this.accentSecondary,
    required this.accentSecondarySoft,
    required this.success,
    required this.warning,
    required this.danger,
    required this.info,
    required this.chartOrange,
    required this.chartPink,
    required this.heatmap,
  });

  final Color canvasBase; // 1,3,4,8 cool-gray app canvas
  final Color canvasRaised; // all — cards
  final Color canvasSubtle; // 2 — row hover
  final Color canvasSoft; // 2 — tracks, segmented control
  final Color chromeInk; // 1,10 — dark dock, primary CTA, toasts
  final Color inkPrimary; // 2
  final Color inkSecondary; // 2
  final Color inkSoft; // 2 — disabled, overlines, timestamps
  final Color borderBase; // 2 — hairlines
  final Color borderSubtle; // 2
  final Color accentPrimary; // 2 — links/selection (blue)
  final Color accentSoft; // 2 — selected fills, skeletons
  final Color accentWash; // 2 — hover wash
  final Color accentSecondary; // 6,7,9,11 — violet
  final Color accentSecondarySoft; // 6,7
  final Color success; // 2 == TaskStatus.Done
  final Color warning; // 2 == TaskStatus.InReview
  final Color danger; // 2 == Priority.Urgent, overdue
  final Color info; // 2 == TaskStatus.InProgress
  final Color chartOrange; // 1,5 == Priority.High
  final Color chartPink; // 6,7
  final List<Color> heatmap; // 8 — 6-step blue ramp

  /// Light theme tokens (default — 12/12 inspos are light-canvas).
  static const light = GriotColors(
    canvasBase: Color(0xFFF7F8FA),
    canvasRaised: Color(0xFFFFFFFF),
    canvasSubtle: Color(0xFFFAFAFA),
    canvasSoft: Color(0xFFEDEDED),
    chromeInk: Color(0xFF1E2022),
    inkPrimary: Color(0xFF282828),
    inkSecondary: Color(0xFF757575),
    inkSoft: Color(0xFFA1A1A1),
    borderBase: Color(0xFFECECEC),
    borderSubtle: Color(0xFFF7F7F7),
    accentPrimary: Color(0xFF3572F6),
    accentSoft: Color(0xFFCEDAF3),
    accentWash: Color(0xFFFFF7ED),
    accentSecondary: Color(0xFF7C6CF6),
    accentSecondarySoft: Color(0xFFEDE9FE),
    success: Color(0xFF47BA39),
    warning: Color(0xFFFF7F1C),
    danger: Color(0xFFF14C43),
    info: Color(0xFF0BC1E6),
    chartOrange: Color(0xFFF59E0B),
    chartPink: Color(0xFFEC4899),
    heatmap: [
      Color(0xFFEFF4FF),
      Color(0xFFBFDBFE),
      Color(0xFF93C5FD),
      Color(0xFF60A5FA),
      Color(0xFF3572F6),
      Color(0xFF1D4ED8),
    ],
  );

  /// Derived dark variant (ui-tokens.md: dark is derived, not default).
  static const dark = GriotColors(
    canvasBase: Color(0xFF0E0F11),
    canvasRaised: Color(0xFF17181B),
    canvasSubtle: Color(0xFF1D1F23),
    canvasSoft: Color(0xFF26282D),
    chromeInk: Color(0xFF1E2022),
    inkPrimary: Color(0xFFF2F3F5),
    inkSecondary: Color(0xFFA1A1A1),
    inkSoft: Color(0xFF757575),
    borderBase: Color(0x14FFFFFF),
    borderSubtle: Color(0x0FFFFFFF),
    accentPrimary: Color(0xFF3572F6),
    accentSoft: Color(0xFF2B3A55),
    accentWash: Color(0xFF2A2520),
    accentSecondary: Color(0xFF7C6CF6),
    accentSecondarySoft: Color(0xFF2C2A45),
    success: Color(0xFF47BA39),
    warning: Color(0xFFFF7F1C),
    danger: Color(0xFFF14C43),
    info: Color(0xFF0BC1E6),
    chartOrange: Color(0xFFF59E0B),
    chartPink: Color(0xFFEC4899),
    heatmap: [
      Color(0xFF1B2A4A),
      Color(0xFF24407A),
      Color(0xFF2E56AA),
      Color(0xFF3572F6),
      Color(0xFF5B8DFF),
      Color(0xFF8FB4FF),
    ],
  );

  /// Resolves the extension from the nearest [Theme].
  static GriotColors of(BuildContext context) =>
      Theme.of(context).extension<GriotColors>() ?? light;

  @override
  GriotColors copyWith() => this; // tokens are immutable; use lerp for animation

  @override
  GriotColors lerp(ThemeExtension<GriotColors>? other, double t) {
    if (other is! GriotColors) return this;
    Color c(Color a, Color b) => Color.lerp(a, b, t)!;
    return GriotColors(
      canvasBase: c(canvasBase, other.canvasBase),
      canvasRaised: c(canvasRaised, other.canvasRaised),
      canvasSubtle: c(canvasSubtle, other.canvasSubtle),
      canvasSoft: c(canvasSoft, other.canvasSoft),
      chromeInk: c(chromeInk, other.chromeInk),
      inkPrimary: c(inkPrimary, other.inkPrimary),
      inkSecondary: c(inkSecondary, other.inkSecondary),
      inkSoft: c(inkSoft, other.inkSoft),
      borderBase: c(borderBase, other.borderBase),
      borderSubtle: c(borderSubtle, other.borderSubtle),
      accentPrimary: c(accentPrimary, other.accentPrimary),
      accentSoft: c(accentSoft, other.accentSoft),
      accentWash: c(accentWash, other.accentWash),
      accentSecondary: c(accentSecondary, other.accentSecondary),
      accentSecondarySoft: c(accentSecondarySoft, other.accentSecondarySoft),
      success: c(success, other.success),
      warning: c(warning, other.warning),
      danger: c(danger, other.danger),
      info: c(info, other.info),
      chartOrange: c(chartOrange, other.chartOrange),
      chartPink: c(chartPink, other.chartPink),
      heatmap: List.generate(6, (i) => c(heatmap[i], other.heatmap[i])),
    );
  }
}

/// Radii + layout constants (spacing uses the 8-pt grid via [GriotSpacing]).
@immutable
class GriotRadii extends ThemeExtension<GriotRadii> {
  const GriotRadii({
    this.sm = 10, // 1,4 buttons/inputs
    this.md = 16, // 3,5,8 cards/web popovers
    this.lg = 20, // modals
    this.xl = 24, // 9,10,11 mobile cards/sheets/CTA
  });

  final double sm;
  final double md;
  final double lg;
  final double xl;

  static const light = GriotRadii();
  static const dark = GriotRadii();

  @override
  GriotRadii copyWith() => this;

  @override
  GriotRadii lerp(ThemeExtension<GriotRadii>? other, double t) =>
      other is! GriotRadii ? this : GriotRadii(
        sm: lerpDouble(sm, other.sm, t)!,
        md: lerpDouble(md, other.md, t)!,
        lg: lerpDouble(lg, other.lg, t)!,
        xl: lerpDouble(xl, other.xl, t)!,
      );
}

/// 8-pt spacing rhythm (ui-tokens.md). Static — identical in both modes.
abstract final class GriotSpacing {
  static const xs = 4.0;
  static const sm = 8.0;
  static const md = 12.0;
  static const lg = 16.0;
  static const xl = 20.0; // mobile card padding (9,11)
  static const xl2 = 24.0;
  static const xl3 = 32.0;
  static const xl4 = 48.0;
}

/// Status/priority severity maps — visual == EF Core enum (ui-tokens.md).
Color taskStatusColor(GriotColors c, String status) => switch (status) {
      'Backlog' => c.inkSoft,
      'Todo' => c.accentPrimary,
      'InProgress' => c.info,
      'InReview' => c.warning,
      'Done' => c.success,
      _ => c.inkSoft,
    };

Color priorityColor(GriotColors c, String priority) => switch (priority) {
      'Low' => c.info,
      'Medium' => c.warning,
      'High' => c.chartOrange,
      'Urgent' => c.danger,
      _ => c.inkSoft,
    };

// ---------------------------------------------------------------------------
// ThemeData builders
// ---------------------------------------------------------------------------

/// Alias used by [_withHeight] to keep the text-style plumbing readable.
typedef TextWithHeight = TextStyle;

/// Inter (UI) + Space Grotesk (display/KPI) — family names per ui-tokens.md.
/// Font *bundling* (google_fonts or assets) is wired in mobile feature 01;
/// these families resolve to platform fallbacks until then.
const _kUiFont = 'Inter';
const _kDisplayFont = 'Space Grotesk';

/// [ThemeExtension]s shared by light and dark [ThemeData]s.
const _extensions = <ThemeExtension<dynamic>>{GriotColors.light, GriotRadii.light};

TextWithHeight _withHeight(
  TextStyle base,
  double size,
  double height, {
  double? spacing,
  FontWeight? weight,
  Color? color,
  String? family,
}) =>
    base.copyWith(
      fontSize: size,
      height: height / size,
      letterSpacing: spacing,
      fontWeight: weight,
      color: color,
      fontFamily: family,
      fontFamilyFallback: const [bodyFontFallback, 'sans-serif'],
    );

const bodyFontFallback = 'system-ui';

TextTheme _griotTextTheme(ColorScheme scheme) {
  final ink = scheme.onSurface;
  final muted = scheme.onSurfaceVariant;
  return TextTheme(
    // Display/KPI roles use Space Grotesk (inspo 1, 8 hero numerals).
    displayLarge: _withHeight(TextStyle(color: ink), 32, 38, spacing: -0.5, weight: FontWeight.w700, family: _kDisplayFont), // 8 hero KPI
    displaySmall: _withHeight(TextStyle(color: ink), 28, 34, spacing: -0.5, weight: FontWeight.w700, family: _kDisplayFont), // 1 display
    headlineMedium: _withHeight(TextStyle(color: ink), 22, 28, spacing: -0.3, weight: FontWeight.w700, family: _kDisplayFont), // 1,8 page titles
    titleMedium: _withHeight(TextStyle(color: ink), 16, 22, weight: FontWeight.w600, family: _kUiFont), // 3,5 card titles
    bodyLarge: _withHeight(TextStyle(color: ink), 14, 20, family: _kUiFont), // default
    bodyMedium: _withHeight(TextStyle(color: ink), 13, 18, family: _kUiFont), // 1,4 meta
    bodySmall: _withHeight(TextStyle(color: muted), 12, 16, family: _kUiFont),
    labelLarge: _withHeight(TextStyle(color: ink), 14, 20, weight: FontWeight.w600, family: _kUiFont), // buttons
    labelMedium: _withHeight(TextStyle(color: ink), 12, 16, family: _kUiFont),
    labelSmall: _withHeight(TextStyle(color: muted), 11, 16, spacing: 0.6, weight: FontWeight.w600, family: _kUiFont), // 4,8 overlines
  );
}

ColorScheme _scheme(Brightness brightness) {
  final c = brightness == Brightness.light ? GriotColors.light : GriotColors.dark;
  return ColorScheme(
    brightness: brightness,
    primary: c.accentPrimary, // 2 — links/selection (blue)
    onPrimary: Colors.white,
    primaryContainer: c.accentSoft, // 2 — selected fills
    onPrimaryContainer: c.inkPrimary,
    secondary: c.accentSecondary, // 6,7,9,11 — violet
    onSecondary: Colors.white,
    secondaryContainer: c.accentSecondarySoft,
    onSecondaryContainer: c.inkPrimary,
    error: c.danger,
    onError: Colors.white,
    errorContainer: c.danger.withValues(alpha: 0.14),
    onErrorContainer: c.danger,
    surface: c.canvasRaised, // all — cards
    onSurface: c.inkPrimary,
    onSurfaceVariant: c.inkSecondary,
    outline: c.borderBase,
    outlineVariant: c.borderSubtle,
    surfaceContainerHighest: c.canvasSoft, // tracks/segmented (2)
    surfaceContainerHigh: c.canvasSubtle, // row hover (2)
    surfaceContainer: c.canvasBase,
    surfaceContainerLow: c.canvasBase,
    surfaceContainerLowest: c.canvasRaised,
    surfaceTint: Colors.transparent, // flat-first: no tint overlay (4,8)
  );
}

/// Griot light theme (default — 12/12 inspos are light-canvas).
ThemeData griotTheme() => _theme(Brightness.light);

/// Derived dark variant (ui-tokens.md: dark is derived, never the default).
ThemeData griotDarkTheme() => _theme(Brightness.dark);

ThemeData _theme(Brightness brightness) {
  final scheme = _scheme(brightness);
  final c = brightness == Brightness.light ? GriotColors.light : GriotColors.dark;
  final radii = GriotRadii.light; // radii identical in both modes
  final textTheme = _griotTextTheme(scheme);

  return ThemeData(
    useMaterial3: true,
    colorScheme: scheme,
    scaffoldBackgroundColor: scheme.surfaceContainer, // canvas base (1,3,4,8)
    extensions: _extensions,
    textTheme: textTheme,
    appBarTheme: AppBarTheme(
      backgroundColor: Colors.transparent,
      foregroundColor: scheme.onSurface,
      elevation: 0,
      scrolledUnderElevation: 0,
      centerTitle: true, // 9,11 centered titles with white circular side buttons
      titleTextStyle: (textTheme.headlineMedium ?? textTheme.titleLarge!).copyWith(fontSize: 17),
    ),
    cardTheme: CardTheme(
      color: scheme.surface, // white cards on canvas
      elevation: 0, // flat-first hairline cards (4,8)
      margin: EdgeInsets.zero,
      clipBehavior: Clip.antiAlias,
      shape: RoundedRectangleBorder(
        borderRadius: BorderRadius.circular(radii.xl), // 24 (9,10,11)
        side: BorderSide(color: scheme.outline), // 1px border.base (2)
      ),
    ),
    elevatedButtonTheme: ElevatedButtonThemeData(
      // Full-width primary CTA: blue, radius 24 (9,11 — Submit/Continue).
      style: ElevatedButton.styleFrom(
        backgroundColor: scheme.primary,
        foregroundColor: Colors.white,
        elevation: 0,
        minimumSize: const Size(double.infinity, 52),
        shape: RoundedRectangleBorder(borderRadius: BorderRadius.circular(radii.xl)),
        textStyle: (textTheme.labelLarge ?? textTheme.bodyLarge!).copyWith(fontSize: 15),
      ),
    ),
    filledButtonTheme: FilledButtonThemeData(
      // Chrome-ink action (10 "Stop Charging"): dark pill, white label.
      style: FilledButton.styleFrom(
        backgroundColor: c.chromeInk,
        foregroundColor: Colors.white,
        elevation: 0,
        minimumSize: const Size(0, 48),
        padding: const EdgeInsets.symmetric(horizontal: GriotSpacing.xl2),
        shape: RoundedRectangleBorder(borderRadius: BorderRadius.circular(radii.xl)),
        textStyle: textTheme.labelLarge,
      ),
    ),
    outlinedButtonTheme: OutlinedButtonThemeData(
      // White secondary buttons (9 "Unlock Port"): white bg + hairline.
      style: OutlinedButton.styleFrom(
        backgroundColor: scheme.surface,
        foregroundColor: scheme.onSurface,
        side: BorderSide(color: scheme.outline),
        minimumSize: const Size(0, 48),
        padding: const EdgeInsets.symmetric(horizontal: GriotSpacing.xl2),
        shape: RoundedRectangleBorder(borderRadius: BorderRadius.circular(radii.xl)),
        textStyle: textTheme.labelLarge,
      ),
    ),
    textButtonTheme: TextButtonThemeData(
      // Blue text-link actions (12 "Pay Now").
      style: TextButton.styleFrom(foregroundColor: scheme.primary, textStyle: textTheme.labelLarge),
    ),
    inputDecorationTheme: InputDecorationTheme(
      filled: true,
      fillColor: scheme.surface, // white fields (11)
      contentPadding: const EdgeInsets.symmetric(horizontal: GriotSpacing.lg, vertical: GriotSpacing.md),
      hintStyle: textTheme.bodyMedium?.copyWith(color: c.inkSoft),
      border: OutlineInputBorder(borderRadius: BorderRadius.circular(radii.md), borderSide: BorderSide(color: scheme.outline)),
      enabledBorder: OutlineInputBorder(borderRadius: BorderRadius.circular(radii.md), borderSide: BorderSide(color: scheme.outline)),
      focusedBorder: OutlineInputBorder(borderRadius: BorderRadius.circular(radii.md), borderSide: BorderSide(color: scheme.primary)),
    ),
    chipTheme: ChipThemeData(
      // Pills everywhere (1,3,4,8); selected = soft fill + base-color text (§4.3).
      backgroundColor: scheme.surface,
      selectedColor: scheme.primaryContainer,
      disabledColor: c.canvasSoft,
      labelStyle: textTheme.labelMedium,
      padding: const EdgeInsets.symmetric(horizontal: GriotSpacing.md, vertical: 6),
      shape: const StadiumBorder(),
      side: BorderSide(color: scheme.outline),
      showCheckmark: false,
    ),
    snackBarTheme: SnackBarThemeData(
      // Toast = chrome ink, white text, radius 12 (1,10 — §5.11).
      backgroundColor: c.chromeInk,
      contentTextStyle: textTheme.bodyMedium?.copyWith(color: Colors.white),
      behavior: SnackBarBehavior.floating,
      elevation: 4,
      shape: RoundedRectangleBorder(borderRadius: BorderRadius.circular(12)),
    ),
    dialogTheme: DialogTheme(
      backgroundColor: scheme.surface,
      surfaceTintColor: Colors.transparent,
      shape: RoundedRectangleBorder(borderRadius: BorderRadius.circular(radii.lg)),
    ),
    bottomSheetTheme: BottomSheetThemeData(
      backgroundColor: scheme.surface,
      surfaceTintColor: Colors.transparent,
      showDragHandle: true,
      dragHandleColor: c.canvasSoft,
      shape: RoundedRectangleBorder(
        borderRadius: BorderRadius.vertical(top: Radius.circular(radii.xl)), // 9,11 sheets
      ),
    ),
    navigationBarTheme: NavigationBarThemeData(
      // Bottom pill nav is wrapped in a white radius-24 container by AppShell;
      // these values style the bar itself (9).
      backgroundColor: scheme.surface,
      indicatorColor: scheme.primaryContainer, // soft blue active pill
      surfaceTintColor: Colors.transparent,
      elevation: 0,
      height: 64,
      labelBehavior: NavigationDestinationLabelBehavior.alwaysShow,
      iconTheme: const WidgetStatePropertyAll(IconThemeData(size: 24)),
      labelTextStyle: WidgetStatePropertyAll(textTheme.labelMedium),
    ),
    listTileTheme: ListTileThemeData(
      // 44px rows (§4.5) with radius-16 hover shape; lead chips per 12.
      contentPadding: const EdgeInsets.symmetric(horizontal: GriotSpacing.lg),
      minTileHeight: 44,
      iconColor: c.inkSecondary,
      titleTextStyle: textTheme.bodyLarge,
      subtitleTextStyle: textTheme.bodySmall,
      shape: RoundedRectangleBorder(borderRadius: BorderRadius.circular(radii.md)),
    ),
    progressIndicatorTheme: ProgressIndicatorThemeData(
      // 8px pill progress track (7,8,9,11 — §5.5).
      color: scheme.primary,
      linearTrackColor: scheme.surfaceContainerHighest, // canvasSoft track
      linearMinHeight: 8,
      circularTrackColor: scheme.surfaceContainerHighest,
      refreshBackgroundColor: scheme.primary,
    ),
    dataTableTheme: DataTableThemeData(
      dataRowHeight: 44,
      headingRowHeight: 40,
      headingTextStyle: textTheme.labelSmall, // overline headers (4,8)
      dataTextStyle: textTheme.bodyMedium,
    ),
    dividerTheme: const DividerThemeData(color: Color(0xFFF7F7F7), thickness: 1, space: 1),
    iconButtonTheme: IconButtonThemeData(
      // White circular icon buttons (9,11 back/close, 12 actions), 44px a11y.
      style: ButtonStyle(
        backgroundColor: const WidgetStatePropertyAll(Colors.white),
        foregroundColor: WidgetStatePropertyAll(scheme.onSurface),
        fixedSize: const WidgetStatePropertyAll(Size(44, 44)),
        shape: const WidgetStatePropertyAll(CircleBorder()),
        elevation: const WidgetStatePropertyAll(0),
      ),
    ),
    switchTheme: SwitchThemeData(
      thumbColor: const WidgetStatePropertyAll(Colors.white),
      trackColor: WidgetStateProperty.resolveWith(
        (states) => states.contains(WidgetState.selected) ? scheme.primary : c.canvasSoft,
      ),
      trackOutlineColor: const WidgetStatePropertyAll(Colors.transparent),
    ),
    scrollbarTheme: ScrollbarThemeData(
      thumbColor: WidgetStatePropertyAll(c.inkSoft),
      thickness: const WidgetStatePropertyAll(4.0),
      radius: const Radius.circular(999),
    ),
  );
}

/// Header tint gradient — `#E4E6F7 → canvas.base` (9,11).
LinearGradient griotHeaderTint(GriotColors c) => LinearGradient(
      begin: Alignment.topCenter,
      end: Alignment.bottomCenter,
      colors: [const Color(0xFFE4E6F7), c.canvasBase],
      stops: const [0.0, 0.6],
    );
