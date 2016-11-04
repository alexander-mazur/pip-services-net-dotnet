using System.Collections.Generic;
using PipServices.Commons.Config;
using PipServices.Commons.Data;

namespace PipServices.Net.Connect
{
    public sealed class ConnectionParams : ConfigParams
    {
        private static long SerialVersionUid { get; }= 5769508200513539527L;

        /**
         * Creates an empty instance of connection parameters.
         */
        public ConnectionParams() { }

        /**
         * Create an instance of service address with free-form configuration map.
         * @param content a map with the address configuration parameters. 
         */
        public ConnectionParams(IDictionary<string, object> map)
            : base(map)
        {
        }

        /**
         * Checks if discovery registration or resolution shall be performed.
         * The discovery is requested when 'discover' parameter contains 
         * a non-empty string that represents the discovery name.
         * @return <b>true</b> if the address shall be handled by discovery 
         * and <b>false</b> when all address parameters are defined statically.
         */
        public bool UseDiscovery => ContainsKey("discovery_key");

        /**
         * Key under which the connection shall be registered or resolved by discovery service. 
         */
        public string DiscoveryKey
        {
            get { return GetAsNullableString("discovery_key"); }
            set { this["discovery_key"] = value; }
        }

        /**
         * Gets or sets the connection protocol
         */

        public string Protocol
        {
            get
            {
                return GetAsNullableString("protocol") ?? "http";
            }
            set { this["protocol"] = value; }
        }

        /**
         * Gets or sets the connection protocol
         * @param defaultValue the default protocol
         * @return the connection protocol
         */
        public string GetProtocol(string defaultValue)
        {
            return GetAsStringWithDefault("protocol", defaultValue);
        }

        /**
         * Gets or sets the service host name or IP address.
         */
        public string Host
        {
            get
            {
                var host = GetAsNullableString("host");
                host = host ?? GetAsNullableString("ip");
                return string.IsNullOrWhiteSpace(host) ? "localhost" : host;
            }
            set
            {
                this["host"] = value;
            }
        }

        /**
         * Gets or sets the service port number
         */
        public int Port
        {
            get { return GetAsInteger("port"); }
            set { SetAsObject("port", value); }
        }

        /**
         * Gets the endpoint uri constructed from protocol, host and port
         * @return uri as <protocol>://<host | ip>:<port>
         */
        public string Uri => Protocol + "://" + Host + ":" + Port;

        public new static ConnectionParams FromString(string line)
        {
            var map = StringValueMap.FromString(line);
            return new ConnectionParams(map);
        }
    }
}
