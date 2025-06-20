#!/bin/bash
# upload_game_dll_to_s3.sh - Upload Game DLL file to S3
# This script uploads the compiled game DLL to S3 for distribution

# Exit on any error
set -e

# Colors for output
RED='\033[0;31m'
GREEN='\033[0;32m'
YELLOW='\033[1;33m'
NC='\033[0m' # No Color

# Get project root directory
PLATFORM_PATH="$(cd "$(dirname "$0")/.." && pwd)"
echo -e "${YELLOW}Project root directory: $PLATFORM_PATH${NC}"

# Read game name from config file
CONFIG_FILE="$PLATFORM_PATH/game_config_template.json"
if [ -f "$CONFIG_FILE" ]; then
    GAME_NAME=$(jq -r '.game_name' "$CONFIG_FILE")
else
    echo -e "${RED}Error: Config file not found at $CONFIG_FILE${NC}"
    exit 1
fi

# Define all key paths and names at the top for reuse
DLL_PATH="Assets/Games/$GAME_NAME/$GAME_NAME.dll"

# Use PR_NUMBER from environment or default to "unknown-pr"
: "${PR_NUMBER:=unknown-pr}"

# S3 Upload configuration
UPLOAD_API_ENDPOINT="https://connect-api.glance.com/api/v2/upload-file"
UPLOAD_DESTINATION_PATH="game-dlls/$GAME_NAME/pr-$PR_NUMBER"

# Check if the DLL file exists
if [ ! -f "$DLL_PATH" ]; then
    echo -e "${RED}Error: Game DLL file not found at $DLL_PATH${NC}"
    echo "Make sure the DLL extraction process completed successfully."
    exit 1
fi

echo -e "${YELLOW}=== S3 Upload Details ===${NC}"
echo -e "File: ${GREEN}$DLL_PATH${NC}"
echo -e "Size: ${GREEN}$(ls -lh "$DLL_PATH" | awk '{print $5}')${NC}"

# Upload to S3
echo -e "${YELLOW}Uploading DLL to S3...${NC}"

RESPONSE=$(curl --location "$UPLOAD_API_ENDPOINT" \
  --form "media=@$DLL_PATH" \
  --form "path=$UPLOAD_DESTINATION_PATH")

echo "Upload response: $RESPONSE"

# Extract URL from response (assuming it returns a JSON with `url`)
UPLOAD_URL=$(echo "$RESPONSE" | jq -r '.data.cdn_url // empty')

echo "Upload URL: $UPLOAD_URL"

if [[ -n "$UPLOAD_URL" ]]; then
  echo -e "${GREEN}Upload successful: $UPLOAD_URL${NC}"
  
  # Create a simple results file if specified
  if [ -n "$RESULTS_FILE" ]; then
    echo "{\"game\": \"$GAME_NAME\", \"pr\": \"$PR_NUMBER\", \"dll_url\": \"$UPLOAD_URL\"}" > "$RESULTS_FILE"
    echo -e "${GREEN}Results saved to $RESULTS_FILE${NC}"
  fi
  
else
  echo -e "${RED}Upload failed. Response: $RESPONSE${NC}"
  exit 1
fi
