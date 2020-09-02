# MR.EntityFrameworkCore.KeysetPagination

<a href="http://use-the-index-luke.com/no-offset">
  <img src="http://use-the-index-luke.com/img/no-offset.q200.png" alt="100% offset-free" target="_blank" align="right" width="106" height="106">
</a>

[![NuGet version](https://badge.fury.io/nu/MR.EntityFrameworkCore.KeysetPagination.svg)](https://www.nuget.org/packages/MR.EntityFrameworkCore.KeysetPagination)
[![License](https://img.shields.io/badge/license-MIT-blue.svg)](https://opensource.org/licenses/MIT)

Keyset pagination for EntityFrameworkCore. Also known as seek pagination.

Learn about why the standard offset based pagination (`Take().Skip()`) is bad [here](http://use-the-index-luke.com/no-offset).

## Usage

`KeysetPaginate` takes a few arguments:
```cs
KeysetPaginate(
   b => b.Ascending(entity => entity.Id), // This configures the columns we want to act on.
   reference, // The reference entity.
   direction, // The direction we want to take (after/before reference).
)
```

If we want to display the "next page" (`KeysetPaginationReferenceDirection.After`), then the `reference` entity will be the last entity of the current page.
If we want to display the "previous page"  (`KeysetPaginationReferenceDirection.Before`), then the `reference` entity will be the first entity of the current page.

`KeysetPaginate` can be called without reference and direction. In which case this is equivalent to only ordering (equivalent to 1st page in offset pagination):
```cs
KeysetPaginate(
   b => b.Ascending(entity => entity.Id),
)
```

`KeysetPaginate` works with composite keyset as well. Just configure all the columns you want:
```cs
KeysetPaginate(
   b => b.Ascending(entity => entity.Id).Ascending(entity => entity.Score),
   ...
)
```

You can also mix ASC/DESC columns. `KeysetPaginate` knows how to handle that (even with a reference/direction specified):
```cs
KeysetPaginate(
   b => b.Ascending(entity => entity.Id).Descending(entity => entity.Score),
   ...
)
```

## Extra sample usages

Basic example usage:
```cs
var users = await dbContext.Users
  .KeysetPaginate(...).Query
  .Take(20)
  .ToListAsync();
```

`KeysetPaginate` returns a context object that includes a `Query` property. This `Query` is what you'll chain more operators to.
It doesn't return that directly because the context object returned can be further reused by other helper methods in this package (such as `HasPreviousAsync`/`HasNextAsync`).

As a shortcut for when you don't need this context object, there's a `KeysetPaginateQuery` method:
```cs
var users = await dbContext.Users
  .KeysetPaginateQuery(...)
  .Take(20)
  .ToListAsync();
```

Using this context object with helper methods:
```cs
var keysetContext = dbContext.Users
  .KeysetPaginate(...);
var users = await keysetContext.Query.Take(20).ToListAsync();

// This is true when there is more data before the returned list.
var hasPrevious = await keysetContext.HasPreviousAsync(users);

// This is true when there is more data after the returned list.
var hasNext = await keysetContext.HasNextAsync(users);
```

`HasPreviousAsync`/`HasNextAsync` can be used when you want to render Previous/Next (Older/Newer) buttons properly.

If you want to obtain the total count for your query to display somewhere:
```cs
// Assuming this api returns users who are admins only.
var query = dbContext.Users.Where(x => x.IsAdmin);

// This will be the count of all admins.
var count = await query.CountAsync();

// And then we apply keyset pagination.
var users = await query
  .KeysetPaginateQuery(...)
  .Take(20)
  .ToListAsync();
```

As you can see, `KeysetPaginate` adds more predicates to the query so we have to call count before it.

Implementing a First/Last page is quite easy as well:
```cs
// First page
var firstPageUsers = await dbContext.Users
  .KeysetPaginateQuery(b => b.Ascending(entity => entity.Id).Descending(entity => entity.Score))
  .Take(20)
  .ToListAsync();

// Last page
// Notice how we simply flip the sorting order of all properties.
var lastPageUsers = await dbContext.Users
  .KeysetPaginateQuery(b => b.Descending(entity => entity.Id).Ascending(entity => entity.Score))
  .Take(20)
  .ToListAsync();
// Don't forget to reverse to get the proper order of the users in the last page!
lastPageUsers = lastPageUsers.Reverse().ToList();
```
