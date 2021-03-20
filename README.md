# auxmic
 
[![auxmic build](https://github.com/osmanovv/auxmic/workflows/auxmic%20build/badge.svg)](https://github.com/osmanovv/auxmic/actions)
[![GitHub license](https://img.shields.io/github/license/osmanovv/auxmic)](https://github.com/osmanovv/auxmic/blob/master/LICENSE)

`auxmic` is an open source audio synchronization software.

![how it works: matching files](http://auxmic.com/sites/default/files/pictures/sync.png)

The only purpose is to help you synchronize audio files from different sources. Assume you filming any event with DSLR or camcorder while recording audio to an external microphone. You do not need a clapperboard anymore - using auxmic you can easily cut audio from your master record. Just drag and drop your records and get right timecodes or even export synced files.

Since version `0.8.1.115` you can export synced files with `FFmpeg` without using any NLE software.

## Supported Formats
Format | File Extensions | Windows 7 | Windows 8/10
------ | --------------- | --------- | ------------
AVI | .avi | + | +
MPEG-4 | .m4a, .m4v, .mov, .mp4 | + | +
MP3 | .mp3 | + | +
WAVE | .wav | + | +
3GP | .3g2, .3gp, .3gp2, .3gpp | + | +
Advanced Streaming Format (ASF) | .asf, .wma, .wmv | + | +
Audio Data Transport Stream (ADTS) | .aac, .adts | + | +
MPEG-2 | .mpg, .mpeg | - | +
MPEG transport stream | .m2t, .m2ts, .mp2v, .mts, .ts | - | +
MPEG program stream | .vob, .mod | - | +

[Supported Media Formats in Media Foundation](http://msdn.microsoft.com/en-us/library/dd757927(VS.85).aspx)

## Screenshots
![main window: on-screen instruction](http://www.auxmic.com/sites/default/files/pictures/main-form-startup.png)

![main window: synching in progress](http://www.auxmic.com/sites/default/files/pictures/main-form.png)
