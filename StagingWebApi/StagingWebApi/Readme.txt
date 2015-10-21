
1) create a stage

curl -v -X POST -d "{\"ownerName\":\"tom\",\"stageName\":\"test\",\"baseService\":\"http://api.nuget.org/v3/index.json\"}" http://nugetpush.azurewebsites.net/create/stage

2) check we created it successfully

curl -v -X GET http://nugetpush.azurewebsites.net/stage/tom/test

3) and we can always delete the stage - but let's not do this rigth now!

curl -v -X DELETE http://nugetpush.azurewebsites.net/stage/tom/test

4) we can push a package to this stage

curl -v -X POST -T c:\data\nupkgs\entityframework.10.0.0.nupkg http://nugetpush.azurewebsites.net/create/package/tom/test

curl -v -X POST -T c:\data\nupkgs\zombie.1.0.1-beta.nupkg http://nugetpush.azurewebsites.net/create/package/tom/test

5) and now we have a package source we can use in Visual Studio

http://nugetpush.azurewebsites.net/stage/v3/tom/test/index.json

6) switch to this package source and we can search

 * we see EF version 10 merged in with the other results
 * we can search for and install Zombie 1.0.1-beta

7) lets take a quick look at some other results:

http://nugetpush.azurewebsites.net/v3/registration/tom/test/entityframework/index.json
http://nugetpush.azurewebsites.net/v3/registration/tom/test/zombie/index.json

http://nugetpush.azurewebsites.net/v3/query/tom/test?q=zombie&prerelease=true

http://nugetpush.azurewebsites.net/v3/flat/tom/test/entityframework/index.json
http://nugetpush.azurewebsites.net/v3/flat/tom/test/zombie/index.json
http://nugetpush.azurewebsites.net/v3/flat/tom/test/zombie/1.0.1-beta/zombie.nuspec
http://nugetpush.azurewebsites.net/v3/flat/tom/test/zombie/1.0.1-beta/zombie.1.0.1-beta.nupkg

