# TODO
# MSBuild.exe .\Audibly.sln /t:Rebuild /p:Configuration=Release /p:AppxPackageDir="Packages" /p:UapAppxPackageBuildMode=SideloadOnly /p:AppxBundle= /p:GenerateAppxPackageOnBuild=true

$version = "2.1.9.0"
$targetFramework = "net8.0-windows10.0.19041.0"

$packagesDirectory = ".\Packages"

if (!(Test-Path $packagesDirectory)) {
    New-Item -ItemType Directory -Path $packagesDirectory
}

$msixDirectory = ".\Packages\msix"

if (!(Test-Path $msixDirectory)) {
    New-Item -ItemType Directory -Path $msixDirectory
}

Remove-Item -Path $msixDirectory\* -Recurse -Force

$targetDirectories = @(
    ".\Audibly.App\bin\ARM64\Release\$targetFramework\AppPackages\",
    ".\Audibly.App\bin\x64\Release\$targetFramework\AppPackages\",
    ".\Audibly.App\bin\x86\Release\$targetFramework\AppPackages\"
)

foreach ($targetDirectory in $targetDirectories) {
    $versionDirectory = Get-ChildItem -Path $targetDirectory -Directory | Where-Object { $_.Name -match $version }
    $versionDirectoryPath = $versionDirectory.FullName
    $msixFile = Get-ChildItem -Path $versionDirectoryPath -Filter "*.msix"

    Copy-Item -Path $msixFile.FullName -Destination $msixDirectory
}

# create the msixbundle
$msixBundleDirectory = ".\Packages\msixbundle"

if (!(Test-Path $msixBundleDirectory)) {
    New-Item -ItemType Directory -Path $msixBundleDirectory
}

$msixBundleFilePath = Join-Path -Path $msixBundleDirectory -ChildPath "Audibly_$version.msixbundle"

# delete the msixbundle if it already exists
if (Test-Path $msixBundleFilePath) {
    Remove-Item -Path $msixBundleFilePath -Force
}

MakeAppx bundle /bv $version /d $msixDirectory /p $msixBundleFilePath

# $signedMsixBundleFilePath = Join-Path -Path $msixBundleDirectory -ChildPath "Audibly_Signed_$version.msixbundle"
# 
# # make a copy of the msixbundle to sign
# Copy-Item -Path $msixBundleFilePath -Destination $signedMsixBundleFilePath
# 
# # Path to the .pfx certificate and its password
# $pfxCertificatePath = ".\Audibly.App\Audibly.App_TemporaryKey.pfx"
# $pfxCertificatePassword = "<password>"
# 
# # Sign the msixbundle
# # $signToolPath = "C:\Program Files (x86)\Windows Kits\10\bin\10.0.19041.0\x64\signtool.exe"
# & signtool.exe sign /fd SHA256 /a /f $pfxCertificatePath /p $pfxCertificatePassword /tr http://timestamp.digicert.com /td SHA256 /debug $signedMsixBundleFilePath