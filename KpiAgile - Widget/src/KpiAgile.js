var intLeadTime = new Array();
var countWorkItems = 0;
var settings = null;
var dtStartThroughput = new Date();
var dtEndThroughput = new Date(1969);
var client = null;

VSS.init({
    explicitNotifyLoaded: true,
    usePlatformStyles: true
});

VSS.require(["TFS/Dashboards/WidgetHelpers", "TFS/WorkItemTracking/RestClient"],
    function(WidgetHelpers, TFS_Wit_WebApi) {
        WidgetHelpers.IncludeWidgetStyles();
        VSS.register("LeadTimeMetric", function() {
            var getLeadTime = function(widgetSettings) {

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
                    $('h2.title').text("Lead Time");
                    $('#query-info-container').empty().text("");
                    $("<img></img>").attr("src", "img/loadingAnimation.gif").appendTo($('#query-info-container'));
                    $('#footer').empty().text("...");

                    //Get a tfs query to get it's id
                    return client.getQuery(projectId, settings.queryPath).then(query => {
                            //Get query result
                            client.queryById(query.id).then(ResultQuery,
                                function(error) {
                                    $('#error').text("There is an error in query " + settings.queryPath.substr(15) + ": " + error.message);
                                    return WidgetHelpers.WidgetStatusHelper.Failure(error.message);
                                });
                            return WidgetHelpers.WidgetStatusHelper.Success();
                        },
                        function(error) {
                            return WidgetHelpers.WidgetStatusHelper.Failure(error.message);
                        }
                    );
                }
            };

            return {
                load: function(widgetSettings) {
                    return getLeadTime(widgetSettings);
                },
                reload: function(widgetSettings) {
                    return getLeadTime(widgetSettings);
                }
            };
        });
        VSS.notifyLoadSucceeded();
    }
);

function ResultQuery(resultQuery) {

    //ForEach workItem in query, get the respective Revision
    if (resultQuery.queryType == 1) { //flat query
        countWorkItems = resultQuery.workItems.length;
        if (countWorkItems > 0) {
            resultQuery.workItems.forEach(workItem => {
                client.getRevisions(workItem.id).then(ProcessRevisions);
            });
        }
    } else {
        countWorkItems = resultQuery.workItemRelations.length;
        if (countWorkItems > 0) {
            resultQuery.workItemRelations.forEach(workItem => {
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

function ProcessRevisions(workItem) {

    if (workItem[workItem.length - 1].fields["System.State"] != "Done") {
        intLeadTime.push(1);
        return;
    }
    var RevApproved = workItem.find(workItemRevision => {
        return workItemRevision.fields["System.State"] == "Approved";
    });

    var RevDone = workItem.find(workItemRevision => {
        return workItemRevision.fields["System.State"] == "Done";
    });

    var dateApproved = (RevApproved != null && RevApproved.fields != undefined) ? new Date(RevApproved.fields["System.ChangedDate"]) : new Date();
    var dateDone = (RevDone != null && RevDone.fields != undefined) ? new Date(RevDone.fields["System.ChangedDate"]) : new Date();

    //Throughput - Range date
    if (dtStartThroughput > dateApproved) {
        dtStartThroughput = dateApproved;
    }
    if (dtEndThroughput < dateDone) {
        dtEndThroughput = dateDone;
    }

    intLeadTime.push(1);

    ShowResult();
}

function ShowResult() {
    if (countWorkItems == intLeadTime.length) {
        var tsIntervaloTotal = DaysBetween(dtStartThroughput, dtEndThroughput)

        $('#error').empty();
        $('h2.title').text(settings.queryPath.substr(15));


        if (settings.metric == "cycletime") {
            var cycleTime = (tsIntervaloTotal / intLeadTime.length);
            $('#query-info-container').empty().html(Math.round(cycleTime * 100) / 100);
            $('#footer').empty().text("(Cycle Time) Days per Item");
        } else if (settings.metric == "throughput") {
            // var throughput = (intLeadTime.length / tsIntervaloTotal);
            // $('#query-info-container').empty().html(Math.round(throughput * 100) / 100);
            // $('#footer').empty().text("(Throughput) Items per Day");
            var throughputPerWeek = (intLeadTime.length / (tsIntervaloTotal / 7));
            $('#query-info-container').empty().html(Math.round(throughputPerWeek * 100) / 100);
            $('#footer').empty().text("(Throughput) Items per Week");
        }
        //else if (settings.metric == "leadtime") {
        // var leadTime = (intLeadTime.length / tsIntervaloTotal);
        // $('#query-info-container').empty().html(Math.round(leadTime * 100) / 100);
        // $('#footer').empty().text("(Lead Time) Average in Days");
        //}

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