# Compatibility report

Detailed overview of what specifics of all file formats are supported in Flipnic file tools (latest development build).

## Containers (.BIN)

- [X] List file(s)
- [X] Extract file(s)
- [X] Replace file(s)
- [ ] Add file(s)
- [ ] Remove file(s)
- [ ] Rename file(s)
- [ ] Open subfolder

## Collision maps (.COL)

- [X] Filter sections
- [X] Decode vertices
- [ ] Encode vertices

## Cameras (.FPC)

- [X] Decode default position/FOV
- [X] Decode animation frames
- [X] Encode default position/FOV
- [X] Encode animation frames

## Trajectories (.FPD)

- [X] Path coordinates
- [ ] Path rotation
- [ ] Path flags

## Texture list (.FTL)

- [X] List textures
- [X] List size and offsets
- [ ] List generator

## Voicebank header (.HD)

- [X] Note ranges
- [X] Pitch correction
- [X] Volume and pan
- [X] Locate samples in matching .BD file
- [X] ADSR envelope approximation
- [X] Sustain sate
- [X] Flags: Reverb
- [X] Flags: Vibrato
- [ ] Flags: Pitch bend
- [ ] Flags: High priority
- [ ] Breath waves
- [ ] Velocity chunk
- [X] Embedded sequence (required for sound effects conversion)
- [X] Convert to SF2 (no sustain rate)

## Voicebank body (.BD)

- [X] Convert samples to PCM
- [X] Loop detection
- [ ] Loop correction

## Save icon (.ICO)

- [X] Vertices
- [X] UVs
- [X] Normals
- [X] Uncompressed texture
- [X] RLE compressed texture
- [ ] Aniamtion timelines

## Layouts (.LAY)

- [X] Size/morph
- [X] Position
- [ ] Lightmaps
- [X] Labels
- [X] Modify values

## Light tables (.LIT)

- [X] Color decoding
- [ ] Modify values

## 3D models (.LP4)

- [X] Timelines
- [X] Bounding box
- [X] Lightmaps
- [X] Hitbox
- [X] Labels
- [X] Decompression
- [X] Vertex decoding
- [X] UVs
- [X] Normals
- [ ] Embedded materials
- [X] Textured materials
- [X] Joints
- [ ] Model animation
- [ ] Skews
- [ ] Combine multiple layouts
- [X] Wavefront OBJ generation

## Music sequences (.MID)

- [X] Process MIDI events
- [ ] Modify MIDI events

## Menus (.MLB)

- [X] Section filtering
- [X] Positioning
- [X] Scaling
- [X] Z-order
- [ ] Blend
- [X] Colors
- [ ] Animation
- [ ] Editor
- [X] Convert to PNG

## Strings table (.MSG)

- [X] Decode entries
- [X] Modify entries
- [X] Generate MSG file
- [X] Convert to TXT

## Interleaved audio/video streams (.PSS)

- [X] List streams
- [X] Extract streams
- [X] Create streams from .IPU/.INT
- [ ] Modify streams
- [X] Convert to MP4

## Video (.IPU)

- [X] Convert to MOV
- [ ] Generate from M2V

## Audio (.INT/.SVAG)

- [X] Convert to WAV
- [ ] Generate from WAV

## Source Code Control files (.SCC)

- [X] List project GUID
- [X] List file revisions
- [X] List file IDs
- [ ] Checksum verification
- [ ] List modified date and time

## Stage information file (.SST)

- [X] String lists (\*N/STGNAME)
  - [X] Decode
  - [ ] Modify
- [X] Camera metadata (CAMD)
  - [X] Decode
  - [ ] Modify
- [ ] Area metadata (KUINF)
- [X] Gimmick tables (GMK\*)
  - [X] Decode
  - [X] Modify
- [ ] CAMKUD
- [X] Area codes (KUIDX)
  - [X] Decode
  - [ ] Modify
- [X] Respawn metadata (REBIRTH)
  - [X] Decode
  - [ ] Modify
- [X] SGKTBL
  - [X] Decode
  - [ ] Modify
- [ ] SGKIDX
- [ ] SETBL
- [ ] MTTBL
- [ ] PTTBL
- [ ] MTTBLDEF
- [X] Draw distance and stage mirror (DRAWD)
  - [X] Decode
  - [ ] Modify
- [ ] EVTBL
- [ ] EVTIDX
- [X] Event system (EVENT)
  - [X] Decode
  - [ ] Modify
- [ ] Zero gravity stick-figure particles (FLIESFPB)
- [ ] Stage info (STGINF)
- [X] Default missions data (EVTINF)
- [X] Default ranks (RECORD)
  - [X] Decode
  - [X] Modify

## Texture files (.TM2)

- [X] Decode bitmap
- [X] Paletized color support
- [X] Unscramble CLUT
- [X] Transparency support
- [ ] Mipmaps

## Vibration data (.VSD)

- [X] Decode floats
- [ ] Modify values
