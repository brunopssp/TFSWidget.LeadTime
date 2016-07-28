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

        private static string _teamProjectName = "PortalTjsp";
        private static string _collectionUri = "http://tfs.tjsp.jus.br:8080/tfs/SistemasTJSP";

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
            Console.WriteLine("De: {0} Até: {1}", strSecondStage, strThirdStage + " [4]");
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
            WorkItemQueryResult result = GetQueryResult(out witClient, "Força Tarefa", "Páginas Estáticas");
            if (result.WorkItems.Any())
            {
                List<WorkItem> workItems = witClient.GetWorkItemsAsync(result.WorkItems.Select(wir => wir.Id)).Result;
                //--Regra Geral--//
                //List<decimal> decTempoPorPBI = new List<decimal>();
                //--------------//
                #region Migração
                List<decimal> decPaginasPorDia = new List<decimal>();
                List<decimal> decTempoPorPagina = new List<decimal>();
                List<decimal> nTotalPaginas = new List<decimal>();
                #endregion
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

                                #region Migração
                                //--Regra Migração--//
                                decPaginasPorDia.Add(nPaginas / (horasUteis / 24));
                                decTempoPorPagina.Add(horasUteis / nPaginas);
                                nTotalPaginas.Add(nPaginas);
                                ///-------------///
                                #endregion

                                #region Regra Geral
                                //--Regra Geral--//
                                //decimal horasUteis = ((Convert.ToDecimal(difDate.TotalHours)) - GetNumberOfWorkingHours(dtStart, dtEnd));
                                //decTempoPorPBI.Add(horasUteis);
                                ///-------------///
                                #endregion

                                Console.WriteLine("{0} {1}", workItem.Id, workItem.Fields["System.Title"]);
                            }
                        }
                    }
                }
                #region Regra Geral
                //--Regra Geral--//
                //TimeSpan tsTempoPorPBI = DateTimeOffset.Now.AddHours(Convert.ToDouble(Math.Round(decTempoPorPBI.Average(), 2))) - DateTimeOffset.Now;
                //Console.WriteLine("=======================================================");
                //Console.WriteLine("De: {0} Até: {1}", startFieldValue, endFieldValue);
                //Console.WriteLine("Quantidade de PBIs: {0}", decTempoPorPBI.Count);//Throughput
                //Console.WriteLine("Média de tempo gasto por PBI: {0}", tsTempoPorPBI.ToString());//CycleTime
                ///-------------///
                #endregion
                #region Migração
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
                #endregion
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

        private static WorkItemQueryResult GetQueryResult(out WorkItemTrackingHttpClient witClient, string queryNameOrSubFolder, string queryName = "")
        {
            VssConnection connection = new VssConnection(new Uri(_collectionUri), new VssCredentials());

            // Create instance of WorkItemTrackingHttpClient using VssConnection
            witClient = connection.GetClient<WorkItemTrackingHttpClient>();

            // Get 2 levels of query hierarchy items
            List<QueryHierarchyItem> queryHierarchyItems = witClient.GetQueriesAsync(_teamProjectName, depth: 2).Result;

            // Search for 'Shared Queries' folder
            QueryHierarchyItem myQueriesFolder = queryHierarchyItems.FirstOrDefault(qhi => qhi.Name.Equals("Shared Queries"));
            if (myQueriesFolder != null)
            {
                QueryHierarchyItem leadTimeQuery = null;
                if (myQueriesFolder.Children != null)
                {
                    leadTimeQuery = myQueriesFolder.Children.FirstOrDefault(qhi => qhi.Name.Equals(queryNameOrSubFolder));
                    if (leadTimeQuery.HasChildren == true)
                    {
                        leadTimeQuery = leadTimeQuery.Children.FirstOrDefault(qhi => qhi.Name.Equals(queryName));
                    }
                }

                return witClient.QueryByIdAsync(leadTimeQuery.Id).Result;
            }

            return new WorkItemQueryResult();

        }
    }
}

