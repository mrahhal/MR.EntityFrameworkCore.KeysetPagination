# Observations

"stable" means HasNext/HasPrevious didn't incur a sizable perf hit with respect to the total time of the request.

Example1:
`Id ASC`:
stable 100ms in all cases

Example2: (close to million <--> close to 0)
with index on Created:
`Created DESC`:
  - without access predicate: stable 100ms
  - with access predicate: stable 100ms
`Created DESC, Id ASC`:
  - without access predicate: next: 100/400, prev: 400/700  <-->  next: 400 stable, prev: stable 100ms
  - with access predicate: next: 100/500, prev: 500/900 <--> next: 400 stable, prev: stable 100ms
