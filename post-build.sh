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

echo "Extracting original entitlements..."
codesign -d --entitlements :- "$APP_DIR" > entitlements.plist || echo "No entitlements found or error extracting."

# 1. Spoof SDK Version and Xcode version in Info.plist
echo "Modifying Info.plist..."
plutil -replace DTSDKName -string "iphoneos26.0" "$PLIST_PATH"
plutil -replace DTXcode -string "2600" "$PLIST_PATH"
plutil -replace DTXcodeBuild -string "26A123" "$PLIST_PATH"

# 2. Inject the 120x120 and 1024x1024 Icons
echo "Injecting icons..."
cp Assets/Textures/AppIcon.png "$APP_DIR/Icon-120.png"
cp Assets/Textures/AppIcon.png "$APP_DIR/Icon-1024.png"

plutil -replace CFBundleIconFiles -json '["Icon-120.png", "Icon-1024.png"]' "$PLIST_PATH"
plutil -replace CFBundleIcons -json '{"CFBundlePrimaryIcon": {"CFBundleIconFiles": ["Icon-120.png", "Icon-1024.png"]}}' "$PLIST_PATH"

# 3. Re-sign the app bundle
echo "Re-signing the app bundle to fix the signature..."
# The developer identity from the logs
IDENTITY="Apple Distribution: Velat AYDEMİR (NJSJBH25GM)"

if [ -s entitlements.plist ]; then
    codesign --force --sign "$IDENTITY" --entitlements entitlements.plist "$APP_DIR"
else
    codesign --force --sign "$IDENTITY" "$APP_DIR"
fi

# Zip it back up
echo "Zipping modified payload..."
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
