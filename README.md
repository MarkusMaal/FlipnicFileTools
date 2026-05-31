# Flipnic file tools

Several command line tools with a GUI front-end for various file formats used by Flipnic.

Useful links:

* [READ ME before using GUI version](GUIREADME.md)
* [Build instructions](BUILD.md)
* [Progress report](https://github.com/MarkusMaal/FlipnicPatterns?tab=readme-ov-file#progress)
* [ImHex patterns](https://github.com/MarkusMaal/FlipnicPatterns/?tab=readme-ov-file#flipnic-hex-patterns)
* [File format wiki (incomplete)](https://github.com/MarkusMaal/FlipnicPatterns/wiki)
* [The Cutting Room Floor page (currently managed by SuperFromND)](https://tcrf.net/Flipnic:_Ultimate_Pinball)
* [Freecam mod](https://www.youtube.com/watch?v=31-pnJw6PFo)
* [Gameplay guide (with illustrations)](https://paktc.markusmaal.ee/?doc=en/FlipnicGuide.md)
* [Teleporting the ball to other areas (guide)](https://paktc.markusmaal.ee/?doc=en/flipnic_ball_tp.md)
* [Decompilation](https://github.com/MarkusMaal/FlipnicDecomp)

Prerequisites:

* [FFmpeg](https://ffmpeg.org/) - required for some video related operations
* [ImageMagick](https://imagemagick.org/) - required for creating PNG mock-ups from menu files (when using the CLI version, no longer needed for latest builds)

Jump to section:

* [GUI version](#gui-version)
* [Command line syntax](#command-line-syntax)
* [Disc images (*.ISO)](#disc-images-iso)
* [Blob files (*.BIN)](#blob-files-bin)
* [Movies (*.PSS)](#movies-pss)
    * [Creating custom FMVs from video files](#creating-custom-fmvs-from-video-files)
* [Sound files (*.INT / *.SVAG)](#sound-files-int--svag)
* [Texture files (*.TM2)](#texture-files-tm2)
* [Menu files (*.MLB)](#menu-files-mlb)
* [Camera sequences (*.FPC)](#camera-sequences-fpc)
    + [Creating animated camera sequences](#creating-animated-camera-sequences)
* [Stage information files (*.SST)](#stage-information-files-sst)
* [Message files (*.MSG)](#message-files-msg)
    + [Generating custom message files](#generating-custom-message-files)
* [Resource files (*.LP4)](#resource-files-lp4)
* [VAB header files (*.HD)](#vab-header-files-hd)
* [VAB body files (*.BD)](#vab-body-files-bd)
* [Save file icon (*.ICO)](#save-file-icon-ico)
* [Collision maps (*.COL)](#collision-maps-col)
* [Environment lighting (*.LIT)](#environment-lighting-lit)
* [Layout files (*.LAY)](#layout-files-lay)
* [Texture lists (*.FTL)](#texture-lists-ftl)
* [Dummy files (*.DAT)](#dummy-files-dat)
* [Source code control files (VSSVER.SCC)](#source-code-control-files-vssverscc)
* [FlipnicLib](#flipniclib)

## GUI version

There's also a graphical front-end for this CLI tool, which is easier to use, but also gives you better preview for stuff like textures, audio samples, stuff like that without having to type down a bunch of commands with the disadvantages being lack of automation options and in some cases compatibility.

[More information](GUIREADME.md)

![Screenshot of GUI version](preview.png)

## Command line syntax

To see command line syntax at any time, you can run: `FlipnicFileTool --help`

## Disc images (*.ISO)

This is the final disc image that can be burned to a disc and run by the PlayStation 2.

Listing files: `FlipnicFileTool --show-iso --input Flipnic.iso`

Outputs:
```
PlayStation 2 ISO file
File system: UDF
Volume label: Untitled

+----------------------+------------+
| Filename             | Size       | 
+----------------------+------------+
| modules\ioprp270.img | 243.28 kiB | 
| modules\libsd.irx    | 27.89 kiB  | 
| modules\mcman.irx    | 93.63 kiB  | 
| modules\mcserv.irx   | 7.24 kiB   | 
| modules\padman.irx   | 42.79 kiB  | 
| modules\sdrdrv.irx   | 7.88 kiB   | 
| modules\sg2iop.irx   | 26.76 kiB  | 
| modules\sio2man.irx  | 6.49 kiB   | 
| SYSTEM.CNF           | 56 B       | 
| SLES_520.65          | 1.17 MiB   | 
| FONT.BIN             | 304 kiB    | 
| RES.BIN              | 189.06 MiB | 
| STR.BIN              | 955.66 MiB | 
| TUTO.BIN             | 1.47 GiB   | 
| DUMMY.DAT            | 2 kiB      | 
+----------------------+------------+
```

Extract files: `FlipnicFileTool --extract-iso --input Flipnic.iso --output ./FlipnicISO`

Repack a file: `FlipnicFileTool --replace-iso RES.BIN --input RES.BIN --output Flipnic.iso` (CAUTION: this operation WILL overwrite the contents of the ISO file)

## Blob files (*.BIN)

These are big files that contain smaller files within themselves (basically like an uncompressesed .ZIP or .TAR file, but proprietary to Flipnic).

Listing files: `FlipnicFileTool --list-files --input TUTO.BIN`

Outputs: 
```
+-----------------+-----------------+-----------------+
| Path            | Offset          | Size            | 
+-----------------+-----------------+-----------------+
| \CHAP01.PSS     | 0x800           | 203.13 MiB      | 
| \CHAP02.PSS     | 0xCB22000       | 235.73 MiB      | 
| \CHAP03.PSS     | 0x1B6DE000      | 247.46 MiB      | 
| \CHAP04.PSS     | 0x2AE54800      | 162.96 MiB      | 
| \CHAP05.PSS     | 0x35149800      | 315.28 MiB      | 
| \CHAP06.PSS     | 0x48C91000      | 212.51 MiB      | 
| \CHAP07.PSS     | 0x56113800      | 128.83 MiB      | 
+-----------------+-----------------+-----------------+
```

Extract files: `FlipnicFileTool --extract-files --input RES.BIN --output ./RES`

Repack a file: `FlipnicFileTool --replace-file RETRO1\RETRO1.SST --input RETRO1.SST --output RES.BIN`  (CAUTION: this operation WILL overwrite existing data)

## Movies (*.PSS)

These are not a standard PlayStation video streams, instead they're a special container format in Flipnic, which contain audio and video streams.

If you want to convert a PSS file directly into a MOV file (requires FFmpeg): `FlipnicFileTool --convert-pss-mp4 --input ./FREEZE_OVER.PSS --output ./FREEZE_OVER.MOV` (this will also integrate multiple audio streams into the output file if the .PSS file has multiple audio streams)

If you have the PAL version, you should also add a --pal flag: `FlipnicFileTool --convert-pss-mp4 --input ./FREEZE_OVER.PSS --output ./FREEZE_OVER.MOV --pal`

**NOTE**: PSS to MP4 option is lossy. If you want lossless conversion, you must demux the streams first and convert each file separately.

If you just want to see what streams a .PSS file contains, run this: `FlipnicFileTool SHUKYAKUDEMO.PSS --list-pss-streams`

Outputs:
```
Stream summary
+-----------------+-----------------+
| Stream          | Size            | 
+-----------------+-----------------+
| Audio 1         | 4.09 MiB        | 
| Audio 2         | 4.09 MiB        | 
| Audio 3         | 4.09 MiB        | 
| Audio 4         | 4.09 MiB        | 
| Audio 5         | 4.09 MiB        | 
| Video           | 101.48 MiB      | 
+-----------------+-----------------+

Audio duration: 00:01:24.984
Video duration: 00:01:23.720
Video standard: PAL (interlaced)
Total frames: 2093

Interleaving data
+---------+------------+----------+
| Stream  | Fr./Sampl. | Time     |
+---------+------------+----------+
| Audio 1 | 34405      | 780.16ms |
| Audio 2 | 34405      | 780.16ms |
| Audio 3 | 34405      | 780.16ms |
| Audio 4 | 34405      | 780.16ms |
| Audio 5 | 34405      | 780.16ms |
| Video   | 0          | 0ms      |
| Video   | 1          | 40ms     |
| Video   | 0          | 0ms      |
| Video   | 1          | 40ms     |
| Video   | 1          | 40ms     |
...
```

If you just want to separate streams and not convert any of the files: `FlipnicFileTool --extract-pss-streams --input ./FREEZE_OVER.PSS --output .`

If you want to create a new .PSS container from 1 audio and 1 interlaced PAL video stream, you can use the following command: `FlipnicFileTool --generate-pss SAMPLE.INT --input SAMPLE.IPU --output SAMPLE.PSS --pal`

While there is NTSC support, it is still in experimental status, so created .PSS files will stop playing at some point in-game.

### Creating custom FMVs from video files

First, you must create compatible source files.

Video: PlayStation 2 IPU

* PAL progressive: 256x512@50p
* PAL interlaced: 512x512@25i
* NTSC progressive: 256x512@60p
* NTSC interlaced: 512x448@30i

Audio: Sony Compressed ADPCM

* Sample rate: 44100Hz
* Interleave: 0x400
* Channels: 2

For creating the .INT file, you can use MFAudio. Yes, it's old AF, but it works.

For creating the .IPU file, you have to first encode your source video to MPEG-2. You may use TMPEGEnc for best results, but you can also just use FFmpeg with this command: `ffmpeg -i <source video> -c:v mpeg2video -profile:v main -level:v 8 -h:v 4.531M -maxrate 5M -minrate 4.531M -bufsize 1835k -pix_fmt yuv420p -g 1 -hf 0 -flags +ildct+ilme -top 1 -r <framerate for the specific video format> -s <resolution for the specific video format> <output .m2v>`. Then you can import this M2V file to the ps2str and from there you can convert it to .IPU.

It's possible that the created .IPU file will be broken. To fix it, you can use the following command: `FlipnicFileTool --ipu-duct-tape [--pal] [--progressive] --input <.ipu file>` (use the flags in brackets only when applicable to chosen video format).

After you have both .IPU and .INT files, you can merge them. Use the following command: `FlipnicFileTool --generate-pss <int file> --input <ipu file> --output <pss file> [--pal] [--progressive]`. Once again, use the flags in brackets only when applicable for the chosen video format.

## Sound files (*.INT / *.SVAG)

The difference between the two is that .INT is stereo and .SVAG is mono.

Convert .SVAG file to .WAV: `FlipnicFileTool --convert-svag --input MSG_001.SVAG --output MSG_001.WAV`

Convert .INT file to .WAV: `FlipnicFileTool --convert-int --input FREEZE_OVER.1.INT --output FREEZE_OVER.WAV`

## Texture files (*.TM2)

These are texture files used by many PlayStation 2 games and Flipnic is no exception in that regard. Note that this parser doesn't support TM2 files with mipmaps or userdata sections (if you try to open such files, a not implemented exception is thrown). However, none of the textures used by Flipnic utilize these features of the TIM2 format.

To view information about a texture file: `FlipnicFileTool --show-tim2 --input FLOWER_TUTUJI0.TM2`

Outputs:
```
TIM2 texture file

Name: FLOWER_TUTUJI0.TM2
Width: 16
Height: 16
Colors: 256
Palette type: IDTEX8 (8-bit indexed/Compound)
Pictures: 1

Palette:
+------+---------+-------+
| ID   | RGB     | Alpha | 
+------+---------+-------+
| 0x0  | #000000 | 0     | 
| 0x1  | #092813 | 180   | 
| 0x2  | #0C2710 | 209   | 
| 0x3  | #0F2E14 | 168   | 
| 0x4  | #123216 | 132   | 
| 0x5  | #133518 | 120   | 
| 0x6  | #173C1C | 74    | 
| 0x7  | #183C1A | 121   | 
| 0x8  | #1A421F | 29    | 
| 0x9  | #194421 | 2     | 
...
```

To convert a texture file into a PNG file: `FlipnicFileTool --convert-tim2 --input FLOWER_TUTUJI0.TM2 --output FLOWER_TUTUJI0.PNG`

To convert a texture file into a PNG file (legacy versions): `FlipnicFileTool --png --convert-tim2 --input FLOWER_TUTUJI0.TM2 --output FLOWER_TUTUJI0.PNG`

## Menu files (*.MLB)

To view various sections used by a menu file: `FlipnicFileTool --show-mlb --input BG01_A0.MLB`

Outputs:

```
+--------------------+--------------------+--------------------+--------------------+--------------------+
| Section            | Index              | Texture            | Position           | Dimensions         | 
+--------------------+--------------------+--------------------+--------------------+--------------------+
| bg1_A0             | 0                  | Bg\A0\bg1_00a.tm2  | 0x0                | 512x256            | 
| bg1_A0             | 1                  | Bg\A0\bg1_00b.tm2  | 512x0              | 128x256            | 
| bg1_A0             | 2                  | Bg\A0\bg1_00c.tm2  | 0x256              | 512x128            | 
| bg1_A0             | 3                  | Bg\A0\bg1_00d.tm2  | 512x256            | 128x128            | 
| bg1_A0             | 4                  | Bg\A0\bg1_00e.tm2  | 0x384              | 512x64             | 
| bg1_A0             | 5                  | Bg\A0\bg1_00f.tm2  | 512x384            | 128x64             | 
| bg0_A0             | 0                  | Bg\A0\bg0_00a.tm2  | 0x0                | 512x256            | 
| bg0_A0             | 1                  | Bg\A0\bg0_00b.tm2  | 512x0              | 128x256            | 
| bg0_A0             | 2                  | Bg\A0\bg0_00c.tm2  | 0x256              | 512x128            | 
| bg0_A0             | 3                  | Bg\A0\bg0_00d.tm2  | 512x256            | 128x128            | 
| bg0_A0             | 4                  | Bg\A0\bg0_00e.tm2  | 0x384              | 512x64             | 
| bg0_A0             | 5                  | Bg\A0\bg0_00f.tm2  | 512x384            | 128x64             | 
| bg0_A0             | 6                  | Bg\A0\bg0_00a.tm2  | 640x0              | 512x256            | 
| bg0_A0             | 7                  | Bg\A0\bg0_00b.tm2  | 1152x0             | 128x256            | 
| bg0_A0             | 8                  | Bg\A0\bg0_00c.tm2  | 640x256            | 512x128            | 
| bg0_A0             | 9                  | Bg\A0\bg0_00d.tm2  | 1152x256           | 128x128            | 
| bg0_A0             | 10                 | Bg\A0\bg0_00e.tm2  | 640x384            | 512x64             | 
| bg0_A0             | 11                 | Bg\A0\bg0_00f.tm2  | 1152x384           | 128x64             | 
+--------------------+--------------------+--------------------+--------------------+--------------------+
```

To create a mock-up of the menu as a .PNG file: `FlipnicFileTool --generate-mockup --input MAINMENU.MLB --output MAINMENU.PNG`

You can also only include a specific section from the menu file mockup: `FlipnicFileTool --generate-mockup --input MAINMENU.MLB --output MAINMENU.PNG --mlb-section MainMenu`

## Camera sequences (*.FPC)

These files contain information about the camera and optionally can have keyframes to create an animated camera sequence.

Example:

`FlipnicFileTool --input CAM_Y3_EVNT01.FPC --show-fpc`

Outputs:

```
Frames: 140, Sequences: 6
Field of view: 34.999996
Origin:  (824.95325; 25.8749; 771.5084)
Target:  (850.5004; 1.8749; 771.5004)

+-----------------+-----------------+-----------------+-----------------+-----------------+-----------------+-----------------+-----------------+
| Frame           | OriginX         | OriginY         | OriginZ         | TargetX         | TargetY         | TargetZ         | FOV             | 
+-----------------+-----------------+-----------------+-----------------+-----------------+-----------------+-----------------+-----------------+
| 1               | 797             | 29              | 759             | 838             | 5               | 759             | 34.999996       | 
| 2               | 797.0022        | 28.999382       | 759.0034        | 838.0025        | 4.999382        | 759.0025        | 34.999996       | 
| 3               | 797.00854       | 28.99754        | 759.0116        | 838.0098        | 4.9975395       | 759.0098        | 34.999996       | 
| 4               | 797.0194        | 28.994492       | 759.02466       | 838.02203       | 4.9944906       | 759.02203       | 34.999996       | 

...

| 139             | 840.9867        | 25.000618       | 774.9975        | 853.9975        | 1.0006182       | 774.9975        | 34.999996       | 
| 140             | 841             | 25              | 775             | 854             | 1               | 775             | 34.999996       | 
+-----------------+-----------------+-----------------+-----------------+-----------------+-----------------+-----------------+-----------------+
```

To create a XML file containing this information, which you can easily modify, you can run `FlipnicFileTool --input EXAMPLE.FPC --convert-fpc-to-xml --output EXAMPLE.XML`. Once you've modified the created XML file, you can turn it back into a .FPC file by running `FlipnicFileTool --input EXAMPLE.XML --convert-xml-to-fpc --output EXAMPLE_NEW.FPC`.

### Creating animated camera sequences

This tool allows you to interpolate between two .FPC files to create an animated sequence. Here's an example use case:

* Using Flipnic freecam, setup the camera for first frame
* Click the "Generate FPC" button and save it as a .FPC file
* Now setup the camera for the final frame
* Click the "Generate FPC" button again and save it as another .FPC file
* Now in FlipnicFileTool, you can run a command like this to generate a 300 frame animation: `FlipnicFileTool --input A.FPC --input B.FPC --generate-animation 300 --output C.FPC`
* You can now use this C.FPC file inside the game by either modifying an existing .FPC file or adding it as a new file by repacking RES.BIN container and then using it with the .SST event script

## Stage information files (*.SST)

These files store various things, stuff like game events, list of filenames, gimmicks, etc.

Example:
`FlipnicFileTool --input ISEKI1.SST --show-sst-toc`

Outputs:
```
+-----------------+-----------------+-----------------+-----------------+
| Name            | Offset          | Entry count     | Entry size      | 
+-----------------+-----------------+-----------------+-----------------+
| BALLN           | 0x3C0           | 3               | 0x40            | 
| SKYN            | 0x480           | 1               | 0x20            | 
| CAMN            | 0x4A0           | 43              | 0x20            | 
| CAMD            | 0xA00           | 25              | 0x30            | 
| CAMLD           | 0xEB0           | 25              | 0x28            | 
| CAMID           | 0x12A0          | 0               | 0x70            | 
| LIGHTN          | 0x12A0          | 1               | 0x20            | 
| PATHN           | 0x12C0          | 265             | 0x20            | 

...

| GMK18180        | 0x35DD0         | 0               | 0x80            | 
| REBIRTH_        | 0x35DD0         | 24              | 0x8             | 
| FLIESFPB        | 0x35E90         | 1               | 0xDA0           | 
+-----------------+-----------------+-----------------+-----------------+
```

You can also see the event script in human-readable format by running: `FlipnicFileTool --input ISEKI_1.SST --msg-path ../JA.MSG --get-pseudo-code`

Outputs:
```
func START ()                                                        @ 0x7B30
nop

func GAME_EVENT (Balls: [5, 3, 3], Credits: [4, 1, 0])               @ 0x7B70
        GameEvent (SetMission, MASTER, Status::Started)
        SequenceEvent (BgmEvent, Filename: SOUNDDATA\ISEKI_0.MID)
        GameEvent (5, ???: 0, ???: 0)
        GameEvent (SetSpawn, AreaCode: 21160)
        SequenceEvent (ScreenFade, FadeOut: false, Ticks: 60)
        BallEvent (15, ???: 3, ???: 16)
        SequenceEvent (CameraSequence, Filename: CAM_CR_EVNT01.FPC)
do
        SequenceEvent (CameraSequence, Filename: CAM_Y2.FPC:NEG)
        BallEvent (15, ???: 3, ???: 15)
end


func RESET_EVENT ()                 
...
```

## Message files (*.MSG)

These files store text strings, which may be referenced by IDs elsewhere (basically just that just helps save disc space).

Example:
`FlipnicFileTool --input JA.MSG --show-messages`

Outputs:
```
Magic: FpnMsg00
Entries: 92
+-------------------------+-------------------------+
| ID                      | Message                 | 
+-------------------------+-------------------------+
| 0                       | ja                      | 
| 1                       |                         | 
| 2                       | 0                       | 
| 3                       | 1                       | 
| 4                       | 2                       | 

...

| 90                      | 100 BLOCKS              | 
| 91                      | RED BLOCKS              | 
+-------------------------+-------------------------+
```

### Generating custom message files

First, convert the JA.MSG to a TXT file by running the following command:

`FlipnicFileTool --input JA.MSG --show-messages --simple > converted.txt`

Note that the --simple flag is important here.

You can edit the converted file with notepad. Each line is one message. Make your changes and save the file. After that, you can convert the TXT back into MLB by running this command

`FlipnicFileTool --input converted.txt --generate-msg --output JA_MOD.MSG`

The generated file will have the same layout that the game can understand.

## Resource files (*.LP4)

These files define stuff like 3D models and 2D animation sequences (e.g. the WONDERFUL text when you complete a mission). You can view all models and other information about the LP4 file.

Example: `FlipnicFileTool --input CHOU01.LP4 --show-lp4`

Outputs:
```
Type: StaticModel
Model count: 0
Has bounding box: Yes
Is 2D animation: No
Timelines: 3
Animation joints: 15

Bounding box:
+-----------+------------+------------+
| X         | Y          | Z          | 
+-----------+------------+------------+
| 6.7750607 | -1.4439601 | -5.1376605 | 
| -6.77506  | -1.4439601 | -5.1376605 | 
| 6.7750607 | -1.4439601 | 4.9989805  | 
...
| -6.77506  | -1.4439601 | -5.1376605 | 
| -6.77506  | 0.22978    | -5.1376605 | 
| -6.77506  | 0.22978    | 4.9989805  | 
+-----------+------------+------------+


Models:
+------------+---------+-------+--------+-------------------+----------+
| Name       | Address | Scale | Offset | Texture           | Polygons | 
+------------+---------+-------+--------+-------------------+----------+
| MO_CHOU_UV | F0      | 1x1x1 | 0x0x0  | mo_chou_tex_1.tm2 | 8304     | 
+------------+---------+-------+--------+-------------------+----------+

Joints:
+--------------------+----------+-------------+-------------+
| Name               | Vertices | Position    | Size        | 
+--------------------+----------+-------------+-------------+
| CHOU_ASHI_2_L_NULL | 63       | NaNxNaNxNaN | NaNxNaNxNaN | 
| CHOU_ASHI_2_R_NULL | 63       | NaNxNaNxNaN | NaNxNaNxNaN | 
| HANE_L_L_NULL      | 10       | NaNxNaNxNaN | NaNxNaNxNaN | 
| HANE_L_R_NULL      | 12       | NaNxNaNxNaN | NaNxNaNxNaN | 
| HANE_S_L_NULL      | 8        | NaNxNaNxNaN | NaNxNaNxNaN | 
| HANE_S_R_NULL      | 10       | NaNxNaNxNaN | NaNxNaNxNaN | 
| JNT1_1             | 69       | NaNxNaNxNaN | NaNxNaNxNaN | 
| JNT11_1            | 21       | NaNxNaNxNaN | NaNxNaNxNaN | 
| JNT2_1             | 201      | NaNxNaNxNaN | NaNxNaNxNaN | 
| JNT2_2             | 30       | NaNxNaNxNaN | NaNxNaNxNaN | 
| JNT3_1             | 6        | NaNxNaNxNaN | NaNxNaNxNaN | 
| JNT5_1             | 6        | NaNxNaNxNaN | NaNxNaNxNaN | 
| JNT6_1             | 8        | NaNxNaNxNaN | NaNxNaNxNaN | 
| JNT8_1             | 10       | NaNxNaNxNaN | NaNxNaNxNaN | 
| JNT9_1             | 22       | NaNxNaNxNaN | NaNxNaNxNaN | 
+--------------------+----------+-------------+-------------+

```

You can also attempt to convert the 3D models stored inside LP4 files to Wavefront OBJ by running the following command: `FlipnicFileTool --input CHOU01.LP4 --convert-obj --output CHOU01.OBJ`

**Note**: LP4 parser is unreliable, so this will often fall back to a brute-force method, which will comb through the entire file trying to find patterns that match 3D model data. Also, not all LP4 files contain model data. If you want to always skip the parsing part and go straight for the brute-force method, you can append the `--force-brute-force` parameter to the above command.

Sometimes this will produce a file, which has incorrect normals (shading looks weird). If that's the case, you may need to append `--alternate-normals` parameter to the above command.

In addition, you can extract the bounding box of the LP4 file (if it has one) and save it as a Wavefront OBJ by running the following command: `FlipnicFileTool --input CHOU01.LP4 --convert-box-obj --output CHOU01_BOX.OBJ`

## VAB header files (*.HD)

These files contain information about VAB soundbank file pairs (.HD/.BD), used mainly for background music, but sometimes also sound effects.

Example: `FlipnicFileTool --input NATURE_1.HD --show-hd`

Outputs:
```
Programme 2
Count: 2
BaseVolume: 49.6%, BasePan: 0 (C), BasePitch: 12
LfoTableIndex: N/A
+--------+--------+-----------+-----------+-----------+-----------+------------+-----------+--------+--------+--------+--------+-------------+---------+
| Volume | Pan    | Note min. | Note max. | Base note | Fine tune | Pitch Bend | LFO index | Flags  | Offset | Attack | Decay  | Sustain     | Release | 
+--------+--------+-----------+-----------+-----------+-----------+------------+-----------+--------+--------+--------+--------+-------------+---------+
| 70.9%  | 25 (R) | CNg1      | A4        | B4        | −8        | N/A        | N/A       | P----R | 64AF0  | 0 s    | 13.6 s | 2 s (100 %) | 1.72 s  | 
| 70.9%  | 27 (R) | A#4       | G9        | F5        | −9        | N/A        | N/A       | P----R | 6AD80  | 0 s    | 13.6 s | 2 s (100 %) | 1.72 s  | 
+--------+--------+-----------+-----------+-----------+-----------+------------+-----------+--------+--------+--------+--------+-------------+---------+


...
```

If the HD/BD pair has a corresponding MIDI file, you can convert it to SF2 soundfont, by running the following command: `FlipnicFileTool --convert-sf2 --input NATURE_0.HD --bd-file NATURE_0.BD --midi-file NATURE_0.MID --output NATURE_0.SF2`.

If the BD and MIDI files have the same name, you can omit them from the command and this tool will try to figure out the names automatically: `FlipnicFileTool --convert-sf2 --input NATURE_0.HD --output NATURE_0.SF2`. 

## VAB body files (*.BD)

These contain samples for background music or sound effects. You can scan for them and see their offsets.

Example: `FlipnicFileTool --input SE_ST01.BD --show-bd`

Outputs:

```
+----+---------+------------+----------+
| Id | Offset  | Loop start | Loop end | 
+----+---------+------------+----------+
| 0  | 0x10    | 0x0        | 0x0      | 
| 1  | 0x62A0  | 0x0        | 0x0      | 
| 2  | 0xF710  | 0x0        | 0x0      | 
| 3  | 0x13B60 | 0x0        | 0x0      | 
| 4  | 0x14C70 | 0x0        | 0x0      | 
| 5  | 0x185F0 | 0x0        | 0x0      | 
| 6  | 0x1A2D0 | 0x0        | 0x0      | 
| 7  | 0x1E170 | 0x0        | 0x0      | 
| 8  | 0x1ED80 | 0x0        | 0x0      | 
| 9  | 0x1F9D0 | 0x0        | 0x0      | 
| 10 | 0x24880 | 0x0        | 0x0      | 
| 11 | 0x29C50 | 0x0        | 0x0      | 
+----+---------+------------+----------+
```

In addition, you can extract all the samples to a folder by running this command: `FlipnicFileTool --input SE_ST01.BD --extract-samples --output .`

## Save file icon (*.ICO)

This is just a standard PlayStation 2 save icon with an RLE compressed texture. But this tool can still do some things with it.

**Note**: This tool is specific to Flipnic, so save icons with animations will not work with this tool.

Example: `FlipnicFileTool --input FICON.ICO --show-ico`

Outputs:
```
PlayStation 2 save icon

Magic: 10000
Shapes: 1
Polygons: 128

Texture:

Width: 128
Height: 128

Type: RLE compressed
Compressed size: 4.41 kiB
Decompressed size: 29.37 kiB
```

You can extract the texture by running this command: `FlipnicFileTool --input FICON.ICO --convert-ico-texture --output FICON.PNG`.

You can convert it to Wavefront OBJ by running this command: `FlipnicFileTool --input FICON.ICO --convert-ico-obj --output FICON.OBJ`.

## Collision maps (*.COL)

Contains boundaries and hitboxes for various areas on a stage. These files contain various sections labelled "WAL" and "GRD" (short for wall and ground respectively).

Example: `FlipnicFileTool --input COL_23_20.COL --show-col`

Outputs
````
Collision map
Walls: 10
Grounds: 3
+-------+---------------+----------+
| Label | Base vertices | Vertices | 
+-------+---------------+----------+
| GRD02 | 4             | 33       | 
| GRD04 | 4             | 42       | 
| GRD05 | 4             | 129      | 
| WAL08 | 4             | 156      | 
| WAL09 | 4             | 222      | 
| WAL10 | 4             | 78       | 
| WAL11 | 4             | 186      | 
| WAL12 | 4             | 156      | 
| WAL13 | 4             | 30       | 
| WAL14 | 4             | 6        | 
| WAL15 | 4             | 42       | 
| WAL16 | 4             | 42       | 
| WAL17 | 4             | 6        | 
+-------+---------------+----------+
````

You can convert each individual part of the collision map to a Wavefront OBJ file using this command: `FlipnicFileTool --input COL_23_20.COL --export-col-obj GRD02 --output COL_02_05.GRD02.OBJ`

You can convert all parts of the collision map at once to a Wavefront OBJ file using this command: `FlipnicFileTool --input COL_23_20.COL --export-col-obj ALL --output COL_02_05.OBJ`

## Environment lighting (*.LIT)

Determines how the environment of a stage is lit using RGB light intensity values.

Example: `FlipnicFileTool --input LIT_DEF.LIT --show-lit`

Outputs:
```
Color intensities:
+-----------+-----------+------------+
| Red       | Green     | Blue       | 
+-----------+-----------+------------+
| -73.21529 | 3.9162624 | -52.386494 | 
| 54.085926 | 6.349018  | -35.757645 | 
| 32.440845 | -79.44349 | 61.301685  | 
| 0.2       | 0.2       | 0.2        | 
| 0.2       | 0.2       | 0.2        | 
| 0.9       | 0.9       | 0.9        | 
| 0.1       | 0.1       | 0.1        | 
+-----------+-----------+------------+
```

## Layout files (*.LAY)

Determines where things are placed on the stage and how they are scaled/skewed.

**Note**: LAY implementation is unfinished

Example: `FlipnicFileTool --input LAY_13_14.LAY --show-lay`

Outputs:
````
+--------------------------------------+--------------------------------------+--------------------------------------+--------------------------------------+
| Label                                | Size                                 | Skew                                 | Position                             | 
+--------------------------------------+--------------------------------------+--------------------------------------+--------------------------------------+
| LAY_13_14                            | 1/1/1                                | 0/0/0                                | 0/0/0                                |
| PIN1_BMP_SHADOW_                     | 1/1/1                                | 0/0/0                                | 480.3896/-3.5159056/522.5597         |
...
````

## Texture lists (*.FTL)

The FTL file contains basically the same info as the corresponding .TXT file, but in binary form. This program also allows you to decode the binary file.

Example: `FlipnicFileTool --input FTEXLIST.FTL --show-ftl`

Outputs:
```
Texture list
Count: 269

+--------------+-------------------+
| Texture      | Offset/Dimensions | 
+--------------+-------------------+
| FA0X0000.TM2 | 32x8x23x15        | 
| FA0X0001.TM2 | 32x6x25x19        | 
| FA0X0002.TM2 | 32x32x0x−32       | 
| FA0X0020.TM2 | 32x8x23x15        | 
| FA0X0021.TM2 | 32x10x21x11       | 
...
```

## Dummy files (*.DAT)

Flipnic has a dummy file to prevent disc drive overseek related issues. You can view some basic information about it.

Example: `FlipnicFileTool --input DUMMY.DAT --show-dummy`

Outputs:
```
Dummy file

Purpose: Prevent system crash when DVD laser tries to read past the game data
Zero padded: Yes
Total size: 2 kiB
```

# Source code control files (VSSVER.SCC)

In some versions of Flipnic, there are VSSVER.SCC files, which are accidental development left-overs that were never supposed to ship with the final game. These were used to speed up source control for Microsoft Visual SourceSafe and contain some information about the source code files used during development (not much though) and the project GUID.

You can partially decode these with Flipnic file tools (timestamp format is proprietary and I have no idea how to decode it, which is unfortunate).

Example: `FlipnicFileTool --input VSSVER.SCC --show-vss`

Outputs:
```
Microsoft Visual SourceSafe
Source Code Control file

Project GUID: {00000002-EE3C-0068-00FF-000000000000}
Checksum: 0996AD63
Project ID: 67F4h

Associated files:
+---------+----------+-----------+----------+
| File ID | Checksum | Timestamp | Revision | 
+---------+----------+-----------+----------+
| 67F5h   | 7D3EE67B | 37E91A83  | 1        | 
| 67F6h   | 9122A6A3 | 48FBCD1C  | 2        | 
| 67F7h   | F0B5FA2C | 37F1F395  | 1        | 
| 67F8h   | 8B5F2638 | 37F7903B  | 1        | 
| 67F9h   | 66576DFE | 37F8CCE7  | 1        | 
| 67FAh   | 4E6236FD | 37FE690D  | 1        | 
| 67FBh   | 8F7FA59B | 488ABC86  | 2        | 
+---------+----------+-----------+----------+
```

## FlipnicLib

If you want to work with Flipnic file formats on your own projects, you can add FlipnicLib as a dependancy to your project. Both the GUI and CLI of FlipnicFileTool are just front-ends to this library.

Example:

```C#
using FlipnicLib;
using FlipnicLib.Formats;

namespace FlipnicLibExample {
    public static void Main(string[] args) {
        // Print library version
        Console.WriteLine($"FlipnicLib version: {StaticUtils.DotFloatString(StaticUtils.LibVersion)}");
        // SST file example
        if (args.Length > 0) {
            Console.Write($"Filename: {args[0]}");
            var sst = new FpnSst(File.OpenRead(args[0]));
            Console.Write(sst.ListEntries());
            return;
        }
        Console.WriteLine("Please provide args!");
    }
}
```
