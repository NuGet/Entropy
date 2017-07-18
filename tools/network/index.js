// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

var Sync = require("sync");
var fs = require("fs");
var Machine = require("./machine");
var HarCapture = require("./capture-har");
var Process = require("./process");
var NodeZip = require("node-zip");

var API_DOMAIN = "api.nuget.org";
var INDEX_URL = "https://" + API_DOMAIN + "/v3/index.json";
var ZIP_NAME = "nuget-network-diagnostics.zip";
var OUTPUT_FILE = "ConnectivityTrace.txt";
var HAR_FILE = "api.nuget.org.har";

Sync(function () {
    try {
        var result = {};
        var time = new Date();
        result["Timestamp"] = time.toISOString();

        console.log("Gathering machine details...");
        result["Machine"] = Machine.details();

        // Get trace route for api.nuget.org
        console.log("Running traceroute against " + API_DOMAIN + "...");
        result["Traceroute"] = Process.execute.sync(null, "tracert " + API_DOMAIN);

        // Get pathping for api.nuget.org, this is same as getting MTR logs
        console.log("Running pathping for " + API_DOMAIN + "... This may take upto 5 minutes...");
        result["PathPing"] = Process.execute.sync(null, "pathping " + API_DOMAIN);

        // Collect HAR file for index page.
        console.log("Capturing HAR for " + INDEX_URL + "...");
        var har = HarCapture.generate.sync(null, INDEX_URL);

        // Write the output files, zip them up, send an email with this zip attached to support@nuget.org
        var zip = new NodeZip();
        zip.file(HAR_FILE, JSON.stringify(har, null, 2));

        var out = processJsonToText(result);
        zip.file(OUTPUT_FILE, out);

        var data = zip.generate({base64: false, compression: "DEFLATE"});
        fs.writeFileSync(ZIP_NAME, data, "binary");
    } catch (exception) {
        console.log("Error occurred: " + exception);
    }
});

function processJsonToText(json) {
    var output = "";
    for (var key in json) {
        switch (key) {
            case "Traceroute":
                output += key + ": " + "\r\n";
                output += json[key] + "\r\n";
                break;
            case "PathPing":
                output += key + "/MTR: " + "\r\n";
                output += json[key] + "\r\n";
                break;
            case "Machine":
                var details = processJsonToText(json[key]);
                output += "Machine Details:" + "\r\n";
                output += details + "\r\n";
                break;
            default:
                output += key + ": " + json[key] + "\r\n";
        }
    }

    return output;
}
