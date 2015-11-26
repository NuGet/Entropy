
(function () {
    var app = angular.module('myApp', []);
    app.controller('myCtrl', function ($scope, $http) {

        var address = 'http://localhost:1500/stage/tom';

        $http.get(address).
            success(function (data, status, headers, config) {
                $scope.owner = data;
                addMethods($http, address, $scope.owner);
            }).
            error(function (data, status, headers, config) {
                // log error
            });
    });
})();

var addMethods = function ($http, baseAddress, owner) {
    for (var i = 0; i < owner.stages.length; i += 1) {
        var stage = owner.stages[i];
        stage.commit = createCommitFunction($http, baseAddress + stage['@id']);
        stage.delete = createDeleteFunction($http, baseAddress + stage['@id']);
        for (var j = 0; j < stage.packages.length; j += 1) {
            var package = stage.packages[j];
            package.delete = createDeleteFunction($http, baseAddress + package['@id']);
            for (var k = 0; k < package.versions.length; k += 1) {
                var packageVersion = package.versions[k];
                packageVersion.delete = createDeleteFunction($http, baseAddress + packageVersion['@id']);
                packageVersion.show = createShowFunction($http, stage.sources.v3, package.id, packageVersion);
            }
        }
    }
}

var createCommitFunction = function ($http, address) {
    return function () {
        alert('commit(' + address + ') is very much forbidden');
    }
}

var createDeleteFunction = function ($http, address) {
    return function () {
        $http.delete(address);
    }
}

var createShowFunction = function ($http, source, id, packageVersion) {

    return function () {

        if (packageVersion.description) {
            packageVersion.showDetails = true;
            return;
        }

        $http.get(source)
            .success(function (data, status, headers, config) {
                var registrationBaseAddress = '';

                for (var i = 0; i < data.resources.length; i += 1) {
                    var resource = data.resources[i];
                    if (resource['@type'] === 'RegistrationsBaseUrl/3.0.0-beta') {
                        registrationBaseAddress = resource['@id'];
                        break;
                    }
                }

                var registrationAddress = registrationBaseAddress + id.toLowerCase() + '/index.json';
                $http.get(registrationAddress)
                    .success(function (data, status, headers, config) {
                        var catalogEntry;
                        for (var i = 0; i < data.items.length; i += 1) {
                            for (var j = 0; j < data.items[i].items.length; j += 1) {
                                var catalogEntry = data.items[i].items[j].catalogEntry;
                                if (catalogEntry.version.toLowerCase() === packageVersion.version.toLowerCase()) {
                                    break;
                                }
                            }
                        }

                        packageVersion.description = catalogEntry.description;
                        packageVersion.authors = catalogEntry.authors;
                        packageVersion.tags = JSON.stringify(catalogEntry.tags);
                        packageVersion.showDetails = true;
                    });
            });
    }
}
