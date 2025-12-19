# XML Documentation Templates

Complete templates for all .NET member types. Reference this file when you need detailed templates beyond the quick start guide.

## Generic Types

### Generic Class with Constraints

```csharp
/// <summary>
/// Represents a generic repository for entity operations.
/// </summary>
/// <typeparam name="TEntity">
/// The type of entity stored in the repository. Must implement <see cref="IEntity"/>.
/// </typeparam>
/// <typeparam name="TKey">
/// The type of the entity's primary key. Must be a value type.
/// </typeparam>
/// <remarks>
/// This repository provides standard CRUD operations with support for async operations,
/// query expression support, and unit of work pattern integration.
/// <para>
/// Thread Safety: This class is not thread-safe. Use one instance per unit of work.
/// </para>
/// </remarks>
public class Repository<TEntity, TKey>
    where TEntity : class, IEntity<TKey>
    where TKey : struct
{
}
```

### Generic Method

```csharp
/// <summary>
/// Converts the collection to a different type using the specified converter function.
/// </summary>
/// <typeparam name="TSource">The type of elements in the source collection.</typeparam>
/// <typeparam name="TResult">The type of elements in the result collection.</typeparam>
/// <param name="source">The source collection to convert. Cannot be <see langword="null"/>.</param>
/// <param name="converter">The function to convert each element. Cannot be <see langword="null"/>.</param>
/// <returns>
/// A new collection containing the converted elements.
/// </returns>
/// <exception cref="ArgumentNullException">
/// Thrown when <paramref name="source"/> or <paramref name="converter"/> is <see langword="null"/>.
/// </exception>
public IEnumerable<TResult> Convert<TSource, TResult>(
    IEnumerable<TSource> source,
    Func<TSource, TResult> converter)
{
}
```

## Async Methods

### Async Method with CancellationToken

```csharp
/// <summary>
/// Asynchronously retrieves user data from the database.
/// </summary>
/// <param name="userId">The unique identifier of the user to retrieve.</param>
/// <param name="cancellationToken">
/// A <see cref="CancellationToken"/> to observe while waiting for the task to complete.
/// </param>
/// <returns>
/// A task that represents the asynchronous operation. The task result contains the
/// <see cref="User"/> if found; otherwise, <see langword="null"/>.
/// </returns>
/// <exception cref="OperationCanceledException">
/// Thrown when the operation is canceled via the <paramref name="cancellationToken"/>.
/// </exception>
/// <exception cref="DbException">
/// Thrown when a database error occurs during the operation.
/// </exception>
/// <remarks>
/// This method uses async/await for non-blocking I/O operations.
/// The cancellation token is propagated to the underlying database query.
/// </remarks>
/// <example>
/// <code>
/// var cts = new CancellationTokenSource(TimeSpan.FromSeconds(30));
/// try
/// {
///     var user = await GetUserAsync(123, cts.Token);
///     if (user is not null)
///     {
///         Console.WriteLine($"Found: {user.DisplayName}");
///     }
/// }
/// catch (OperationCanceledException)
/// {
///     Console.WriteLine("Operation timed out");
/// }
/// </code>
/// </example>
public async Task<User?> GetUserAsync(
    int userId,
    CancellationToken cancellationToken = default)
{
}
```

### Async Void Method (Event Handlers Only)

```csharp
/// <summary>
/// Handles the button click event asynchronously.
/// </summary>
/// <param name="sender">The source of the event.</param>
/// <param name="e">The event data.</param>
/// <remarks>
/// This async void method is acceptable only as an event handler.
/// Exceptions thrown from this method cannot be caught by the caller.
/// </remarks>
private async void OnButtonClick(object sender, EventArgs e)
{
}
```

## Extension Methods

### Basic Extension Method

```csharp
/// <summary>
/// Converts the string to title case where the first letter of each word is capitalized.
/// </summary>
/// <param name="value">The string to convert. Cannot be <see langword="null"/>.</param>
/// <returns>
/// A new string in title case. Returns an empty string if <paramref name="value"/> is empty.
/// </returns>
/// <exception cref="ArgumentNullException">
/// Thrown when <paramref name="value"/> is <see langword="null"/>.
/// </exception>
/// <remarks>
/// This extension method preserves whitespace and punctuation.
/// Words are defined as sequences of characters separated by whitespace.
/// </remarks>
/// <example>
/// <code>
/// string title = "the quick brown fox".ToTitleCase();
/// Console.WriteLine(title);
/// // Output: The Quick Brown Fox
/// </code>
/// </example>
public static string ToTitleCase(this string value)
{
}
```

### Extension Method with Additional Parameters

