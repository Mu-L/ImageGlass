#Requires -Version 7.0
<#
.SYNOPSIS
    Build (and optionally sign) an MSIX package of ImageGlass.Win32 for x64 or arm64.

.DESCRIPTION
    Produces two flavours of MSIX from the same source, selected by the -Sign switch:

      * MSSTORE   (default)  -> for the Microsoft Store.
            The Store re-signs the package itself, so it is built UNSIGNED and
            carries the Store-reserved Identity (Name + Publisher) and the
            Store-supplied artwork (appxmanifest/Assets-msstore).
            Output: artifacts/dist/ImageGlass_<label>_win-<arch>-msstore.msix

      * SIGNED    (-Sign)    -> for direct download / GitHub Releases (sideload).
            Every payload .exe / .dll is Authenticode-signed, then the whole .msix
            is signed. The package Identity/Publisher is set to the EXACT Subject of
            the signing certificate (a hard MSIX requirement), a plain Identity Name
            is used, and the artwork is rendered from the app logo
            (appxmanifest/Assets-signed).
            If NO signing certificate is found, the package is still built (same
            identity/artwork) but left UNSIGNED — sign it later before publishing.
            Output: artifacts/dist/ImageGlass_<label>_win-<arch>.msix

    Both flavours share the same package version: Major.Minor (from
    <IgBundleShortVersion>) . <IgBundleBuild> . 0 — e.g. 10.0.535.0. The 4th
    (revision) part is 0 because the Microsoft Store reserves it.

    The script publishes a fresh self-contained AOT build first (so the package
    always matches the current source and the version baked into the binary),
    stages the payload under an "ImageGlass\" subfolder, generates AppxManifest.xml
    from the template, packs with makeappx, and (when -Sign) signs with signtool.
    makeappx.exe / signtool.exe are auto-located in the latest Windows 10/11 SDK.

.PARAMETER Platform
    Target architecture: x64 (default) or arm64.

.PARAMETER Sign
    Build the signed (sideload / GitHub) flavour. The package is signed when a
    certificate is available; if none is found it is built UNSIGNED (a warning is
    printed). Omit for the msstore build.

.PARAMETER CertSubject
    Substring of the code-signing certificate Subject to select it from the
    Current User / Local Machine "My" store (passed to signtool /n). Ignored when
    -CertFile is supplied. Default: "Duong Dieu Phap".

.PARAMETER CertFile
    Path to a PFX certificate to sign with instead of a store certificate.

.PARAMETER CertPassword
    Password for -CertFile (if any).

.PARAMETER TimestampUrl
    RFC-3161 timestamp server. Default: http://timestamp.sectigo.com

.PARAMETER PackageVersion
    Override the 4-part package version. Defaults to
    <Major>.<Minor>.<IgBundleBuild>.0 derived from Directory.Build.props.

.PARAMETER SkipPublish
    Reuse the existing artifacts/publish/win-<arch> output instead of re-publishing
    (faster iteration; the package may not reflect uncommitted source changes).

.EXAMPLE
    pwsh _assets/win/script-pack-win-msix.ps1 -Platform x64
    # Unsigned x64 package for the Microsoft Store (msstore).

.EXAMPLE
    pwsh _assets/win/script-pack-win-msix.ps1 -Platform arm64 -Sign
    # Signed arm64 package for GitHub Releases (cert selected by Subject).

.EXAMPLE
    pwsh _assets/win/script-pack-win-msix.ps1 -Platform x64 -Sign -CertFile C:\ig.pfx -CertPassword hunter2
#>

