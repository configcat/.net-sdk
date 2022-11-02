using System;

namespace ConfigCat.Client
{
    /// <summary>
    /// Provides data for the <see cref="ConfigCatClient.Error"/> event.
    /// </summary>
    public class ConfigCatClientErrorEventArgs : EventArgs
    {
        internal ConfigCatClientErrorEventArgs(string message, Exception exception)
        {
            Message = message;
            Exception = exception;
        }

        /// <summary>
        /// Error message.
        /// </summary>
        public string Message { get; }

        /// <summary>
        /// The <see cref="System.Exception"/> object related to the error (if any).
        /// </summary>
        public Exception Exception { get; }
    }
}
