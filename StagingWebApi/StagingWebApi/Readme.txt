
<<<<<<< HEAD
=======
To set things up:
- git clone and build
- you can run locally but you need a SQL database (sorry) (for example in Azure but local is also fine) and an Azure Storage Account
- before anything will work we must add the tables to the database and the stored procs (get over it)

0) create an owner

curl -X POST -d "{\"ownerName\":\"tom\"}" http://localhost:1500/create/owner

NOTE this will return an ApiKey which you will need later - query your DB if you need a reminder

>>>>>>> 15898dffd7c655c67c3d2a9a02c8142b328fef7d
1) create a stage

curl -v -X POST -d "{\"ownerName\":\"tom\",\"stageName\":\"test\",\"baseService\":\"http://api.nuget.org/v3/index.json\"}" http://nugetpush.azurewebsites.net/create/stage

2) check we created it successfully

<<<<<<< HEAD
curl -v -X GET http://nugetpush.azurewebsites.net/stage/tom/test

3) and we can always delete the stage - but let's not do this rigth now!

curl -v -X DELETE http://nugetpush.azurewebsites.net/stage/tom/test

4) we can push a package to this stage

curl -v -X POST -T c:\data\nupkgs\entityframework.10.0.0.nupkg http://nugetpush.azurewebsites.net/create/package/tom/test

curl -v -X POST -T c:\data\nupkgs\zombie.1.0.1-beta.nupkg http://nugetpush.azurewebsites.net/create/package/tom/test

5) and now we have a package source we can use in Visual Studio

http://nugetpush.azurewebsites.net/stage/v3/tom/test/index.json

6) switch to this package source and we can search
=======
curl -v -X GET http://localhost:1500/stage/tom/test

3) and we can always delete the stage - but let's not do this right now!

curl -v -X DELETE http://localhost:1500/stage/tom/test

4) we can push a package to this stage using the regular nuget.exe and the source that now exists

BUT NOTE the user with this ApiKey must be a co-owner of this stage

nuget push c:\data\nupkgs\Manhattan.1.0.0-beta.nupkg -Source http://localhost:1500/push/package/tom/test -ApiKey 772F6D7F-59D1-43AE-B111-91EDB5396D01

5) and now we have a package source we can use in Visual Studio

http://localhost:1500/stage/v3/tom/test/index.json

6) add a co-owner (of the stage) now dick can push packages

curl -X POST -d "{\"@id\":\"stage/tom/test\",\"ownerName\":\"dick\"}" http://localhost:1500/add

7) switch to this package source and we can search
>>>>>>> 15898dffd7c655c67c3d2a9a02c8142b328fef7d

 * we see EF version 10 merged in with the other results
 * we can search for and install Zombie 1.0.1-beta

<<<<<<< HEAD
7) lets take a quick look at some other results:
=======
8) handy web page (we can make this pretty - basically this page is a view on the state of an owner's staging areas [hardcode to tom!!!])

http://localhost:1500/content/display.html


Lets take a quick look at some other results:
>>>>>>> 15898dffd7c655c67c3d2a9a02c8142b328fef7d

http://nugetpush.azurewebsites.net/v3/registration/tom/test/entityframework/index.json
http://nugetpush.azurewebsites.net/v3/registration/tom/test/zombie/index.json

http://nugetpush.azurewebsites.net/v3/query/tom/test?q=zombie&prerelease=true

http://nugetpush.azurewebsites.net/v3/flat/tom/test/entityframework/index.json
http://nugetpush.azurewebsites.net/v3/flat/tom/test/zombie/index.json
http://nugetpush.azurewebsites.net/v3/flat/tom/test/zombie/1.0.1-beta/zombie.nuspec
http://nugetpush.azurewebsites.net/v3/flat/tom/test/zombie/1.0.1-beta/zombie.1.0.1-beta.nupkg

