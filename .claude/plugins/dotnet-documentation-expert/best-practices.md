# Best Practices for .NET Documentation

Detailed guidance for writing high-quality .NET documentation following Microsoft standards and modern C# patterns.

## Microsoft .NET Documentation Standards

### Summary Tag Best Practices

**Start with the Right Verb**

Methods should start with action verbs in third person:
- ✅ "Gets the user's display name"
- ✅ "Creates a new instance"
- ✅ "Calculates the total amount"
- ❌ "Get the user's display name" (wrong tense)
- ❌ "This method gets..." (too verbose)

**Be Concise and Clear**

- Keep summaries to 1-2 sentences
- Focus on *what* the member is or does, not *how* it works
- Avoid implementation details in summaries (use `<remarks>` instead)

```csharp
/// <summary>
/// Gets the authenticated user's profile information.
/// </summary>
/// <remarks>
/// This method queries the database and caches results for 5 minutes.
/// </remarks>
```

### Parameter Documentation

**Be Specific About Requirements**

Document constraints, valid ranges, and special values:

```csharp
/// <param name="pageSize">The number of items per page. Must be between 1 and 100.</param>
/// <param name="email">The user's email address. Cannot be <see langword="null"/> or whitespace.</param>
/// <param name="options">Configuration options, or <see langword="null"/> to use defaults.</param>
```

**Document Side Effects**

If a parameter is modified by the method, document it:

```csharp
/// <param name="buffer">
/// The buffer to write data into. The buffer's position will be advanced by the number of bytes written.
/// </param>
```

### Return Value Documentation

**Describe the Result and Conditions**

```csharp
/// <returns>
/// The <see cref="User"/> if found; otherwise, <see langword="null"/>.
/// </returns>

/// <returns>
/// A task that represents the asynchronous operation. The task result contains the number
/// of records updated, which may be 0 if no matching records were found.
/// </returns>

/// <returns>
/// An enumerable collection of <see cref="Product"/> instances. The collection may be empty
/// if no products match the criteria.
/// </returns>
```

### Exception Documentation

**Document All Thrown Exceptions**

Document exceptions your method throws directly (not exceptions from called methods unless significant):

```csharp
/// <exception cref="ArgumentNullException">
/// Thrown when <paramref name="source"/> is <see langword="null"/>.
/// </exception>
/// <exception cref="ArgumentOutOfRangeException">
/// Thrown when <paramref name="count"/> is less than 0.
/// </exception>
/// <exception cref="InvalidOperationException">
/// Thrown when the service has not been initialized.
/// </exception>
```

### Remarks Tag Usage

Use `<remarks>` for:

**Implementation Details That Affect Usage**

```csharp
/// <remarks>
/// This method uses a least-recently-used (LRU) cache. Cached items expire after 5 minutes
/// of inactivity. Cache size is limited to 1000 items.
/// </remarks>
```

**Performance Characteristics**

```csharp
/// <remarks>
/// This operation has O(n log n) time complexity and requires O(n) additional space.
/// For large datasets (>10,000 items), consider using <see cref="ProcessInBatches"/> instead.
/// </remarks>
```

**Thread Safety Guarantees**

```csharp
/// <remarks>
/// This method is thread-safe and can be called concurrently from multiple threads.
/// </remarks>

/// <remarks>
/// This class is not thread-safe. Use one instance per thread or provide external synchronization.
/// </remarks>
```

**Design Rationale**

```csharp
/// <remarks>
/// This method returns a copy of the internal collection to prevent external modification.
/// For read-only access without copying, use <see cref="GetReadOnlyView"/> instead.
/// </remarks>
```

### Example Tag Guidelines

**Provide Runnable, Realistic Examples**

```csharp
/// <example>
/// <code>
/// var service = new UserService(connectionString);
/// try
/// {
///     var user = await service.GetUserAsync(userId);
///     if (user is not null)
///     {
///         Console.WriteLine($"Found: {user.DisplayName}");
///     }
///     else
///     {
///         Console.WriteLine("User not found");
///     }
/// }
/// finally
/// {
///     service.Dispose();
/// }
/// </code>
/// </example>
```

