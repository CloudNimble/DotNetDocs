using System;
using CloudNimble.DotNetDocs.WasmRunner;
using FluentAssertions;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace CloudNimble.DotNetDocs.Tests.WasmRunner
{

    [TestClass]
    [DoNotParallelize]
    public class OutputCaptureTests
    {

        #region Capture Tests

        [TestMethod]
        public void StartCapture_CapturesConsoleWriteLine()
        {
            using var capture = new OutputCapture();

            capture.StartCapture();
            Console.WriteLine("Hello");
            var output = capture.StopCapture();

            output.Should().Contain("Hello");
        }

        [TestMethod]
        public void StartCapture_CapturesMultipleLines()
        {
            using var capture = new OutputCapture();

            capture.StartCapture();
            Console.WriteLine("Line 1");
            Console.WriteLine("Line 2");
            var output = capture.StopCapture();

            output.Should().Contain("Line 1");
            output.Should().Contain("Line 2");
        }

        [TestMethod]
        public void StopCapture_RestoresOriginalConsoleOut()
        {
            var originalOut = Console.Out;
            using var capture = new OutputCapture();

            capture.StartCapture();
            capture.StopCapture();

            Console.Out.Should().BeSameAs(originalOut);
        }

        [TestMethod]
        public void StartCapture_CalledTwice_ResetsBuffer()
        {
            using var capture = new OutputCapture();

            capture.StartCapture();
            Console.WriteLine("First");
            capture.StopCapture();

            capture.StartCapture();
            Console.WriteLine("Second");
            var output = capture.StopCapture();

            output.Should().NotContain("First");
            output.Should().Contain("Second");
        }

        #endregion

    }

}
