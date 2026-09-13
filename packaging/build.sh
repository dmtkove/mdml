#!/usr/bin/env bash
set -euo pipefail

root="$(cd "$(dirname "$0")/.." && pwd)"
packaging="$root/packaging"
artifacts="$root/artifacts"
export COPYFILE_DISABLE=1
version="$(dotnet msbuild "$root/src/Mdml.Cli/Mdml.Cli.csproj" -nologo -getProperty:Version | tr -d '[:space:]')"

host_rid() {
  local os arch
  os="$(uname -s)"
  arch="$(uname -m)"
  case "$os" in
    Darwin)
      if [[ "$arch" == "arm64" ]]; then echo "osx-arm64"; else echo "osx-x64"; fi
      ;;
    Linux)
      if [[ "$arch" == "aarch64" ]]; then echo "linux-arm64"; else echo "linux-x64"; fi
      ;;
    MINGW*|MSYS*|CYGWIN*)
      echo "win-x64"
      ;;
    *)
      echo "unsupported host: $os $arch" >&2
      exit 1
      ;;
  esac
}

publish_rid() {
  local rid="$1"
  local out="$artifacts/publish/$rid"
  rm -rf "$out"
  dotnet publish "$root/src/Mdml.Cli/Mdml.Cli.csproj" \
    -c Release \
    -r "$rid" \
    --self-contained true \
    -p:PublishSingleFile=true \
    -p:IncludeNativeLibrariesForSelfExtract=true \
    -p:EnableCompressionInSingleFile=true \
    -p:DebugType=embedded \
    -o "$out"
}

macos_arch_for_rid() {
  case "$1" in
    osx-arm64) echo "arm64" ;;
    osx-x64) echo "x86_64" ;;
    *) echo "" ;;
  esac
}

build_macos() {
  local rid="$1"
  local arch
  arch="$(macos_arch_for_rid "$rid")"
  if [[ -z "$arch" ]]; then
    echo "Skipping macOS installer for $rid" >&2
    return
  fi

  local publish="$artifacts/publish/$rid"
  local app="$artifacts/app/$rid/mdml.app"
  local payload="$artifacts/payload/$rid"
  local resources="$artifacts/resources/$rid"
  local pkgs="$artifacts/pkg/$rid"

  rm -rf "$app" "$payload" "$resources" "$pkgs"
  mkdir -p "$app/Contents/MacOS" "$app/Contents/Resources" "$payload" "$resources" "$pkgs"

  swiftc -O -parse-as-library -framework AppKit \
    -o "$app/Contents/MacOS/mdml" \
    "$packaging/macos/Launcher.swift"

  cp "$publish/mdml" "$app/Contents/MacOS/mdml-cli"
  chmod +x "$app/Contents/MacOS/mdml" "$app/Contents/MacOS/mdml-cli"
  xattr -cr "$app" 2>/dev/null || true
  codesign --force --deep --sign - "$app"

  sed -e "s/@VERSION@/$version/g" "$packaging/macos/Info.plist" > "$app/Contents/Info.plist"
  cp "$packaging/macos/welcome.html" "$resources/welcome.html"

  mkdir -p "$payload/Applications" "$payload/usr/local/bin"
  ditto --norsrc --noextattr --noqtn "$app" "$payload/Applications/mdml.app"
  ln -sf "/Applications/mdml.app/Contents/MacOS/mdml-cli" "$payload/usr/local/bin/mdml"

  local component_plist="$pkgs/component.plist"
  pkgbuild --analyze --root "$payload" "$component_plist"
  if [[ -f "$component_plist" ]]; then
    /usr/libexec/PlistBuddy -c "Set :0:BundleIsRelocatable false" "$component_plist" 2>/dev/null || true
  fi

  pkgbuild \
    --root "$payload" \
    --identifier "dev.mdml.converter" \
    --version "$version" \
    --install-location "/" \
    --component-plist "$component_plist" \
    "$pkgs/mdml-component.pkg"

  sed -e "s/@VERSION@/$version/g" -e "s/@ARCH@/$arch/g" \
    "$packaging/macos/distribution.xml" > "$pkgs/distribution.xml"

  productbuild \
    --distribution "$pkgs/distribution.xml" \
    --package-path "$pkgs" \
    --resources "$resources" \
    "$artifacts/mdml-$version-$rid.pkg"

  echo "Built $artifacts/mdml-$version-$rid.pkg"
}

build_windows() {
  local rid="$1"
  local publish="$artifacts/publish/$rid"
  local stage="$artifacts/stage/$rid"

  rm -rf "$stage"
  mkdir -p "$stage"
  cp "$publish/mdml.exe" "$stage/mdml.exe"
  cp "$packaging/windows/install.ps1" "$stage/install.ps1"
  cp "$packaging/windows/uninstall.ps1" "$stage/uninstall.ps1"

  local zip="$artifacts/mdml-$version-$rid.zip"
  rm -f "$zip"
  ditto -c -k --norsrc --noextattr --noqtn "$stage" "$zip"
  echo "Built $zip"
}

package_rid() {
  local rid="$1"
  echo "Publishing $rid..."
  publish_rid "$rid"
  case "$rid" in
    osx-*) build_macos "$rid" ;;
    win-*) build_windows "$rid" ;;
    *)
      echo "No installer defined for $rid (publish output is in artifacts/publish/$rid)"
      ;;
  esac
}

targets=("${@:-$(host_rid)}")
if [[ ${#targets[@]} -eq 1 && "${targets[0]}" == "all" ]]; then
  targets=(osx-arm64 osx-x64 win-x64 win-arm64)
fi

mkdir -p "$artifacts"
for rid in "${targets[@]}"; do
  package_rid "$rid"
done

echo
echo "Done. Installers:"
ls -lh "$artifacts"/mdml-"$version"-* 2>/dev/null || true
