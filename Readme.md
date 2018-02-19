# Pixel Graphics Video Player

A video player designed solely for navigating lossless video formats with pixel graphics in mind. PGVP allows you to navigate videos frame-by-frame and bookmark specific frames to keep track of timing between events. Frames with differences can also be located in either direction and the differences highlighted. Comes with an optional waveform visualizer to help locate the start of a playing sound. This tool was designer for the [Zelda Oracle Engine](https://github.com/trigger-death/ZeldaOracle) project for determining game mechanics with precision.

[![Window Preview](https://i.imgur.com/3CrXaF6.png)](https://i.imgur.com/UdDqCXg.gifv)

### [Wiki](https://github.com/trigger-death/PGVideoPlayer/wiki) | [Credits](https://github.com/trigger-death/PGVideoPlayer/wiki/Credits) | [Image Album](https://imgur.com/a/65Wbu)

## About

* **Created by:** Robert Jordan
* **Version:** 1.0.0.0
* **Language:** C#, WPF

## Requirements for Running

* .NET Framework 4.6.1 | [Offline Installer](https://www.microsoft.com/en-us/download/details.aspx?id=49982) | [Web Installer](https://www.microsoft.com/en-us/download/details.aspx?id=49981)
* Windows 7 or later
* Video Codec must support frame playback precision and have a natural size less than or equal to 800x800. <sup>(The tool was never designed to play videos at less than the natural size as it was designed for GameBoy Color recordings which are already very small.)</sup>
