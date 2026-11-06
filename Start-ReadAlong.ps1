param([switch]$NoBuild)
$ErrorActionPreference = 'Stop'
$checkout = [IO.Path]::GetFullPath($PSScriptRoot)
# Never restart the QA app while one of its alignment workers is still running.
$runningAlignment = Get-CimInstance Win32_Process -Filter "Name='node.exe'" | Where-Object {
    $_.CommandLine -and $_.CommandLine.Replace('/','\').IndexOf((Join-Path $checkout '.research\qa-app\read-along\align.mjs'), [StringComparison]::OrdinalIgnoreCase) -ge 0
}
if ($runningAlignment) { throw 'Read Along preparation is still running. Wait for it to finish before updating or restarting this app.' }
$output = Join-Path $checkout 'Audibly.App\bin\x64\Debug\net8.0-windows10.0.19041.0\win-x64'
$developmentRoot = [IO.Path]::GetFullPath((Join-Path $checkout '.research\qa-app'))
if (-not $developmentRoot.StartsWith($checkout.TrimEnd('\') + '\', [StringComparison]::OrdinalIgnoreCase)) {
    throw 'Unexpected development output path.'
}
if (-not $NoBuild) {
    Push-Location $checkout
    try {
        dotnet build Audibly.App/Audibly.App.csproj -p:Platform=x64 -p:RuntimeIdentifier=win-x64
        if ($LASTEXITCODE -ne 0) { throw 'Build failed. The registered development app was not changed.' }
    } finally { Pop-Location }
}
$manifestPath = Join-Path $output 'AppxManifest.xml'
if (-not (Test-Path -LiteralPath $manifestPath)) { throw 'Build the application before using -NoBuild.' }
foreach ($component in @('align.mjs','convert.mjs','prepare-epub.mjs','node_modules\node\bin\node.exe','node_modules\ffmpeg-static\ffmpeg.exe','node_modules\@storyteller-platform\align\package.json')) {
    if (-not (Test-Path -LiteralPath (Join-Path $output ('read-along\' + $component)))) {
        throw 'The build is missing bundled Read Along components. Run npm ci --prefix tools/read-along, then rebuild.'
    }
}
$packageName = 'Audibly.ReadAlong.Development'
$registered = Get-AppxPackage -Name $packageName
if ($registered -and -not [string]::Equals([IO.Path]::GetFullPath($registered.InstallLocation), $developmentRoot, [StringComparison]::OrdinalIgnoreCase)) {
    throw "The development identity is registered elsewhere: $($registered.InstallLocation). No package was changed."
}
# Close only this checkout's QA process, allowing its existing close handler to save progress.
$developmentExe = Join-Path $developmentRoot 'Audibly.App.exe'
foreach ($process in @(Get-Process Audibly.App -ErrorAction SilentlyContinue)) {
    if ([string]::Equals($process.Path, $developmentExe, [StringComparison]::OrdinalIgnoreCase)) {
        if (-not $process.CloseMainWindow() -or -not $process.WaitForExit(15000)) {
            throw 'Close Audibly Read Along QA manually, then run this launcher again. It was not force-stopped.'
        }
    }
}
New-Item -ItemType Directory -Force $developmentRoot | Out-Null
Copy-Item -Path (Join-Path $output '*') -Destination $developmentRoot -Recurse -Force
$developmentManifest = Join-Path $developmentRoot 'AppxManifest.xml'
[xml]$manifest = Get-Content -LiteralPath $developmentManifest
$manifest.Package.Identity.Name = $packageName
# Windows requires a higher version when the development payload changes.
# Updating this identity preserves its existing LocalState/Settings data.
if ($registered -and [version]$manifest.Package.Identity.Version -le [version]$registered.Version) {
    $version = [version]$registered.Version
    if ($version.Revision -ge 65535) { throw 'Development revision exhausted; update the source package version.' }
    $manifest.Package.Identity.Version = '{0}.{1}.{2}.{3}' -f $version.Major,$version.Minor,$version.Build,($version.Revision + 1)
}
$manifest.Package.Properties.DisplayName = 'Audibly Read Along QA'
$application = $manifest.Package.Applications.Application
$application.VisualElements.DisplayName = 'Audibly Read Along QA'
# QA should never become a handler for the user's audiobook file associations.
if ($application.Extensions) { [void]$application.RemoveChild($application.Extensions) }
if ($manifest.Package.Identity.Name -ne $packageName) { throw 'Refusing to register the production identity.' }
$manifest.Save($developmentManifest)
Add-AppxPackage -Register $developmentManifest
$registered = Get-AppxPackage -Name $packageName
if (-not $registered) { throw 'Development package registration failed.' }
Start-Process explorer.exe -ArgumentList ('shell:AppsFolder\' + $registered.PackageFamilyName + '!App') -WindowStyle Hidden
Write-Output 'Launched Audibly Read Along QA with its own library and settings.'
Write-Output 'Read Along is the book icon beside volume and playback speed.'
