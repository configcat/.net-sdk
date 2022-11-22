using System;

namespace ConfigCat.Client
{
    /// <summary>
    /// Contains the result of a <see cref="IConfigCatClient.ForceRefresh"/> or <see cref="IConfigCatClient.ForceRefreshAsync"/> operation.
    /// </summary>
    public readonly record struct RefreshResult
    {
        /// <summary>
        /// Creates an instance which indicates that the operation was successful.
        /// </summary>
        /// <returns></returns>
        public static RefreshResult Success()
        {
            return default;
        }

        /// <summary>
        /// Creates an instance which indicates that the operation failed.
        /// </summary>
        /// <returns></returns>
        public static RefreshResult Failure(string errorMessage, Exception errorException = null)
        {
            return new RefreshResult(errorMessage ?? throw new ArgumentNullException(nameof(errorMessage)), errorException);
        }

        private RefreshResult(string errorMessage, Exception errorException)
        {
            ErrorMessage = errorMessage;
            ErrorException = errorException;
        }

        /// <summary>
        /// Indicates whether the operation was successful or not.
        /// </summary>
        public bool IsSuccess => ErrorMessage is null;

        /// <summary>
        /// Error message in case the operation failed, otherwise <see langword="null" />.
        /// </summary>
        public string ErrorMessage { get; }

        /// <summary>
        /// The <see cref="Exception"/> object related to the error in case the operation failed (if any).
        /// </summary>
        public Exception ErrorException { get; }
    }
}