**Show Common Patterns**

```csharp
/// <example>
/// <para>Basic usage:</para>
/// <code>
/// var result = calculator.Add(5, 3);
/// Console.WriteLine(result); // Output: 8
/// </code>
/// <para>With error handling:</para>
/// <code>
/// try
/// {
///     var result = calculator.Divide(10, 0);
/// }
/// catch (DivideByZeroException ex)
/// {
///     Console.WriteLine($"Error: {ex.Message}");
/// }
/// </code>
/// </example>
```

## Modern C# Features

### Nullable Reference Types

**Document Null Behavior Explicitly**

```csharp
/// <summary>
/// Gets the user's middle name.
/// </summary>
/// <value>
/// The middle name, or <see langword="null"/> if the user has no middle name.
/// </value>
public string? MiddleName { get; set; }

/// <summary>
/// Attempts to find a user by email address.
/// </summary>
/// <param name="email">The email address. Cannot be <see langword="null"/>.</param>
/// <returns>
/// The <see cref="User"/> if found; otherwise, <see langword="null"/>.
/// </returns>
public User? FindByEmail(string email)
{
}
```

**Trust the Type System**

Don't add null checks in documentation when the type is non-nullable:

```csharp
/// <summary>
/// Processes the user account.
/// </summary>
/// <param name="user">The user to process.</param>
/// <remarks>
/// This method assumes <paramref name="user"/> is not <see langword="null"/>
/// as enforced by the type system.
/// </remarks>
public void Process(User user) // Non-nullable, so null is not expected
{
}
```

### Pattern Matching

**Document Pattern-Based Behavior**

```csharp
/// <summary>
/// Calculates the discount based on the customer type.
/// </summary>
/// <param name="customer">The customer. Cannot be <see langword="null"/>.</param>
/// <returns>
/// The discount percentage as a decimal between 0 and 1.
/// Returns 0.10 for premium customers, 0.05 for standard customers,
/// and 0 for all other customer types.
/// </returns>
public decimal GetDiscount(Customer customer)
{
}
```

### Records

**Document Immutability and Value Semantics**

```csharp
/// <summary>
/// Represents an immutable point in 2D space.
/// </summary>
/// <param name="X">The X coordinate.</param>
/// <param name="Y">The Y coordinate.</param>
/// <remarks>
/// This record provides value-based equality comparison. Two points with the same
/// X and Y coordinates are considered equal.
/// <para>
/// Use the with expression to create modified copies:
/// <code>
/// var point = new Point(10, 20);
/// var movedPoint = point with { X = 15 };
/// </code>
/// </para>
/// </remarks>
public record Point(int X, int Y);
```

### Init-Only Properties

**Document Initialization Requirements**

```csharp
/// <summary>
/// Gets the configuration section name.
/// </summary>
/// <value>
/// The section name. This property can only be set during object initialization.
/// </value>
/// <remarks>
/// This property must be set when creating the options instance:
/// <code>
/// var options = new MyOptions { SectionName = "MySection" };
/// </code>
/// </remarks>
public string SectionName { get; init; }
```

### Required Members

**Document Required Properties**

```csharp
/// <summary>
/// Gets or sets the user's email address.
/// </summary>
/// <value>
/// The email address. This property is required and must be set during initialization.
/// </value>
/// <remarks>
/// This property must be set when creating a User instance:
/// <code>
/// var user = new User { Email = "user@example.com", DisplayName = "John Doe" };
/// </code>
/// Attempting to create a User without setting Email will result in a compilation error.
/// </remarks>
public required string Email { get; init; }
```

## Cross-Referencing

### See Tag

**Link to Related Types**

```csharp
/// <summary>
/// Provides data access for <see cref="User"/> entities.
/// </summary>
/// <remarks>
/// This repository implements the <see cref="IRepository{T}"/> interface.
/// For customer data access, see <see cref="CustomerRepository"/>.
/// </remarks>
public class UserRepository : IRepository<User>
{
}
```

