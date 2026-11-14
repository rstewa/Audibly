<a name="readme-top"></a>



<!-- PROJECT SHIELDS -->
[![Version][version-shield]][version-url]
[![Contributors][contributors-shield]][contributors-url]
[![Forks][forks-shield]][forks-url]
[![Stargazers][stars-shield]][stars-url]
[![Issues][issues-shield]][issues-url]
[![MIT License][license-shield]][license-url]
[![LinkedIn][linkedin-shield]][linkedin-url]



<!-- PROJECT LOGO -->
<br />
<div align="center">
  <a href="https://github.com/rstewa/Audibly">
    <img src="images/logo.svg" alt="Logo" width="250" height="250">
  </a>

<h3 align="center">Audibly — Audiobook Player</h3>

  <p align="center">
    Desktop Audiobook player for Windows 10/11. Supports `.m4b` and `.mp3` file types. It also supports audiobooks that
    consist of multiple files. I created this because Audible removed their desktop player (and I really hate browser based players).
    <br />
    <a href="https://github.com/rstewa/Audibly"><strong>Explore the docs »</strong></a>
    <br />
    <br />
    <a href="https://github.com/rstewa/Audibly">View Demo</a>
    ·
    <a href="https://github.com/rstewa/Audibly/issues/new?labels=bug&template=bug-report---.md">Report Bug</a>
    ·
    <a href="https://github.com/rstewa/Audibly/issues/new?labels=enhancement&template=feature-request---.md">Request Feature</a>
  </p>
</div>


<!-- link to store page -->
<a href="https://apps.microsoft.com/detail/9p6r1m1gg9jr?mode=direct">
	<img src="https://get.microsoft.com/images/en-us%20dark.svg" width="200"/>
</a>

<br>
<br>


<!-- TABLE OF CONTENTS -->
<details>
  <summary>Table of Contents</summary>
  <ol>
    <li>
      <a href="#about-the-project">About The Project</a>
      <ul>
        <li><a href="#built-with">Built With</a></li>
      </ul>
    </li>
    <li>
      <a href="#getting-started">Getting Started</a>
      <ul>
        <li><a href="#prerequisites">Prerequisites</a></li>
        <li><a href="#installation">Installation</a></li>
      </ul>
    </li>
    <li><a href="#usage">Usage</a></li>
    <li><a href="#roadmap">Roadmap</a></li>
    <li><a href="#contributing">Contributing</a></li>
    <li><a href="#license">License</a></li>
    <li><a href="#contact">Contact</a></li>
    <li><a href="#acknowledgments">Acknowledgments</a></li>
  </ol>
</details>



<!-- ABOUT THE PROJECT -->
## About The Project

This desktop audiobook player for Windows 10/11 supports playing .m4b and .mp3 files, including audiobooks composed of multiple files.

<p align="right">(<a href="#readme-top">back to top</a>)</p>



## Screenshots

### Home/Library
<p align="middle">
  <img src="images/v2.2.7/audibly_home_page.png" />
</p> 

### Expanded Player
<p align="middle">
  <img src="images/v2.2.7/audibly_player_page.png" />
</p> 

### Mini-player
<p align="middle">
  <img src="images/v2.2.7/audibly_mini_player.png" />
</p> 

### Sleep Timer
<p align="middle">
    <img src="images/v2.2.7/audibly_sleep_timer.png" />
</p>

### More Info Dialog
<p align="middle">
  <img src="images/v2.2.7/audibly_more_info.png" />
</p> 

<p align="right">(<a href="#readme-top">back to top</a>)</p>



### Built With

[![WinUI 3][winui]][winui-url]

<p align="right">(<a href="#readme-top">back to top</a>)</p>



## Building locally

Use Windows 10/11 with the .NET 8 SDK or newer and the Windows build tools for WinUI 3.
The repository's `NuGet.Config` includes the Community Toolkit Labs feed needed by the existing preview controls.

```powershell
dotnet build Audibly.App/Audibly.App.csproj -p:Platform=x64 -p:WindowsPackageType=None -p:AppxPackageSigningEnabled=false -p:GenerateAppxPackageOnBuild=false
dotnet test Audibly.Audio.Tests/Audibly.Audio.Tests.csproj
```

### Pitch-preserving playback

The speed slider snaps to 1x, 1.1x, 1.25x, 1.5x, 1.75x, 2x, 2.5x, 3x, 3.5x and 4x while preserving the narrator's pitch.
The main player, mini-player, default-speed settings and keyboard controls share these presets. Previously saved speeds snap to the nearest preset when loaded.
NAudio decodes the file, SoundTouch.NET changes tempo, and Windows MediaPlayer plays the resulting PCM audio at 1x.
Normal-speed playback bypasses time-stretching. Audio is streamed in small buffers rather than converting or caching an entire audiobook.

