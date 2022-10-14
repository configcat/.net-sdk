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
        /// Time of <see cref="ProjectConfig"/>'s successful download
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

        /// <summary>
        /// Determines whether this instance and another specified ProjectConfig struct have the same value.
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
            isEmpty = Equals(Empty);
            return isEmpty || TimeStamp + expiration < DateTime.UtcNow;
        }
    }
}