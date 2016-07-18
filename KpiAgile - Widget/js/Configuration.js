"use strict";

VSS.init({
    explicitNotifyLoaded: true,
    usePlatformStyles: true
});

VSS.require(["TFS/Dashboards/WidgetHelpers", "TFS/WorkItemTracking/RestClient", "TFS/WorkItemTracking/Contracts"], function (WidgetHelpers, TFS_Wit_WebApi, TFS_contracts) {
    VSS.register("LeadTimeMetric.Configuration", function () {
        var $queryDropdown = $("#query-path-dropdown");

        return {
            load: function load(widgetSettings, widgetConfigurationContext) {

                var settings = JSON.parse(widgetSettings.customSettings.data);
                if (settings && settings.queryPath) {
                    $queryDropdown.val(settings.queryPath);
                }

                var client = TFS_Wit_WebApi.getClient();
                var projectId = VSS.getWebContext().project.id;
                client.getQuery(projectId, "Shared Queries", TFS_contracts.QueryExpand.None, 2).then(function (queries) {
                    //Get query result

                    queries.children.forEach(function (element) {
                        if (element.hasChildren == undefined) {
                            $("<option>" + element.path + "</option>").attr("value", element.path).appendTo($queryDropdown);
                            console.log("Children: " + element.children);
                            console.log("hasChildren: " + element.hasChildren);
                            console.log("Querypath: " + element.path);
                            $queryDropdown.val(settings.queryDropdown);
                        }
                    });
                });

                //Enable Live Preview
                $queryDropdown.on("change", function () {
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
            function: function _function(error) {
                return WidgetHelpers.WidgetStatusHelper.Failure(error.message);
            },


            onSave: function onSave() {
                var customSettings = {
                    data: JSON.stringify({
                        queryPath: $queryDropdown.val()
                    })
                };
                return WidgetHelpers.WidgetConfigurationSave.Valid(customSettings);
            }
        };
    });
    VSS.notifyLoadSucceeded();
});
//# sourceMappingURL=Configuration.js.map