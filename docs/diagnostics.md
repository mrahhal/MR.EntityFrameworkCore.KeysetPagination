# Diagnostics

This library ships with analyzers that help with detecting misuse and offering suggestions. The analyzers can detect and report the following diagnostics.

## KeysetPagination1000

Keyset contains a nullable column.

Nullable columns are not supported in the keyset.

Check the caveat document about [NULLs](./caveats.md#null) for more info about the problem and a workaround using computed columns or keyset coalescing.
