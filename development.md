# Development

## Release

Wait for GitHub CI to run and test the build.

If CI succeeds (also double check by running the tests locally):
- Optionally update the version in "version.props" and commit
- Tag the current commit for the version: `git tag -m [version] version`
- Invoke "build.ps1"
- Invoke "nuget-push.ps1"
