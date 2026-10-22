# Changelog

## [Unreleased]

### Changed
- The minimum Unity version is now 6000.0.

### Added
- EditMode tests for `ProductCollection`, `InAppPurchaseService` and the store listener, run on every push and pull
  request to `master` and `develop`.
- Releases: a pull request from `release/X.Y.Z` or `hotfix/X.Y.Z` into `master` is checked for the version, the
  changelog and the release notes, and once merged it publishes `vX.Y.Z` with a `.unitypackage` attached.

## [2.0.0] - 2025-11-03

Initial release