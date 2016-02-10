
# Testing with cURL

Note compression appears to be enabled by default on Azure Web Apps.

curl --header "Accept-Encoding:gzip" http://minimizerwebapplication.azurewebsites.net/registration/ravendb.client/index.json -o ravendb.client.json.gzip

curl http://minimizerwebapplication.azurewebsites.net/registration/ravendb.client/index.json -o ravendb.client.json

