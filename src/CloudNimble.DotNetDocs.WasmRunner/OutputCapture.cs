using System;
using System.IO;

namespace CloudNimble.DotNetDocs.WasmRunner
{

    /// <summary>
    /// Captures console output by redirecting <see cref="Console.Out"/> to a <see cref="StringWriter"/>.
    /// </summary>
    /// <remarks>
    /// This class is used to intercept <c>Console.WriteLine</c> calls during code execution
    /// so their output can be returned to the caller. Call <see cref="StartCapture"/> before
    /// executing user code and <see cref="StopCapture"/> afterward to retrieve the output.
    /// </remarks>
    public class OutputCapture : IDisposable
    {

        #region Fields

        private bool _disposed;
        private TextWriter? _originalOut;
        private StringWriter? _writer;

        #endregion

        #region Public Methods

        /// <inheritdoc/>
        public void Dispose()
        {
            if (_disposed)
            {
                return;
            }

            if (_originalOut is not null)
            {
                Console.SetOut(_originalOut);
                _originalOut = null;
            }

            _writer?.Dispose();
            _writer = null;
            _disposed = true;
        }

        /// <summary>
        /// Begins capturing console output by redirecting <see cref="Console.Out"/> to an internal buffer.
        /// </summary>
        public void StartCapture()
        {
            _originalOut = Console.Out;
            _writer = new StringWriter();
            Console.SetOut(_writer);
        }

        /// <summary>
        /// Stops capturing console output and returns the captured text.
        /// </summary>
        /// <returns>The captured console output as a string.</returns>
        public string StopCapture()
        {
            var output = _writer?.ToString() ?? string.Empty;

            if (_originalOut is not null)
            {
                Console.SetOut(_originalOut);
                _originalOut = null;
            }

            _writer?.Dispose();
            _writer = null;

            return output;
        }

        #endregion

    }

}
