VSS.init({
    explicitNotifyLoaded: true,
    usePlatformStyles: true
});

VSS.require(["TFS/Dashboards/WidgetHelpers", "TFS/WorkItemTracking/RestClient"],
    function(WidgetHelpers, TFS_Wit_WebApi) {
        VSS.register("LeadTimeMetric.Configuration", function() {
            var $queryDropdown = $("#query-path-dropdown");
            var getLeadTimeConfig = function(widgetSettings) {
                var client = TFS_Wit_WebApi.getClient();
                var projectId = VSS.getWebContext().project.id;
                return client.getQueries(projectId).then(queries => {
                        //Get query result
                        queries.children.forEach(element => {
                            $("<option>" + element.path + "</option>").attr("value", element.path).appendTo($queryDropdown);

                            console.log("Querypath: " + element.path);
                            $queryDropdown.val(settings.queryDropdown)
                        });

                        return WidgetHelpers.WidgetStatusHelper.Success();
                    },
                    function(error) {
                        return WidgetHelpers.WidgetStatusHelper.Failure(error.message);
                    }
                );
            };
            return {
                load: function(widgetSettings, widgetConfigurationContext) {

                    var settings = JSON.parse(widgetSettings.customSettings.data);
                    if (settings && settings.queryPath) {
                        $queryDropdown.val(settings.queryPath);
                    }
                    //Enable Live Preview
                    // $queryDropdown.on("change", function() {
                    //     var customSettings = {
                    //         data: JSON.stringify({
                    //             queryPath: $queryDropdown.val()
                    //         })
                    //     };
                    //     var eventName = WidgetHelpers.WidgetEvent.ConfigurationChange;
                    //     var eventArgs = WidgetHelpers.WidgetEvent.Args(customSettings);
                    //     widgetConfigurationContext.notify(eventName, eventArgs);
                    // });
                    //^^^^^^
                    return getLeadTimeConfig(widgetSettings);
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