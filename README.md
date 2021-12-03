# MR.EntityFrameworkCore.KeysetPagination

<a href="http://use-the-index-luke.com/no-offset">
  <img src="http://use-the-index-luke.com/img/no-offset.q200.png" alt="100% offset-free" target="_blank" align="right" width="106" height="106">
</a>

[![CI](https://github.com/mrahhal/MR.EntityFrameworkCore.KeysetPagination/actions/workflows/ci.yml/badge.svg)](https://github.com/mrahhal/MR.EntityFrameworkCore.KeysetPagination/actions/workflows/ci.yml)
[![NuGet version](https://badge.fury.io/nu/MR.EntityFrameworkCore.KeysetPagination.svg)](https://www.nuget.org/packages/MR.EntityFrameworkCore.KeysetPagination)
[![License](https://img.shields.io/badge/license-MIT-blue.svg)](https://opensource.org/licenses/MIT)

Keyset pagination for EF Core (Entity Framework Core). Also known as seek pagination or cursor pagination.

Learn about why the standard offset based pagination (`Take().Skip()`) is bad [here](http://use-the-index-luke.com/no-offset).

**Note:** If you're using ASP.NET Core, you can use MR.AspNetCore.Pagination (coming soon) which wraps this package and offers an easier to consume keyset pagination behavior with additional features for ASP.NET Core. This is a lower level library that implements keyset pagination for EF Core.

## Usage

`KeysetPaginate` is an extension method on `IQueryable<T>` (same as all other queryable Linq methods), and it takes a few arguments:

```cs
KeysetPaginate(
    // This configures the columns and their order.
    b => b.Ascending(entity => entity.Id),
    // The direction we want to walk relative to the order above (Forward/Backward). Default is Forward.
    direction,
    // The reference entity (used to query previous/next pages). Default is null.
    reference
)
```

Using this method we can do all kinds of keyset queries: first page, previous page, next page, last page.

These queries usually follow the same patterns, shown in the "Common patterns" section. Practical code examples are shown in the "Getting the data" section.

But first, let's talk a bit more about `KeysetPaginate` and how it works.

Here's a small visual representation:

<img src="images/exp.jpg" width="300" />

The columns and their configured order are used to order the data, and then the direction decides if we're getting the data before or after the reference row.

**Note:** You'll want to reverse the result whenever you use `KeysetPaginationDirection.Backward` to get the proper order of the data, since walking `Backward` gives results in the opposite order to the configured columns order. There's a helper method on KeysetContext for this, shown in a snippet later.

`KeysetPaginate` returns a context object which you can use to get secondary info and get the data result.

It can be called without direction and reference, in which case this is equivalent to querying the first page:

```cs
KeysetPaginate(
    b => b.Ascending(entity => entity.Id)
)
```

It works with composite keyset as well. Just configure all the columns you want:

```cs
KeysetPaginate(
    b => b.Ascending(entity => entity.Id).Ascending(entity => entity.Score),
    ...
)
```

You can also mix ASC/DESC columns. `KeysetPaginate` knows how to handle that:

```cs
KeysetPaginate(
    b => b.Ascending(entity => entity.Id).Descending(entity => entity.Score),
    ...
)
```

**Note:** Review the "Avoiding skipping over data" section for an important note about the columns you're configuring.

## Common patterns

Here are the 4 most common patterns of using `KeysetPaginate`.

#### First page

Not specifying direction and reference gives you the first page of data.

```cs
KeysetPaginate(
    b => ...
)
```

#### Last page

We get the last page by specifying a `Backward` direction.

```cs
KeysetPaginate(
    b => ...,
    KeysetPaginationDirection.Backward
)
```

#### Previous page

You get previous/next pages by providing a direction and a reference. In this case, the reference should be the first item of the current page, and the direction is `Backward`:

```cs
KeysetPaginate(
    b => ...,
    KeysetPaginationDirection.Backward,
    reference
)
```

#### Next page

You get previous/next pages by providing a direction and a reference. In this case, the reference should be the last item of the current page, and the direction is `Forward`:

```cs
KeysetPaginate(
    b => ...,
    KeysetPaginationDirection.Forward,
    reference
)
```

## Getting the data

Let's now see how to work with the context object that `KeysetPaginate` returns.

The following is a basic example usage. We're querying the data and getting back 20 items:

```cs
var keysetContext = dbContext.Users.KeysetPaginate(...);

var users = await keysetContext
    .Query
    .Take(20)
    .ToListAsync();

// As noted in several places above, don't forget to ensure the data is correctly ordered:
keysetContext.EnsureCorrectOrder(users);
```

`KeysetPaginate` returns a context object that includes a `Query` property. This `Query` is what you'll chain more linq operators to and then use to get your data.

The context object itself can be further reused by other helper methods in this package such as `HasPreviousAsync`/`HasNextAsync` to get more info.

As a shortcut for when you don't need this context object, there's a `KeysetPaginateQuery` method:

```cs
var users = await dbContext.Users
    .KeysetPaginateQuery(...)
    .Take(20)
    .ToListAsync();
```

Using the context object with helper methods:

```cs
// Store it in a variable because we'll be using it in more than one way.
var keysetContext = dbContext.Users
    .KeysetPaginate(...);

// First, we'll get our actual data. We do this by using the `Query` property.
var users = await keysetContext.Query
    .Take(20)
    .ToListAsync();
// Make sure you call EnsureCorrectOrder before anything else.
keysetContext.EnsureCorrectOrder(users);

// This is true when there is more data before the returned list.
var hasPrevious = await keysetContext.HasPreviousAsync(users);

// This is true when there is more data after the returned list.
var hasNext = await keysetContext.HasNextAsync(users);
```

`HasPreviousAsync`/`HasNextAsync` are useful when you want to know when to render Previous/Next (Older/Newer) buttons.

**Note**: The reference/data these methods accept are always loosely typed. This is designed so that you have the freedom of transforming/mapping your data to DTOs if you want, and still have the ability to ask more info using the same context object. You just need to make sure the columns you configured still exist on the transformed objects.

Here's another example showing how to obtain the total count for the data to display somewhere:

```cs
// Assuming we're in an api that should return admin users.

// Do the initial query first.
var query = dbContext.Users.Where(x => x.IsAdmin);

// This will be the count of all admins.
var count = await query.CountAsync();

// And then we apply keyset pagination at the end.
// You can optionally use the context object too as explained above to get additional info.
var keysetContext = dbContext.Users.KeysetPaginate(...);
var users = await keysetContext.Query
    .Take(20)
    .ToListAsync();
keysetContext.EnsureCorrectOrder(users);
```

`KeysetPaginate` adds ordering and more predicates to the query so we have to get the count before we apply it.

## Avoiding skipping over data

You'll want to make sure the combination of the columns you configure uniquely identify an entity, otherwise you might skip over data while navigating pages. This is a general rule to keep in mind when doing keyset pagination.

If you have configured some columns that don't uniquely identify entities, an easy fix is to just add the `Id` column.

Doing this correctly means you'll never skip over data, a behavior that offset based pagination can never guarantee.

## Indexing

Keyset pagination, as is the case with any other kind of query, can benefit a lot from good database indexing. In the case of keyset pagination, you'll want to add a composite index that is compatible with the columns and the order of the query.

Here's an example. Let's say we're doing the following pagination query:

```cs
KeysetPaginate(
    b => b.Descending(entity => entity.Created),
    ...
)
```

You'll want to add an index on the `Created` column for this query to be as fast as it can no matter the size of the data.

Another more complex example:

```cs
KeysetPaginate(
    b => b.Descending(entity => entity.Score).Ascending(entity => entity.Id),
    ...
)
```

In this case you'll want to create a composite index on `Score` + `Id`, but make sure they're compatible with the order above. i.e You'll want to make the index descending on `Score` and ascending on `Id` (or the opposite) for it to be effective.

**Note**: Refer to [this document](https://docs.microsoft.com/en-us/ef/core/modeling/indexes) on how to create indexes in EF Core.

## Samples

Check the [samples](samples) folder for project samples.

- [Basic](samples/Basic): This is a quick example of a page that has First/Previous/Next/Last links (using razor pages).
