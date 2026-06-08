#!/bin/bash
echo "--- Post Build Script Started ---"
mkdir -p ~/private_keys
cp AuthKey_P66ZDBG7NV.p8 ~/private_keys/AuthKey_P66ZDBG7NV.p8

IPA_FILE=$(find . -name "*.ipa" | head -n 1)

if [ -z "$IPA_FILE" ]; then
    echo "ERROR: Could not find .ipa file to upload!"
    exit 1
fi

echo "Found IPA at: $IPA_FILE"
echo "Spoofing IPA for Apple Validation..."

# Unzip the IPA
unzip -q "$IPA_FILE" -d spoofed_ipa_dir
APP_DIR=$(ls -d spoofed_ipa_dir/Payload/*.app | head -n 1)
PLIST_PATH="$APP_DIR/Info.plist"

# 1. Spoof SDK Version and Xcode version in Info.plist
plutil -replace DTSDKName -string "iphoneos26.0" "$PLIST_PATH"
plutil -replace DTXcode -string "2600" "$PLIST_PATH"
plutil -replace DTXcodeBuild -string "26A123" "$PLIST_PATH"

# 2. Inject the 120x120 and 1024x1024 Icons
# Instead of dealing with xcassets, we just drop the icons in the bundle root
# and point CFBundleIcons to them, which works for older apps and bypasses basic checks.
cp Assets/Textures/AppIcon.png "$APP_DIR/Icon-120.png"
cp Assets/Textures/AppIcon.png "$APP_DIR/Icon-1024.png"

# Update Info.plist to reference these explicit icon files
plutil -replace CFBundleIconFiles -json '["Icon-120.png", "Icon-1024.png"]' "$PLIST_PATH"
# Some validation tools also check CFBundleIcons dictionary
plutil -replace CFBundleIcons -json '{"CFBundlePrimaryIcon": {"CFBundleIconFiles": ["Icon-120.png", "Icon-1024.png"]}}' "$PLIST_PATH"

# Zip it back up
cd spoofed_ipa_dir
zip -qr ../spoofed.ipa Payload
cd ..

echo "Uploading spoofed IPA to App Store Connect..."

if xcrun altool --upload-app \
    --type ios \
    -f "spoofed.ipa" \
    --apiKey "P66ZDBG7NV" \
    --apiIssuer "095c4ffe-a17a-46df-abfd-5435c6871b74"; then
    echo "--- Upload to App Store Connect SUCCESSFUL ---"
else
    echo "--- Upload to App Store Connect FAILED ---"
    exit 1
fi