Changing speed reopens the stream at the current audiobook position and preserves the play/pause state; a brief buffering pause may occur.
Chapter positions, skipping, progress and saved resume positions use the original audiobook timeline.
The Windows-only audio tests measure pitch and duration, decode MP3/M4B files, and exercise seeking, speed changes and end-of-file playback without audible output.

Audio dependencies: [NAudio](https://github.com/naudio/NAudio) (MIT) and
[SoundTouch.NET](https://github.com/owoudenberg/soundtouch.net) (LGPL-2.1-or-later, C# port by Olaf Woudenberg of Olli Parviainen's SoundTouch).
They are used as separate, unmodified assemblies. Their license notices are included in `Audibly.App/Assets/ThirdPartyNotices`.



<!-- TODO: USAGE EXAMPLES -->



<!-- ROADMAP -->
## Roadmap

- [x] Add `.mp3` file support
- [x] Add support for multiple files in an audiobook
- [x] Add mini-player

See the [open issues](https://github.com/rstewa/Audibly/issues) for a full list of proposed features (and known issues).

<p align="right">(<a href="#readme-top">back to top</a>)</p>



<!-- CONTRIBUTING -->
## Contributing

Contributions are what make the open source community such an amazing place to learn, inspire, and create. Any contributions you make are **greatly appreciated**.

If you have a suggestion that would make this better, please fork the repo and create a pull request. You can also simply open an issue with the tag "enhancement".
Don't forget to give the project a star! Thanks again!

1. Fork the Project
2. Create your Feature Branch (`git checkout -b feature/AmazingFeature`)
3. Commit your Changes (`git commit -m 'Add some AmazingFeature'`)
4. Push to the Branch (`git push origin feature/AmazingFeature`)
5. Open a Pull Request

### Top contributors:

<a href="https://github.com/rstewa/audibly/graphs/contributors">
  <img src="https://contrib.rocks/image?repo=rstewa/audibly" alt="contrib.rocks image" />
</a>

<p align="right">(<a href="#readme-top">back to top</a>)</p>



<!-- LICENSE -->
## License

Distributed under the GNU General Public License v3.0. See `LICENSE.txt` for more information.

<p align="right">(<a href="#readme-top">back to top</a>)</p>



<!-- CONTACT -->
## Contact

rstewa - dev@audibly.info

Project Link: [https://github.com/rstewa/Audibly](https://github.com/rstewa/Audibly)

<p align="right">(<a href="#readme-top">back to top</a>)</p>



<!-- ACKNOWLEDGMENTS -->
## Acknowledgments

* [Rise Media Player](https://github.com/Rise-Software/Rise-Media-Player) for inspiration
* [Libation](https://github.com/rmcrackan/Libation) for the FolderIcon code
* [OpenAudible](https://openaudible.org/) for giving me a license and for helping me find/fix a bug in my import code
* [Best-README-Template](https://github.com/othneildrew/Best-README-Template) for the README template

<p align="right">(<a href="#readme-top">back to top</a>)</p>



<!-- MARKDOWN LINKS & IMAGES -->
<!-- https://www.markdownguide.org/basic-syntax/#reference-style-links -->
[version-shield]: https://img.shields.io/github/v/release/rstewa/audibly?style=for-the-badge
[version-url]: https://github.com/rstewa/audibly/releases/latest
[contributors-shield]: https://img.shields.io/github/contributors/rstewa/Audibly.svg?style=for-the-badge
[contributors-url]: https://github.com/rstewa/Audibly/graphs/contributors
[forks-shield]: https://img.shields.io/github/forks/rstewa/Audibly.svg?style=for-the-badge
[forks-url]: https://github.com/rstewa/Audibly/network/members
[stars-shield]: https://img.shields.io/github/stars/rstewa/Audibly.svg?style=for-the-badge
[stars-url]: https://github.com/rstewa/Audibly/stargazers
[issues-shield]: https://img.shields.io/github/issues/rstewa/Audibly.svg?style=for-the-badge
[issues-url]: https://github.com/rstewa/Audibly/issues
[license-shield]: https://img.shields.io/github/license/rstewa/Audibly.svg?style=for-the-badge
[license-url]: https://github.com/rstewa/Audibly/blob/master/LICENSE.txt
[linkedin-shield]: https://img.shields.io/badge/-LinkedIn-black.svg?style=for-the-badge&logo=linkedin&colorB=555
[linkedin-url]: https://www.linkedin.com/in/stewart-ryan-p/
[winui]: https://img.shields.io/badge/-WinUI_3-512BD4?style=for-the-badge&logo=.net&logoColor=white
[winui-url]: https://learn.microsoft.com/en-us/windows/apps/winui
