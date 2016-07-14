"use strict";

var intLeadTime = new Array();
VSS.init({
    explicitNotifyLoaded: true,
    usePlatformStyles: true
});

function DaysBetween(date1, date2) {
    //Get 1 day in milliseconds
    var one_day = 1000 * 60 * 60 * 24;

    // Convert both dates to milliseconds
    var date1_ms = date1.getTime();
    var date2_ms = date2.getTime();

    // Calculate the difference in milliseconds
    var difference_ms = date2_ms - date1_ms;

    // Convert back to days and return
    return Math.round(difference_ms / one_day);
}

function ProcessRevisions(workItem) {

    var RevApproved = workItem.find(function (workItemRevision) {
        return workItemRevision.fields["System.State"] == "Approved";
    });

    var RevDone = workItem.find(function (workItemRevision) {
        return workItemRevision.fields["System.State"] == "Done";
    });

    var dateApproved = RevApproved != null && RevApproved.fields != undefined ? new Date(RevApproved.fields["System.ChangedDate"]) : new Date();
    var dateDone = RevDone != null && RevDone.fields != undefined ? new Date(RevDone.fields["System.ChangedDate"]) : new Date();

    intLeadTime.push(DaysBetween(dateApproved, dateDone));

    ShowResult();
}

function ShowResult() {
    var sum = 0;
    intLeadTime.forEach(function (item) {
        sum += item;
    });
    console.log(intLeadTime.length + '/' + sum);

    var avg = sum / intLeadTime.length;
    $('#query-info-container').empty().html("<strong>Lead Time Avg (Days):</strong> " + avg);
}

VSS.require(["TFS/Dashboards/WidgetHelpers", "TFS/WorkItemTracking/RestClient"], function (WidgetHelpers, TFS_Wit_WebApi) {
    WidgetHelpers.IncludeWidgetStyles();
    VSS.register("HelloWorldWidget2", function () {
        var getLeadTime = function getLeadTime(widgetSettings) {
            // Get a WIT client to make REST calls to VSTS
            var client = TFS_Wit_WebApi.getClient();
            var projectId = VSS.getWebContext().project.id;

            //Get a tfs query to get it's id
            return client.getQuery(projectId, "Shared Queries/Feedback").then(function (query) {
                //Get query result
                client.queryById(query.id).then(function (resultQuery) {
                    //ForEach workItem in query, get the respective Revision
                    resultQuery.workItems.forEach(function (workItem) {
                        client.getRevisions(workItem.id).then(ProcessRevisions);
                    });
                });
                return WidgetHelpers.WidgetStatusHelper.Success();
            }, function (error) {
                return WidgetHelpers.WidgetStatusHelper.Failure(error.message);
            });
        };

        return {
            load: function load(widgetSettings) {
                var $title = $('h2.title');
                $title.text('Hello World');

                return getLeadTime(widgetSettings);
            }
        };
    });
    VSS.notifyLoadSucceeded();
});

//# sourceMappingURL=KpiAgile.js.map