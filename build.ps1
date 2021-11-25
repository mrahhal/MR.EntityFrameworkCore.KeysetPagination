function WriteFailed($Text) {
	Write-Host $Text -ForegroundColor Red
}

function ExitIfFailed() {
	if ($LASTEXITCODE -ne 0) {
		WriteFailed "Failed with code $LASTEXITCODE. Exiting..."
		Exit 1
	}
}

function CreateStamp() {
	return ([long]([DateTime]::UtcNow - (New-Object DateTime 2020, 1, 1)).TotalSeconds).ToString().PadLeft(11, '0')
}

if (Test-Path "artifacts") {
	Remove-Item -Recurse artifacts
}

# Resolve src/test projects

$srcProjects = @(Get-ChildItem -Path ./src/*/*.csproj -File)
$testProjects = @(Get-ChildItem -Path ./test/*/*.csproj -File)
$allProjects = $srcProjects + $testProjects

# ---

foreach ($project in $allProjects) {
	dotnet restore $project
}
ExitIfFailed

foreach ($srcProject in $srcProjects) {
	dotnet build $srcProject --no-restore -c Release
}
ExitIfFailed

foreach ($testProject in $testProjects) {
	dotnet test $testProject --no-restore
}
ExitIfFailed

$versionSuffixArg = "";

if (Test-Path env:GITHUB_ACTIONS) {
	$versionSuffixArg = "--version-suffix dev-$(CreateStamp)"
}

foreach ($srcProject in $srcProjects) {
	Invoke-Expression "dotnet pack $srcProject --no-restore -c Release -o artifacts/packages $versionSuffixArg"
}
