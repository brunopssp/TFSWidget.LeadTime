"use strict";

var queryDropdown = "#query-path-dropdown";
var optionsMetric = "input[type='radio'][name='radio']:checked";;

var settings = null;

VSS.init({
    explicitNotifyLoaded: true,
    usePlatformStyles: true
});

VSS.require(["TFS/Dashboards/WidgetHelpers", "TFS/WorkItemTracking/RestClient", "TFS/WorkItemTracking/Contracts"], function (WidgetHelpers, TFS_Wit_WebApi, TFS_contracts) {
    VSS.register("LeadTimeMetric.Configuration", function () {
        return {
            load: function load(widgetSettings, widgetConfigurationContext) {
                settings = JSON.parse(widgetSettings.customSettings.data);
                if (settings && settings.queryPath && settings.metric) {
                    $(queryDropdown).val(settings.queryPath);
                    if (settings.metric == "throughput") $("input[name=radio]")[0].checked = true;else if (settings.metric == "cycletime") $("input[name=radio]")[1].checked = true;
                }

                TFS_Wit_WebApi.getClient().getQuery(VSS.getWebContext().project.id, "Shared Queries", TFS_contracts.QueryExpand.None, 2).then(getListQueries);

                //Enable Live Preview
                $(queryDropdown).on("change", function () {
                    var customSettings = {
                        data: JSON.stringify({
                            queryPath: $(queryDropdown).val(),
                            metric: $(optionsMetric, "#optionsMetric").val()
                        })
                    };
                    var eventName = WidgetHelpers.WidgetEvent.ConfigurationChange;
                    var eventArgs = WidgetHelpers.WidgetEvent.Args(customSettings);
                    widgetConfigurationContext.notify(eventName, eventArgs);
                });
                $("#optionsMetric input").on("change", function () {
                    var customSettings = {
                        data: JSON.stringify({
                            queryPath: $(queryDropdown).val(),
                            metric: $(optionsMetric, "#optionsMetric").val()
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
                        queryPath: $(queryDropdown).val(),
                        metric: $(optionsMetric, "#optionsMetric").val()
                    })
                };
                return WidgetHelpers.WidgetConfigurationSave.Valid(customSettings);
            }
        };
    });
    VSS.notifyLoadSucceeded();
});

function getListQueries(queries) {
    //Get query result
    queries.children.forEach(function (rootFolderQuery) {
        if (rootFolderQuery.hasChildren == undefined) {
            setDropDownList(rootFolderQuery);
        }
        if (rootFolderQuery.hasChildren == true) {
            rootFolderQuery.children.forEach(function (subFolderQuery) {
                if (subFolderQuery.hasChildren == undefined) {
                    setDropDownList(subFolderQuery);
                }
            });
        }
    });
}

function setDropDownList(rootFolderQuery) {
    //Set results to DropDownList
    $("<option>" + rootFolderQuery.path + "</option>").attr("value", rootFolderQuery.path).appendTo($(queryDropdown));
    $(queryDropdown).val(settings.queryPath);
}
//# sourceMappingURL=Configuration.js.map