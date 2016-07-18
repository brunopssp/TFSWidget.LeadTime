VSS.init({
    explicitNotifyLoaded: true,
    usePlatformStyles: true
});

VSS.require(["TFS/Dashboards/WidgetHelpers", "TFS/WorkItemTracking/RestClient", "TFS/WorkItemTracking/Contracts"],
    function(WidgetHelpers, TFS_Wit_WebApi, TFS_contracts) {
        VSS.register("LeadTimeMetric.Configuration", function() {
            var $queryDropdown = $("#query-path-dropdown");

            return {
                load: function(widgetSettings, widgetConfigurationContext) {

                    var settings = JSON.parse(widgetSettings.customSettings.data);
                    if (settings && settings.queryPath) {
                        $queryDropdown.val(settings.queryPath);
                    }

                    var client = TFS_Wit_WebApi.getClient();
                    var projectId = VSS.getWebContext().project.id;
                    client.getQuery(projectId, "Shared Queries", TFS_contracts.QueryExpand.None, 2).then(queries => {
                        //Get query result
                        queries.children.forEach(rootFolderQuery => {
                            if (rootFolderQuery.hasChildren == undefined) {
                                $("<option>" + rootFolderQuery.path + "</option>").attr("value", rootFolderQuery.path).appendTo($queryDropdown);
                                $queryDropdown.val(settings.queryDropdown);
                            }
                            if (rootFolderQuery.hasChildren == true) {
                                rootFolderQuery.children.forEach(subFolderQuery => {
                                    if (subFolderQuery.hasChildren == undefined) {
                                        $("<option>" + subFolderQuery.path + "</option>").attr("value", subFolderQuery.path).appendTo($queryDropdown);
                                        $queryDropdown.val(settings.queryDropdown);
                                    }
                                });
                            }
                        });
                    });
                    //Enable Live Preview
                    $queryDropdown.on("change", function() {
                        var customSettings = {
                            data: JSON.stringify({
                                queryPath: $queryDropdown.val()
                            })
                        };
                        var eventName = WidgetHelpers.WidgetEvent.ConfigurationChange;
                        var eventArgs = WidgetHelpers.WidgetEvent.Args(customSettings);
                        widgetConfigurationContext.notify(eventName, eventArgs);
                    });
                    //^^^^^^
                    return WidgetHelpers.WidgetStatusHelper.Success();
                },
                function(error) {
                    return WidgetHelpers.WidgetStatusHelper.Failure(error.message);
                },
                onSave: function() {
                    var customSettings = {
                        data: JSON.stringify({
                            queryPath: $queryDropdown.val()
                        })
                    };
                    return WidgetHelpers.WidgetConfigurationSave.Valid(customSettings);
                }
            }
        });
        VSS.notifyLoadSucceeded();
    });