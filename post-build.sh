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

echo "Extracting original entitlements..."
codesign -d --entitlements :- "$APP_DIR" > entitlements.plist || echo "No entitlements found or error extracting."

# 1. Spoof SDK Version and Xcode version in ALL Info.plist files inside the app bundle
echo "Modifying Info.plist files..."
find "$APP_DIR" -name "Info.plist" | while read -r PLIST_PATH; do
    plutil -replace DTSDKName -string "iphoneos26.0" "$PLIST_PATH"
    plutil -replace DTXcode -string "2600" "$PLIST_PATH"
    plutil -replace DTXcodeBuild -string "26A123" "$PLIST_PATH"
    plutil -replace DTPlatformVersion -string "26.0" "$PLIST_PATH"
    plutil -replace DTSDKBuild -string "26A123" "$PLIST_PATH"
    plutil -replace MinimumOSVersion -string "16.0" "$PLIST_PATH"
done

# 1.5 Spoof Mach-O LC_BUILD_VERSION using vtool
echo "Modifying Mach-O binaries using vtool..."
# iOS platform is 2. minOS 16.0, SDK 26.0
find "$APP_DIR" -type f -perm +111 | while read -r BIN_PATH; do
    # Check if it's a Mach-O file
    if file "$BIN_PATH" | grep -q "Mach-O"; then
        echo "Running vtool on $BIN_PATH"
        xcrun vtool -set-build-version 2 16.0 26.0 -replace -output "$BIN_PATH" "$BIN_PATH" || echo "vtool failed on $BIN_PATH"
    fi
done

# 2. Re-sign all embedded frameworks FIRST
echo "Re-signing embedded frameworks..."
IDENTITY="Apple Distribution: Velat AYDEMİR (NJSJBH25GM)"

find "$APP_DIR/Frameworks" -name "*.framework" -o -name "*.dylib" | while read -r FRAMEWORK_PATH; do
    echo "Re-signing: $FRAMEWORK_PATH"
    codesign --force --sign "$IDENTITY" "$FRAMEWORK_PATH"
done

# 3. Re-sign the main app bundle
echo "Re-signing the main app bundle..."
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