[CmdletBinding()]
param(
    [ValidateSet('x64', 'arm64')]
    [string]$Platform = 'x64',

    [switch]$Sign,

    [string]$CertSubject = 'Duong Dieu Phap',
    [string]$CertFile = '',
    [string]$CertPassword = '',
    [string]$TimestampUrl = 'http://timestamp.sectigo.com',

    [string]$PackageVersion = '',

    # msstore identity (unsigned build) — reserved name assigned by Partner Center.
    [string]$MsStoreIdentityName = '9662DuongDieuPhap.ImageGlass',
    [string]$MsStorePublisher = 'CN=29F1B9EC-D220-4DC3-BEDB-01A9CCA51904',

    # Sideload identity (signed build) — Publisher is overwritten with the cert Subject.
    [string]$SideloadIdentityName = 'DuongDieuPhap.ImageGlass',
    [string]$PublisherDisplayName = 'Duong Dieu Phap',

    [switch]$SkipPublish
)

$ErrorActionPreference = 'Stop'
Set-StrictMode -Version Latest

# --- Paths ---------------------------------------------------------------------
$WorkspaceDir = (Resolve-Path (Join-Path $PSScriptRoot '..\..')).Path
$ProjectFile  = Join-Path $WorkspaceDir 'ImageGlass.Win32\ImageGlass.Win32.csproj'
$BuildProps   = Join-Path $WorkspaceDir 'Directory.Build.props'
$ManifestTpl  = Join-Path $PSScriptRoot 'appxmanifest\AppxManifest.xml'
# Signed build uses logos rendered from the app logo; msstore uses the
# Store-supplied artwork. (Regenerate the signed set with script-generate-msix-assets.ps1.)
$AssetsDir    = Join-Path $PSScriptRoot ($Sign ? 'appxmanifest\Assets-signed' : 'appxmanifest\Assets-msstore')
$AppExtras    = Join-Path $WorkspaceDir '_assets\_app'

$Rid          = "win-$Platform"
$MsbuildPlat  = if ($Platform -eq 'x64') { 'x64' } else { 'ARM64' }
$PublishDir   = Join-Path $WorkspaceDir "artifacts\publish\$Rid"
$StagingDir   = Join-Path $WorkspaceDir "artifacts\bundle\$Rid-msix"
$PayloadDir   = Join-Path $StagingDir 'ImageGlass'
$DistDir      = Join-Path $WorkspaceDir 'artifacts\dist'

# --- Helpers -------------------------------------------------------------------

# Locate a Windows SDK tool (makeappx.exe / signtool.exe), preferring the newest
# SDK and an x64 host build, falling back to whatever is already on PATH.
function Find-SdkTool([string]$Name) {
    $roots = @(
        "${env:ProgramFiles(x86)}\Windows Kits\10\bin",
        "${env:ProgramFiles}\Windows Kits\10\bin"
    ) | Where-Object { $_ -and (Test-Path $_) }

    foreach ($root in $roots) {
        $hit = Get-ChildItem -Path $root -Directory -ErrorAction SilentlyContinue |
            Where-Object { $_.Name -match '^10\.' } |
            Sort-Object { [version]$_.Name } -Descending |
            ForEach-Object { Join-Path $_.FullName "x64\$Name" } |
            Where-Object { Test-Path $_ } |
            Select-Object -First 1
        if ($hit) { return $hit }
    }

    $onPath = Get-Command $Name -ErrorAction SilentlyContinue
    if ($onPath) { return $onPath.Source }

    throw "Could not find $Name. Install the Windows 10/11 SDK (includes makeappx & signtool)."
}

# Read a single <Tag>value</Tag> from Directory.Build.props.
function Get-BuildProp([string]$Tag) {
    $m = Select-String -Path $BuildProps -Pattern "<$Tag>(.*?)</$Tag>" | Select-Object -First 1
    if ($m) { return $m.Matches[0].Groups[1].Value.Trim() }
    return ''
}

