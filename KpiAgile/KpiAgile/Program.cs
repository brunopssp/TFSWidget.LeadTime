using Microsoft.TeamFoundation.WorkItemTracking.WebApi;
using Microsoft.TeamFoundation.WorkItemTracking.WebApi.Models;
using Microsoft.VisualStudio.Services.Client;
using Microsoft.VisualStudio.Services.Common;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace KpiAgile
{
    class Program
    {
        static void Main(string[] args)
        {
            SampleREST();
        }

        /// <summary>
        /// This sample creates a new work item query for New Bugs, stores it under 'MyQueries', runs the query, and then sends the results to the console.
        /// </summary>
        public static void SampleREST()
        {
            string collectionUri = "http://vsalm:8080/tfs/FabrikamFiberCollection";
            string teamProjectName = "FabrikamFiber";
            // Create a connection object, which we will use to get httpclient objects.  This is more robust
            // then newing up httpclient objects directly.  Be sure to send in the full collection uri.
            // For example:  http://myserver:8080/tfs/defaultcollection
            // We are using default VssCredentials which uses NTLM against a Team Foundation Server.  See additional provided
            // examples for creating credentials for other types of authentication.
            VssConnection connection = new VssConnection(new Uri(collectionUri), new VssCredentials());

            // Create instance of WorkItemTrackingHttpClient using VssConnection
            WorkItemTrackingHttpClient witClient = connection.GetClient<WorkItemTrackingHttpClient>();
            
            WorkItemQueryResult result = GetQuery(teamProjectName, witClient);
            if (result.WorkItems.Any())
            {
                List<WorkItem> workItems = witClient.GetWorkItemsAsync(result.WorkItems.Select(wir => wir.Id)).Result;
                List<int> intLead = new List<int>();
                foreach (WorkItem workItem in workItems)
                {
                    // write work item to console
                    List<WorkItem> workItemsRevisions = witClient.GetRevisionsAsync((int)workItem.Id).Result;
                    WorkItem workItemApproved = workItemsRevisions.Where(s => s.Fields["System.State"].Equals("Approved")).First();
                    if (workItemsRevisions.Where(s => s.Fields["System.State"].Equals("Done")).Count() > 0)
                    {
                        WorkItem workItemDone = workItemsRevisions.Where(s => s.Fields["System.State"].Equals("Done")).Last();
                        TimeSpan difDate = (DateTime)workItemDone.Fields["System.ChangedDate"] - (DateTime)workItemApproved.Fields["System.ChangedDate"];
                        intLead.Add(difDate.Days);

                        Console.WriteLine("{0} {1}", workItem.Id, workItem.Fields["System.Title"]);
                    }
                }
                if (intLead.Count > 0)
                {
                    Console.WriteLine("=======================================================");
                    Console.WriteLine("Lead Time (avg): {0} Days", intLead.Average()); ;
                }
            }
            else
            {
                Console.WriteLine("No work items were returned from query.");
            }
            Console.ReadLine();
        }

        private static WorkItemQueryResult GetQuery(string teamProjectName, WorkItemTrackingHttpClient witClient)
        {
            // Get 2 levels of query hierarchy items
            List<QueryHierarchyItem> queryHierarchyItems = witClient.GetQueriesAsync(teamProjectName, depth: 2).Result;

            // Search for 'My Queries' folder
            QueryHierarchyItem myQueriesFolder = queryHierarchyItems.FirstOrDefault(qhi => qhi.Name.Equals("My Queries"));
            if (myQueriesFolder != null)
            {

                string queryName = "LeadTime";

                // See if our 'REST Sample' query already exists under 'My Queries' folder.
                QueryHierarchyItem leadTimeQuery = null;
                if (myQueriesFolder.Children != null)
                {
                    leadTimeQuery = myQueriesFolder.Children.FirstOrDefault(qhi => qhi.Name.Equals(queryName));
                }

                // run the 'REST Sample' query
                return witClient.QueryByIdAsync(leadTimeQuery.Id).Result;
            }

            return new WorkItemQueryResult();

        }
    }
}

