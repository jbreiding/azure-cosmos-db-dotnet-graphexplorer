namespace GraphExplorer.Controllers
{
    using Gremlin.Net.Driver;
    using Gremlin.Net.Structure.IO.GraphSON;
    using Microsoft.Azure.Documents;
    using System.Linq;
    using GraphExplorer.Configuration;
    using System.Collections.Generic;
    using System.Threading.Tasks;
    using Microsoft.AspNetCore.Mvc;
    using Microsoft.Azure.Documents.Client;
    using System;
    using Microsoft.Extensions.Options;
    using Newtonsoft.Json;

    [Route("api/[controller]")]
    public class GremlinController : Controller
    {
        private readonly DocDbConfig dbConfig;
        private readonly GremlinConfig gremlinConfig;
        private readonly DocumentClient client;
        private GremlinClient gremlinClient;

        public GremlinController(IOptions<DocDbConfig> configSettings, IOptions<GremlinConfig> gremlinConfigSettings)
        {
            dbConfig = configSettings.Value;
            gremlinConfig = gremlinConfigSettings.Value;

            client = new DocumentClient(new Uri(dbConfig.Endpoint), dbConfig.AuthKey, new ConnectionPolicy { EnableEndpointDiscovery = false });
        }

        [HttpGet]
        public async Task<dynamic> Get(string query, string collectionId)
        {
            Database database = client.CreateDatabaseQuery("SELECT * FROM d WHERE d.id = \"" + dbConfig.Database + "\"").AsEnumerable().FirstOrDefault();
            List<DocumentCollection> collections = client.CreateDocumentCollectionQuery(database.SelfLink).ToList();
            DocumentCollection coll = collections.Where(x => x.Id == collectionId).FirstOrDefault();

            var tasks = new List<Task>();
            var results = new List<dynamic>();
            var queries = query.Split(';');

            //split query on ; to allow for multiple queries
            foreach (var q in queries)
            {
                if (!string.IsNullOrEmpty(q))
                {
                    var singleQuery = q.Trim();

                    await ExecuteQuery(coll, singleQuery)
                            .ContinueWith(
                                (task) =>
                                {
                                    results.Add(new { queryText = singleQuery, queryResult = task.Result });
                                }
                            );
                }
            }

            return results;
        }

        private async Task<IReadOnlyCollection<dynamic>> ExecuteQuery(DocumentCollection coll, string query)
        {
            if(null == gremlinClient)
            {
                var gremlinContext = new GremlinServer(
                        hostname: gremlinConfig.Endpoint,
                        port: gremlinConfig.Port,
                        enableSsl: true,
                        username: $"/coll.AltLink",
                        password: gremlinConfig.AuthKey);

                gremlinClient = new GremlinClient(
                    gremlinContext, 
                    new GraphSON2Reader(), 
                    new GraphSON2Writer(), 
                    GremlinClient.GraphSON2MimeType
                    );
            }

            var gremlinQuery = await gremlinClient.SubmitAsync<dynamic>(query);

            return gremlinQuery;
        }
    }
}