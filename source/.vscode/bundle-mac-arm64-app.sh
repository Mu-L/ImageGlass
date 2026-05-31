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

# Relocate non-code data folders into Contents/Resources/ and symlink them back
# into Contents/MacOS/. codesign treats Contents/MacOS/ as the executable dir and
# rejects loose resource files in nested subfolders ("code object is not signed at
# all / In subcomponent ..."). Resources must live under Contents/Resources/.
# The symlinks keep AppDomain.CurrentDomain.BaseDirectory (Contents/MacOS) lookups
# working at runtime, so no app code changes are needed.
for data_dir in _themes _credits; do
	if [[ -d "$CONTENTS_DIR/MacOS/$data_dir" ]]; then
		mv "$CONTENTS_DIR/MacOS/$data_dir" "$CONTENTS_DIR/Resources/$data_dir"
		ln -s "../Resources/$data_dir" "$CONTENTS_DIR/MacOS/$data_dir"
	fi
done

echo "Created bundle: $APP_DIR"
