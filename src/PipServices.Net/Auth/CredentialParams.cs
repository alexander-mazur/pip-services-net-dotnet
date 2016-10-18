using System.Collections.Generic;
using PipServices.Commons.Config;
using PipServices.Commons.Data;

namespace PipServices.Net.Auth
{
    /**
     * Credentials such as login and password, client id and key,
     * certificates, etc. Separating credentials from connection parameters
     * allow to store them in secure location and share among multiple
     * connections.
     */
    public sealed class CredentialParams : ConfigParams
    {
        private static long SerialVersionUid { get; } = 4144579662501676747L;

        /**
         * Creates an empty instance of credential parameters.
         */

        public CredentialParams()
        {
        }

        /**
         * Create an instance of credentials from free-form configuration map.
         * @param content a map with the credentials. 
         */
        public CredentialParams(IDictionary<string, object> map)
            : base(map)
        {
        }

        /**
         * Checks if credential lookup shall be performed.
         * The credentials are requested when 'store_key' parameter contains 
         * a non-empty string that represents the name in credential store.
         * @return <b>true</b> if the credentials shall be resolved by credential store 
         * and <b>false</b> when all credential parameters are defined statically.
         */
        public bool UseCredentialStore() => ContainsKey("store_key");

        /**
         * Gets or sets the key under which the connection shall be looked up in credential store. 
         */
        public string StoreKey
        {
            get { return GetAsNullableString("store_key"); }
            set { this["store_key"] = value; }
        }

        /**
         * Gets or sets the user name / login.
         */
        public string Username
        {
            get { return GetAsNullableString("username"); }
            set { this["username"] = value; }
        }

        /**
         * Gets or sets the service user password.
         */
        public string Password
        {
            get { return GetAsNullableString("password"); }
            set { this["password"] = value; }
        }

        /**
         * Gets or sets the client or access id
         */
        public string AccessId
        {
            get
            {
                string accessId = GetAsNullableString("access_id");
                accessId = accessId ?? GetAsNullableString("client_id");
                return accessId;
            }
            set { this["access_id"] = value; }
        }

        /**
         * Gets or sets the client or access key
         */
        public string AccessKey
        {
            get
            {
                var accessKey = GetAsNullableString("access_key");
                accessKey = accessKey ?? GetAsNullableString("access_key");
                return accessKey;
            }
            set { this["access_key"] = value; }
        }

        public new static CredentialParams FromString(string line)
        {
            var map = StringValueMap.FromString(line);
            return new CredentialParams(map);
        }
    }
}
