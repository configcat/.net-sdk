using System;
using System.Collections.Generic;

namespace ConfigCat.Client
{
    /// <summary>
    /// ConfigCat ProjectConfig definition
    /// </summary>
    public struct ProjectConfig : IEquatable<ProjectConfig>
    {
        /// <summary>
        ///  A read-only instance of the ProjectConfig structure whose value is empty.
        /// </summary>
        public static readonly ProjectConfig Empty = new(null, DateTime.MinValue, null);

        /// <summary>
        /// ProjectConfig in json string format
        /// </summary>
        public string JsonString { get; set; }

        /// <summary>
        /// TimeStamp of the ProjectConfig's acquire
        /// </summary>
        public DateTime TimeStamp { get; set; }

        /// <summary>
        /// Http entity tag of the ProjectConfig
        /// </summary>
        public string HttpETag { get; set; }

        internal ProjectConfig(string jsonString, DateTime timeStamp, string httpETag)
        {
            this.JsonString = jsonString;
            this.TimeStamp = timeStamp;
            this.HttpETag = httpETag;
        }

        /// <inheritdoc />
        public override bool Equals(object obj) => obj is ProjectConfig config && this.Equals(config);

        /// <summary>
        /// Determines whether this instance and another specified ProjectConfig struct have the same value.
        /// </summary>
        /// <param name="other">The ProjectConfig to compare to this instance.</param>
        /// <returns>
        /// True if the value of the value parameter is the same as the value of this instance otherwise, false.         
        /// </returns>
        public bool Equals(ProjectConfig other)
        {
            return this.HttpETag == other.HttpETag && this.JsonString == other.JsonString;
        }

        /// <inheritdoc />
        public override int GetHashCode()
        {
            unchecked
            {
                var hashCode = 1098790081;
                hashCode = hashCode * -1521134295 + base.GetHashCode();
                hashCode = hashCode * -1521134295 + EqualityComparer<string>.Default.GetHashCode(JsonString);
                hashCode = hashCode * -1521134295 + EqualityComparer<string>.Default.GetHashCode(HttpETag);

                return hashCode; 
            }
        }
    }
}