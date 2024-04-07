# IN PROGRESS

# TODO
- [ ] add keyboard accelerators

- [ ] add setting to allow user to add a directory to automatically import audiobooks from
  - [ ] need to research background processes in WinUI 3

- [ ] add setting for audiobook tile size
- [ ] disable buttons while import is in progress (and maybe while loading)
- [ ] create function in vim that allows you to yank and then comment selected text

- [ ] if you click play on an audiobook that is already playing, the app crashes

- [ ] use stackednotificationsbehavior to show notifications

- [ ] add playback speed slider
- [ ] add volume slider

- [ ] need a delete audiobook button

- [ ] add radial gauge to audiobooks for progress in book in list view

- [ ] progress bar
  - [ ] add text underneath importing progress bar
  - [x] add card around progress bar
  - [x] create into a user control

- [ ] Add a context menu entry that supports startup parameters

- [ ] create or find a tool that finds all todo's in solution and puts them in a file with line numbers

- [ ] add ability to check for updates
  - [ ] popup message to user about update
  - [ ] genie type thing to show them the new features
  - [ ] maybe add a way to auto import any book that has ever been played
  - [ ] popup to tell user an update is available in the store (if they dont have auto update turned on in store)

- [ ] add multiple language support (whichever languages people ask for)

- [ ] allow user to minimize to taskbar
- [ ] should mini player and main player be allowed to both be open at the same time?

- [ ] filtering:
  - [ ] add ability to see most recent
  - [ ] add filter button to change how audiobooks are sorted by default
  - [ ] show recent searches as suggestions

- [x] add now playing bar
  - [x] make all buttons for player work
  - [ ] make chapter combobox a fixed width -- not sure if i need to do this or not
  - [x] disable media controls when no audiobook is opened
  - [x] open most recently played audiobook on startup

- [ ] what happens if there are multiple users on the same pc?

- [ ] BUG: move seeker many times == buggy

- [ ] add check that verifies that the audiobooks in the database are pointing to valid files (i.e., they haven't been moved or deleted)
- [ ] fix TODO (which is a bug) in SqlAudiobookRepository.cs
- [ ] todo: update ui as each book is imported
- [ ] fix light mode
- [ ] bug when you refresh audiobooks and some of the images in the list view are blank
- [ ] create annoted scroll bar
- [ ] add support for different file types
- [ ] allow user to select accent color in settings
- [ ] add check to see if user has modified or deleted app files (e.g., the db file or the cover image files)
- [ ] allow user to change skip button amounts
  - [ ] add a setting for this
- [ ] add setting to allow user to choose to set audio for global or each book
- [ ] add setting to allow user to choose to set playback speed for global or each book

# Completed
- [x] add start message when there are no audiobooks in library
- [x] only set now playing audiobook when user clicks on the play audiobook button (not when an audiobook is selected in the list view)
- [x] add button to open mini audiobook player -> use OpenInNewWindow icon <FontIcon Glyph="&#xE8A7;" />
  - [x] add check to make sure only one instance of mini player is open
- [x] import chapters when adding audiobook to database
- [x] add progress bar when importing audiobooks
- [x] add file dialog when importing audiobooks
- [x] automatically refresh audiobooks list after importing audiobooks
- [x] check all `x:Bind` and make sure they are using `Mode=OneWay`

- [x] create full screen player
  - [x] add button on mini player to allow user to make it full screen
  - [ ] ~~add player as a page in the navigation view~~

- [x] high memory usage even after deleting all audiobooks
  - [x] ~~is garbage collection slow or do we have a memory leak?~~
    - [x] ~~turns out ListView uses a fuck ton of memory~~
      * ~~listview unloaded (card view is loaded) : 122.5 mb~~
      * ~~listview loaded (card view unloaded) : \~700 mb (goes up from 500 mb when you make the window full screen)~~
  - [x] ~~need to try cleaning up library page maybe? (source: https://github.com/microsoft/WindowsAppSDK/issues/1895#issuecomment-991754095)~~
  - [x] turns out the audiobook cover images were insanely large. converted them to thumbnails and no longer have any memory problems


# TODO but later

- [ ] allow user to minimize to taskbar and then control media by clicking on icon in taskbar



# NOTES:

make a card:

```
<StackPanel 
                    CornerRadius="8"
                    Padding="12"
                    BorderThickness="1"
                    BorderBrush="{ThemeResource CardStrokeColorDefaultBrush}"
                    Background="{ThemeResource CardBackgroundFillColorDefaultBrush}">
```
