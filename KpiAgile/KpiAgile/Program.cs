using Microsoft.TeamFoundation.WorkItemTracking.WebApi;
using Microsoft.TeamFoundation.WorkItemTracking.WebApi.Models;
using Microsoft.VisualStudio.Services.Client;
using Microsoft.VisualStudio.Services.Common;
using System;
using System.Collections.Generic;
using System.Linq;


namespace KpiAgile
{
    static class Program
    {

        private static string _teamProjectName = "PortalTjsp";
        //private static string _teamProjectName = "Institucionais";
        private static string _collectionUri = "http://tfs.tjsp.jus.br:8080/tfs/SistemasTJSP";

        static void Main()
        {
            string strStartStage = "1.Cópia e Ajustes";
            //string strSecondStage = "2.Implementar Design";
            //string strThirdStage = "3.Publicado em Dev";
            //string strFouthStage = "4.Validação de Conteúdo";
            string strLastStage = "5.Disponibilizado Para Homologação UAT";
            //string strIsWeStart = "Committed";
            //string strIsWeEnd = "Done";

            #region PorCiclo
            //Console.WriteLine("Selecione uma opção para medir o Cycle Time e Throughput:");
            //Console.WriteLine("{0} De: \"{1}\" Até: \"{2}\"", " [1]", strStartStage, strLastStage);
            //Console.WriteLine("{0} De: \"{1}\" Até: \"{2}\"", " [2]", strStartStage, strSecondStage);
            //Console.WriteLine("{0} De: \"{1}\" Até: \"{2}\"", " [3]", strStartStage, strThirdStage);
            //Console.WriteLine("{0} De: \"{1}\" Até: \"{2}\"", " [4]", strStartStage, strFouthStage);
            //Console.WriteLine("{0} De: \"{1}\" Até: \"{2}\"", " [5]", strSecondStage, strThirdStage);
            //Console.WriteLine("{0} \"{1}\"", " [6]", strStartStage);
            //Console.WriteLine("{0} \"{1}\"", " [7]", strSecondStage);
            //Console.WriteLine("{0} \"{1}\"", " [8]", strFouthStage);
            //Console.WriteLine("{0} De: \"{1}\" Até: \"{2}\"", " [9]", strIsWeStart, strIsWeEnd);

            //string strOpt = Console.ReadLine();

            //switch (strOpt)
            //{
            //    case "1":
            CycleTime(strStartStage, strLastStage, false);
            //        break;
            //    case "2":
            //        CycleTime(strStartStage, strSecondStage, false);
            //        break;
            //    case "3":
            //        CycleTime(strStartStage, strThirdStage, false);
            //        break;
            //    case "4":
            //        CycleTime(strStartStage, strFouthStage, false);
            //        break;
            //    case "5":
            //        CycleTime(strSecondStage, strThirdStage, false);
            //        break;
            //    case "6":
            //        CycleTime(strStartStage, strStartStage, true);
            //        break;
            //    case "7":
            //        CycleTime(strSecondStage, strSecondStage, true);
            //        break;
            //    case "8":
            //        CycleTime(strFouthStage, strFouthStage, true);
            //        break;
            //    case "9":
            //CycleTimeGeral(strIsWeStart, strIsWeEnd, false);
            //        break;
            //    default:
            //        Console.WriteLine("Opção Invalida!");
            //        Console.ReadLine();
            //        break;
            //}
            #endregion
        }

