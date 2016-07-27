using Microsoft.TeamFoundation.WorkItemTracking.WebApi;
using Microsoft.TeamFoundation.WorkItemTracking.WebApi.Models;
using Microsoft.VisualStudio.Services.Client;
using Microsoft.VisualStudio.Services.Common;
using System;
using System.Collections.Generic;
using System.Linq;


namespace KpiAgile
{
    class Program
    {
        static void Main(string[] args)
        {
            string strStartStage = "Cópia e Ajustes";
            string strSecondStage = "Implementar Design";
            string strThirdStage = "Publicado em Dev";

            string strLastStage = "Disponibilizado Para Homologação UAT";

            Console.WriteLine("Selecione uma opção para medir o Cycle Time:");
            Console.WriteLine("De: {0} Até: {1}", strStartStage, strSecondStage + " [1]");
            Console.WriteLine("De: {0} Até: {1}", strStartStage, strThirdStage + " [2]");
            Console.WriteLine("De: {0} Até: {1}", strStartStage, strLastStage + " [3]");
            Console.WriteLine("De: {0} Até: {1}", strSecondStage, strThirdStage+" [4]");
            string strOpt = Console.ReadLine();

            switch (strOpt)
            {
                case "1":
                    CycleTime(strStartStage, strSecondStage);
                    break;
                case "2":
                    CycleTime(strStartStage, strThirdStage);
                    break;
                case "3":
                    CycleTime(strStartStage, strLastStage);
                    break;
                case "4":
                    CycleTime(strSecondStage, strThirdStage);
                    break;
                default:
                    Console.WriteLine("Opção Invalida!");
                    Console.ReadLine();
                    break;
            }
        }

        public static void CycleTime(string startFieldValue, string endFieldValue)
        {
            WorkItemTrackingHttpClient witClient;
            WorkItemQueryResult result = GetQueryResult(out witClient);
            if (result.WorkItems.Any())
            {
                List<WorkItem> workItems = witClient.GetWorkItemsAsync(result.WorkItems.Select(wir => wir.Id)).Result;
                List<decimal> decPaginasPorDia = new List<decimal>();
                List<decimal> decTempoPorPagina = new List<decimal>();
                List<decimal> nTotalPaginas = new List<decimal>();

                string fieldName = "System.BoardColumn";

                foreach (WorkItem workItem in workItems)
                {

                    List<WorkItem> workItemsRevisions = witClient.GetRevisionsAsync((int)workItem.Id).Result;
                    if (workItemsRevisions.Where(s => s.Fields[fieldName].Equals(startFieldValue)).Count() > 0)
                    {
                        WorkItem workItemApproved = workItemsRevisions.Where(s => s.Fields[fieldName].Equals(startFieldValue)).First();
                        if (workItemsRevisions.Where(s => s.Fields[fieldName].Equals(endFieldValue)).Count() > 0)
                        {
                            if (workItemsRevisions.Last().Fields[fieldName].ToString() != startFieldValue)
                            {
                                WorkItem workItemDone = workItemsRevisions.Where(s => s.Fields[fieldName].Equals(endFieldValue)).First();//Retorna a primeira revision que entrou no field

                                ///Metricas--------
                                DateTime dtStart = (DateTime)workItemApproved.Fields["System.ChangedDate"];
                                DateTime dtEnd = (DateTime)workItemDone.Fields["System.ChangedDate"];

                                TimeSpan difDate = dtEnd - dtStart;

                                decimal horasUteis = ((Convert.ToDecimal(difDate.TotalHours)) - GetNumberOfWorkingHours(dtStart, dtEnd));
                                decimal nPaginas = Convert.ToDecimal(workItemsRevisions.Last().Fields["Microsoft.VSTS.Scheduling.OriginalEstimate"]);

                                decPaginasPorDia.Add(nPaginas / (horasUteis / 24));
                                decTempoPorPagina.Add(horasUteis / nPaginas);
                                ///------------------------------------///

                                nTotalPaginas.Add(nPaginas);
                                Console.WriteLine("{0} {1}", workItem.Id, workItem.Fields["System.Title"]);
                            }
                        }
                    }
                }
                if (decPaginasPorDia.Count > 0)
                {

                    TimeSpan tsTempoPorPagina = DateTimeOffset.Now.AddHours(Convert.ToDouble(Math.Round(decTempoPorPagina.Average(), 2))) - DateTimeOffset.Now;

                    Console.WriteLine("=======================================================");
                    Console.WriteLine("De: {0} Até: {1}", startFieldValue, endFieldValue);
                    Console.WriteLine("Quantidade de PBIs: {0}", decPaginasPorDia.Count);
                    Console.WriteLine("Quantidade de Páginas: {0}", nTotalPaginas.Sum());
                    Console.WriteLine("Média de tempo (em horas) gasto por página: {0}", tsTempoPorPagina.ToString());
                    Console.WriteLine("Média de {0} páginas por dia", Math.Round(decPaginasPorDia.Average(), 2));
                }
            }
            else
            {
                Console.WriteLine("No work items were returned from query.");
            }
            Console.ReadLine();
        }

