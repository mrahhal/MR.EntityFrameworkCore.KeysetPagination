function ExitIfFailed() {
	if ($LASTEXITCODE -ne 0) {
		WriteFailed "Failed with code $LASTEXITCODE. Exiting..."
		Exit 1
	}
}

dotnet test
ExitIfFailed

dotnet pack -c Release -o artifacts/packages