        public static void CycleTime(string strStartStage, string strLastStage, bool itemDone)
        {
            WorkItemTrackingHttpClient witClient;
            WorkItemQueryResult queryResult = GetQueryResult(out witClient, "Páginas Migração");
            //WorkItemQueryResult queryResult = GetQueryResult(out witClient, "Força Tarefa", "Páginas Estáticas");
            if (queryResult.WorkItems.Any())
            {
                List<WorkItem> workItems = witClient.GetWorkItemsAsync(queryResult.WorkItems.Select(wir => wir.Id)).Result;
                List<double> nTotalPaginas = new List<double>();
                List<double> nWIP = new List<double>();

                string fieldName = "System.BoardColumn";

                DateTime dtStartThroughput = DateTime.Today;
                DateTime dtEndThroughput = new DateTime();
                foreach (WorkItem workItem in workItems)
                {
                    if (workItem.Fields[fieldName].Equals("New"))
                        continue;

                    #region Throughput - WIP

                    if (!workItem.Fields[fieldName].Equals(strLastStage) && workItem.Fields.Any(s => s.Key.Equals("Microsoft.VSTS.Scheduling.OriginalEstimate")))
                    {
                        double nPaginasWIP = Convert.ToDouble(workItem.Fields["Microsoft.VSTS.Scheduling.OriginalEstimate"]);
                        nWIP.Add(nPaginasWIP);//Throughput
                    }
                    
                    #endregion

                    List<WorkItem> workItemsRevisions = witClient.GetRevisionsAsync((int)workItem.Id).Result;

                    #region Validacoes Stages
                   
                    if (!workItemsRevisions.Any(s => s.Fields[fieldName].Equals(strStartStage) || s.Fields[fieldName].Equals(strStartStage.Remove(0, 2))))//Valida se o PBI passou pelo stage Inicial
                        continue;
                    if (!workItemsRevisions.Any(s => s.Fields[fieldName].Equals(strLastStage) || s.Fields[fieldName].Equals(strLastStage.Remove(0, 2))))//Valida se o PBI chegou no stage Final
                        continue;
                    if (!itemDone && workItemsRevisions.Last().Fields[fieldName].ToString() == strStartStage || workItemsRevisions.Last().Fields[fieldName].ToString() == strStartStage.Remove(0, 2))//Valida se o PBI voltou ao stage inicial
                        continue;
                    #endregion

                    WorkItem workItemApproved = workItemsRevisions.First(s => s.Fields[fieldName].Equals(strStartStage) || s.Fields[fieldName].Equals(strStartStage.Remove(0, 2)));
                    WorkItem workItemDone = workItemsRevisions.First(s => s.Fields[fieldName].Equals(strLastStage) || s.Fields[fieldName].Equals(strLastStage.Remove(0, 2)));//Retorna a primeira revision que entrou no stage

                    #region Throughput - Range date

                    DateTime dtStart = (DateTime)workItemApproved.Fields["System.ChangedDate"];
                    DateTime dtEnd = (DateTime)workItemDone.Fields["System.ChangedDate"];

                    //excluir PBIS prontos fora do intervalo
                    //if (dtEnd < new DateTime(2016, 8, 8) || dtEnd > new DateTime(2016, 8, 11))

                    //if (dtEnd > new DateTime(2016, 8, 13))
                    //    continue;

                    if (dtStartThroughput > dtStart)
                        dtStartThroughput = dtStart;
                    if (dtEndThroughput < dtEnd)
                        dtEndThroughput = dtEnd;

                    #endregion

                    #region Specific Cycle Stage

                    if (itemDone && workItemDone.Fields["System.BoardColumnDone"] == null)
                        continue;
                    #endregion

                    double nPaginas = Convert.ToDouble(workItem.Fields["Microsoft.VSTS.Scheduling.OriginalEstimate"]);
                    nTotalPaginas.Add(nPaginas);

                    Console.WriteLine("{0} {1}", workItem.Id, workItem.Fields["System.Title"]);
                }

                #region Summary

                Console.WriteLine("=======================================================");
                Console.WriteLine("Quantidade de PBIs Prontos: {0}", nTotalPaginas.Count);
                Console.WriteLine("Quantidade de Páginas Prontas: {0}", nTotalPaginas.Sum());
                if (!itemDone)
                    Console.WriteLine("Quantidade de Páginas Em Progresso: {0}", nWIP.Sum());

                //Intervalo Total
                TimeSpan difDateThroughput = dtEndThroughput - dtStartThroughput;
                DateTimeOffset dtNow = DateTimeOffset.Now;
                TimeSpan tsIntervaloTotal = DateTimeOffset.Now.AddHours(difDateThroughput.TotalHours - GetNumberOfWorkingHours(dtStartThroughput, dtEndThroughput)) - dtNow;

                Console.WriteLine("Intervalo Total: {0}", tsIntervaloTotal.ToString());

                //Throughput
                Console.WriteLine("(Throughput) -> Média de {0} páginas por dia", Math.Round(nTotalPaginas.Sum() / tsIntervaloTotal.TotalDays, 2)); // TotalDeItens(Paginas) / TempoTotal

                //CycleTime
                double dCycleTime = tsIntervaloTotal.TotalDays / nTotalPaginas.Sum();
                TimeSpan tsTempoCycleTime = DateTimeOffset.Now.AddDays(Math.Round(dCycleTime, 2)) - dtNow;
                Console.WriteLine("(CycleTime) -> Média de tempo gasto por PBI: {0}", tsTempoCycleTime.ToString());// ----TempoTotal / TotalDeItens(Paginas)
                if (!itemDone)
                {
                    TimeSpan tsTempoLeadTime = DateTimeOffset.Now.AddDays(Math.Round(nWIP.Sum() * dCycleTime, 2)) - dtNow;
                    Console.WriteLine("(LeadTime) -> Tempo que uma página leva para ficar pronta: {0}", tsTempoLeadTime.ToString());//Media Geral ---"WIP * CycleTime" ou "WIP / Throughput
                }
                #endregion
            }
            else
            {
                Console.WriteLine("No work items were returned from query.");
            }
            Console.ReadLine();
            Console.Clear();
            Program.Main();
        }
        public static void CycleTimeGeral(string strStartStage, string strLastStage, bool itemDone)
        {
            WorkItemTrackingHttpClient witClient;
            WorkItemQueryResult queryResult = GetQueryResult(out witClient, "IsWe - Velocidade");
            if (queryResult.WorkItems.Any())
            {
                List<WorkItem> workItems = witClient.GetWorkItemsAsync(queryResult.WorkItems.Select(wir => wir.Id)).Result;
                List<double> nTotalPaginas = new List<double>();
                List<double> nWIP = new List<double>();

                string fieldName = "System.State";

                DateTime dtStartThroughput = DateTime.Today;
                DateTime dtEndThroughput = new DateTime();
                foreach (WorkItem workItem in workItems)
                {

                    #region Throughput - WIP

                    if (!workItem.Fields[fieldName].Equals(strLastStage))
                    {
                        nWIP.Add(1);//Throughput
                    }

                    #endregion

                    List<WorkItem> workItemsRevisions = witClient.GetRevisionsAsync((int)workItem.Id).Result;

                    #region Validacoes Stages
                    if (workItem.Fields[fieldName].Equals("New"))
                        continue;
                    if (!workItemsRevisions.Any(s => s.Fields[fieldName].Equals(strStartStage) || s.Fields[fieldName].Equals(strStartStage.Remove(0, 2))))//Valida se o PBI passou pelo stage Inicial
                        continue;
                    if (!workItemsRevisions.Any(s => s.Fields[fieldName].Equals(strLastStage) || s.Fields[fieldName].Equals(strLastStage.Remove(0, 2))))//Valida se o PBI chegou no stage Final
                        continue;
                    if (!itemDone && workItemsRevisions.Last().Fields[fieldName].ToString() == strStartStage || workItemsRevisions.Last().Fields[fieldName].ToString() == strStartStage.Remove(0, 2))//Valida se o PBI voltou ao stage inicial
                        continue;
                    #endregion

                    WorkItem workItemApproved = workItemsRevisions.Last(s => s.Fields[fieldName].Equals(strStartStage) || s.Fields[fieldName].Equals(strStartStage.Remove(0, 2)));
                    WorkItem workItemDone = workItemsRevisions.Last(s => s.Fields[fieldName].Equals(strLastStage) || s.Fields[fieldName].Equals(strLastStage.Remove(0, 2)));//Retorna a primeira revision que entrou no stage inicial

                    #region Throughput - Range date

                    DateTime dtStart = (DateTime)workItemApproved.Fields["System.ChangedDate"];
                    DateTime dtEnd = (DateTime)workItemDone.Fields["Microsoft.VSTS.Common.ClosedDate"];

                    //excluir PBIS prontos fora do intervalo
                    //if (dtEnd < new DateTime(2016, 7, 1) || dtEnd > new DateTime(2016, 8, 1))
                    //    continue;

                    if (dtStartThroughput > dtStart)
                        dtStartThroughput = dtStart;
                    if (dtEndThroughput < dtEnd)
                        dtEndThroughput = dtEnd;

                    #endregion

                    #region Specific Cycle Stage

                    //if (itemDone && workItemDone.Fields["System.BoardColumnDone"] == null)
                    //    continue;
                    #endregion

                    nTotalPaginas.Add(1);

                    Console.WriteLine("{0} {1}", workItem.Id, workItem.Fields["System.Title"]);
                }

                #region Summary

                Console.WriteLine("=======================================================");
                Console.WriteLine("Quantidade de PBIs Prontos: {0}", nTotalPaginas.Count);
                if (!itemDone)
                    Console.WriteLine("Quantidade de Páginas Em Progresso: {0}", nWIP.Sum());

                //Intervalo Total
                TimeSpan difDateThroughput = dtEndThroughput - dtStartThroughput;
                DateTimeOffset dtNow = DateTimeOffset.Now;

                Console.WriteLine("Intervalo Total: {0}", difDateThroughput.ToString());

                //Throughput
                Console.WriteLine("(Throughput) -> Média de {0} PBI por dia", Math.Round((nTotalPaginas.Sum() / (difDateThroughput.TotalDays)), 2)); // TotalDeItens(Paginas) / TempoTotal

                //CycleTime
                TimeSpan tsTempoCycleTime = DateTimeOffset.Now.AddDays(Math.Round(difDateThroughput.TotalDays / nTotalPaginas.Sum(), 2)) - dtNow;
                Console.WriteLine("(CycleTime) -> Média de tempo gasto por PBI: {0}", tsTempoCycleTime.ToString());// ----TempoTotal / TotalDeItens(Paginas)
                if (!itemDone)
                {
                    tsTempoCycleTime = DateTimeOffset.Now.AddDays(Math.Round(nWIP.Sum() * tsTempoCycleTime.TotalDays, 2)) - dtNow;
                    Console.WriteLine("(LeadTime) -> Tempo que um PBI leva para ficar pronto: {0}", tsTempoCycleTime.ToString());//Media Geral ---"WIP * CycleTime" ou "WIP / Throughput
                }
                #endregion
            }
            else
            {
                Console.WriteLine("No work items were returned from query.");
            }
            Console.ReadLine();
            Console.Clear();
            Program.Main();
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
            QueryHierarchyItem myQueriesFolder = queryHierarchyItems.FirstOrDefault(qhi => qhi.Name.Equals("My Queries"));
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

