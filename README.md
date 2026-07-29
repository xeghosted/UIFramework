# UIFramework

[![License: MIT](https://img.shields.io/badge/license-MIT-blue.svg)](LICENSE)
[![Used by OrbisForge](https://img.shields.io/badge/used%20by-OrbisForge-orange)](https://github.com/xeghosted/OrbisForge)

**A skinned WinForms control library for .NET Framework: DPI-aware, light/dark theming, and a
high-performance virtualized data grid.**

Every control draws itself from an active `ISkin` — no hardcoded colors anywhere in a control's
own code — so switching `SkinManager.Current` between `LightSkin` and `DarkSkin` (or a custom skin)
re-themes the whole UI, including the window's non-client title bar via DWM.

## Controls

- `SkinnedForm` / `SkinnedControl` — base classes handling skin-aware painting and (for forms) a
  themed title bar
- `SkinButton`, `SkinLabel`, `SkinPanel`, `SkinTextBox`, `SkinComboBox`, `SkinTabControl`,
  `SkinScrollBar` — custom-drawn standard controls
- `SpinEdit`, `DateEdit` (with a drawn month calendar), `CheckEdit` — editors built on a shared
  `ButtonEditBase`: one native text core, a painted button zone, and a `PopupHost` anchor.
  Invalid input is filtered as you type and falls back silently on commit
- `MenuBar` / `PopupMenu` — a skinned menu bar and context menu with submenus, separators,
  check items, mnemonics (`&File`), app-wide shortcuts and full keyboard navigation. Menu
  popups never take focus (`WS_EX_NOACTIVATE`): the focused control keeps its caret while
  a menu is open, and the window title bar stays active
- `RibbonControl` — a skinned ribbon with tabs, groups, large/small image buttons, toggle and
  dropdown buttons (dropdowns are regular menu popups). Painted element-wise, never steals
  focus, sizes itself from the skin's font metrics
- `GridControl` — virtualized grid built to stay smooth at a million rows, with sorting, filtering,
  column resize/reorder, keyboard navigation, and in-cell editing (F2, double click or type-to-edit;
  Tab moves on) that writes back through sorted and filtered views to the correct source row

## Requirements

- .NET Framework 4.8, WinForms (`UseWindowsForms`)

## Used by

[OrbisForge](https://github.com/xeghosted/OrbisForge) — a CMake project scaffolder for the
OpenOrbis PS4 Toolchain — uses this for its Windows GUI wizard.

## License

MIT — see [LICENSE](LICENSE).
