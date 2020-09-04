function ExitIfFailed() {
	if ($LASTEXITCODE -ne 0) {
		WriteFailed "Failed with code $LASTEXITCODE. Exiting..."
		Exit 1
	}
}

function CreateStamp() {
	return ([long]([DateTime]::UtcNow - (New-Object DateTime 2020, 1, 1)).TotalSeconds).ToString().PadLeft(11, '0')
}

if (Test-Path artifacts) {
	rm -r artifacts
}

dotnet restore
ExitIfFailed

dotnet build --no-restore -c Release
ExitIfFailed

dotnet test --no-restore
ExitIfFailed

$versionSuffixArg = "";

if (Test-Path env:GITHUB_ACTIONS) {
	$versionSuffixArg = "--version-suffix dev-$(CreateStamp)"
}

Invoke-Expression "dotnet pack --no-restore -c Release -o artifacts/packages $versionSuffixArg"
