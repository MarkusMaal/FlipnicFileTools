Write-Output "Building CLI"
dotnet publish FlipnicFileTool -c Release -o out -p:PublishReadyToRun=true -p:PublishSingleFile=true -p:PublishTrimmed=true --self-contained true -p:IncludeNativeLibrariesForSelfExtract=true -p:DebugSymbols=false
Write-Output "Building GUI"
dotnet publish FlipnicFileToolGUI -c Release -o out -p:PublishReadyToRun=true -p:PublishSingleFile=true -p:PublishTrimmed=true --self-contained true -p:IncludeNativeLibrariesForSelfExtract=true -p:DebugSymbols=false
Write-Output "Finished!"
