# Documentation Examples

This file provides comprehensive examples of documentation patterns supported by the .NET Documentation Expert plugin.

## Table of Contents

1. [Service Classes](#service-classes)
2. [Repository Pattern](#repository-pattern)
3. [Configuration Classes](#configuration-classes)
4. [Extension Methods](#extension-methods)
5. [Async Patterns](#async-patterns)
6. [Generic Types](#generic-types)
7. [Events and Delegates](#events-and-delegates)
8. [Enumerations](#enumerations)
9. [Interfaces](#interfaces)
10. [Conceptual Documentation](#conceptual-documentation)

---

## Service Classes

### Example: UserService

```csharp
namespace MyApp.Services
{
    /// <summary>
    /// Provides user management operations including authentication, authorization, and profile management.
    /// </summary>
    /// <remarks>
    /// This service coordinates between the user repository, authentication provider, and email service
    /// to provide a unified interface for user-related operations.
    /// <para>
    /// All methods are thread-safe and can be safely called from multiple threads concurrently.
    /// The service uses dependency injection to obtain required dependencies.
    /// </para>
    /// <para>
    /// Thread Safety: This class is thread-safe. All methods can be called concurrently.
    /// </para>
    /// </remarks>
    /// <example>
    /// Basic usage:
    /// <code>
    /// var userService = serviceProvider.GetRequiredService&lt;UserService&gt;();
    ///
    /// // Authenticate user
    /// var user = await userService.AuthenticateAsync("john.doe", "password123");
    /// if (user is not null)
    /// {
    ///     Console.WriteLine($"Welcome, {user.DisplayName}!");
    ///
    ///     // Update profile
    ///     user.Email = "john.doe@example.com";
    ///     await userService.UpdateProfileAsync(user);
    /// }
    /// </code>
    /// </example>
    public class UserService
    {
        /// <summary>
        /// Asynchronously authenticates a user with their credentials.
        /// </summary>
        /// <param name="username">The username. Cannot be <see langword="null"/> or whitespace.</param>
        /// <param name="password">The password. Cannot be <see langword="null"/> or whitespace.</param>
        /// <param name="cancellationToken">A <see cref="CancellationToken"/> to observe while waiting for the task to complete.</param>
        /// <returns>
        /// A task that represents the asynchronous operation. The task result contains the authenticated
        /// <see cref="User"/> if credentials are valid; otherwise, <see langword="null"/>.
        /// </returns>
        /// <exception cref="ArgumentException">
        /// Thrown when <paramref name="username"/> or <paramref name="password"/> is <see langword="null"/> or whitespace.
        /// </exception>
        /// <remarks>
        /// This method validates credentials against the configured authentication provider.
        /// Failed authentication attempts are logged for security monitoring.
        /// <para>
        /// The method uses constant-time comparison for password validation to prevent timing attacks.
        /// </para>
        /// </remarks>
        public async Task<User?> AuthenticateAsync(
            string username,
            string password,
            CancellationToken cancellationToken = default)
        {
            ArgumentException.ThrowIfNullOrWhiteSpace(username);
            ArgumentException.ThrowIfNullOrWhiteSpace(password);

            // Implementation...
        }
    }
}
```

---

## Repository Pattern

### Example: Generic Repository

```csharp
namespace MyApp.Data
{
    /// <summary>
    /// Represents a generic repository for entity CRUD operations.
    /// </summary>
    /// <typeparam name="TEntity">
    /// The type of entity stored in the repository. Must implement <see cref="IEntity"/>.
    /// </typeparam>
    /// <typeparam name="TKey">
    /// The type of the entity's primary key. Must be a value type.
    /// </typeparam>
    /// <remarks>
    /// This repository provides standard CRUD operations with support for:
    /// <list type="bullet">
    /// <item>Async operations for all database access</item>
    /// <item>Query expression support via IQueryable</item>
    /// <item>Unit of work pattern integration</item>
    /// <item>Change tracking and concurrency handling</item>
    /// </list>
    /// <para>
    /// Thread Safety: This class is not thread-safe. Use one instance per unit of work.
    /// </para>
    /// </remarks>
    /// <example>
    /// <code>
    /// public class UserRepository : Repository&lt;User, int&gt;
    /// {
    ///     public UserRepository(DbContext context) : base(context) { }
    ///
    ///     public async Task&lt;User?&gt; FindByEmailAsync(string email)
    ///     {
    ///         return await Query()
    ///             .FirstOrDefaultAsync(u => u.Email == email);
    ///     }
    /// }
    /// </code>
    /// </example>
    public class Repository<TEntity, TKey>
        where TEntity : class, IEntity<TKey>
        where TKey : struct
    {
        /// <summary>
        /// Asynchronously finds an entity by its primary key.
        /// </summary>
        /// <param name="id">The primary key value of the entity to find.</param>
        /// <param name="cancellationToken">A <see cref="CancellationToken"/> to observe while waiting for the task to complete.</param>
        /// <returns>
        /// A task that represents the asynchronous operation. The task result contains the entity
        /// if found; otherwise, <see langword="null"/>.
        /// </returns>
        /// <remarks>
        /// This method queries the database directly and does not use cached values.
        /// For frequently accessed entities, consider implementing a caching layer.
        /// </remarks>
        public async Task<TEntity?> FindByIdAsync(TKey id, CancellationToken cancellationToken = default)
        {
            // Implementation...
        }
    }
}
```

---

## Configuration Classes

### Example: Email Settings

```csharp
namespace MyApp.Configuration
{
    /// <summary>
    /// Represents configuration settings for the email service.
    /// </summary>
    /// <remarks>
    /// This configuration class is typically bound from application settings using the Options pattern.
    /// All properties must be configured before the email service can be used.
    /// <para>
    /// Configuration is validated on application startup to ensure all required values are present.
    /// </para>
    /// </remarks>
    /// <example>
    /// Configure in appsettings.json:
    /// <code>
    /// {
    ///   "EmailSettings": {
    ///     "SmtpServer": "smtp.example.com",
    ///     "Port": 587,
    ///     "UseSsl": true,
    ///     "FromAddress": "noreply@example.com",
    ///     "FromName": "My Application"
    ///   }
    /// }
    /// </code>
    ///
    /// Register in Startup.cs:
    /// <code>
    /// services.Configure&lt;EmailSettings&gt;(
    ///     Configuration.GetSection("EmailSettings"));
    /// </code>
    /// </example>
    public class EmailSettings
    {
        /// <summary>
        /// Gets or sets the SMTP server hostname or IP address.
        /// </summary>
        /// <value>
        /// The SMTP server hostname or IP address. This property is required and cannot be <see langword="null"/> or empty.
        /// </value>
        /// <remarks>
        /// Common examples: "smtp.gmail.com", "smtp.office365.com", "localhost"
        /// </remarks>
        public string SmtpServer { get; set; } = string.Empty;

        /// <summary>
        /// Gets or sets the SMTP server port number.
        /// </summary>
        /// <value>
        /// The port number. Common values are 25 (unencrypted), 587 (TLS), or 465 (SSL). The default is 587.
        /// </value>
        public int Port { get; set; } = 587;

        /// <summary>
        /// Gets or sets a value indicating whether to use SSL/TLS encryption.
        /// </summary>
        /// <value>
        /// <see langword="true"/> to use SSL/TLS encryption; otherwise, <see langword="false"/>. The default is <see langword="true"/>.
        /// </value>
        /// <remarks>
        /// It is strongly recommended to use SSL/TLS in production environments.
        /// Unencrypted SMTP should only be used in development or trusted internal networks.
        /// </remarks>
        public bool UseSsl { get; set; } = true;
    }
}
```

---

## Extension Methods

### Example: String Extensions

```csharp
namespace MyApp.Extensions
{
    /// <summary>
    /// Provides extension methods for <see cref="string"/> objects.
    /// </summary>
    public static class StringExtensions
    {
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
        /// <example>
        /// <code>
        /// string title = "the quick brown fox".ToTitleCase();
        /// Console.WriteLine(title);
        /// // Output: The Quick Brown Fox
        ///
        /// string empty = "".ToTitleCase();
        /// Console.WriteLine($"Empty: '{empty}'");
        /// // Output: Empty: ''
        /// </code>
        /// </example>
        /// <remarks>
        /// This extension method preserves whitespace and punctuation.
        /// Words are defined as sequences of characters separated by whitespace.
        /// </remarks>
        public static string ToTitleCase(this string value)
        {
            ArgumentNullException.ThrowIfNull(value);
            // Implementation...
        }

        /// <summary>
        /// Truncates the string to the specified maximum length and appends an ellipsis if truncated.
        /// </summary>
        /// <param name="value">The string to truncate. Cannot be <see langword="null"/>.</param>
        /// <param name="maxLength">The maximum length including the ellipsis. Must be at least 3.</param>
        /// <returns>
        /// The truncated string with ellipsis appended if the original length exceeds <paramref name="maxLength"/>;
        /// otherwise, the original string.
        /// </returns>
        /// <exception cref="ArgumentNullException">
        /// Thrown when <paramref name="value"/> is <see langword="null"/>.
        /// </exception>
        /// <exception cref="ArgumentOutOfRangeException">
        /// Thrown when <paramref name="maxLength"/> is less than 3.
        /// </exception>
        /// <example>
        /// <code>
        /// string text = "This is a long sentence that needs truncation";
        /// string truncated = text.Truncate(20);
        /// Console.WriteLine(truncated);
        /// // Output: This is a long se...
        ///
        /// string short = "Short".Truncate(20);
        /// Console.WriteLine(short);
        /// // Output: Short
        /// </code>
        /// </example>
        public static string Truncate(this string value, int maxLength)
        {
            ArgumentNullException.ThrowIfNull(value);
            ArgumentOutOfRangeException.ThrowIfLessThan(maxLength, 3);
            // Implementation...
        }
    }
}
```

---

## Async Patterns

### Example: Async Data Access

```csharp
namespace MyApp.Services
{
    /// <summary>
    /// Provides asynchronous data access operations for user management.
    /// </summary>
    public class UserDataService
    {
        /// <summary>
        /// Asynchronously retrieves a user by their unique identifier.
        /// </summary>
        /// <param name="userId">The unique identifier of the user to retrieve.</param>
        /// <param name="cancellationToken">A <see cref="CancellationToken"/> to observe while waiting for the task to complete.</param>
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
        /// <example>
        /// <code>
        /// var cts = new CancellationTokenSource(TimeSpan.FromSeconds(30));
        /// try
        /// {
        ///     var user = await dataService.GetUserAsync(123, cts.Token);
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
        /// <remarks>
        /// This method uses async/await pattern for non-blocking I/O operations.
        /// The cancellation token is passed through to the underlying database query
        /// to allow graceful cancellation of long-running operations.
        /// </remarks>
        public async Task<User?> GetUserAsync(int userId, CancellationToken cancellationToken = default)
        {
            // Implementation...
        }
    }
}
```

---

## Generic Types

See Repository Pattern example above for generic type documentation.

---

## Events and Delegates

### Example: Custom Events

```csharp
namespace MyApp.Events
{
    /// <summary>
    /// Provides data for the <see cref="OrderProcessor.OrderCompleted"/> event.
    /// </summary>
    public class OrderCompletedEventArgs : EventArgs
    {
        /// <summary>
        /// Gets the order that was completed.
        /// </summary>
        /// <value>
        /// The completed <see cref="Order"/> instance.
        /// </value>
        public Order Order { get; }

        /// <summary>
        /// Gets the timestamp when the order was completed.
        /// </summary>
        /// <value>
        /// The UTC timestamp of completion.
        /// </value>
        public DateTime CompletedAt { get; }
    }

    /// <summary>
    /// Processes customer orders through the fulfillment pipeline.
    /// </summary>
    public class OrderProcessor
    {
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
    }
}
```

---

## Enumerations

### Example: Status Enumeration

```csharp
namespace MyApp.Models
{
    /// <summary>
    /// Specifies the current status of an order in the processing pipeline.
    /// </summary>
    /// <remarks>
    /// Orders progress through states in a specific order: Pending → Processing → Shipped → Delivered.
    /// Orders can be transitioned to Cancelled from any state except Delivered.
    /// </remarks>
    [Flags]
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
}
```

---

## Interfaces

### Example: Service Interface

```csharp
namespace MyApp.Abstractions
{
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
        /// <param name="cancellationToken">A <see cref="CancellationToken"/> to observe while waiting for the task to complete.</param>
        /// <returns>
        /// A task that represents the asynchronous operation. The task result contains the authenticated
        /// <see cref="User"/> if credentials are valid; otherwise, <see langword="null"/>.
        /// </returns>
        Task<User?> AuthenticateAsync(string username, string password, CancellationToken cancellationToken = default);
    }
}
```

---

## Conceptual Documentation

### Example: usage.mdz

**File**: `conceptual/MyApp.Services/UserService/usage.mdz`

```markdown
# Using UserService

The `UserService` provides comprehensive user management functionality for your application.

## Basic Setup

First, register the service in your dependency injection container:

\```csharp
services.AddScoped<IUserService, UserService>();
\```

## Common Tasks

### Authenticating a User

\```csharp
var userService = serviceProvider.GetRequiredService<IUserService>();

var user = await userService.AuthenticateAsync("john.doe", "password123");
if (user is not null)
{
    // Authentication successful
    Console.WriteLine($"Welcome, {user.DisplayName}!");
}
else
{
    // Authentication failed
    Console.WriteLine("Invalid credentials");
}
\```

### Creating a New User

\```csharp
var newUser = new User
{
    Username = "jane.doe",
    Email = "jane@example.com",
    DisplayName = "Jane Doe"
};

await userService.CreateUserAsync(newUser, "initialPassword");
\```

### Updating User Profile

\```csharp
var user = await userService.GetUserAsync(userId);
if (user is not null)
{
    user.Email = "newemail@example.com";
    user.PhoneNumber = "+1-555-0123";

    await userService.UpdateProfileAsync(user);
}
\```

## See Also

- [Best Practices](best-practices.mdz)
- [Security Considerations](considerations.mdz)
- [API Reference](/api-reference/MyApp/Services/UserService)
```

---

## Summary

These examples demonstrate the comprehensive documentation patterns supported by the .NET Documentation Expert plugin. Each example shows:

- Complete XML documentation comments
- Proper tag usage and ordering
- Clear, actionable examples
- Cross-references and links
- Project-specific standards compliance
- Integration with conceptual documentation

For more information, see the [plugin README](README.md).
