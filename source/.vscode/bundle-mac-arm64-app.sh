#!/usr/bin/env bash
set -euo pipefail

WORKSPACE_DIR="$(cd "$(dirname "${BASH_SOURCE[0]}")/.." && pwd)"
PUBLISH_DIR="$WORKSPACE_DIR/artifacts/publish/osx-arm64"
APP_DIR="$WORKSPACE_DIR/artifacts/bundle/osx-arm64/ImageGlass.app"
CONTENTS_DIR="$APP_DIR/Contents"
INFO_PLIST="$CONTENTS_DIR/Info.plist"
BUILD_PROPS_FILE="$WORKSPACE_DIR/Directory.Build.props"
ICON_SOURCE_FILE="$WORKSPACE_DIR/_assets/Logo.icns"
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

INFO_PLIST_TEMPLATE="$WORKSPACE_DIR/_assets/Info.plist"

sed -e "s/\${IG_VERSION}/$IG_VERSION/g" \
    -e "s/\${IG_SHORT_VERSION}/$IG_SHORT_VERSION/g" \
    "$INFO_PLIST_TEMPLATE" > "$INFO_PLIST"

chmod +x "$CONTENTS_DIR/MacOS/ImageGlass"
echo "Created bundle: $APP_DIR"
