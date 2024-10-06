# MR.EntityFrameworkCore.KeysetPagination

<a href="http://use-the-index-luke.com/no-offset">
  <img src="https://github.com/mrahhal/MR.EntityFrameworkCore.KeysetPagination/blob/4be043bf88f49cb3c4e9f96d371699b283031167/images/no-offset.png" alt="100% offset-free" target="_blank" align="right" width="120" height="120">
</a>

[![CI](https://github.com/mrahhal/MR.EntityFrameworkCore.KeysetPagination/actions/workflows/ci.yml/badge.svg)](https://github.com/mrahhal/MR.EntityFrameworkCore.KeysetPagination/actions/workflows/ci.yml)
[![NuGet version](https://badge.fury.io/nu/MR.EntityFrameworkCore.KeysetPagination.svg)](https://www.nuget.org/packages/MR.EntityFrameworkCore.KeysetPagination)
[![License](https://img.shields.io/badge/license-MIT-blue.svg)](LICENSE.txt)

Keyset pagination for EF Core (Entity Framework Core). Also known as seek pagination or cursor pagination.

Learn about why the standard offset based pagination (`Take().Skip()`) is bad in many common cases [here](http://use-the-index-luke.com/no-offset).

Check the [benchmarks](#benchmarks) section below for a quick look at the different performance characteristics between offset and keyset.

> [!NOTE]
> If you're using ASP.NET Core, you can use [MR.AspNetCore.Pagination](https://github.com/mrahhal/MR.AspNetCore.Pagination) which wraps this package and offers an easier to consume keyset pagination behavior with additional features for ASP.NET Core. This is a lower level library that implements keyset pagination for EF Core.

## Usage

`KeysetPaginate` is an extension method on `IQueryable<T>` (same as all other queryable Linq methods), and it takes a few arguments:

```cs
KeysetPaginate(
    // This configures the keyset columns and their order.
    b => b.Ascending(entity => entity.Id),
    // The direction we want to walk relative to the order above (Forward/Backward). Default is Forward.
    direction,
    // The reference object (used to query previous/next pages). Default is null.
    reference
)
```

Using this method we can do all kinds of keyset queries: first page, previous page, next page, last page.

These queries usually follow the same patterns, shown in the "Common patterns" section. Practical code examples are shown in the "Getting the data" section.

But first, let's talk a bit more about `KeysetPaginate` and how it works.

Here's a small visual representation:

<img src="images/exp.jpg" width="300" />

The columns and their configured order are used to order the data, and then the direction decides if we're getting the data before or after the reference row.

> [!WARNING]
> You'll want to reverse the result whenever you use `KeysetPaginationDirection.Backward` to get the proper order of the data, since walking `Backward` gives results in the opposite order to the configured columns order. There's a helper method on `KeysetContext` for this, shown in a snippet later.

`KeysetPaginate` returns a context object which you can use to get secondary info and get the data result.

It can be called without direction and reference, in which case this is equivalent to querying the first page:

```cs
KeysetPaginate(
    b => b.Ascending(entity => entity.Id)
)
```

Configuring a composite keyset is easy as well. Just add all the columns you want:

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

> [!IMPORTANT]
> Make sure to read the "Deterministic keysets" and "Indexing" sections for important notes about configuring keysets.

## Common patterns

Here are the 4 most common patterns of using `KeysetPaginate`.

#### First page

Not specifying direction and reference gives you the first page of data.

```cs
KeysetPaginate(
    b => ...
)
```

This is equivalent to the following:

```cs
KeysetPaginate(
    b => ...,
    KeysetPaginationDirection.Forward,
    null
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

## Prebuilt keyset query definition

Although all the examples here build the keyset directly inside the `KeysetPaginate` call for brevity, the recommended way of doing this is to prebuild the keyset query definition. Prebuilding will allow reusing of internal caches, leading to more performance and less allocations.

To prebuild, all you need to do is move the keyset building code out of the `KeysetPaginate` call and into a long lived instance (such as a static field).

```cs
// In the ctor or someplace similar, set this to a static field for example.
_usersKeysetQuery = KeysetQuery.Build<User>(b => b.Ascending(x => x.Id));

// Then when calling KeysetPaginate, we use the prebuilt definition.
dbContext.Users.KeysetPaginate(
    _usersQueryKeyset,
    ...);
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

> [!NOTE]
> The reference/data these methods accept are loosely typed to allow flexibility when projecting your models (to DTOs for example). For more info check [this document](docs/loose-typing.md).

Here's another example showing how to obtain the total count for the data to display somewhere:

```cs
// Assuming we're in an api that should return admin users.

// Prepare the base query first.
var query = dbContext.Users.Where(x => x.IsAdmin);

// This will be the count of all admins.
var count = await query.CountAsync();

// And then we apply keyset pagination at the end.
// `KeysetPaginate` adds ordering and more predicates to the query so we have to get the count before we apply it.
var keysetContext = query.KeysetPaginate(...);
var admins = await keysetContext.Query
    .Take(20)
    .ToListAsync();

// You can optionally use the context object too as explained above to get additional info.
keysetContext.EnsureCorrectOrder(admins);
```

## Nested properties

Nested properties are also supported when defining a keyset. Just make sure the reference contains the same nested chain of properties.

```cs
// If you're using a loaded entity for the reference.
var reference = await dbContext.Users
    // Load it, otherwise you won't get the correct result.
    .Include(x => x.Nested)
    .FirstOrDefaultAsync(x => x.Id == id);

// If you're using another type for the reference.
var reference = new
{
    Nested = new
    {
        Created = ...,
    },
};

var keysetContext = dbContext.Users.KeysetPaginate(
    // Defining the keyset using a nested property.
    b => b.Ascending(entity => entity.Nested.Created),
    direction,
    reference);
var result = await keysetContext.Query
    // You'll want to load it here too if you plan on calling any context methods.
    .Include(x => x.Nested)
    .Take(20)
    .ToListAsync();
```

## Deterministic keysets

A deterministic keyset is a keyset that can uniquely identify entities. This is an important concept to understand, so let's start by looking at an example.

```
b.Ascending(x => x.Created)
```

The keyset above consists of only one column that accesses `Created`. If by design multiple entities might have the same `Created`, then this is *not* a deterministic keyset.

There are a few problems with a non deterministic keyset. Most importantly, you'll be skipping over data when paginating. This is a side effect of how keyset pagination works.

Fixing this is easy enough. In most cases, you can just add more columns until it becomes deterministic. Most commonly, you can add a column that accesses `Id`.

```
b.Ascending(x => x.Created).Ascending(x => x.Id)
```

This makes the keyset deterministic because the combination of these particular columns will always resolve to uniquely identified entities.

If you can maintain this rule, and if your keyset's data doesn't change, you'll never skip over or duplicate data, a behavior that offset based pagination can never guarantee. We call this behavior _stable pagination_.

Keep in mind that to get the most performance out of this we should have proper indexing that takes into account this composite keyset. This is discussed in the next section.

## Indexing

Keyset pagination — as is the case with any other kind of database query — can benefit a lot from good database indexing. Said in other words, not having a proper index defeats the purpose of using keyset pagination in the first place.

You'll want to add a composite index that is compatible with the columns and the order of your keyset.

Here's an example. Let's say we're doing the following:

```cs
KeysetPaginate(
    b => b.Descending(entity => entity.Created),
    ...
)
```

We should add an index on the `Created` column for this query to be as fast as it can.

Another more complex example:

```cs
KeysetPaginate(
    b => b.Descending(entity => entity.Score).Ascending(entity => entity.Id),
    ...
)
```

In this case you'll want to create a composite index on `Score` + `Id`, but make sure they're compatible with the order above. i.e You should make the index descending on `Score` and ascending on `Id` (or the opposite) for it to be effective.

> [!NOTE]
> Refer to [this document](https://docs.microsoft.com/en-us/ef/core/modeling/indexes) on how to create indexes with EF Core. Note that support for specifying sort order in a composite index was introduced in EF Core 7.0.

## Benchmarks

To give you an idea of the performance gains, here's a graph comparing using offset pagination vs keyset pagination when querying first, middle, and last pages under different table records counts.

The following are the different methods being benchmarked:
- FirstPage: Query the first page
- MidPage: Query the middle page (i.e for N=1K this benchmark queries the data starting from the 500's record)
- LastPage: Query the last page

For a common use case, this is when the data is ordered in `Created` descending (a `DateTime` property).

<img src="images/benchmarks/benchmark-CreatedDesc-grid.png" width="600" />

Notice that when querying the first page, offset pagination does just as well as keyset. Offset pagination starts falling behind remarkably the further away the page you want to read is. Do consider this when choosing what method you want to use.

To that point, the keyset bars (green) are barely visible in the MidPage and LastPage graphs. This shows a major advantage of keyset pagination over offset pagination, that is the stable performance characteristic over large amounts of data even when querying _further away_ pages.

<!-- Another example with a more complicated order, a composite keyset of `Created` descending + `Id` Descending.

<img src="benchmarks/Benchmarks.Basic/Plot/out/benchmark-CreatedDescIdDesc.png" width="600" /> -->

Check the [benchmarks](benchmarks) folder for the source code.

Check this [blog post](https://github.com/mrahhal/blog/blob/main/posts/2023-05-14-offset-vs-keyset-pagination/post.md) for a more detailed look into the benchmarks.

## Caveats

Check [this document](docs/caveats.md) on a few caveats to keep in mind when working with keyset pagination.

## Samples

Check the [samples](samples) folder for project samples.

- [Basic](samples/Basic): This is a quick example of a page that has First/Previous/Next/Last links (using razor pages).

## Talks

[.NET Standup session](https://www.youtube.com/watch?v=DIKH-q-gJNU) where we discuss pagination and showcase this package.

[![.NET Standup session](https://img.youtube.com/vi/DIKH-q-gJNU/0.jpg)](https://www.youtube.com/watch?v=DIKH-q-gJNU)
