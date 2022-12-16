using System;
using System.Collections.Generic;

namespace ConfigCat.Client
{
    /// <summary>
    /// ConfigCat ProjectConfig definition
    /// </summary>
    public record class ProjectConfig : IEquatable<ProjectConfig>
    {
        /// <summary>
        ///  A read-only instance of the <see cref="ProjectConfig"/> record whose value is empty.
        /// </summary>
        public static readonly ProjectConfig Empty = new(null, DateTime.MinValue, null);

        /// <summary>
        /// The <see cref="ProjectConfig"/> in json string format
        /// </summary>
        public string JsonString
        {
            get;
#if NET5_0_OR_GREATER
            init;
#else
            internal set;
#endif
        }

        /// <summary>
        /// Time of <see cref="ProjectConfig"/>'s last successful download (regardless of whether the config has changed or not)
        /// </summary>
        public DateTime TimeStamp
        {
            get;
#if NET5_0_OR_GREATER
            init;
#else
            internal set;
#endif
        }

        /// <summary>
        /// Http entity tag of the <see cref="ProjectConfig"/>
        /// </summary>
        public string HttpETag
        {
            get;
#if NET5_0_OR_GREATER
            init;
#else
            internal set;
#endif
        }

        internal bool IsEmpty => HttpETag is null && JsonString is null;

        /// <summary>
        /// Creates an instance of <see cref="ProjectConfig"/>.
        /// </summary>
        public ProjectConfig() { }

        /// <summary>
        /// Creates an instance of <see cref="ProjectConfig"/>.
        /// </summary>
        /// <param name="jsonString">ProjectConfig in json string format.</param>
        /// <param name="timeStamp">TimeStamp of the ProjectConfig's acquire.</param>
        /// <param name="httpETag">Http entity tag of the ProjectConfig.</param>
        public ProjectConfig(string jsonString, DateTime timeStamp, string httpETag)
        {
            JsonString = jsonString;
            TimeStamp = timeStamp;
            HttpETag = httpETag;
        }

        internal static bool ContentEquals(string httpETag1, string jsonString1, string httpETag2, string jsonString2)
        {
            // NOTE: When both ETags are available, we don't need to check the JSON content
            // (because of how HTTP ETags work - see https://developer.mozilla.org/en-US/docs/Web/HTTP/Headers/ETag).
            // However we can't use this logic for implementing the standard Equals method,
            // because then we couldn't implement GetHashCode according to the specification
            // ("Two objects that are equal return hash codes that are equal." -
            // see https://learn.microsoft.com/en-us/dotnet/api/system.object.gethashcode#remarks).

            return httpETag1 is not null && httpETag2 is not null
                ? httpETag1 == httpETag2
                : jsonString1 == jsonString2;
        }

        internal static bool ContentEquals(ProjectConfig config1, ProjectConfig config2)
        {
            return ContentEquals(config1.HttpETag, config1.JsonString, config2.HttpETag, config2.JsonString);
        }

        /// <summary>
        /// Determines whether this instance and another specified ProjectConfig record have the same value.
        /// </summary>
        /// <param name="other">The ProjectConfig to compare to this instance.</param>
        /// <returns>
        /// True if the value of the value parameter is the same as the value of this instance otherwise, false.
        /// </returns>
        public virtual bool Equals(ProjectConfig other)
        {
            return
                other is not null &&
                HttpETag == other.HttpETag &&
                JsonString == other.JsonString;
        }

        /// <inheritdoc />
        public override int GetHashCode()
        {
            int hashCode = 1098790081;
            hashCode = hashCode * -1521134295 + EqualityComparer<string>.Default.GetHashCode(HttpETag);
            hashCode = hashCode * -1521134295 + EqualityComparer<string>.Default.GetHashCode(JsonString);
            return hashCode;
        }

        internal bool IsExpired(TimeSpan expiration, out bool isEmpty)
        {
            isEmpty = IsEmpty;
            return isEmpty || TimeStamp + expiration < DateTime.UtcNow;
        }
    }
}