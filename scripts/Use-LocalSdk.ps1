$repositoryPath = Split-Path $PSScriptRoot
$workspaceToolsPath = Join-Path (Split-Path $repositoryPath) '.tools'
$localSdkPath = Join-Path $workspaceToolsPath 'dotnet'
if (Test-Path -LiteralPath (Join-Path $localSdkPath 'dotnet.exe')) {
    $env:DOTNET_ROOT = $localSdkPath
    $env:PATH = $localSdkPath + ';' + $env:PATH
    $env:DOTNET_CLI_HOME = Join-Path $workspaceToolsPath 'cli-home'
    $env:NUGET_PACKAGES = Join-Path $workspaceToolsPath 'nuget-packages'
}
$env:DOTNET_CLI_TELEMETRY_OPTOUT = '1'
