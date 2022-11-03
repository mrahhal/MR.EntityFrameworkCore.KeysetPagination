# Diagnostics

This library ships with analyzers to help you with using properly. The analyzers can detect and report the following diagnostics.

## KeysetPagination1000

Keyset contains a nullable property.

Nullable properties are not supported in the keyset.

Check the caveat document about [NULLs](./caveats.md#null) for more info about the problem and a workaround using computed columns.