```csharp
/// <summary>
/// Truncates the string to the specified maximum length and appends an ellipsis if truncated.
/// </summary>
/// <param name="value">The string to truncate. Cannot be <see langword="null"/>.</param>
/// <param name="maxLength">The maximum length including the ellipsis. Must be at least 3.</param>
/// <param name="ellipsis">The ellipsis string to append. Defaults to "...".</param>
/// <returns>
/// The truncated string with ellipsis if the original exceeds <paramref name="maxLength"/>;
/// otherwise, the original string.
/// </returns>
/// <exception cref="ArgumentNullException">
/// Thrown when <paramref name="value"/> or <paramref name="ellipsis"/> is <see langword="null"/>.
/// </exception>
/// <exception cref="ArgumentOutOfRangeException">
/// Thrown when <paramref name="maxLength"/> is less than the ellipsis length.
/// </exception>
public static string Truncate(
    this string value,
    int maxLength,
    string ellipsis = "...")
{
}
```

## Events and Delegates

### Event with Custom EventArgs

```csharp
/// <summary>
/// Occurs when an order has been successfully processed and completed.
/// </summary>
/// <remarks>
/// Subscribers receive <see cref="OrderCompletedEventArgs"/> containing the completed order
/// and completion timestamp.
/// <para>
/// This event is raised after payment confirmation and before shipping notification.
/// Event handlers are invoked synchronously in registration order.
/// </para>
/// <para>
/// Thread Safety: This event can be raised from any thread. Subscribers must handle
/// their own thread synchronization if necessary.
/// </para>
/// </remarks>
/// <example>
/// <code>
/// var processor = new OrderProcessor();
/// processor.OrderCompleted += (sender, e) =>
/// {
///     Console.WriteLine($"Order {e.Order.Id} completed at {e.CompletedAt}");
/// };
///
/// await processor.ProcessOrderAsync(myOrder);
/// </code>
/// </example>
public event EventHandler<OrderCompletedEventArgs>? OrderCompleted;
```

### Custom Delegate

```csharp
/// <summary>
/// Represents a method that validates an entity before saving.
/// </summary>
/// <typeparam name="T">The type of entity to validate.</typeparam>
/// <param name="entity">The entity to validate.</param>
/// <returns>
/// <see langword="true"/> if the entity is valid; otherwise, <see langword="false"/>.
/// </returns>
/// <remarks>
/// Validators should not throw exceptions. Return <see langword="false"/> for invalid entities.
/// </remarks>
public delegate bool EntityValidator<T>(T entity) where T : class;
```

## Enumerations

### Simple Enumeration

```csharp
/// <summary>
/// Specifies the current status of an order in the processing pipeline.
/// </summary>
/// <remarks>
/// Orders progress through states: Pending → Processing → Shipped → Delivered.
/// Orders can transition to Cancelled from any state except Delivered.
/// </remarks>
public enum OrderStatus
{
    /// <summary>
    /// The order has been created but not yet processed.
    /// Payment authorization is pending.
    /// </summary>
    Pending = 0,

    /// <summary>
    /// The order is currently being processed.
    /// Payment has been authorized and inventory is being allocated.
    /// </summary>
    Processing = 1,

    /// <summary>
    /// The order has been shipped to the customer.
    /// Tracking information is available.
    /// </summary>
    Shipped = 2,

    /// <summary>
    /// The order has been delivered to the customer.
    /// This is a terminal state - no further transitions are allowed.
    /// </summary>
    Delivered = 3,

    /// <summary>
    /// The order has been cancelled.
    /// If payment was captured, a refund has been initiated.
    /// </summary>
    Cancelled = 4
}
```

### Flags Enumeration

```csharp
/// <summary>
/// Specifies the file access permissions that can be combined.
/// </summary>
/// <remarks>
/// This enumeration supports bitwise combination of its member values.
/// Multiple permissions can be combined using the | operator.
/// </remarks>
/// <example>
/// <code>
/// var permissions = FilePermissions.Read | FilePermissions.Write;
/// if (permissions.HasFlag(FilePermissions.Read))
/// {
///     Console.WriteLine("Has read permission");
/// }
/// </code>
/// </example>
[Flags]
public enum FilePermissions
{
    /// <summary>
    /// No permissions granted.
    /// </summary>
    None = 0,

    /// <summary>
    /// Permission to read the file.
    /// </summary>
    Read = 1,

    /// <summary>
    /// Permission to write to the file.
    /// </summary>
    Write = 2,

    /// <summary>
    /// Permission to execute the file.
    /// </summary>
    Execute = 4,

    /// <summary>
    /// All permissions (read, write, and execute).
    /// </summary>
    All = Read | Write | Execute
}
```

## Interfaces

### Interface with Multiple Methods

