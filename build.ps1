#!/usr/bin/env pwsh
if ($IsMacOS -or $IsLinux) {
    'Use build.sh on non-Windows platforms'
    exit 1
}
$PSScriptRoot = Split-Path $MyInvocation.MyCommand.Path -Parent

[string] $DotNetVersion= ''
foreach($line in Get-Content (Join-Path $PSScriptRoot 'build.config'))
{
  if ($line -like 'DOTNET_VERSION=*') {
      $DotNetVersion =$line.SubString(15)
  }
}


if ([string]::IsNullOrEmpty($DotNetVersion)) {
    '.NET Core SDK Version'
    exit 1
}

$TestSubmodulePath = Join-Path $PSScriptRoot "extern/thorncompiler/CMakeLists.txt"
if (!(Test-Path $TestSubmodulePath)) {
    if (Get-Command git -ErrorAction SilentlyContinue) {
        "WARNING: Submodules not present. Attempting to clone."
        git submodule update --init --recursive
        if ($LastExitCode -ne 0) {
            "ERROR: Submodules not present and unable to clone"
            exit 1
        }
    } else {
        "ERROR: Submodules not present and unable to clone"
        exit 1
    }
}

if ($PSVersionTable.PSEdition -ne 'Core') {
    # Attempt to set highest encryption available for SecurityProtocol.
    # PowerShell will not set this by default (until maybe .NET 4.6.x). This
    # will typically produce a message for PowerShell v2 (just an info
    # message though)
    try {
        # Set TLS 1.2 (3072), then TLS 1.1 (768), then TLS 1.0 (192), finally SSL 3.0 (48)
        # Use integers because the enumeration values for TLS 1.2 and TLS 1.1 won't
        # exist in .NET 4.0, even though they are addressable if .NET 4.5+ is
        # installed (.NET 4.5 is an in-place upgrade).
        [System.Net.ServicePointManager]::SecurityProtocol = 3072 -bor 768 -bor 192 -bor 48
      } catch {
        Write-Output 'Unable to set PowerShell to use TLS 1.2 and TLS 1.1 due to old .NET Framework installed. If you see underlying connection closed or trust errors, you may need to upgrade to .NET Framework 4.5+ and PowerShell v3'
      }
}

$env:DOTNET_SKIP_FIRST_TIME_EXPERIENCE=1
$env:DOTNET_CLI_TELEMETRY_OPTOUT=1
$env:DOTNET_ROLL_FORWARD_ON_NO_CANDIDATE_FX=2

# Get .NET Core CLI path if installed.
$SDKResult = "notfound";
if (Get-Command dotnet -ErrorAction SilentlyContinue) {
    $FoundDotNetCliVersion = dotnet --list-sdks;
    if([string]::IsNullOrWhitespace($FoundDotNetCliVersion)) {
        "No .NET SDKS listed. CLI Path: "
        (Get-Command dotnet).Path
        exit 2
    }
    Foreach($str in $FoundDotNetCliVersion.Split("\n")) {
        if($str.StartsWith($DotNetVersion)) {
            $SDKResult = "found";
        }
    }
    if((Get-Command dotnet).Path.StartsWith([Environment]::GetFolderPath("programfilesx86")))
    {
        "x86 dotnet CLI detected on PATH. This is likely a version mismatch. Please uninstall the x86 SDK, or edit your PATH"
        exit 2
    }
}

if($SDKResult -eq "notfound") {
    "Required SDK $DotNetVersion not found, exiting"
    exit 2
}

###########################################################################
# RUN BUILD SCRIPT
###########################################################################
Set-Location -Path $PSScriptRoot
dotnet run --project ./scripts/BuildLL/BuildLL.csproj -p:RestoreUseStaticGraphEvaluation=true -- $args
