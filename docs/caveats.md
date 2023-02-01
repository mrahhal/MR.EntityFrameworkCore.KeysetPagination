# Caveats

## NULL

NULL is a very special value in databases. You can't compare against it as it invalidates the whole WHERE clause, resulting in no results. Different databases sort NULLs in different ways too, some treating it as the smallest value while others treating it as the largest value.

This is why having a nullable column as part of the keyset _is not supported_ in this library, as we can't predictably deal with its special handling in databases.

### Workaround

The recommended way of working around this when you naturally have a nullable column that you want as part of your keyset is through using a non-nullable computed column (one for each nullable column in your keyset) and using that in your keyset instead.

You'll have to do this with any kind of nullable column type, but let's look at an example for a DateTime column.

Let's assume we want to create a keyset comprising of two columns: Created + Id. Let's assume that we need to make Created nullable for a business requirement.

First, we'll need to introduce a property in our model that we'll configure as computed along our nullable property:

```cs
public class User
{
    public int Id { get; set; }

    public DateTime? Created { get; set; } // Let's assume we need to have this as nullable

    public DateTime CreatedComputed { get; } // We'll be configuring this as a computed column later
}
```

Next, in the `OnModelCreating` method of our `DbContext`, we'll be configuring our computed column. The computation will be a coalescing operation that converts NULLs to a value we can deal with and predictably reason about.

Note that the exact format will be different from a Db to the other, so make sure to use the right format both for the COALESCE sql and for the Date format, or you might get wrong results.

```cs
protected override void OnModelCreating(ModelBuilder modelBuilder)
{
    modelBuilder.Entity<User>()
        .Property(x => x.CreatedComputed)
        // We're coalescing NULLs into a max date.
        // This results in NULLs effectively being sorted last (if ASC), irrelevant of the Db.
        // You're writing sql here, make sure you have the right format for your particular database.
        // This is for sqlite.
        .HasComputedColumnSql("COALESCE(Created, '9999-12-31 00:00:00')");

    // Make sure to properly index columns as per your expected queries.
    modelBuilder.Entity<User>()
        .HasIndex(x => new { x.CreatedComputed, x.Id });
}
```

Here's another COALESCE example for Sql Server:

```cs
.HasComputedColumnSql("COALESCE(Created, CONVERT(datetime2, '9999-12-31', 102))");
```

> In Sql Server, the computed value has to be deterministic to be able to create an index for it, that's why we need to use CONVERT.

As shown in the snippets above, don't forget to properly index as always.

Now that we have configured our computed column, we can go ahead and use that in the keyset the same way we use any other column:

```cs
_dbContext.Users.KeysetPaginateQuery(
    b => b.Ascending(x => x.CreatedComputed).Ascending(x => x.Id),
    KeysetPaginationDirection.Forward,
    reference)
    ...
```

Check this [sample page](../samples/Basic/Pages/Computed.cshtml) for a working example you can run and play with.
