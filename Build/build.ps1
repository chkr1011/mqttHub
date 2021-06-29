param([string]$assemblyVersion)

if ([string]::IsNullOrEmpty($assemblyVersion)) {$assemblyVersion = "0.0.1"}

$vswhere = ${Env:\ProgramFiles(x86)} + '\Microsoft Visual Studio\Installer\vswhere'
$msbuild = &$vswhere -products * -requires Microsoft.Component.MSBuild -latest -find MSBuild\**\Bin\MSBuild.exe

Write-Host
Write-Host "Assembly version = $assemblyVersion"
Write-Host "MSBuild path     = $msbuild"
Write-Host

New-Item -Path ((Convert-Path .) + "\..\") -Name "Publish" -ItemType "directory" -Force

# Build MQTTnet.Server Portable
&dotnet publish ..\Source\MQTTnet.Server.csproj --configuration Release /p:FileVersion=$assemblyVersion /p:Version=$assemblyVersion --framework net5.0

$source = (Convert-Path .) + "\..\Source\bin\Release\net5.0\publish"
$destination = (Convert-Path .) + "\..\Publish\MQTTnet.Server-Portable-v$assemblyVersion.zip"
If(Test-path $destination) {Remove-item $destination}
 Add-Type -assembly "system.io.compression.filesystem"
[io.compression.zipfile]::CreateFromDirectory($source, $destination) 

####################################################################

# Build MQTTnet.Server Linux-x64
&dotnet publish ..\Source\MQTTnet.Server.csproj --configuration Release /p:FileVersion=$assemblyVersion /p:Version=$assemblyVersion --self-contained --runtime linux-x64 --framework net5.0

$source = (Convert-Path .) + "\..\Source\bin\Release\net5.0\linux-x64\publish"
$destination = (Convert-Path .) + "\..\Publish\MQTTnet.Server-Linux-x64-v$assemblyVersion.zip"
If(Test-path $destination) {Remove-item $destination}
 Add-Type -assembly "system.io.compression.filesystem"
[io.compression.zipfile]::CreateFromDirectory($source, $destination) 

####################################################################

# Build MQTTnet.Server Linux-ARM
&dotnet publish ..\Source\MQTTnet.Server.csproj --configuration Release /p:FileVersion=$assemblyVersion /p:Version=$assemblyVersion --self-contained --runtime linux-arm --framework net5.0

$source = (Convert-Path .) + "\..\Source\bin\Release\net5.0\linux-ARM\publish"
$destination = (Convert-Path .) + "\..\Publish\MQTTnet.Server-Linux-ARM-v$assemblyVersion.zip"
If(Test-path $destination) {Remove-item $destination}
 Add-Type -assembly "system.io.compression.filesystem"
[io.compression.zipfile]::CreateFromDirectory($source, $destination) 

####################################################################

# Build MQTTnet.Server Windows-x64
&dotnet publish ..\Source\MQTTnet.Server.csproj --configuration Release /p:FileVersion=$assemblyVersion /p:Version=$assemblyVersion --self-contained --runtime win-x64 --framework net5.0

$source = (Convert-Path .) + "\..\Source\bin\Release\net5.0\win-x64\publish"
$destination = (Convert-Path .) + "\..\Publish\MQTTnet.Server-Windows-x64-v$assemblyVersion.zip"
If(Test-path $destination) {Remove-item $destination}
 Add-Type -assembly "system.io.compression.filesystem"
[io.compression.zipfile]::CreateFromDirectory($source, $destination) 
