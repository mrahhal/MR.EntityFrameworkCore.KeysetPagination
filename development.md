# Development

## Release

Wait for GitHub CI to run and test the build. Also run the tests locally.

If everything succeeds:
- Optionally update the version in "version.props" and commit
- Tag the current commit for the version: `git tag -m [version] version`
- Invoke "build.ps1"
- Invoke "nuget-push.ps1"
- Update the patch version in "version.props" and commit