# Find a usable signing certificate and report its EXACT Subject DN (needed for
# the manifest Publisher, which must match the signature byte-for-byte) and which
# store it lives in (so signtool searches the same one). Returns a hashtable
# @{ Subject; Machine } or $null when none is found — the caller then builds an
# UNSIGNED package rather than failing.
$script:UseMachineStore = $false
function Resolve-SigningCert {
    if ($CertFile) {
        if (-not (Test-Path $CertFile)) {
            Write-Warning "Certificate file not found: $CertFile"
            return $null
        }
        try {
            $cert = [System.Security.Cryptography.X509Certificates.X509Certificate2]::new($CertFile, $CertPassword)
            return @{ Subject = $cert.Subject; Machine = $false }
        }
        catch {
            Write-Warning "Could not load certificate '$CertFile': $($_.Exception.Message)"
            return $null
        }
    }
    foreach ($store in @(
            @{ Path = 'Cert:\CurrentUser\My';  Machine = $false },
            @{ Path = 'Cert:\LocalMachine\My'; Machine = $true })) {
        $cert = Get-ChildItem $store.Path -CodeSigningCert -ErrorAction SilentlyContinue |
            Where-Object { $_.Subject -like "*$CertSubject*" -and $_.HasPrivateKey } |
            Select-Object -First 1
        if ($cert) { return @{ Subject = $cert.Subject; Machine = $store.Machine } }
    }
    return $null
}

# Sign one or more files with signtool (Authenticode, SHA-256, timestamped).
# Returns $true on success, $false on failure (the caller decides whether to
# continue UNSIGNED) — never throws.
function Invoke-SignTool([string]$SignTool, [string[]]$Files) {
    if ($Files.Count -eq 0) { return $true }
    $common = @('sign', '/fd', 'SHA256', '/tr', $TimestampUrl, '/td', 'SHA256')
    if ($CertFile) {
        $common += @('/f', $CertFile)
        if ($CertPassword) { $common += @('/p', $CertPassword) }
    }
    else {
        $common += @('/n', $CertSubject, '/a')
        # signtool /n defaults to the CurrentUser store; switch to the machine
        # store when that is where the certificate was found.
        if ($script:UseMachineStore) { $common += '/sm' }
    }
    & $SignTool @common @Files
    return ($LASTEXITCODE -eq 0)
}

# --- Version -------------------------------------------------------------------
$igVersion = Get-BuildProp 'IgVersion'
if (-not $igVersion) { throw "Could not read <IgVersion> from $BuildProps" }
$igReleaseType = Get-BuildProp 'IgReleaseType'

$relLabel = if ($igReleaseType) { "$igVersion-$igReleaseType" } else { $igVersion }

# Package version = Major.Minor (from IgBundleShortVersion) . IgBundleBuild . 0
# e.g. short=10.0.2 + build=535 -> 10.0.535.0. The 4th (revision) part is 0
# because the Microsoft Store reserves it. The build number lives in the 3rd part
# so it is preserved in both the signed and msstore packages.
if ($PackageVersion) {
    $pkgVersion = $PackageVersion
}
else {
    $shortVer = Get-BuildProp 'IgBundleShortVersion'
    if (-not $shortVer) { $shortVer = $igVersion }
    $bundleBuild = Get-BuildProp 'IgBundleBuild'
    if (-not $bundleBuild) { $bundleBuild = '0' }

    $sp    = $shortVer.Split('.')
    $major = $sp[0]
    $minor = if ($sp.Count -gt 1) { $sp[1] } else { '0' }
    $pkgVersion = "$major.$minor.$bundleBuild.0"
}

# --- Identity / publisher per flavour -----------------------------------------
if ($Sign) {
    $identityName = $SideloadIdentityName
    $cert         = Resolve-SigningCert
    if ($cert) {
        $publisher              = $cert.Subject
        $doSign                 = $true
        $script:UseMachineStore = $cert.Machine
    }
    else {
        # No usable certificate — build the GitHub package UNSIGNED. Use a
        # placeholder Publisher; the package must be signed before it can install.
        $publisher = "CN=$PublisherDisplayName"
        $doSign    = $false
        Write-Warning "No signing certificate found — building an UNSIGNED package."
    }
    $outName = "ImageGlass_${relLabel}_$Rid.msix"
}
else {
    $identityName = $MsStoreIdentityName
    $publisher    = $MsStorePublisher
    $doSign       = $false
    $outName      = "ImageGlass_${relLabel}_$Rid-msstore.msix"
}
$outMsix = Join-Path $DistDir $outName

