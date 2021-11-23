# Development

## Releasing

We use https://github.com/mrahhal/release-cycle as a reference when releasing.

Wait for GitHub CI to run and test the build. Also run the tests locally.

If everything succeeds:
- Optionally update the version in "version.props" and commit
- Tag the current commit for the version: `git tag -m [version] version`
- Invoke "build.ps1"
- Invoke "nuget-push.ps1"
- Update the patch version in "version.props" and commit
