# unlist-redudant-packages

The tool generates a unlists redundant package id and versions.

After the tool has been run, every known package will: 

* Remove all prerelease versions if they are not higher than the latest stable version of the package id.
* Keep exactly 1 major/minor version. IE, if 6.2.0 and 6.2.1 are pushed, only 6.2.1 will remain.

Sample commandline arguments:

```console
NuGetReleaseTool.exe unlist-redundant-packages --api-key <API_KEY>
```

or 

```
NuGetReleaseTool.exe unlist-redundant-packages --dry-run
```


## *Always do a dry run first*

Figure out how many packages are going to be unlisted. You may hit throttling limits if you are unlisting a lot of packages.