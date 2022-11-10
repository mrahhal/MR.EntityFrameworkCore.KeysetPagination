[CmdletBinding()]
param (
	[ValidateSet('info', 'build', 'test', 'pack', 'tag-version')]
	[string]$Script = 'info',
	[switch]$NoBuildInfo = $false
)

function WriteInfo([string] $text) {
	Write-Host $text -ForegroundColor Blue
}

function WriteError([string] $text) {
	Write-Host $text -ForegroundColor Red
}

function WriteHeader([string] $text) {
	WriteInfo '#------------'
	WriteInfo "# $text"
	WriteInfo '#------------'
}

function ExitIfFailed() {
	if ($LASTEXITCODE -ne 0) {
		WriteError "Failed with code $LASTEXITCODE. Exiting..."
		Exit 1
	}
}

function CreateStamp() {
	return Get-Date -UFormat '+%Y%m%dT%H%M%S'
}

function CleanArtifacts() {
	if (Test-Path 'artifacts') {
		Remove-Item -Recurse 'artifacts'
	}
}

function CompoundVersionSuffix([string[]] $versionSuffixes) {
	return (@($versionSuffixes) | Where-Object { $_ }) -join '.'
}

function LoadBuildInfo() {
	# Resolve src/test projects

	$srcProjects = @(Get-ChildItem -Path './src/*/*.csproj' -File)
	$testProjects = @(Get-ChildItem -Path './test/*/*.csproj' -File)
	$allProjects = $srcProjects + $testProjects

	# git info

	$sha = git rev-parse HEAD
	$hasUncommittedChanges = !([string]::IsNullOrEmpty($(git status --porcelain)))
	$tags = [string[]]@(git tag --points-at HEAD) | Where-Object { $_ -Match 'v[0-9]+\.[0-9]+\.[0-9]+.*' }
	$tagged = !!$tags.Length

	# ci info

	$ci = Test-Path env:GITHUB_ACTIONS
	$addCIPrerelease = !$tagged

	# Load version info

	$versionPrefix = Select-Xml -Path 'version.props' -XPath '/Project/PropertyGroup/VersionPrefix' | ForEach-Object { $_.Node.InnerXML }
	$versionSuffix = Select-Xml -Path 'version.props' -XPath '/Project/PropertyGroup/VersionSuffix' | ForEach-Object { $_.Node.InnerXML }
	$version = (@($versionPrefix, $versionSuffix) | Where-Object { $_ }) -join '-'

	# ---

	$stamp = CreateStamp

	return [PSCustomObject]@{
		SrcProjects           = $srcProjects
		TestProjects          = $testProjects
		AllProjects           = $allProjects

		Sha                   = $sha
		HasUncommittedChanges = $hasUncommittedChanges
		Tags                  = $tags
		Tagged                = $tagged

		CI                    = $ci
		AddCIPrerelease       = $addCIPrerelease
		Stamp                 = $stamp

		VersionPrefix         = $versionPrefix
		VersionSuffix         = $versionSuffix
		Version               = $version
	}
}

function PathToName($path) {
	return Split-Path $path -leaf
}

function PrintBuildInfo() {
	$buildInfo |
	Select-Object @{name = 'SrcProjects'; expression = { PathToName $_.SrcProjects } }, @{name = 'TestProjects'; expression = { PathToName $_.TestProjects } }, Sha, Tags, Tagged, CI, AddCIPrerelease, Stamp, VersionPrefix, VersionSuffix, Version |
	Format-List
}

function TagVersion() {
	if ($buildInfo.HasUncommittedChanges) {
		WriteError "You have uncommitted changes. Tagging failed."
		Exit 1
	}

	$version = (@($buildInfo.VersionPrefix, $buildInfo.VersionSuffix) | Where-Object { $_ }) -join '-'
	$tag = "v$version"

	WriteInfo "Tagging: $tag"

	# Create an annotated tag.
	git tag -m $tag $tag
}

function BuildProjects() {
	WriteHeader 'Building'

	foreach ($srcProject in $buildInfo.SrcProjects) {
		dotnet build $srcProject -c Release
	}
	ExitIfFailed
}

function TestProjects() {
	WriteHeader 'Testing'

	$testLoggersArg = ''
	if ($ci) {
		$testLoggersArg = '--logger "GitHubActions;report-warnings=false"'
	}

	foreach ($testProject in $buildInfo.TestProjects) {
		Invoke-Expression "dotnet test $testProject -c Debug $testLoggersArg"
	}
	ExitIfFailed
}

function PackProjects {
	WriteHeader 'Packing'

	CleanArtifacts

	$versionSuffixArg = ''
	if ($buildInfo.AddCIPrerelease) {
		$versionSuffix = CompoundVersionSuffix ($buildInfo.VersionSuffix, "ci.$($buildInfo.Stamp)+sha.$($buildInfo.Sha)")
		$versionSuffixArg = "--version-suffix $versionSuffix"
	}

	foreach ($srcProject in $buildInfo.SrcProjects) {
		Invoke-Expression "dotnet pack $srcProject -c Release -o artifacts/packages $versionSuffixArg"
	}
	ExitIfFailed
}

# ===

$buildInfo = LoadBuildInfo

if (!$NoBuildInfo) {
	PrintBuildInfo
}

if ($Script -eq 'build') {
	BuildProjects
}

if ($Script -eq 'test') {
	TestProjects
}

if ($Script -eq 'pack') {
	PackProjects
}

if ($Script -eq 'tag-version') {
	TagVersion
}
