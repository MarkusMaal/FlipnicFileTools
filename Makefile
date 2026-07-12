BUILD_FILES := $(wildcard out/*)
BUILD_DIR := ./out
BUILD_FLAGS := -c Release -o $(BUILD_DIR) -p:PublishReadyToRun=true -p:PublishSingleFile=true -p:PublishTrimmed=true --self-contained true -p:IncludeNativeLibrariesForSelfExtract=true -p:DebugSymbols=false
DOTNET := $(shell which dotnet)
DEPENDS := FlipnicFileToolGUI/FlipnicFileToolGUI.csproj FlipnicLib/FlipnicLib.csproj SoundFont2/SoundFont2/SoundFont2.csproj $(DOTNET)

all : clean restore publish

clean: $(BUILD_FILES)
	rm -rf $(BUILD_DIR)/*

build: $(DEPENDS)
	$(DOTNET) build FlipnicFileToolGUI
	$(DOTNET) build FlipnicFileTool

restore: $(DEPENDS)
	$(DOTNET) restore FlipnicFileToolGUI
	$(DOTNET) restore FlipnicFileTool

publish: ./build_native.ps1 $(DEPENDS)
	@echo "Building CLI"
	$(DOTNET) publish FlipnicFileTool $(BUILD_FLAGS)
	@echo "Building GUI"
	$(DOTNET) publish FlipnicFileToolGUI $(BUILD_FLAGS)
	@echo "Finished!"


publish-macos: clean ./deploy_macos.shell $(DEPENDS)
	sh deploy_macos.shell

publish-appimage: $(shell which appimagetool) ./publish-appimage ./publish-appimage.conf $(DEPENDS)
	bash publish-appimage -y

test: $(DEPENDS)
	$(DOTNET) test --verbosity normal FlipnicLib.Tests/FlipnicLib.Tests.csproj

run: $(DEPENDS)
	$(DOTNET) run --project FlipnicFileToolGUI

run-cli: $(DEPENDS)
	$(DOTNET) run --project FlipnicFileTool