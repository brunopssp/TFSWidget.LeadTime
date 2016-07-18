"use strict";

VSS.init({
    explicitNotifyLoaded: true,
    usePlatformStyles: true
});

VSS.require("TFS/Dashboards/WidgetHelpers", function (WidgetHelpers) {
    VSS.register("LeadTimeMetric.Configuration", function () {
        var $queryDropdown = $("#query-path-dropdown");

        return {
            load: function load(widgetSettings, widgetConfigurationContext) {

                var settings = JSON.parse(widgetSettings.customSettings.data);
                if (settings && settings.queryPath) {
                    $queryDropdown.val(settings.queryPath);
                }
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