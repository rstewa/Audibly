# Changelog

All notable changes to this project will be documented in this file.

The format is based on [Keep a Changelog](https://keepachangelog.com/en/1.1.0/),
and this project adheres to [Semantic Versioning](https://semver.org/spec/v2.0.0.html).

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