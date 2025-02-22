# Changelog

All notable changes to this project will be documented in this file.

The format is based on [Keep a Changelog](https://keepachangelog.com/en/1.1.0/),
and this project adheres to [Semantic Versioning](https://semver.org/spec/v2.0.0.html).

## [2.2.0] - 2-21-25

### Added/Changed

- UI Changes:
  - Added Mini-player
  - Changed the corner radius for all cover image tiles
  - Added zoom option for main library page
  - Added mica background for Win11 users
  - Changes to allow window size to be much smaller
  - Disabled light theme until it can be completed at a future date
  - Several other small UI improvements
- Updated nuget packages

## [2.1.10] - 1-8-25

### Fixed

- Updated all content dialog calls to use the new DialogService, ensuring only one dialog is shown at a time using a
  semaphore. This is to fix the System.Runtime.InteropServices.COMException getting thrown from the old dialog queue.
- Fixed a bug where a books position was being overwritten when the chapter combo box loaded
- Fixed a bug where the app didn't handle when SixLabors was unable to recognize the image format
- Added error handling to WriteCoverImageAsync to handle when access is denied

### Changed

- Changed AudiobookTile and PlayerControl to use default CoverImage if the actual cover image is inaccessible
- Updated Sentry configuration

## [2.1.9] - 12-23-24

### Fixed

- Fixed Sentry logging

## [2.1.8] - 12-21-24

### Fixed

- Fixed advanced settings button crashing the app when clicked
- Fixed file activation where the app would crash if the file was being used by another process
- Fixed bug where exceptions were unhandled if ResetFilters?.Invoke() threw an exception in GetAudiobookListAsync()
- Fixed bug in App.xaml.cs UseSqlite() method where the database was not being created correctly
- Fixed bug in Delete dialog where exceptions weren't handled

## [2.1.7] - 12-18-24

### Changed

- Incrementing version again because Microsoft Store is dumb

## [2.1.6] - 12-17-24

### Changed

- Incrementing version because Microsoft Store is dumb

## [2.1.5] - 12-17-24

### Fixed

- Fixed a bug in the data migration code

### Changed

- Added a ton of error handling to the data migration code

## [2.1.4] - 12-3-24

### Changed

- Added error check to data migration json import

## [2.1.3] - 12-3-24

### Changed

- Re-enabled Sentry logging

## [2.1.2] - 12-3-24

### Fixed

- Fixed bug (hopefully) where the app would crash when trying to show a dialog in the queue
- Fixed data migration code to handle if user quits prematurely
    - Also, updated it to correctly handle an audiobooks progress

## [2.1.1] - 12-1-24

### Fixed

- Fixed bug where UserSettings was using the wrong key for Version

## [2.1.0] - 11-30-24

### Added

- Added support for .mp3 files
- Added support for audiobooks that consist of multiple files (.mp3 & .m4b only)
- Added feature that allows users to mark an audiobook as finished
    - Will mark an audiobook as finished when the progress is at 100% by default
    - This is indicated by a new icon (a checkmark) on the audiobook tile in place of the progress icon
- Added data migration feature to migrate user data to the new database
- Added database migrations so that updating the database going forward will be easier
- Added filter button that allows users to filter their library by:
    - In Progress
    - Not Started
    - Completed
- Added export button to allow users to export their library to a .json file
- Added import button to allow users to import a library from a .json file (has to be exported from Audibly)
- Added "More Info" button to audiobook tile context menu in library card view
    - Shows the audiobook's metadata
- Added a powershell script that packages the .msix files into a .msixbundle file
- Added warning when user switches to light theme that it is in beta and may not appear correctly

### Changed

- Changed playback speed maximum to 2.0x
- Made notifications disappear after 10 seconds
- Improved now playing slider to be more responsive
- Added warning that the light theme is in beta and may not appear correctly
- Modified sidebar to remember its collapsed state on startup
- Removed "Open" button from the library card view
- Modified Player Control title to use Marquee text when the title is too long
- Modified the changelog dialog to use the MarkdownTextBlock when rendering the changelog
- Modified file activation code to open book even if the filepath doesn't match (assuming the title is already in the
  library)
    - The match is made on the Title, Author, and Narrator

### Fixed

- Fixed a bug (#57) where:
    - The icons would turn black on hover when the app was in dark mode but the system was in light mode
- Fixed a bug (#58) where:
    - Added a default cover image when the import operation was unable to get a cover image from the audiobook file
- Fixed a bug where:
    - The now playing book on startup would not update its progress when playing
- Fixed a bug where:
    - The progress icon on the audiobook wouldn't update when the audiobook was playing
- Fixed a bug in the file activation code

## [2.0.15] - 07-21-24

### Changed

- Now opens audiobook after file import (based on suggestion from @OpenAudible)

### Fixed

- Fixed a bug (#51) where:
    - The back button would sometimes lose focus when hovering over it
- Fixed a bug (#52) where:
    - The app would try to display more than one content dialog causing it to crash
    - Added a mutex lock to ProcessDialogQueue to hopefully fix this issue

## [2.0.14] - 07-01-24

### Fixed

- Hotfix for 2.0.13:
    - The app would crash when trying to parse saved user settings (volume & playback speed) from local storage

## [2.0.13] - 06-29-24

### Fixed

- Hotfix for 2.0.12:
    - The app would crash when trying to parse saved user settings (volume & playback speed) from local storage

### Changed

- Updated some dependencies

## [2.0.12] - 06-27-24

### Fixed

- Fixed link to Privacy Policy in CHANGELOG

## [2.0.11] - 06-27-24

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
    - The app would then crash when trying to play the audiobook (because it was trying to load chapters that didn't
      exist).
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