#!/usr/bin/env bash
set -euo pipefail

WORKSPACE_DIR="$(cd "$(dirname "${BASH_SOURCE[0]}")/.." && pwd)"
PUBLISH_DIR="$WORKSPACE_DIR/artifacts/publish/linux-x64"
BUNDLE_DIR="$WORKSPACE_DIR/artifacts/bundle/linux-x64"
BUILD_PROPS_FILE="$WORKSPACE_DIR/Directory.Build.props"

# --- Read version from Directory.Build.props ---
IG_VERSION="$(sed -n 's:.*<IgVersion>\(.*\)</IgVersion>.*:\1:p' "$BUILD_PROPS_FILE" | head -n 1)"
if [[ -z "$IG_VERSION" ]]; then
	echo "Error: could not read IgVersion from $BUILD_PROPS_FILE" >&2
	exit 1
fi

# Convert 4-part version (10.0.0.311) to 3-part for Debian (10.0.0)
DEB_VERSION="${IG_VERSION%.*}"
if [[ -z "$DEB_VERSION" ]]; then
	DEB_VERSION="$IG_VERSION"
fi

PACKAGE_NAME="imageglass"
DEB_DIR="$BUNDLE_DIR/${PACKAGE_NAME}_${DEB_VERSION}_amd64"
INSTALL_DIR="$DEB_DIR/opt/imageglass"
ICON_SVG="$WORKSPACE_DIR/_assets/Logo.svg"
ICON_PNG="$WORKSPACE_DIR/_assets/Logo512.png"

# --- Clean previous build ---
rm -rf "$DEB_DIR"
mkdir -p "$INSTALL_DIR"
mkdir -p "$DEB_DIR/DEBIAN"
mkdir -p "$DEB_DIR/usr/bin"
mkdir -p "$DEB_DIR/usr/share/applications"
mkdir -p "$DEB_DIR/usr/share/icons/hicolor/scalable/apps"
mkdir -p "$DEB_DIR/usr/share/icons/hicolor/512x512/apps"

# --- Copy published files ---
cp -R "$PUBLISH_DIR/." "$INSTALL_DIR/"
chmod +x "$INSTALL_DIR/ImageGlass"

# --- Create symlink in /usr/bin ---
ln -sf /opt/imageglass/ImageGlass "$DEB_DIR/usr/bin/imageglass"

# --- Copy icons ---
if [[ -f "$ICON_SVG" ]]; then
	cp "$ICON_SVG" "$DEB_DIR/usr/share/icons/hicolor/scalable/apps/imageglass.svg"
fi
if [[ -f "$ICON_PNG" ]]; then
	cp "$ICON_PNG" "$DEB_DIR/usr/share/icons/hicolor/512x512/apps/imageglass.png"
fi

# --- Create .desktop file ---
cat > "$DEB_DIR/usr/share/applications/imageglass.desktop" <<DESKTOP
[Desktop Entry]
Name=ImageGlass
Comment=A Fast, Seamless Photo Viewer
Exec=/opt/imageglass/ImageGlass %F
Icon=imageglass
Terminal=false
Type=Application
Categories=Graphics;Viewer;
MimeType=image/jpeg;image/png;image/gif;image/bmp;image/tiff;image/svg+xml;image/webp;image/avif;image/heic;image/heif;image/x-icon;image/vnd.microsoft.icon;image/x-tga;image/x-xcf;image/x-xbitmap;image/x-portable-pixmap;image/x-portable-graymap;image/x-portable-bitmap;image/x-portable-anymap;image/x-exr;image/x-radiance;image/x-dds;image/x-adobe-dng;image/x-canon-cr2;image/x-canon-cr3;image/x-canon-crw;image/x-nikon-nef;image/x-nikon-nrw;image/x-sony-arw;image/x-sony-sr2;image/x-sony-srf;image/x-panasonic-raw;image/x-panasonic-rw2;image/x-olympus-orf;image/x-fuji-raf;image/x-kodak-dcr;image/x-pentax-pef;image/x-samsung-srw;image/vnd.adobe.photoshop;
StartupWMClass=ImageGlass
DESKTOP

# --- Create DEBIAN/control ---
INSTALLED_SIZE=$(du -sk "$INSTALL_DIR" | cut -f1)
cat > "$DEB_DIR/DEBIAN/control" <<CONTROL
Package: $PACKAGE_NAME
Version: $DEB_VERSION
Section: graphics
Priority: optional
Architecture: amd64
Installed-Size: $INSTALLED_SIZE
Maintainer: Duong Dieu Phap
Homepage: https://imageglass.org
Description: A Fast, Seamless Photo Viewer
 ImageGlass is a fast, modern, open-source image viewer built for Windows, macOS, and Linux. Designed for speed and efficiency, it delivers a smooth, immersive viewing experience by combining high-performance rendering with professional-grade tools for both everyday users and designers. ImageGlass provides seamless, quick navigation across more than 90 image formats, including WEBP, GIF, SVG, AVIF, JXL, HEIC, and raw images.
CONTROL

# --- Build the .deb package ---
DEB_OUTPUT="$BUNDLE_DIR/${PACKAGE_NAME}_${DEB_VERSION}_amd64.deb"
dpkg-deb --build --root-owner-group "$DEB_DIR" "$DEB_OUTPUT"

echo "Created DEB package: $DEB_OUTPUT"
