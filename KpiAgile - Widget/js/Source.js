var intLeadTime = new Array();
var count = 0;
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

    var RevApproved = workItem.find(workItemRevision => {
        return workItemRevision.fields["System.State"] == "Approved";
    });

    var RevDone = workItem.find(workItemRevision => {
        return workItemRevision.fields["System.State"] == "Done";
    });

    var dateApproved = (RevApproved != null && RevApproved.fields != undefined) ? new Date(RevApproved.fields["System.ChangedDate"]) : new Date();
    var dateDone = (RevDone != null && RevDone.fields != undefined) ? new Date(RevDone.fields["System.ChangedDate"]) : new Date();

    intLeadTime.push(DaysBetween(dateApproved, dateDone));

    ShowResult();
}

function ShowResult() {
    var sum = 0;
    intLeadTime.forEach(item => {
        sum += item;
    });
    var avg = (sum / intLeadTime.length);
    console.log("Count: " + count);
    console.log("intLeadTime.length: " + intLeadTime.length);

    if (count == intLeadTime.length) {
        $('#query-info-container').empty().html(Math.round(avg * 10) / 10);
    }
}

VSS.require(["TFS/Dashboards/WidgetHelpers", "TFS/WorkItemTracking/RestClient"],
    function(WidgetHelpers, TFS_Wit_WebApi) {
        WidgetHelpers.IncludeWidgetStyles();
        VSS.register("LeadTimeMetric", function() {
            var getLeadTime = function(widgetSettings) {
                // Get a WIT client to make REST calls to VSTS
                var client = TFS_Wit_WebApi.getClient();
                var projectId = VSS.getWebContext().project.id;

                //Get a tfs query to get it's id
                return client.getQuery(projectId, "Shared Queries/Feedback").then(query => {
                        //Get query result
                        client.queryById(query.id).then(resultQuery => {
                            //ForEach workItem in query, get the respective Revision
                            count = resultQuery.workItems.length;
                            resultQuery.workItems.forEach(workItem => {
                                client.getRevisions(workItem.id).then(ProcessRevisions);
                            });
                        });
                        return WidgetHelpers.WidgetStatusHelper.Success();
                    },
                    function(error) {
                        return WidgetHelpers.WidgetStatusHelper.Failure(error.message);
                    });
            };

            return {
                load: function(widgetSettings) {
                    // var $title = $('h2.title');
                    // $title.text('Lead Time');
                    return getLeadTime(widgetSettings);
                }
            };
        });
        VSS.notifyLoadSucceeded();
    }
);