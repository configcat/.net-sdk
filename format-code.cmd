@ECHO OFF
REM Run this script to format code manually.

dotnet format ConfigCatSdk.slnx --severity warn --report .