### SeeAlso Tag

**Provide Related Documentation Links**

```csharp
/// <summary>
/// Validates user input before saving.
/// </summary>
/// <seealso cref="User"/>
/// <seealso cref="ValidationResult"/>
/// <seealso cref="IValidator{T}"/>
public class UserValidator : IValidator<User>
{
}
```

### ParamRef and TypeParamRef

**Reference Parameters in Documentation**

```csharp
/// <summary>
/// Combines two values using the specified operator.
/// </summary>
/// <param name="left">The left operand.</param>
/// <param name="right">The right operand.</param>
/// <param name="operation">The operation to perform.</param>
/// <returns>
/// The result of applying <paramref name="operation"/> to <paramref name="left"/> and <paramref name="right"/>.
/// </returns>
public int Combine(int left, int right, Func<int, int, int> operation)
{
}

/// <summary>
/// Converts a collection of one type to another.
/// </summary>
/// <typeparam name="TSource">The source element type.</typeparam>
/// <typeparam name="TResult">The result element type.</typeparam>
/// <remarks>
/// This method projects each <typeparamref name="TSource"/> element to <typeparamref name="TResult"/>.
/// </remarks>
public IEnumerable<TResult> Convert<TSource, TResult>(
    IEnumerable<TSource> source,
    Func<TSource, TResult> converter)
{
}
```

### LangWord Tag

**Reference Language Keywords**

Always use `<see langword="..."/>` for language keywords and literals:

```csharp
/// <returns>
/// <see langword="true"/> if the operation succeeded; otherwise, <see langword="false"/>.
/// </returns>

/// <param name="value">The value to check, or <see langword="null"/>.</param>

/// <remarks>
/// This property returns <see langword="default"/> if not initialized.
/// </remarks>
```

Common langwords: `null`, `true`, `false`, `default`, `async`, `await`, `sealed`, `abstract`, `virtual`, `static`

## Thread Safety

### Document Thread Safety Explicitly

**Thread-Safe Class**

```csharp
/// <summary>
/// Provides thread-safe caching functionality.
/// </summary>
/// <remarks>
/// All public members of this class are thread-safe and may be used concurrently
/// from multiple threads. Internal locking ensures data consistency.
/// </remarks>
public class ThreadSafeCache<TKey, TValue>
{
}
```

**Not Thread-Safe**

```csharp
/// <summary>
/// Manages a collection of user sessions.
/// </summary>
/// <remarks>
/// This class is not thread-safe. If instances are accessed by multiple threads,
/// callers must provide external synchronization.
/// </remarks>
public class SessionManager
{
}
```

**Conditionally Thread-Safe**

```csharp
/// <summary>
/// Provides buffered writing functionality.
/// </summary>
/// <remarks>
/// Thread Safety: This class is thread-safe for concurrent reads, but write operations
/// must be externally synchronized. The <see cref="Flush"/> method must not be called
/// concurrently with <see cref="Write"/>.
/// </remarks>
public class BufferedWriter
{
}
```

## Performance Documentation

### Document Performance Characteristics

**Algorithmic Complexity**

```csharp
/// <summary>
/// Sorts the collection in ascending order.
/// </summary>
/// <remarks>
/// This method uses a quicksort algorithm with O(n log n) average time complexity
/// and O(log n) space complexity. Worst-case time complexity is O(n²) for already
/// sorted data.
/// </remarks>
public void Sort()
{
}
```

**Resource Usage**

```csharp
/// <summary>
/// Loads the entire file into memory.
/// </summary>
/// <remarks>
/// Warning: This method loads the complete file contents into memory. For files
/// larger than 100 MB, consider using <see cref="ReadLinesAsync"/> to process
/// the file incrementally.
/// </remarks>
public async Task<string> ReadAllTextAsync(string path)
{
}
```

**Optimization Notes**

