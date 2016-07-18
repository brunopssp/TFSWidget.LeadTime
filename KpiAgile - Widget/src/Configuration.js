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
                        queries.forEach(element => {
                            var option = document.createElement("option");
                            option.text = element.path;
                            $queryDropdown.add(option);
                            console.log("Option: " + option);
                            console.log(element.path);
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