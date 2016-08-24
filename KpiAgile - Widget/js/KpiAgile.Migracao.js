"use strict";

var intLeadTime = new Array();
var nWIP = new Array();
var nTotalPaginas = new Array();
var countWorkItems = 0;
var settings = null;
var dtStartThroughput = new Date();
var dtEndThroughput = new Date(1969);
var client = null;
var fieldName = "System.BoardColumn";
var strStartStage = "1.Cópia e Ajustes";
var strLastStage = "5.Disponibilizado Para Homologação UAT";

VSS.init({
    explicitNotifyLoaded: true,
    usePlatformStyles: true
});

VSS.require(["TFS/Dashboards/WidgetHelpers", "TFS/WorkItemTracking/RestClient"], function (WidgetHelpers, TFS_Wit_WebApi) {
    WidgetHelpers.IncludeWidgetStyles();
    VSS.register("LeadTimeMetric", function () {
        var getLeadTime = function getLeadTime(widgetSettings) {

            // Get a WIT client to make REST calls to VSTS
            client = TFS_Wit_WebApi.getClient();
            var projectId = VSS.getWebContext().project.id;
            settings = JSON.parse(widgetSettings.customSettings.data);
            if (!settings || !settings.queryPath) {
                $('#query-info-container').empty().text("0");
                $('#footer').empty().text("Please configure a query path");
                return WidgetHelpers.WidgetStatusHelper.Success();
            }
            if (WidgetHelpers.WidgetEvent.ConfigurationChange) {
                $('#error').empty();
                $('h2.title').text("");
                $('#query-info-container').empty().text("");
                $('#widget').css('background-color', 'rgb(0, 0, 0)');
                $("<img></img>").attr("src", "img/loadingAnimation.gif").appendTo($('#query-info-container'));
                $('#footer').empty().text("");

                //Get a tfs query to get it's id
                return client.getQuery(projectId, settings.queryPath).then(function (query) {
                    //Get query result
                    client.queryById(query.id).then(ResultQuery, function (error) {
                        $('#error').text("There is an error in query " + settings.queryPath.substr(15) + ": " + error.message);
                        return WidgetHelpers.WidgetStatusHelper.Failure(error.message);
                    });
                    return WidgetHelpers.WidgetStatusHelper.Success();
                }, function (error) {
                    return WidgetHelpers.WidgetStatusHelper.Failure(error.message);
                });
            }
        };

        return {
            load: function load(widgetSettings) {
                return getLeadTime(widgetSettings);
            },
            reload: function reload(widgetSettings) {
                return getLeadTime(widgetSettings);
            }
        };
    });
    VSS.notifyLoadSucceeded();
});

function ResultQuery(resultQuery) {

    //ForEach workItem in query, get the respective Revision
    intLeadTime = new Array();
    if (resultQuery.queryType == 1) {
        //flat query
        countWorkItems = resultQuery.workItems.length;
        if (countWorkItems > 0) {
            resultQuery.workItems.forEach(function (workItem) {
                //Validations
                if (workItem.fields["System.State"] == "New") {
                    return;
                }
                client.getRevisions(workItem.id).then(ProcessRevisions);
            });
        }
    } else {
        countWorkItems = resultQuery.workItemRelations.length;
        if (countWorkItems > 0) {
            resultQuery.workItemRelations.forEach(function (workItem) {
                client.getRevisions(workItem.target.id).then(ProcessRevisions);
            });
        }
    }
    if (countWorkItems == 0) {
        $('#error').empty();
        $('h2.title').text(settings.queryPath.substr(15));
        $('#query-info-container').empty().text("-");
        $('#footer').empty().text("This query does not return any work items");
        return WidgetHelpers.WidgetStatusHelper.Success();
    }
}

function ProcessRevisions(revisions) {

    //Count WIP
    if (revisions[revisions.length].fields[fieldName] != strLastStage && revisions.some(function (s) {
        return s.Key == "Microsoft.VSTS.Scheduling.OriginalEstimate";
    })) {
        nWIP.push(revisions[revisions.length].fields["Microsoft.VSTS.Scheduling.OriginalEstimate"]);
    }

    //Validations
    if (revisions.some(function (s) {
        return s.Fields[fieldName] != strStartStage || s.Fields[fieldName] != strStartStage.Remove(0, 2);
    })) //Valida se o PBI passou pelo stage Inicial
        return;
    if (revisions.some(function (s) {
        return s.Fields[fieldName] != strLastStage || s.Fields[fieldName] != strLastStage.Remove(0, 2);
    })) //Valida se o PBI chegou no stage Final
        return;
    if (revisions[revisions.length].fields[fieldName] == strStartStage || revisions[revisions.length].fields[fieldName] == strStartStage.Remove(0, 2)) //Valida se o PBI voltou ao stage inicial
        return;
    //Validations^^^^^^^^

    var RevApproved = revisions.find(function (workItemRevision) {
        return workItemRevision.fields[fieldName] == strStartStage || workItemRevision.fields[fieldName] == strStartStage.Remove(0, 2);
    });

    var RevDone = revisions.find(function (workItemRevision) {
        return workItemRevision.fields[fieldName] == strLastStage || workItemRevision.fields[fieldName] == strLastStage.Remove(0, 2);
    });

    var dateApproved = RevApproved != null && RevApproved.fields != undefined ? new Date(RevApproved.fields["System.ChangedDate"]) : new Date();
    var dateDone = RevDone != null && RevDone.fields != undefined ? new Date(RevDone.fields["System.ChangedDate"]) : new Date();

    //Throughput - Range date
    if (dtStartThroughput > dateApproved) {
        dtStartThroughput = dateApproved;
    }
    if (dtEndThroughput < dateDone) {
        dtEndThroughput = dateDone;
    }

    intLeadTime.push(1);
    nTotalPaginas.push(revisions[revisions.length].fields["Microsoft.VSTS.Scheduling.OriginalEstimate"]);

    ShowResult();
}

function ShowResult() {
    if (countWorkItems == intLeadTime.length) {
        var tsIntervaloTotal = DaysBetween(dtStartThroughput, dtEndThroughput);

        $('#error').empty();
        $('h2.title').text(settings.queryPath.substr(15));
        $('#widget').css({ 'color': 'white', 'background-color': 'rgb(0, 156, 204)' });

        var sumWIP = 0;
        nWIP.forEach(function (item) {
            sumWIP += item;
        });
        var sumPag = 0;
        nTotalPaginas.forEach(function (item) {
            sumPag += item;
        });
        var throughput = sumPag / tsIntervaloTotal;
        if (settings.metric == "throughput") {
            $('#query-info-container').empty().html(Math.round(throughput * 10) / 10);
            $('#footer').empty().text("(Throughput) Items by Day");
        } else if (settings.metric == "leadtime") {
            var leadTime = sumWIP / throughput; //---"WIP * CycleTime" ou "WIP / Throughput
            $('#query-info-container').empty().html(Math.round(leadTime * 10) / 10);
            $('#footer').empty().text("(Lead Time) Estimate in Days");
        }
    }
}

function DaysBetween(date1, date2) {
    //Get 1 day in milliseconds
    var one_day = 1000 * 60 * 60 * 24;

    // Convert both dates to milliseconds
    var date1_ms = date1.getTime();
    var date2_ms = date2.getTime(); //

    // Calculate the difference in milliseconds
    var difference_ms = date2_ms - date1_ms;

    // Convert back to days and return
    return Math.round(difference_ms / one_day);
}
//# sourceMappingURL=KpiAgile.Migracao.js.map