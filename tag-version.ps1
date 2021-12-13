$hasUncommittedChanges = !([string]::IsNullOrEmpty($(git status --porcelain)))
if ($hasUncommittedChanges) {
	Write-Host "You have uncommitted changes. Tagging failed." -ForegroundColor Red
	Exit 1
}

# Parse version from version.props file.
$version = Select-Xml -Path 'version.props' -XPath '/Project/PropertyGroup/VersionPrefix' | ForEach-Object { $_.Node.InnerXML }
$versionSuffix = Select-Xml -Path 'version.props' -XPath '/Project/PropertyGroup/VersionSuffix' | ForEach-Object { $_.Node.InnerXML }

$version = (@($version, $versionSuffix) | Where-Object { $_ }) -join '-'

$tag = "v$version"

Write-Host "Tagging: $tag"

# Create an annotated tag.
git tag -m $tag $tag
