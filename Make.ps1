[CmdletBinding(PositionalBinding=$false)]

param (
    [string]$command = 'Help'
)

$configuration = 'Release'
$projectName = 'Daemon'
$basePath = Split-Path $MyInvocation.MyCommand.Path -Parent
$srcPath = Join-Path $basePath 'src'
$projectPath = Join-Path $srcPath $projectName
$slnFile = "Demo.sln"
$projectFile = Join-Path $projectPath "$projectName.csproj"
$publishProjectPath = "bin/$configuration/publish"
$publishPath = Join-Path $projectPath $publishProjectPath
$outputPath = "./bin/$configuration/publish"

function Help {
    Write-Output "Usage: -command (Help|Nuke|Build|Test|Pack)"
}

function Nuke {
    $Dirs = Get-Childitem $basePath -recurse | Where-Object {$_.PSIsContainer -and ($_.name -eq "bin" -or $_.name -eq "obj") } | Select-Object -expandproperty fullname
    Write-Output $Dirs
    foreach ($dir in $Dirs)    
    {
        Write-Output $dir
        Remove-Item $dir -Force  -Recurse -ErrorAction SilentlyContinue
    }
}

function Build {
    FailFast { dotnet restore $slnFile}
    FailFast { dotnet publish $projectFile -c $configuration -f net5.0 -r win-x64 -o $publishPath --nologo }
}

function Test {
    FailFast { dotnet test -c Debug --nologo  --logger "console;verbosity=detailed" $slnFile }
}

function Pack {
    $cmd="docker run --rm -v ${env:BASE_PATH}:/work -w /work octopusdeploy/octo pack --overwrite --id $projectName --version $env:EXTENDED_VERSION --basePath $publishPath --outFolder $outputPath"
    echo $cmd
    FailFast { Invoke-Expression $cmd }
}

function FailFast($function) {
    Try
    {
        & $function
        if ($LASTEXITCODE -ne 0) {
            Write-Error "ERROR. ExitCode '$LASTEXITCODE'"
            exit $LASTEXITCODE
        }
    }
    Catch
    {
        Write-Error $_.Exception.Message
        exit -1
    }
}

switch ($command) {
    'Nuke' { Nuke }
    'Build' { Build }
    'Test' { Test }
    'Pack' { Pack }
    default { Help }
}