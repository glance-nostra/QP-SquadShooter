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

LOGPATH=$1
if [ -z "$LOGPATH" ]; then
    echo -e "${RED}Error: No log file path provided.${NC}"
    exit 1
fi

# S3 Upload configuration
UPLOAD_API_ENDPOINT="https://turbolive-staging.glance.inmobi.com/api/v2/upload-file"
UPLOAD_DESTINATION_PATH="addressables/logs"

echo -e "${YELLOW}=== S3 Upload Details ===${NC}"
echo -e "File: ${GREEN}$LOGPATH${NC}"
echo -e "Size: ${GREEN}$(ls -lh "$LOGPATH" | awk '{print $5}')${NC}"

# Upload to S3
echo -e "${YELLOW}Uploading failed logfile to S3...${NC}"

RESPONSE=$(curl --location "$UPLOAD_API_ENDPOINT" \
  --form "media=@$LOGPATH" \
  --form "path=$UPLOAD_DESTINATION_PATH")

UPLOAD_URL=$(echo "$RESPONSE" | jq -r '.data.cdn_url // empty')

if [[ -n "$UPLOAD_URL" ]]; then
  echo -e "${GREEN}Upload successful: $UPLOAD_URL${NC}"
else
  echo -e "${RED}Upload failed. Response: $RESPONSE${NC}"
  exit 1
fi
