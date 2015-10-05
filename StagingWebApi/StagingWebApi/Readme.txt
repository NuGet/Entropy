
Example curl commands used for testing:

curl -v -X POST -d "{\"ownerName\":\"tom\",\"stageName\":\"test\",\"baseService\":\"http://api.nuget.org/v3/index.json\"}" http://localhost:1500/create

curl -v -X GET http://localhost:1500/stage/tom/test

curl -v -X DELETE http://localhost:1500/stage/tom/test

curl -v -X POST -T c:\data\nupkgs\entityframework.5.0.0.nupkg http://localhost:1500/upload/tom/test

curl -v -X GET http://localhost:1500/stage/tom/test/entityframework/5.0.0

and v3 feed...

curl -v -X GET http://localhost:1500/stage/v3/tom/test/index.json

and a batch...

curl -v -X POST -T c:\data\nupkgs\dotnetzip.1.9.6.nupkg http://localhost:1500/upload/tom/test
curl -v -X POST -T c:\data\nupkgs\entityframework.5.0.0.nupkg http://localhost:1500/upload/tom/test
curl -v -X POST -T c:\data\nupkgs\entityframework.6.0.0-beta1.nupkg http://localhost:1500/upload/tom/test
curl -v -X POST -T c:\data\nupkgs\entityframework.6.1.3.nupkg http://localhost:1500/upload/tom/test
curl -v -X POST -T c:\data\nupkgs\entityframework.7.0.0-beta4.nupkg http://localhost:1500/upload/tom/test
curl -v -X POST -T c:\data\nupkgs\microsoft.aspnet.mvc.6.0.0-beta7.nupkg http://localhost:1500/upload/tom/test
curl -v -X POST -T c:\data\nupkgs\newtonsoft.json.5.0.2.nupkg http://localhost:1500/upload/tom/test
curl -v -X POST -T c:\data\nupkgs\newtonsoft.json.6.0.3.nupkg http://localhost:1500/upload/tom/test
curl -v -X POST -T c:\data\nupkgs\newtonsoft.json.6.0.8.nupkg http://localhost:1500/upload/tom/test
curl -v -X POST -T c:\data\nupkgs\newtonsoft.json.7.0.1.nupkg http://localhost:1500/upload/tom/test

and no longer used...

curl -v -X PUT -d "[]" http://localhost:1500/stage/tom/test

