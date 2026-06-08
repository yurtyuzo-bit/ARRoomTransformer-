#!/bin/bash
echo "--- Post Build Script Started ---"
mkdir -p ~/private_keys
cp AuthKey_P66ZDBG7NV.p8 ~/private_keys/AuthKey_P66ZDBG7NV.p8

# Find the .ipa file (Unity Cloud Build puts it in the root or a subfolder usually)
IPA_FILE=$(find . -name "*.ipa" | head -n 1)

if [ -z "$IPA_FILE" ]; then
    echo "ERROR: Could not find .ipa file to upload!"
    exit 1
fi

echo "Found IPA at: $IPA_FILE"
echo "Uploading to App Store Connect..."

if xcrun altool --upload-app \
    --type ios \
    -f "$IPA_FILE" \
    --apiKey "P66ZDBG7NV" \
    --apiIssuer "095c4ffe-a17a-46df-abfd-5435c6871b74"; then
    echo "--- Upload to App Store Connect SUCCESSFUL ---"
else
    echo "--- Upload to App Store Connect FAILED ---"
    exit 1
fi
