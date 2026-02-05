#!/bin/bash
#
# FlipnicFileTool - Automated testing system
# Once you've setup the testing environment, you can run this script to test opening every
# file from Flipnic.
# 
# If the file can be opened, the test passes, but if the program crashes
# when opening a file, the test fails. Note that a test may pass even when
# a file is decoded incorrectly, this test is meant mainly for finding
# file format edge cases.
#
# Requires the "timeout" command (for catching infinite loops), if you're running macOS,
# you can install it with Homebrew: brew install aisk/homebrew-tap/timeout
#

# the out directory must contain an executable named "FlipnicFileTool"
PATH="$PATH:$(pwd)/out"

# *CHANGE THIS* the location where you've extracted the contents of all .BIN files
ROOT_PATH="/Users/markusmaal/Documents/Flipnic"
cd "$ROOT_PATH"


#
# 1 - Extension
# 2 - Dirs array
# 3 - FlipnicFileTool args
#
RUN_TEST () {
  echo
  echo "Testing .$1 files"
  echo "--------------------------------------------------------"
  DIRS="$2"
  IFS=" "
  for dir in $DIRS
  do
    pushd $dir >/dev/null
    for file in $(pwd)/*.$1; do
      FlipnicFileTool --input "$file" $3 --test 2>/dev/null >/dev/null && printf "[ \e[0;32mPASS\e[0m ] $file\n" || printf "[ \e[0;31mFAIL\e[0m ] $file\n"
    done
    popd >/dev/null
  done
}

# Comment to exclude any specific test
RUN_TEST "COL" "BOSS1 BOSS2 BOSS3 BOSS4 HIKARI1 HIKARI2 ISEKI1 ISEKI2 JUNGLE1 JUNGLE2 RETRO1 VS1 VS2 VS3 VS4" "--show-col"
RUN_TEST "LAY" "BOSS1 BOSS2 BOSS3 BOSS4 HIKARI1 HIKARI2 ISEKI1 ISEKI2 JUNGLE1 JUNGLE2 RETRO1 VS1 VS2 VS3 VS4" "--show-lay"
RUN_TEST "FPD" "BOSS1 BOSS2 BOSS3 BOSS4 HIKARI1 HIKARI2 ISEKI1 ISEKI2 JUNGLE1 JUNGLE2 RETRO1 VS1 VS4" "--show-fpd"
RUN_TEST "FPC" "BOSS1 BOSS2 BOSS3 BOSS4 HIKARI1 HIKARI2 ISEKI1 ISEKI2 JUNGLE1 JUNGLE2 RETRO1 VS1 VS2 VS3 VS4" "--show-fpc"
RUN_TEST "SST" ". BOSS1 BOSS2 BOSS3 BOSS4 HIKARI1 HIKARI2 ISEKI1 ISEKI2 JUNGLE1 JUNGLE2 RETRO1 VS1 VS2 VS3 VS4" "--show-sst-toc"
RUN_TEST "PSS" "." "--list-pss-streams"
RUN_TEST "SVAG" "." "--convert-svag --output TEST.WAV"
RUN_TEST "MLB" "MENU MISSION OPTION PMENU" "--show-mlb"
RUN_TEST "TM2" "BOSS1 BOSS2 BOSS3 BOSS4 FONT HIKARI1 HIKARI2 ISEKI1 ISEKI2 JUNGLE1 JUNGLE2 MENU MISSION OPTION PMENU RETRO1 VS1 VS2 VS3 VS4" "--show-tim2"
RUN_TEST "HD" "BOSS1 BOSS2 BOSS3 BOSS4 HIKARI1 HIKARI2 ISEKI1 ISEKI2 JUNGLE1 JUNGLE2 MENU RETRO1 VS1 VS2 VS3 VS4" "--show-hd"
RUN_TEST "BD" "BOSS1 BOSS2 BOSS3 BOSS4 HIKARI1 HIKARI2 ISEKI1 ISEKI2 JUNGLE1 JUNGLE2 MENU RETRO1 VS1 VS2 VS3 VS4" "--show-bd"
RUN_TEST "MID" "BOSS1 BOSS2 BOSS3 BOSS4 HIKARI1 HIKARI2 ISEKI1 ISEKI2 JUNGLE1 JUNGLE2 MENU RETRO1 VS1 VS2 VS3 VS4" "--show-midi"
RUN_TEST "MSG" "." "--show-messages"
RUN_TEST "VSD" "." "--show-vsd"
RUN_TEST "ICO" "MENU" "--show-ico"
# these two tests can fail if you don't have any .BIN or .ISO files in that directory
RUN_TEST "BIN" "." "--list-files"
RUN_TEST "ISO" "." "--show-iso"
# all .LIT tests will currently fail, because FFT doesn't have support for this format yet
RUN_TEST "LIT" "BOSS1 BOSS2 BOSS3 BOSS4 HIKARI1 HIKARI2 ISEKI1 ISEKI2 JUNGLE1 JUNGLE2 RETRO1 VS1 VS2 VS3 VS4" "--show-lit"
RUN_TEST "LP4" "BOSS1 BOSS2 BOSS3 BOSS4 HIKARI1 HIKARI2 ISEKI1 ISEKI2 JUNGLE1 JUNGLE2 RETRO1 VS1 VS2 VS3 VS4" "--show-lp4"