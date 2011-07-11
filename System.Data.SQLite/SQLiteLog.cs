/********************************************************
 * ADO.NET 2.0 Data Provider for SQLite Version 3.X
 * Written by Robert Simpson (robert@blackcastlesoft.com)
 * 
 * Released to the public domain, use at your own risk!
 ********************************************************/

namespace System.Data.SQLite
{
    using System;
    using System.Data.Common;
    using System.Diagnostics;

    /// <summary>
    /// Passed during an Log callback
    /// </summary>
    public class LogEventArgs : EventArgs
    {
        /// <summary>
        /// The error code.
        /// </summary>
        public readonly int ErrorCode;

        /// <summary>
        /// SQL statement text as the statement first begins executing
        /// </summary>
        public readonly string Message;

        /// <summary>
        /// Extra data associated with this event, if any.
        /// </summary>
        public readonly object Data;

        /// <summary>
        /// Constructs the LogEventArgs object.
        /// </summary>
        /// <param name="pUserData">Should be null.</param>
        /// <param name="errorCode">The SQLite error code.</param>
        /// <param name="message">The error message, if any.</param>
        /// <param name="data">The extra data, if any.</param>
        internal LogEventArgs(
            IntPtr pUserData,
            int errorCode,
            string message,
            object data
            )
        {
            ErrorCode = errorCode;
            Message = message;
            Data = data;
        }
    }

    ///////////////////////////////////////////////////////////////////////////

    /// <summary>
    /// Raised when a log event occurs.
    /// </summary>
    /// <param name="sender">The current connection</param>
    /// <param name="e">Event arguments of the trace</param>
    public delegate void SQLiteLogEventHandler(object sender, LogEventArgs e);

    ///////////////////////////////////////////////////////////////////////////

#if !PLATFORM_COMPACTFRAMEWORK
    /// <summary>
    /// Manages the SQLite custom logging functionality and the associated
    /// callback for the whole process.
    /// </summary>
    public static class SQLiteLog
    {
        /// <summary>
        /// Object used to synchronize access to the static instance data
        /// for this class.
        /// </summary>
        private static object syncRoot = new object();

        /// <summary>
        /// Member variable to store the application log handler to call.
        /// </summary>
        private static event SQLiteLogEventHandler _handlers;

        /// <summary>
        /// The default log event handler.
        /// </summary>
        private static SQLiteLogEventHandler _defaultHandler;

        /// <summary>
        /// The log callback passed to native SQLite engine.  This must live
        /// as long as the SQLite library has a pointer to it.
        /// </summary>
        private static SQLiteLogCallback _callback;

        /// <summary>
        /// The base SQLite object to interop with.
        /// </summary>
        private static SQLiteBase _sql;

        /// <summary>
        /// This will be non-zero if logging is currently enabled.
        /// </summary>
        private static bool _enabled;

        /// <summary>
        /// Initializes the SQLite logging facilities.
        /// </summary>
        public static void Initialize()
        {
            lock (syncRoot)
            {
                //
                // NOTE: Create a single "global" 
                //
                if (_sql == null)
                    _sql = new SQLite3(SQLiteDateFormats.ISO8601);

                //
                // NOTE: Create a single "global" (i.e. per-process) callback
                //       to register with SQLite.  This callback will pass the
                //       event on to any registered handler.  We only want to
                //       do this once.
                //
                if (_callback == null)
                {
                    _callback = new SQLiteLogCallback(LogCallback);

                    int rc = _sql.SetLogCallback(_callback);

                    if (rc != 0)
                        throw new SQLiteException(rc,
                            "Failed to initialize logging interface.");
                }

                //
                // NOTE: Logging is enabled by default.
                //
                _enabled = true;

                //
                // NOTE: For now, always setup the default log event handler.
                //
                AddDefaultHandler();
            }
        }

        /// <summary>
        /// This event is raised whenever SQLite raises a logging event.
        /// Note that this should be set as one of the first things in the
        /// application.
        /// </summary>
        public static event SQLiteLogEventHandler Log
        {
            add
            {
                lock (syncRoot)
                {
                    // Remove any copies of this event handler from registered
                    // list.  This essentially means that a handler will be
                    // called only once no matter how many times it is added.
                    _handlers -= value;

                    // Add this to the list of event handlers.
                    _handlers += value;
                }
            }
            remove
            {
                lock (syncRoot)
                {
                    _handlers -= value;
                }
            }
        }

        /// <summary>
        /// If this property is true, logging is enabled; otherwise, logging is
        /// disabled.  When logging is disabled, no logging events will fire.
        /// </summary>
        public static bool Enabled
        {
            get { lock (syncRoot) { return _enabled; } }
            set { lock (syncRoot) { _enabled = value; } }
        }

        /// <summary>
        /// Creates and initializes the default log event handler.
        /// </summary>
        private static void InitializeDefaultHandler()
        {
            lock (syncRoot)
            {
                if (_defaultHandler == null)
                    _defaultHandler = new SQLiteLogEventHandler(LogEventHandler);
            }
        }

        /// <summary>
        /// Adds the default log event handler to the list of handlers.
        /// </summary>
        public static void AddDefaultHandler()
        {
            InitializeDefaultHandler();
            Log += _defaultHandler;
        }

        /// <summary>
        /// Removes the default log event handler from the list of handlers.
        /// </summary>
        public static void RemoveDefaultHandler()
        {
            InitializeDefaultHandler();
            Log -= _defaultHandler;
        }

        /// <summary>
        /// Internal proxy function that calls any registered application log
        /// event handlers.
        /// </summary>
        private static void LogCallback(
            IntPtr pUserData,
            int errorCode,
            IntPtr pMessage
            )
        {
            bool enabled;
            SQLiteLogEventHandler handlers;

            lock (syncRoot)
            {
                enabled = _enabled;
                handlers = _handlers;
            }

            if (enabled && (handlers != null))
                handlers(null, new LogEventArgs(pUserData, errorCode,
                    SQLiteBase.UTF8ToString(pMessage, -1), null));
        }

        /// <summary>
        /// Default logger.  Currently, uses the Trace class (i.e. sends events
        /// to the current trace listeners, if any).
        /// </summary>
        /// <param name="sender">Should be null.</param>
        /// <param name="e">The data associated with this event.</param>
        private static void LogEventHandler(
            object sender,
            LogEventArgs e
            )
        {
            if (e == null)
                return;

            string message = e.Message;

            if (message == null)
                message = "<null>";
            else if (message.Length == 0)
                message = "<empty>";

            Trace.WriteLine(String.Format("SQLite error ({0}): {1}",
                e.ErrorCode, message));
        }
    }
#endif
}
