// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

var har = require("capture-har");

module.exports = {
    generate: function(url, callback) {
        har({ 
                url: url 
            }, 
            {
                withContent: true
            })
            .then(har => {
                callback(null, har);
            });
    }
}