// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

var os = require("os");

module.exports = {
    details: function () {
        var machine = {};
        machine["OS Platform"] = os.platform();
        machine["OS Architecture"] = os.arch();
        machine["OS Release"] + os.release();
        machine["OS Type"] = os.type();

        var interfaces = os.networkInterfaces();
        var addresses = [];
        for (var k in interfaces) {
            for (var k2 in interfaces[k]) {
                var address = interfaces[k][k2];
                if (address.family === 'IPv4' && !address.internal) {
                    addresses.push(address.address);
                }
            }
        }

        machine["IP Address"] = addresses;

        return machine;
    }
}