# Auto Sound Sync
[![GitHub license](https://img.shields.io/github/license/osmanovv/auxmic)](https://github.com/osmanovv/auxmic/blob/master/LICENSE)

Auto Sound Sync (ASS) is a poor man's version of [PluralEyes](https://www.maxon.net/en/red-giant/pluraleyes), which was software that allowed you to automatically synchronise video clips by analysing their audio wave forms.

It means you no longer have to use a clapper-board, or manually align wave-forms in your video editor. Just run ASS, and import the project file into your NLE editor.

Most main-stream NLE video editors now have this included, so PluralEyes has gone away. Arguably there is no longer any need for this software either, unless you use Magix Vegas :-)

ASS is a fork of the most excellent [auxmic](https://github.com/osmanovv/auxmic), and also makes use of [ffmpeg](https://ffmpeg.org/), and [SoundFingerprinting](https://github.com/AddictedCS/soundfingerprinting)

# How to use it:

1. Record some video and audio of the same event.

2. Choose the file that contains the best audio. It can be a video file, or a pure audio recording. Set it as the "High quality audio source".

![Main: Set High quality audio source](images/01-set-high-quality.png?raw=true "Main: Set High quality audio source")

3. Add the other video files as "Media to Synchronize"

![Main: Set Media to Synchronize](images/02-set-media-to-sync.png?raw=true "Main: Set Media to Synchronize")

4. Wait for synchronization to complete, and all bars to turn green.

![Main: Wait for synchronization](images/03-wait-for-sync.png?raw=true "Main: Wait for synchronization")

5. Export the Final Cut project file

![Main: Export project](images/04-export-project.png?raw=true "Main: Export project")

6. Import the project file into your video editor of choice

![Vegas: Import project](images/05-import-project.png?raw=true "Vegas: Import project")

7. Edit as usual
![Vegas: Imported success](images/06-import-success.png?raw=true "Vegas: Imported success")
Notice how the subclips appear based on where their audio files match up.

# Other synchronization options
The options dialog offers three alternative synchronization options:

## Soundfingerprinter
Fast and reliable finger printing. Recommended.

## Auxmic
The original auxmic algorithm. Significantly slower than Soundfingerprinter, and not usually better, but will sometimes match when Soundfingerprinter will not.

## Emy
Even faster finger printer? It might work when others don't, depending on your audio.

# FAQ:

**Q:** So why does ASS exist?  
**A:** I use Magix Vegas Pro, and it's ability to provide this function is subpar. Also because I made this fork ages ago :-)

**Q:** Is it fast?  
**A:** It's WAY fast. When synchronising 4 files over 30 minute period, Vegas Pro takes nearly 2 hours, and then crashes, ASS only takes a few seconds.

**Q:** Why does it create Final Cut Pro 7 project files?  
**A:** Because they are the lingua franca of NLEs. Everything can import them, including Vegas, Davinci, and Premiere.

**Q:** Why didn't this just get merged back into auxmic?  
**A:** Because I refactored the hell out of it, and it became unmergable.

