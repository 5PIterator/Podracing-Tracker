@echo off
setlocal
set "ROOT=%~dp0"
set "CLIEXE=%ROOT%tools\PodracingTracker.Cli\bin\Release\net8.0\PodracingTracker.exe"
set "CLIPROJ=%ROOT%tools\PodracingTracker.Cli\PodracingTracker.Cli.csproj"
if exist "%CLIEXE%" (
  "%CLIEXE%" %*
) else (
  dotnet run --project "%CLIPROJ%" -c Release -- %*
)