```csharp
/// <summary>
/// Defines operations for managing user accounts and authentication.
/// </summary>
/// <remarks>
/// Implementations of this interface provide user management functionality including:
/// <list type="bullet">
/// <item>User authentication and session management</item>
/// <item>Profile creation and updates</item>
/// <item>Password management and resets</item>
/// <item>Role and permission assignment</item>
/// </list>
/// </remarks>
public interface IUserService
{
    /// <summary>
    /// Asynchronously authenticates a user with their credentials.
    /// </summary>
    /// <param name="username">The username. Cannot be <see langword="null"/> or whitespace.</param>
    /// <param name="password">The password. Cannot be <see langword="null"/> or whitespace.</param>
    /// <param name="cancellationToken">
    /// A <see cref="CancellationToken"/> to observe while waiting for the task to complete.
    /// </param>
    /// <returns>
    /// A task that represents the asynchronous operation. The task result contains the
    /// authenticated <see cref="User"/> if credentials are valid; otherwise, <see langword="null"/>.
    /// </returns>
    Task<User?> AuthenticateAsync(
        string username,
        string password,
        CancellationToken cancellationToken = default);
}
```

## Indexers

### Basic Indexer

```csharp
/// <summary>
/// Gets or sets the element at the specified index.
/// </summary>
/// <param name="index">The zero-based index of the element to get or set.</param>
/// <value>
/// The element at the specified index.
/// </value>
/// <exception cref="ArgumentOutOfRangeException">
/// Thrown when <paramref name="index"/> is less than 0 or greater than or equal to <see cref="Count"/>.
/// </exception>
/// <remarks>
/// This indexer provides direct access to the underlying collection.
/// Modifications through this indexer are immediately reflected in the collection.
/// </remarks>
public T this[int index]
{
    get { }
    set { }
}
```

### Multi-Parameter Indexer

```csharp
/// <summary>
/// Gets or sets the value at the specified row and column.
/// </summary>
/// <param name="row">The zero-based row index.</param>
/// <param name="column">The zero-based column index.</param>
/// <value>
/// The value at the specified position.
/// </value>
/// <exception cref="ArgumentOutOfRangeException">
/// Thrown when <paramref name="row"/> or <paramref name="column"/> is out of range.
/// </exception>
public T this[int row, int column]
{
    get { }
    set { }
}
```

## Structs and Records

### Struct

```csharp
/// <summary>
/// Represents a point in 2D space with X and Y coordinates.
/// </summary>
/// <remarks>
/// This is a value type and should be passed by value or by readonly reference.
/// The struct is immutable to ensure thread safety and predictable behavior.
/// </remarks>
/// <example>
/// <code>
/// var point = new Point(10, 20);
/// Console.WriteLine($"X: {point.X}, Y: {point.Y}");
/// </code>
/// </example>
public readonly struct Point
{
    /// <summary>
    /// Gets the X coordinate.
    /// </summary>
    /// <value>
    /// The X coordinate value.
    /// </value>
    public int X { get; }

    /// <summary>
    /// Gets the Y coordinate.
    /// </summary>
    /// <value>
    /// The Y coordinate value.
    /// </value>
    public int Y { get; }

    /// <summary>
    /// Initializes a new instance of the <see cref="Point"/> struct.
    /// </summary>
    /// <param name="x">The X coordinate.</param>
    /// <param name="y">The Y coordinate.</param>
    public Point(int x, int y)
    {
        X = x;
        Y = y;
    }
}
```

### Record

```csharp
/// <summary>
/// Represents an immutable user profile with email and display name.
/// </summary>
/// <param name="Email">The user's email address. Cannot be <see langword="null"/> or whitespace.</param>
/// <param name="DisplayName">The user's display name. Cannot be <see langword="null"/> or whitespace.</param>
/// <remarks>
/// This record provides value-based equality comparison and is immutable by default.
/// Use the with expression to create modified copies.
/// </remarks>
/// <example>
/// <code>
/// var profile = new UserProfile("user@example.com", "John Doe");
/// var updated = profile with { DisplayName = "Jane Doe" };
/// </code>
/// </example>
public record UserProfile(string Email, string DisplayName);
```

## Operators

### Operator Overload

```csharp
/// <summary>
/// Adds two <see cref="Vector"/> instances.
/// </summary>
/// <param name="left">The first vector to add.</param>
/// <param name="right">The second vector to add.</param>
/// <returns>
/// A new <see cref="Vector"/> that is the sum of <paramref name="left"/> and <paramref name="right"/>.
/// </returns>
/// <remarks>
/// This operator performs component-wise addition of the two vectors.
/// </remarks>
public static Vector operator +(Vector left, Vector right)
{
}
```

## Finalizers and Dispose

### IDisposable Implementation

```csharp
/// <summary>
/// Releases all resources used by this instance.
/// </summary>
/// <remarks>
/// This method should be called when you are finished using this object.
/// After calling this method, the object should not be used again.
/// <para>
/// This method can be called multiple times safely. Subsequent calls have no effect.
/// </para>
/// </remarks>
/// <example>
/// <code>
/// using (var resource = new MyResource())
/// {
///     // Use the resource
/// }
/// // Dispose is called automatically at the end of the using block
/// </code>
/// </example>
public void Dispose()
{
}
```
