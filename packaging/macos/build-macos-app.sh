#!/bin/bash
set -euo pipefail

if [ "$#" -ne 4 ]; then
  echo "usage: build-macos-app.sh <publish_dir> <app_dir> <version> <icon_source>" >&2
  exit 1
fi

PUBLISH_DIR="$1"
APP_DIR="$2"
VERSION="$3"
ICON_SOURCE="$4"
SCRIPT_DIR="$(cd "$(dirname "$0")" && pwd)"
PLIST_TEMPLATE="$SCRIPT_DIR/Info.plist"
CONTENTS_DIR="$APP_DIR/Contents"
MACOS_DIR="$CONTENTS_DIR/MacOS"
RESOURCES_DIR="$CONTENTS_DIR/Resources"
PLIST_PATH="$CONTENTS_DIR/Info.plist"
ICONSET_DIR="$RESOURCES_DIR/EarthBackground.iconset"
ICNS_PATH="$RESOURCES_DIR/EarthBackground.icns"

rm -rf "$APP_DIR"
mkdir -p "$MACOS_DIR" "$RESOURCES_DIR"
cp -a "$PUBLISH_DIR"/. "$MACOS_DIR/"
sed "s/__VERSION__/$VERSION/g" "$PLIST_TEMPLATE" > "$PLIST_PATH"

if [ -f "$ICON_SOURCE" ]; then
  cp "$ICON_SOURCE" "$RESOURCES_DIR/$(basename "$ICON_SOURCE")"
fi

if command -v sips >/dev/null 2>&1 && command -v iconutil >/dev/null 2>&1 && [ -f "$ICON_SOURCE" ]; then
  TMP_PNG="$RESOURCES_DIR/EarthBackground.png"
  rm -rf "$ICONSET_DIR"
  mkdir -p "$ICONSET_DIR"
  if sips -s format png "$ICON_SOURCE" --out "$TMP_PNG" >/dev/null 2>&1; then
    for size in 16 32 128 256 512; do
      sips -z "$size" "$size" "$TMP_PNG" --out "$ICONSET_DIR/icon_${size}x${size}.png" >/dev/null 2>&1
      double_size=$((size * 2))
      sips -z "$double_size" "$double_size" "$TMP_PNG" --out "$ICONSET_DIR/icon_${size}x${size}@2x.png" >/dev/null 2>&1
    done
    if iconutil -c icns "$ICONSET_DIR" -o "$ICNS_PATH" >/dev/null 2>&1; then
      rm -rf "$ICONSET_DIR" "$TMP_PNG"
    fi
  fi
fi
