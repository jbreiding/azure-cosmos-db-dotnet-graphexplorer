namespace GraphExplorer.Configuration
{
	/// <summary>
	/// Represents a collection of configuration settings for Gremlin connection
	/// </summary>
	public class GremlinConfig
	{
        public string Endpoint { get; set; }
        public int Port { get; set; }
        public string AuthKey { get; set; }
        public string Database { get; set; }
    }
}