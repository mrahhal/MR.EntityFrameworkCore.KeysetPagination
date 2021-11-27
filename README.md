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

`KeysetPaginate` is an extension method on `IQueryable<T>` (same as all other queryable linq methods), and it takes a few arguments:

```cs
KeysetPaginate(
    // This configures the columns and their order.
    b => b.Ascending(entity => entity.Id),
    // The direction we want to walk relative to the order above (Backward/Forward). Default is Forward.
    direction,
    // The reference entity (unsed to query previous/next pages). Default is null.
    reference
)
```

Using this method we can do all kinds of keyset queries: first page, previous page, next page, last page.

These queries usually follow the same patterns, shown in the "Common patterns" section. Practical code examples are shown in the "Getting the data" section.

But first, let's talk a bit more about `KeysetPaginate` and how it works.

Here's a small visual representation:

<img src="images/exp.jpg" width="300" />

The columns and their configured order are used to order the data, and then the direction decides if we're getting the data before or after the reference row.

**Note:** You'll want to reverse the result whenever you use `KeysetPaginationDirection.Backward` to get the proper order of the data, since walking `Backward` gives results in the opposite order to the configured columns order.

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

**Note:** You'll want to make sure the combination of these columns uniquely identify an entity, otherwise you might get duplicate data. This is a general thing to keep in mind when doing keyset pagination. You can always add the Id column to the composite columns if the columns you provided don't already provide unique identification of entities.

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

**Note**: Since we're specifing a `Backward` direction, we should reverse the data list (sample code shown below).

#### Previous page

You get previous/next pages by providing a direction and a reference. In this case, the reference should be the first item of the current page, and the direction is `Backward`:

```cs
KeysetPaginate(
    b => ...,
    KeysetPaginationDirection.Backward,
    reference
)
```

**Note**: Since we're specifing a `Backward` direction, we should reverse the data list (sample shown below).

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

The following is a basic example usage of the returned context object. We're querying the data and specifying the size as 20 items:

```cs
var users = await dbContext.Users
    .KeysetPaginate(...).Query
    .Take(20)
    .ToListAsync();

// As noted in several places above, don't forget to reverse the data if we specified a Backward direction:
// users.Reverse();
```

`KeysetPaginate` returns a context object that includes a `Query` property. This `Query` is what you'll chain more linq operators to.

The context object returned can be further reused by other helper methods in this package such as `HasPreviousAsync`/`HasNextAsync`.

As a shortcut for when you don't need this context object, there's a `KeysetPaginateQuery` method:

```cs
var users = await dbContext.Users
    .KeysetPaginateQuery(...)
    .Take(20)
    .ToListAsync();
```

Using the context object with helper methods:

```cs
var keysetContext = dbContext.Users
    .KeysetPaginate(...);
var users = await keysetContext.Query.Take(20).ToListAsync();

// This is true when there is more data before the returned list.
var hasPrevious = await keysetContext.HasPreviousAsync(users);

// This is true when there is more data after the returned list.
var hasNext = await keysetContext.HasNextAsync(users);
```

`HasPreviousAsync`/`HasNextAsync` can be used when you want to render Previous/Next (Older/Newer) buttons.

If you want to obtain the total count for the data to display somewhere:

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

## Samples

Check the [samples](samples) folder for project samples.

- [Basic](samples/Basic): This is a quick example of a page that has First/Previous/Next/Last links (using razor pages).