$flavourLabel = if (-not $Sign) { 'MSSTORE (unsigned, Microsoft Store)' }
                elseif ($doSign) { 'SIGNED (sideload / GitHub)' }
                else { 'GitHub (UNSIGNED — no certificate found)' }
Write-Host "==> Packing ImageGlass $igVersion ($Platform) as MSIX"
Write-Host "    Flavour     : $flavourLabel"
Write-Host "    Identity    : $identityName"
Write-Host "    Publisher   : $publisher"
Write-Host "    Version     : $pkgVersion"
Write-Host "    Assets      : $(Split-Path $AssetsDir -Leaf)"
Write-Host "    Output      : $outMsix"

# --- Locate SDK tools ----------------------------------------------------------
$makeappx = Find-SdkTool 'makeappx.exe'
$makepri  = Find-SdkTool 'makepri.exe'
$signtool = if ($doSign) { Find-SdkTool 'signtool.exe' } else { '' }
Write-Host "    makeappx    : $makeappx"
Write-Host "    makepri     : $makepri"
if ($doSign) { Write-Host "    signtool    : $signtool" }

# --- 1. Publish a fresh self-contained AOT build ------------------------------
if ($SkipPublish -and (Test-Path (Join-Path $PublishDir 'ImageGlass.exe'))) {
    Write-Host "==> Reusing existing publish output: $PublishDir"
}
else {
    Write-Host "==> Publishing $Rid (Release, AOT, self-contained)"
    if (Test-Path $PublishDir) { Remove-Item $PublishDir -Recurse -Force }
    & dotnet publish $ProjectFile -c Release -r $Rid -p:Platform=$MsbuildPlat -o $PublishDir
    if ($LASTEXITCODE -ne 0) { throw "dotnet publish failed (exit $LASTEXITCODE)." }
    # Bundle the shared app assets (themes, credits, etc.) — mirrors the publish-win tasks.
    Copy-Item -Path (Join-Path $AppExtras '*') -Destination $PublishDir -Recurse -Force
}
if (-not (Test-Path (Join-Path $PublishDir 'ImageGlass.exe'))) {
    throw "Publish did not produce ImageGlass.exe in $PublishDir"
}

# --- 2. Stage the package layout ----------------------------------------------
#   <staging>\AppxManifest.xml
#   <staging>\Assets\*            (logos)
#   <staging>\ImageGlass\*        (the app payload)
Write-Host "==> Staging package layout"
if (Test-Path $StagingDir) { Remove-Item $StagingDir -Recurse -Force }
New-Item -ItemType Directory -Path $PayloadDir -Force | Out-Null
Copy-Item -Path (Join-Path $PublishDir '*') -Destination $PayloadDir -Recurse -Force

# Drop debug symbols — they bloat the package and are not part of the signed product.
Get-ChildItem -Path $PayloadDir -Recurse -Include '*.pdb' -File -ErrorAction SilentlyContinue |
    Remove-Item -Force

# Copy the logo assets.
Copy-Item -Path $AssetsDir -Destination (Join-Path $StagingDir 'Assets') -Recurse -Force

# --- 3. Generate AppxManifest.xml from the template ---------------------------
Write-Host "==> Generating AppxManifest.xml"
$manifest = Get-Content -Path $ManifestTpl -Raw
$manifest = $manifest.Replace('{{IDENTITY_NAME}}', $identityName).
                      Replace('{{PUBLISHER}}', $publisher).
                      Replace('{{PUBLISHER_DISPLAY_NAME}}', $PublisherDisplayName).
                      Replace('{{VERSION}}', $pkgVersion).
                      Replace('{{ARCH}}', $Platform)
