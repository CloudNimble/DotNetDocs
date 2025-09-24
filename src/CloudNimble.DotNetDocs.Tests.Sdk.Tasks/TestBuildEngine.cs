using System;
using System.Collections;
using System.Collections.Generic;
using Microsoft.Build.Framework;

namespace CloudNimble.DotNetDocs.Tests.Sdk.Tasks
{

    /// <summary>
    /// Test implementation of IBuildEngine for unit testing MSBuild tasks.
    /// </summary>
    internal class TestBuildEngine : IBuildEngine
    {

        #region Properties

        /// <summary>
        /// Gets the list of errors that have been logged.
        /// </summary>
        public List<BuildErrorEventArgs> LoggedErrors { get; } = [];

        /// <summary>
        /// Gets the list of warnings that have been logged.
        /// </summary>
        public List<BuildWarningEventArgs> LoggedWarnings { get; } = [];

        /// <summary>
        /// Gets the list of messages that have been logged.
        /// </summary>
        public List<BuildMessageEventArgs> LoggedMessages { get; } = [];

        /// <summary>
        /// Gets the list of custom events that have been logged.
        /// </summary>
        public List<CustomBuildEventArgs> LoggedCustomEvents { get; } = [];

        /// <summary>
        /// Gets the column number of the task node (not used in tests).
        /// </summary>
        public int ColumnNumberOfTaskNode => 0;

        /// <summary>
        /// Gets whether to continue on error (defaults to false for tests).
        /// </summary>
        public bool ContinueOnError => false;

        /// <summary>
        /// Gets the line number of the task node (not used in tests).
        /// </summary>
        public int LineNumberOfTaskNode => 0;

        /// <summary>
        /// Gets the project file of the task node.
        /// </summary>
        public string ProjectFileOfTaskNode => "TestProject.proj";

        #endregion

        #region Public Methods

        /// <summary>
        /// Builds a project file (not implemented for unit tests).
        /// </summary>
        /// <param name="projectFileName">The project file name.</param>
        /// <param name="targetNames">The target names.</param>
        /// <param name="globalProperties">The global properties.</param>
        /// <param name="targetOutputs">The target outputs.</param>
        /// <returns>true if successful; otherwise, false.</returns>
        public bool BuildProjectFile(string projectFileName, string[] targetNames, IDictionary globalProperties, IDictionary targetOutputs)
        {
            // For unit tests, we typically don't need to build actual projects
            return true;
        }

        /// <summary>
        /// Logs a custom event.
        /// </summary>
        /// <param name="e">The custom build event arguments.</param>
        public void LogCustomEvent(CustomBuildEventArgs e)
        {
            ArgumentNullException.ThrowIfNull(e);
            LoggedCustomEvents.Add(e);
        }

        /// <summary>
        /// Logs an error event.
        /// </summary>
        /// <param name="e">The build error event arguments.</param>
        public void LogErrorEvent(BuildErrorEventArgs e)
        {
            ArgumentNullException.ThrowIfNull(e);
            LoggedErrors.Add(e);
        }

        /// <summary>
        /// Logs a message event.
        /// </summary>
        /// <param name="e">The build message event arguments.</param>
        public void LogMessageEvent(BuildMessageEventArgs e)
        {
            ArgumentNullException.ThrowIfNull(e);
            LoggedMessages.Add(e);
        }

        /// <summary>
        /// Logs a warning event.
        /// </summary>
        /// <param name="e">The build warning event arguments.</param>
        public void LogWarningEvent(BuildWarningEventArgs e)
        {
            ArgumentNullException.ThrowIfNull(e);
            LoggedWarnings.Add(e);
        }

        #endregion

    }

}