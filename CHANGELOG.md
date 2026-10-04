# Changelog

All notable changes to this project will be documented in this file.

The format is based on [Keep a Changelog](https://keepachangelog.com/en/1.1.0/),
and this project adheres to [Semantic Versioning](https://semver.org/spec/v2.0.0.html).

## [Unreleased]

## [1.0.0] - 2026-10-03

### Added

- Season indicator on seeds in the inventory: shows a custom season icon (spring, summer, fall, or winter) when a seasonal crop can be planted in the current in-game season.
- Season data is read from the game's crop metadata, so no crop list is hardcoded.
- No indicator is shown for year-round crops or crops that are out of season.

[Unreleased]: https://github.com/bikinavisho/starsand-seed-season-indicator/compare/v1.0.0...HEAD
[1.0.0]: https://github.com/bikinavisho/starsand-seed-season-indicator/releases/tag/v1.0.0

<!--
How to use this file:

- Add new entries under [Unreleased] as you work.
- When you release, rename [Unreleased] to the new version and date (YYYY-MM-DD),
  then add a fresh empty [Unreleased] section above it.
- Group entries under these headings: Added, Changed, Deprecated, Removed, Fixed, Security.
- Update the link references at the bottom, e.g.:
  [1.0.1]: https://github.com/bikinavisho/starsand-seed-season-indicator/compare/v1.0.0...v1.0.1
  and point [Unreleased] at compare/v1.0.1...HEAD.

Version bumps (MAJOR.MINOR.PATCH):

- PATCH (1.0.1): bug fixes that don't change behavior otherwise.
- MINOR (1.1.0): new features that are backward compatible (e.g. a config option).
- MAJOR (2.0.0): breaking changes (e.g. changed config format, or dropping support for a game version).
-->