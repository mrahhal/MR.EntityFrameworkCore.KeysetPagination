# Development

## Releasing

We use https://github.com/mrahhal/release-cycle as a reference when releasing.

Wait for GitHub CI to run and test the build. Also run the tests locally.

If everything succeeds:
- Invoke "build.ps1"
- Invoke "nuget-push.ps1"
