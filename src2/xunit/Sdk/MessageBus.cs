﻿using System;
using System.Collections.Concurrent;
using System.Threading;
using Xunit.Abstractions;

namespace Xunit.Sdk
{
    /// <summary>
    /// This is an internal class, and is not intended to be called from end-user code.
    /// </summary>
    public class MessageBus : IMessageBus, IDisposable
    {
        readonly IMessageSink messageSink;
        readonly ConcurrentQueue<ITestMessage> reporterQueue = new ConcurrentQueue<ITestMessage>();
        readonly Thread reporterThread;
        readonly AutoResetEvent reporterWorkEvent = new AutoResetEvent(initialState: false);
        volatile bool shutdownRequested;

        public MessageBus(IMessageSink messageSink)
        {
            this.messageSink = messageSink;

            reporterThread = new Thread(ReporterWorker);
            reporterThread.Start();
        }

        public void Dispose()
        {
            shutdownRequested = true;

            reporterWorkEvent.Set();
            reporterThread.Join();

            reporterWorkEvent.Dispose();
        }

        public void QueueMessage(ITestMessage message)
        {
            reporterQueue.Enqueue(message);
            reporterWorkEvent.Set();
        }

        private void ReporterWorker()
        {
            while (!shutdownRequested)
            {
                reporterWorkEvent.WaitOne();

                ITestMessage message;

                while (reporterQueue.TryDequeue(out message))
                    try
                    {
                        messageSink.OnMessage(message);
                    }
                    catch (Exception) { }
            }

            try
            {
                messageSink.Dispose();
            }
            catch (Exception) { }
        }
    }
}