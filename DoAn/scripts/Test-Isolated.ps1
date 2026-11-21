$ErrorActionPreference = "Stop"
$projectRoot = Split-Path -Parent $PSScriptRoot
$testRoot = Join-Path ([System.IO.Path]::GetTempPath()) ("falcon-qa-" + [guid]::NewGuid().ToString("N"))
New-Item -ItemType Directory -Path $testRoot | Out-Null
New-Item -ItemType Directory -Path (Join-Path $testRoot "rendered") | Out-Null
$ref = [System.Security.SecurityElement]::Escape((Join-Path $projectRoot "FALCON_Website.csproj"))
@"
<Project Sdk="Microsoft.NET.Sdk"><PropertyGroup><OutputType>Exe</OutputType><TargetFramework>net8.0</TargetFramework><ImplicitUsings>enable</ImplicitUsings><Nullable>enable</Nullable></PropertyGroup><ItemGroup><ProjectReference Include="$ref"/><PackageReference Include="Microsoft.AspNetCore.Mvc.Testing" Version="8.0.22"/><PackageReference Include="Microsoft.EntityFrameworkCore.Sqlite" Version="8.0.22"/></ItemGroup></Project>
"@ | Set-Content (Join-Path $testRoot "qa.csproj")
"Microsoft Visual Studio Solution File, Format Version 12.00" | Set-Content (Join-Path $testRoot "FALCON.sln")
Copy-Item (Join-Path $projectRoot "docs/verification/QA_SOURCE.cs.txt") (Join-Path $testRoot "Program.cs")
$oldRoot = $env:FALCON_PROJECT_ROOT
$env:FALCON_PROJECT_ROOT = $projectRoot
Push-Location $testRoot
try {
    dotnet run --project qa.csproj -c Release
    if ($LASTEXITCODE -ne 0) { throw "Isolated QA failed. Inspect $testRoot" }
    Write-Host "Isolated QA completed. Artifacts: $testRoot. This does not validate SQL Server."
} finally {
    Pop-Location
    $env:FALCON_PROJECT_ROOT = $oldRoot
}
