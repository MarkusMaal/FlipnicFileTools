# Build instructions

Final files will be located within the `out/` directory.

## Prerequisites

- First clone this repository with `git clone https://github.com/MarkusMaal/FlipnicFileTools.git --recurse-submodules`
- Microsoft .NET Core 9.0 SDK
- **PowerShell** or **make** (build system)
- **appimagetool** when packaging for Linux

## Building with make

Once you have met the prerequisites, you can run `make` to build both the CLI and GUI versions. Really, it's just a wrapper for `dotnet publish` in this case. 

### Packaging (GUI only)

- If you want to make a .app file for macOS, you need to run `make publish-macos`.
- If you want to make an AppImage for Linux, you need to run `make publish-appimage`.

## Building without make

If you don't have make, you can build the app in PowerShell by running the `build_native.ps1` script.

### Packaging (GUI only)

- If you want to make a .app file for macOS, you need to run `sh deploy_macos.shell`
- If you want to make an AppImage for Linux, you need to run `bash publish-appimage`

## Troubleshooting

If you are having issues, you may have to remove `-p:PublishTrimmed=true` from build flags and try again. However, doing this WILL increase the final file sizes significantly.

If the app crashes often, you may have to attach a debugger to see what's going on, as this software may not display errors without a debugger attached.