        private static int GetNumberOfWorkingHours(DateTime start, DateTime stop)
        {
            int hour = 0;
            while (start <= stop)
            {
                if (start.DayOfWeek == DayOfWeek.Saturday || start.DayOfWeek == DayOfWeek.Sunday)
                {
                    hour += 24;
                }
                start = start.AddDays(1);
            }
            return hour;
        }

        private static WorkItemQueryResult GetQueryResult(out WorkItemTrackingHttpClient witClient)
        {
            //string collectionUri = "http://vsalm:8080/tfs/FabrikamFiberCollection";
            string collectionUri = "http://tfs.tjsp.jus.br:8080/tfs/SistemasTJSP";
            string teamProjectName = "PortalTjsp";
            //string teamProjectName = "FabrikamFiber";
            // Create a connection object, which we will use to get httpclient objects.  This is more robust
            // then newing up httpclient objects directly.  Be sure to send in the full collection uri.
            // For example:  http://myserver:8080/tfs/defaultcollection
            // We are using default VssCredentials which uses NTLM against a Team Foundation Server.  See additional provided
            // examples for creating credentials for other types of authentication.
            VssConnection connection = new VssConnection(new Uri(collectionUri), new VssCredentials());

            // Create instance of WorkItemTrackingHttpClient using VssConnection
            witClient = connection.GetClient<WorkItemTrackingHttpClient>();

            // WorkItemQueryResult result = GetQuery(teamProjectName, witClient);



            // Get 2 levels of query hierarchy items
            List<QueryHierarchyItem> queryHierarchyItems = witClient.GetQueriesAsync(teamProjectName, depth: 2).Result;

            // Search for 'My Queries' folder
            QueryHierarchyItem myQueriesFolder = queryHierarchyItems.FirstOrDefault(qhi => qhi.Name.Equals("Shared Queries"));
            if (myQueriesFolder != null)
            {

                string queryNameorFolder = "Força Tarefa";
                string queryName = "Páginas Estáticas";

                // See if our 'REST Sample' query already exists under 'My Queries' folder.
                QueryHierarchyItem leadTimeQuery = null;
                if (myQueriesFolder.Children != null)
                {
                    leadTimeQuery = myQueriesFolder.Children.FirstOrDefault(qhi => qhi.Name.Equals(queryNameorFolder));
                    if (leadTimeQuery.HasChildren == true)
                    {
                        leadTimeQuery = leadTimeQuery.Children.FirstOrDefault(qhi => qhi.Name.Equals(queryName));
                    }
                }

                // run the 'REST Sample' query
                return witClient.QueryByIdAsync(leadTimeQuery.Id).Result;
            }

            return new WorkItemQueryResult();

        }
    }
}

