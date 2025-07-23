#!/bin/bash
# upload_logfile.sh - Upload failed addressables build log file to S3

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

STANDALONE_APK_PATH="$PLATFORM_PATH/Builds/Android/app.apk"
if [ -z "$STANDALONE_APK_PATH" ]; then
    echo -e "${RED}Error: No standalone APK path provided.${NC}"
    exit 1
fi

# S3 Upload configuration
UPLOAD_API_ENDPOINT="https://turbolive-staging.glance.inmobi.com/api/v2/upload-file"
UPLOAD_DESTINATION_PATH="addressables/standalone-testing-apk"

echo -e "${YELLOW}=== S3 Upload Details ===${NC}"
echo -e "File: ${GREEN}$STANDALONE_APK_PATH${NC}"
echo -e "Size: ${GREEN}$(ls -lh "$STANDALONE_APK_PATH" | awk '{print $5}')${NC}"

# Upload to S3
echo -e "${YELLOW}Uploading standalone APK to S3...${NC}"

RESPONSE=$(curl --location "$UPLOAD_API_ENDPOINT" \
  --form "media=@$STANDALONE_APK_PATH" \
  --form "path=$UPLOAD_DESTINATION_PATH")

UPLOAD_URL=$(echo "$RESPONSE" | jq -r '.data.cdn_url // empty')

if [[ -n "$UPLOAD_URL" ]]; then
  echo -e "${GREEN}Upload successful: $UPLOAD_URL${NC}"

  # ✅ Only export output if running inside GitHub Actions
  if [[ -n "$GITHUB_OUTPUT" ]]; then
    echo "standalone_apk_url=$UPLOAD_URL" >> "$GITHUB_OUTPUT"
  fi
else
  echo -e "${RED}Upload failed. Response: $RESPONSE${NC}"
  exit 1
fi
