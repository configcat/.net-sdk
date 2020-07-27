using System.Collections.Generic;

namespace ConfigCat.Client
{
    /// <summary>
    /// Object for variation evaluation
    /// </summary>
    public class User
    {
        internal const string DefaultIdentifierValue = "";

        /// <summary>
        /// Unique identifier for the User or Session. e.g. Email address, Primary key, Session Id
        /// </summary>
        public string Identifier { get; private set; }

        /// <summary>
        /// Optional parameter for easier targeting rule definitions
        /// </summary>
        public string Email { get; set; }

        /// <summary>
        /// Optional parameter for easier targeting rule definitions
        /// </summary>
        public string Country { get; set; }

        /// <summary>
        /// Optional dictionary for custom attributes of the User for advanced targeting rule definitions. e.g. User role, Subscription type
        /// </summary>
        public IDictionary<string, string> Custom { get; set; }

        /// <summary>
        /// Serve all user attributes
        /// </summary>
        [Newtonsoft.Json.JsonIgnore]
        public IReadOnlyDictionary<string, string> AllAttributes
        {
            get
            {
                var result = new Dictionary<string, string>
                {
                    { nameof(Identifier), this.Identifier},
                    { nameof(Email), this.Email},
                    { nameof(Country),  this.Country},
                };

                if (Custom != null && Custom.Count > 0)
                {
                    foreach (var item in this.Custom)
                    {
                        if (item.Key == nameof(Identifier) ||
                            item.Key == nameof(Email) ||
                            item.Key == nameof(Country))
                        {
                            continue;
                        }

                        result.Add(item.Key, item.Value);
                    }
                }

                return result;
            }
        }

        /// <summary>
        /// Create an instance of User
        /// </summary>
        /// <param name="identifier">Unique identifier for the User</param>
        public User(string identifier)
        {
            this.Identifier = string.IsNullOrEmpty(identifier) ? DefaultIdentifierValue : identifier;
            this.Custom = new Dictionary<string, string>(0);
        }
    }
}