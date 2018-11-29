param($projectPath)

$csprojPath = Join-Path $PSScriptRoot $projectPath
[xml]$project = Get-Content -Path $csprojPath
$project.Project.PropertyGroup[0].DebugType = "full"
$project.Save($csprojPath)