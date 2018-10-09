using System.Collections.Generic;
using System.Linq;

namespace ConfigCat.Client.Evaluate
{
    /// <summary>
    /// Object for variation evaluation
    /// </summary>
    public class User
    {
        /// <summary>
        /// Unique identifier for the User or Session. e.g. Email address, Primary key, Session Id
        /// </summary>
        public string Id { get; private set; }

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
        public IReadOnlyDictionary<string, string> AllAttributes
        {
            get
            {
                var result = new Dictionary<string, string>
                {
                    { nameof(Id).ToLowerInvariant(), this.Id},
                    { nameof(Email).ToLowerInvariant(), this.Email},
                    { nameof(Country).ToLowerInvariant(),  this.Country},
                };

                if (Custom != null && Custom.Count > 0)
                {
                    foreach (var item in this.Custom)
                    {
                        if (item.Key.ToLowerInvariant() == nameof(Id).ToLowerInvariant() ||
                            item.Key.ToLowerInvariant() == nameof(Email).ToLowerInvariant() ||
                            item.Key.ToLowerInvariant() == nameof(Country).ToLowerInvariant())
                        {
                            continue;
                        }

                        result.Add(item.Key.ToLowerInvariant(), item.Value);
                    }
                }

                return result;
            }
        }

        /// <summary>
        /// Create an instance of User
        /// </summary>
        /// <param name="id">Unique identifier for the User</param>
        public User(string id)
        {
            this.Id = id;
        }
    }
}