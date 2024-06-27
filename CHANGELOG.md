# Changelog

All notable changes to this project will be documented in this file.

The format is based on [Keep a Changelog](https://keepachangelog.com/en/1.1.0/),
and this project adheres to [Semantic Versioning](https://semver.org/spec/v2.0.0.html).

## [2.0.12]

### Fixed

- Fixed link to Privacy Policy in CHANGELOG

## [2.0.11]

### Changed

- Updated Privacy Policy (because of the addition of Sentry.io for error tracking).
  - You can view the updated Privacy Policy [here](https://github.com/rstewa/Audibly/blob/main/PrivacyPolicy.md)
- Updated the volume and playback speed settings to be saved even after restarting the app

### Added

- Sentry.io for error tracking
- Added contact card on Settings Page 

## [2.0.10] - 06-12-2024

### Added

- Added link to Libation
- Added link to OpenAudible

### Fixed

- Added try/catch to thumbnail generation
- Fixed a bug where:
  - The import would fail if an audiobook had a long title (this prevented the metadata directory from being created)
- Fixed a bug where:
  - The title would cover the play buttons on the now playing bar if it was too long

## [2.0.9] - 06-04-2024

### Added

- Added file activation support
  - Users can now open audiobooks (.m4b) from the file explorer
  - Users can also set Audibly as the default app for opening audiobooks (.m4b)

- Added Donation button

- Added Changelog dialog for new versions
  - Users will now see a dialog with the changelog when they update the app

### Fixed

- Fixed a bug where:
  - The app would crash when deleting the now playing audiobook from the library

## [2.0.8] - 04-24-2024

### Fixed

- Better fix for the chapter bug from 2.0.7
  - When there is no chapter metadata, the app will now default to a single chapter with the title of the audiobook.

## [2.0.7] - 04-24-2024

### Fixed

- Fixed a bug where:
  - A user imports an audiobook that the file importer service was unable to get any chapter metadata from.
  - The app would then crash when trying to play the audiobook (because it was trying to load chapters that didn't exist).
  - The app would then crash until the user reset the app's data (which would remove the problematic audiobook).
- Updated the dialog service 

## [2.0.6] - 04-17-2024

### Changed

- Complete redesign of the UI
- Complete refactor of the code base
- Added a library view
- First v2 version in the store


## [1.0.0] - 08-31-2023

### Added

- 1st version for Microsoft App Store