using System;

namespace CloudNimble.DotNetDocs.Tests.Shared.BasicScenarios
{

    /// <summary>
    /// A class that implements IDisposable for testing interface documentation.
    /// </summary>
    /// <remarks>
    /// This class demonstrates how interface implementation is documented.
    /// </remarks>
    /// <example>
    /// <code>
    /// using (var disposable = new DisposableClass())
    /// {
    ///     disposable.UseResource();
    /// }
    /// </code>
    /// </example>
    public class DisposableClass : IDisposable
    {

        #region Fields

        private bool _disposed;

        #endregion

        #region Properties

        /// <summary>
        /// Gets or sets the resource name.
        /// </summary>
        public string ResourceName { get; set; } = "TestResource";

        #endregion

        #region Public Methods

        /// <summary>
        /// Disposes the resources used by this instance.
        /// </summary>
        /// <remarks>
        /// Implements the IDisposable pattern.
        /// </remarks>
        public void Dispose()
        {
            Dispose(true);
            GC.SuppressFinalize(this);
        }

        /// <summary>
        /// Uses the resource.
        /// </summary>
        /// <exception cref="ObjectDisposedException">Thrown if the object has been disposed.</exception>
        public void UseResource()
        {
            if (_disposed)
            {
                throw new ObjectDisposedException(nameof(DisposableClass));
            }
            // Use the resource
        }

        #endregion

        #region Protected Methods

        /// <summary>
        /// Releases the unmanaged resources and optionally releases the managed resources.
        /// </summary>
        /// <param name="disposing">true to release both managed and unmanaged resources; false to release only unmanaged resources.</param>
        protected virtual void Dispose(bool disposing)
        {
            if (!_disposed)
            {
                if (disposing)
                {
                    // Dispose managed resources
                    ResourceName = null!;
                }
                _disposed = true;
            }
        }

        #endregion

    }

}