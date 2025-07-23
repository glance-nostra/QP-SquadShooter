#!/bin/bash

# Colors for output
RED='\033[0;31m'
GREEN='\033[0;32m'
YELLOW='\033[1;33m'
NC='\033[0m' # No Color

set -e

# Get project root directory
PLATFORM_PATH="$(cd "$(dirname "$0")/.." && pwd)"
echo -e "${YELLOW}Project root directory: $PLATFORM_PATH${NC}"

UNITY_PATH="/Applications/Unity/Hub/Editor/6000.0.30f1/Unity.app/Contents/MacOS/Unity"
LOG_DIR="$PLATFORM_PATH/logs"
LOG_PATH="$LOG_DIR/apk_build.log"
APK_OUTPUT_DIR="$PLATFORM_PATH/Builds/Android"
APK_PATH="$APK_OUTPUT_DIR/app.apk"

GAMES_DIR="$PLATFORM_PATH/Assets/Games"
# DLL_URL=""
DLL_DEST="$GAMES_DIR/${GAME_NAME}.dll"
LINK_XML_PATH="$PLATFORM_PATH/Assets/link.xml"
QUICKPLAY_DLL_PATH="$PLATFORM_PATH/Assets/Plugins/Nostra/QuickPlay.dll"
QUICKPLAY_DLL_URL="https://x-stg.glance-cdn.com/public/content/assets/other/QuickPlay.dll"
BUNDLE_VERSION="0.0.3"
PROJECT_SETTINGS_PATH="$PLATFORM_PATH/ProjectSettings/ProjectSettings.asset"

# Check if jq is installed
if ! command -v jq &> /dev/null; then
    echo -e "${RED}Error: jq is not installed. Please install it using 'brew install jq'${NC}"
    exit 1
fi

if [ -z "$GAME_NAME" ] || [ "$GAME_NAME" == "null" ]; then
    echo -e "${RED}Error: Could not extract game_name from config file${NC}"
    exit 1
fi

echo -e "${GREEN}Using game name: $GAME_NAME from config file${NC}"

# Create output and logs directory
mkdir -p "$APK_OUTPUT_DIR"
mkdir -p "$LOG_DIR"

# Remove contents of Assets/Games folder before build
if [ -d "$GAMES_DIR" ]; then
    echo -e "${YELLOW}Cleaning Assets/Games directory...${NC}"
    rm -rf "$GAMES_DIR"/*
else
    echo -e "${YELLOW}Assets/Games directory does not exist, skipping clean.${NC}"
fi

# Download DLL and rename to <GAME_NAME>.dll in Assets/Games
echo -e "${YELLOW}Downloading DLL from $DLL_URL${NC}"
curl -fSL "$DLL_URL" -o "$DLL_DEST"
if [ $? -eq 0 ]; then
    echo -e "${GREEN}DLL downloaded and saved as $DLL_DEST${NC}"
else
    echo -e "${RED}Failed to download DLL from $DLL_URL${NC}"
    exit 1
fi

# Replace Assets/Plugins/Nostra/QuickPlay.dll with the latest version from the provided URL
echo -e "${YELLOW}Replacing $QUICKPLAY_DLL_PATH with latest QuickPlay.dll...${NC}"
if [ -f "$QUICKPLAY_DLL_PATH" ]; then
    rm -f "$QUICKPLAY_DLL_PATH"
    echo -e "${GREEN}Deleted existing QuickPlay.dll${NC}"
else
    echo -e "${YELLOW}No existing QuickPlay.dll found, skipping delete.${NC}"
fi
curl -fSL "$QUICKPLAY_DLL_URL" -o "$QUICKPLAY_DLL_PATH"
if [ $? -eq 0 ]; then
    echo -e "${GREEN}Downloaded new QuickPlay.dll to $QUICKPLAY_DLL_PATH${NC}"
else
    echo -e "${RED}Failed to download QuickPlay.dll from $QUICKPLAY_DLL_URL${NC}"
    exit 1
fi

# Ensure bundleVersion in ProjectSettings.asset is always set to the desired value
echo -e "${YELLOW}Setting bundleVersion to $BUNDLE_VERSION in $PROJECT_SETTINGS_PATH...${NC}"
sed -i '' "/^[[:space:]]*bundleVersion:/s/:.*/: $BUNDLE_VERSION/" "$PROJECT_SETTINGS_PATH"
if [ $? -eq 0 ]; then
    echo -e "${GREEN}bundleVersion set to $BUNDLE_VERSION in $PROJECT_SETTINGS_PATH${NC}"
else
    echo -e "${RED}Failed to set bundleVersion in $PROJECT_SETTINGS_PATH${NC}"
    exit 1
fi

# Update Assets/link.xml with linker XML for the game DLL
echo -e "${YELLOW}Updating $LINK_XML_PATH with linker XML...${NC}"
cat > "$LINK_XML_PATH" <<EOF
<linker>
  <assembly fullname="$GAME_NAME">
    <type fullname="$GAME_NAMESPACE" preserve="all"/>
  </assembly>
</linker>
EOF
if [ $? -eq 0 ]; then
    echo -e "${GREEN}link.xml updated successfully at $LINK_XML_PATH${NC}"
else
    echo -e "${RED}Failed to update link.xml at $LINK_XML_PATH${NC}"
    exit 1
fi

echo -e "${YELLOW}Starting Android APK build for $GAME_NAME...${NC}"
"$UNITY_PATH" -batchmode -quit -projectPath "$PLATFORM_PATH" \
    -executeMethod nostra.platform.build.BuildTestingAPK.BuildStandaloneTestingAPK \
    -buildTarget Android \
    -gameName "$GAME_NAME" \
    -catalogUrl "$CATALOG_URL" \
    -gameAddress "$GAME_ADDRESS" \
    -isLandscapeGame "$IS_LANDSCAPE_GAME" \
    -logFile "$LOG_PATH" \
    -outputAPK "$APK_PATH"

if [ $? -eq 0 ] && [ -f "$APK_PATH" ]; then
    echo -e "${GREEN}Android APK build completed successfully! APK at $APK_PATH${NC}"
else
    echo -e "${RED}Android APK build failed! Check $LOG_PATH for details.${NC}"
    exit 1
fi