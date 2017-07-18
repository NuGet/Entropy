// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

var process = require("child_process");

module.exports = {
    execute: function(command, callback) {
        process.exec(command, function (err, stdout, stderr) {
            var output = "";
            if (err) {
                output = "Error while running command: " + command + ". Error: " + err;
            } else {
                output = stdout;
            }

            callback(null, output);
        });
    }
}