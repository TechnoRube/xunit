﻿using System;
using System.Collections.Generic;
using System.IO;
using Xunit.Abstractions;

namespace Xunit
{
    /// <summary>
    /// Default implementation of <see cref="IFrontController"/> which supports running tests from
    /// both xUnit.net v1 and v2.
    /// </summary>
    public class XunitFrontController : IFrontController
    {
        readonly string assemblyFileName;
        readonly string configFileName;
        IFrontController innerController;
        readonly bool shadowCopy;
        readonly ISourceInformationProvider sourceInformationProvider;
        readonly Stack<IDisposable> toDispose = new Stack<IDisposable>();

        /// <summary>
        /// This constructor is for unit testing purposes only.
        /// </summary>
        protected XunitFrontController() { }

        /// <summary>
        /// Initializes a new instance of the <see cref="XunitFrontController"/> class.
        /// </summary>
        /// <param name="assemblyFileName">The test assembly.</param>
        /// <param name="configFileName">The test assembly configuration file.</param>
        /// <param name="shadowCopy">If set to <c>true</c>, runs tests in a shadow copied app domain, which allows
        /// <param name="sourceInformationProvider">The source information provider. If <c>null</c>, uses the default (<see cref="VisualStudioSourceInformationProvider"/>).</param>
        /// tests to be discovered and run without locking assembly files on disk.</param>
        public XunitFrontController(string assemblyFileName, string configFileName = null, bool shadowCopy = true, ISourceInformationProvider sourceInformationProvider = null)
        {
            this.assemblyFileName = assemblyFileName;
            this.configFileName = configFileName;
            this.shadowCopy = shadowCopy;
            this.sourceInformationProvider = sourceInformationProvider;

            Guard.FileExists("assemblyFileName", assemblyFileName);

            if (this.sourceInformationProvider == null)
            {
                this.sourceInformationProvider = new VisualStudioSourceInformationProvider(assemblyFileName);
                toDispose.Push(this.sourceInformationProvider);
            }
        }

        private IFrontController InnerController
        {
            get
            {
                if (innerController == null)
                {
                    innerController = CreateInnerController();
                    toDispose.Push(innerController);
                }

                return innerController;
            }
        }

        /// <inheritdoc/>
        public string TestFrameworkDisplayName
        {
            get { return InnerController.TestFrameworkDisplayName; }
        }

        /// <summary>
        /// FOR INTERNAL USE ONLY.
        /// </summary>
        protected virtual IFrontController CreateInnerController()
        {
            var xunitPath = Path.Combine(Path.GetDirectoryName(assemblyFileName), "xunit.dll");
            var xunitExecutionPath = Path.Combine(Path.GetDirectoryName(assemblyFileName), "xunit.execution.dll");

            if (File.Exists(xunitExecutionPath))
                return new Xunit2(sourceInformationProvider, assemblyFileName, configFileName, shadowCopy);
            if (File.Exists(xunitPath))
                return new Xunit1(sourceInformationProvider, assemblyFileName, configFileName, shadowCopy);

            throw new ArgumentException("Unknown test framework: Could not find xunit.dll or xunit.execution.dll.", assemblyFileName);
        }

        /// <inheritdoc/>
        public ITestCase Deserialize(string value)
        {
            return InnerController.Deserialize(value);
        }

        /// <inheritdoc/>
        public void Dispose()
        {
            foreach (var disposable in toDispose)
                disposable.Dispose();
        }

        /// <inheritdoc/>
        public virtual void Find(bool includeSourceInformation, IMessageSink messageSink, ITestFrameworkOptions options)
        {
            InnerController.Find(includeSourceInformation, messageSink, options);
        }

        /// <inheritdoc/>
        public virtual void Find(string typeName, bool includeSourceInformation, IMessageSink messageSink, ITestFrameworkOptions options)
        {
            InnerController.Find(typeName, includeSourceInformation, messageSink, options);
        }

        /// <inheritdoc/>
        public virtual void Run(IMessageSink messageSink, ITestFrameworkOptions discoveryOptions, ITestFrameworkOptions executionOptions)
        {
            InnerController.Run(messageSink, discoveryOptions, executionOptions);
        }

        /// <inheritdoc/>
        public virtual void Run(IEnumerable<ITestCase> testMethods, IMessageSink messageSink, ITestFrameworkOptions options)
        {
            InnerController.Run(testMethods, messageSink, options);
        }

        /// <inheritdoc/>
        public string Serialize(ITestCase testCase)
        {
            return InnerController.Serialize(testCase);
        }
    }
}