# makeappx requires the manifest with a UTF-8 BOM to match the SDK's expectations.
$utf8Bom = [System.Text.UTF8Encoding]::new($true)
[System.IO.File]::WriteAllText((Join-Path $StagingDir 'AppxManifest.xml'), $manifest, $utf8Bom)

# --- 4. Sign payload binaries (signed flavour only) ----------------------------
# Each .exe/.dll is Authenticated individually so installed files carry a trust
# chain, then the package signature is applied in step 7.
if ($doSign) {
    Write-Host "==> Signing payload binaries (.exe / .dll)"
    $binaries = Get-ChildItem -Path $PayloadDir -Recurse -Include '*.exe', '*.dll' -File |
        Select-Object -ExpandProperty FullName
    Write-Host "    $($binaries.Count) file(s) to sign"
    if (-not (Invoke-SignTool -SignTool $signtool -Files $binaries)) {
        Write-Warning "Could not sign the payload binaries — building an UNSIGNED package."
        $doSign = $false
    }
}

# --- 5. Build the resource index (resources.pri) ------------------------------
# The logos are scale-qualified (StoreLogo.scale-100.png, ...) but the manifest
# references the unqualified logical names (Assets\StoreLogo.png). makeappx in
# directory mode only resolves those through a resource index, so build one with
# makepri; it also lets Windows pick the right tile size per display DPI.
Write-Host "==> Building resource index (resources.pri)"
$priConfig   = Join-Path (Split-Path $StagingDir) "$Rid-msix.priconfig.xml"
$manifestOut = Join-Path $StagingDir 'AppxManifest.xml'
$priOut      = Join-Path $StagingDir 'resources.pri'
if (Test-Path $priConfig) { Remove-Item $priConfig -Force }
& $makepri createconfig /cf $priConfig /dq en-US /o
if ($LASTEXITCODE -ne 0) { throw "makepri createconfig failed (exit $LASTEXITCODE)." }
& $makepri new /pr $StagingDir /cf $priConfig /mn $manifestOut /of $priOut /o
if ($LASTEXITCODE -ne 0) { throw "makepri new failed (exit $LASTEXITCODE)." }

# --- 6. Pack the MSIX ----------------------------------------------------------
Write-Host "==> Packing MSIX"
New-Item -ItemType Directory -Path $DistDir -Force | Out-Null
if (Test-Path $outMsix) { Remove-Item $outMsix -Force }
& $makeappx pack /o /d $StagingDir /p $outMsix
if ($LASTEXITCODE -ne 0) { throw "makeappx failed (exit $LASTEXITCODE)." }

# --- 7. Sign the package (only when a certificate was found) -------------------
if ($doSign) {
    Write-Host "==> Signing MSIX package"
    if (Invoke-SignTool -SignTool $signtool -Files @($outMsix)) {
        Write-Host "==> Verifying package signature"
        & $signtool verify /pa $outMsix
        if ($LASTEXITCODE -ne 0) { throw "signtool verify failed (exit $LASTEXITCODE)." }
    }
    else {
        Write-Warning "Could not sign the package — it has been left UNSIGNED."
        $doSign = $false
    }
}

# --- Done ----------------------------------------------------------------------
Write-Host ''
Write-Host 'Done.'
Write-Host "  Package : $outMsix"
if ($doSign) {
    Write-Host '  Signed  : yes (payload binaries + package)'
    Write-Host '  Next    : upload to the GitHub release for this version.'
}
elseif ($Sign) {
    Write-Host '  Signed  : no (no signing certificate was found)'
    Write-Host '  Next    : sign the package before publishing it to the GitHub release.'
}
else {
    Write-Host '  Signed  : no (the Microsoft Store signs it on submission)'
    Write-Host '  Next    : upload to Partner Center (Microsoft Store) as-is.'
    Write-Host '            Do NOT sign this msstore package yourself.'
}