```csharp
/// <summary>
/// Gets the number of items in the collection.
/// </summary>
/// <value>
/// The item count.
/// </value>
/// <remarks>
/// This property is O(1) as the count is maintained internally. Unlike LINQ's Count(),
/// this does not enumerate the collection.
/// </remarks>
public int Count { get; }
```

## Lists and Formatting

### Using Para Tag

```csharp
/// <remarks>
/// This method performs validation in three phases:
/// <para>
/// Phase 1: Structural validation ensures all required fields are present.
/// </para>
/// <para>
/// Phase 2: Semantic validation checks business rules and constraints.
/// </para>
/// <para>
/// Phase 3: Cross-field validation verifies relationships between fields.
/// </para>
/// </remarks>
```

### Using List Tag

**Bullet Lists**

```csharp
/// <remarks>
/// This service provides the following capabilities:
/// <list type="bullet">
/// <item>User authentication and authorization</item>
/// <item>Password reset and recovery</item>
/// <item>Profile management</item>
/// <item>Session tracking</item>
/// </list>
/// </remarks>
```

**Numbered Lists**

```csharp
/// <remarks>
/// To configure the service, follow these steps:
/// <list type="number">
/// <item>Add the service to dependency injection</item>
/// <item>Configure authentication providers</item>
/// <item>Set up database connection</item>
/// <item>Initialize the cache</item>
/// </list>
/// </remarks>
```

**Definition Lists**

```csharp
/// <remarks>
/// Supported file formats:
/// <list type="table">
/// <listheader>
/// <term>Format</term>
/// <description>Description</description>
/// </listheader>
/// <item>
/// <term>JSON</term>
/// <description>JavaScript Object Notation for structured data</description>
/// </item>
/// <item>
/// <term>XML</term>
/// <description>Extensible Markup Language for hierarchical data</description>
/// </item>
/// <item>
/// <term>CSV</term>
/// <description>Comma-Separated Values for tabular data</description>
/// </item>
/// </list>
/// </remarks>
```

## Common Pitfalls to Avoid

### Don't Repeat the Member Name

❌ Bad:
```csharp
/// <summary>
/// UserService constructor.
/// </summary>
public UserService()
{
}
```

✅ Good:
```csharp
/// <summary>
/// Initializes a new instance of the <see cref="UserService"/> class.
/// </summary>
public UserService()
{
}
```

### Don't State the Obvious

❌ Bad:
```csharp
/// <summary>
/// Gets or sets the Name.
/// </summary>
public string Name { get; set; }
```

✅ Good:
```csharp
/// <summary>
/// Gets or sets the user's display name.
/// </summary>
/// <value>
/// The display name shown in the user interface. The default is an empty string.
/// </value>
public string Name { get; set; }
```

### Don't Ignore Async Methods

❌ Bad:
```csharp
/// <summary>
/// Gets a user.
/// </summary>
public Task<User> GetUserAsync(int id)
{
}
```

✅ Good:
```csharp
/// <summary>
/// Asynchronously retrieves a user by ID.
/// </summary>
/// <param name="id">The user's unique identifier.</param>
/// <returns>
/// A task that represents the asynchronous operation. The task result contains
/// the <see cref="User"/> if found; otherwise, <see langword="null"/>.
/// </returns>
public Task<User?> GetUserAsync(int id)
{
}
```

### Don't Forget CancellationToken Documentation

```csharp
/// <summary>
/// Asynchronously processes all pending items.
/// </summary>
/// <param name="cancellationToken">
/// A <see cref="CancellationToken"/> to observe while waiting for the task to complete.
/// </param>
/// <returns>
/// A task that represents the asynchronous operation. The task result contains the
/// number of items processed.
/// </returns>
/// <exception cref="OperationCanceledException">
/// Thrown when the operation is canceled via the <paramref name="cancellationToken"/>.
/// </exception>
/// <remarks>
/// The cancellation token is checked before processing each item. Partial results
/// are committed before cancellation.
/// </remarks>
public async Task<int> ProcessItemsAsync(CancellationToken cancellationToken = default)
{
}
```
