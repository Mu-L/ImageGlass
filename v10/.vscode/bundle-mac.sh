#!/usr/bin/env bash
set -euo pipefail

WORKSPACE_DIR="$(cd "$(dirname "${BASH_SOURCE[0]}")/.." && pwd)"
PUBLISH_DIR="$WORKSPACE_DIR/artifacts/publish/osx-arm64"
APP_DIR="$WORKSPACE_DIR/artifacts/bundle/osx-arm64/ImageGlass.app"
CONTENTS_DIR="$APP_DIR/Contents"
INFO_PLIST="$CONTENTS_DIR/Info.plist"
BUILD_PROPS_FILE="$WORKSPACE_DIR/Directory.Build.props"
ICON_SOURCE_FILE="$WORKSPACE_DIR/assets/Logo.icns"
ICON_TARGET_FILE="$CONTENTS_DIR/Resources/Logo.icns"

IG_VERSION="$(sed -n 's:.*<IgVersion>\(.*\)</IgVersion>.*:\1:p' "$BUILD_PROPS_FILE" | head -n 1)"
if [[ -z "$IG_VERSION" ]]; then
	echo "Error: could not read IgVersion from $BUILD_PROPS_FILE" >&2
	exit 1
fi

IG_SHORT_VERSION="${IG_VERSION%.*}"
if [[ -z "$IG_SHORT_VERSION" ]]; then
	IG_SHORT_VERSION="$IG_VERSION"
fi

rm -rf "$APP_DIR"
mkdir -p "$CONTENTS_DIR/MacOS" "$CONTENTS_DIR/Resources"
cp -R "$PUBLISH_DIR/." "$CONTENTS_DIR/MacOS/"
cp "$ICON_SOURCE_FILE" "$ICON_TARGET_FILE"

cat > "$INFO_PLIST" <<PLIST
<?xml version="1.0" encoding="UTF-8"?>
<!DOCTYPE plist PUBLIC "-//Apple//DTD PLIST 1.0//EN" "http://www.apple.com/DTDs/PropertyList-1.0.dtd">
<plist version="1.0">
<dict>
	<key>CFBundleDevelopmentRegion</key>
	<string>en</string>
	<key>CFBundleInfoDictionaryVersion</key>
	<string>6.0</string>
	<key>CFBundlePackageType</key>
	<string>APPL</string>
	<key>NSPrincipalClass</key>
	<string>NSApplication</string>
	<key>NSHighResolutionCapable</key>
	<true/>
	<key>LSMinimumSystemVersion</key>
	<string>10.13</string>
	<key>NSRequiresAquaSystemAppearance</key>
	<false/>
	<key>NSSupportsAutomaticTermination</key>
	<true/>
	<key>NSSupportsSuddenTermination</key>
	<false/>
	<key>CFBundleSupportedPlatforms</key>
	<array>
		<string>MacOSX</string>
	</array>
	<key>CFBundleIdentifier</key>
	<string>com.duongdieuphap.imageglass</string>
	<key>CFBundleName</key>
	<string>ImageGlass</string>
	<key>CFBundleDisplayName</key>
	<string>ImageGlass</string>
	<key>CFBundleExecutable</key>
	<string>ImageGlass</string>
	<key>CFBundleVersion</key>
	<string>$IG_VERSION</string>
	<key>CFBundleShortVersionString</key>
	<string>$IG_SHORT_VERSION</string>
	<key>CFBundleIconFile</key>
	<string>Logo.icns</string>
	<key>NSHumanReadableCopyright</key>
	<string>Copyright (C) 2010-2026 Dương Diệu Pháp</string>

	<key>CFBundleDocumentTypes</key>
  <array>
    <dict>
      <key>CFBundleTypeName</key>
      <string>Image</string>
      <key>CFBundleTypeRole</key>
      <string>Viewer</string>
      <key>LSItemContentTypes</key>
      <array>
        <string>public.image</string>
        <string>public.camera-raw-image</string>
        
        <string>public.jpeg</string>
        <string>public.png</string>
        <string>public.tiff</string>
        <string>com.compuserve.gif</string>
        <string>com.microsoft.bmp</string>
        <string>com.microsoft.ico</string>
        <string>public.svg-image</string>
        <string>public.webp</string>
        <string>public.avif</string>
        <string>public.heic</string>
        <string>public.heif</string>
        
        <string>com.adobe.photoshop-image</string>
        <string>com.ilm.openexr-image</string>
        <string>public.radiance</string>
        <string>com.truevision.tga-image</string>
        <string>public.xbitmap-image</string>
        <string>com.microsoft.dds</string>
        <string>com.microsoft.cur</string>
        
        <string>com.adobe.raw-image</string>
        <string>com.canon.cr2-raw-image</string>
        <string>com.canon.cr3-raw-image</string>
        <string>com.canon.crw-raw-image</string>
        <string>com.nikon.raw-image</string>
        <string>com.nikon.nrw-raw-image</string>
        <string>com.sony.arw-raw-image</string>
        <string>com.sony.sr2-raw-image</string>
        <string>com.sony.srf-raw-image</string>
        <string>com.panasonic.raw-image</string>
        <string>com.panasonic.rw2-raw-image</string>
        <string>com.olympus.raw-image</string>
        <string>com.fuji.raw-image</string>
        <string>com.hasselblad.3fr-raw-image</string>
        <string>com.hasselblad.fff-raw-image</string>
        <string>com.kodak.raw-image</string>
        <string>com.leica.raw-image</string>
        <string>com.leica.rwl-raw-image</string>
        <string>com.pentax.raw-image</string>
        <string>com.phaseone.raw-image</string>
        <string>com.samsung.raw-image</string>
      </array>
    </dict>
  </array>

</dict>
</plist>
PLIST

chmod +x "$CONTENTS_DIR/MacOS/ImageGlass"
echo "Created bundle: $APP_DIR"
