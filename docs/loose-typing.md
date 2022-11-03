# Loose Typing

The reference/data accepted by the different methods in this library are usually loosely typed.

This is designed to give you the freedom of transforming/mapping/projecting your data to DTOs if you want, and still have the ability to ask more info using the same context object and projected data. But one important requirement here is that you need to make sure the columns you configured also exist on the projected objects, so they have to match. This is supported for both projected models and anonymous types as well.

Let's look at a few examples.

```cs
public class User
{
    public string Name { get; set; }
}

public class UserDto
{
    public string Name { get; set; }
}

// Then

var reference = await _dbContext.Users
    .Where(x => x.Id == someId)
    .Select(x => new
    {
        Name = x.Name,
    })
    .FirstOrDefaultAsync();

var keysetContext = _dbContext.Users.KeysetPaginate(
    b => b.Ascending(x => x.Name),
    KeysetPaginationDirection.Forward,
    // Loose typing works here: reference is an anonymous type, but it has matching keyset properties to User.
    reference
);

var data = await keysetContext.Query
    .ProjectTo<UserDto>() // Automapper
    .ToListAsync();

// Loose typing works here: data has elements of type UserDto, but UserDto has matching keyset properties to User.
var hasNext = await keysetContext.HasNextAsync(data);
```

Let's see an example with a nested property.

```cs
public class User
{
    public UserDetails Details { get; set; }
}

public class UserDetails
{
    public string Name { get; set; }
}

// Then

// We're showing this with an anonymous type here, but the same applies for projected models.

var reference = await _dbContext.Users
    .Where(x => x.Id == someId)
    .Select(x => new
    {
        // We're matching the structure of User for the used keyset columns.
        Details = new
        {
            Name = x.Name,
        },
    })
    .FirstOrDefaultAsync();

var keysetContext = _dbContext.Users.KeysetPaginate(
    b => b.Ascending(x => x.Name),
    KeysetPaginationDirection.Forward,
    // Loose typing works here: reference is an anonymous type, but it has matching keyset properties to User.
    reference
);
```
