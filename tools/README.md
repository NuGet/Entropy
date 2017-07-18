# Engineering
This is a repo to track engineering work items for the NuGet team

## Network connectivity diagnostics tools
This tool runs network diagnostics against api.nuget.org endpoint. It captures the traceroute, pathping, HAR file and zips it up to be sent to nuget server team for diagnosis.

### Installation
- Install the latest version of [nodejs](https://nodejs.org/en/download/).

### Build and Run
- After cloning this repository do `cd network`
- `npm install`
- To run the scripts: `npm start` or `node index.js`

### Generating distributable binaries(windows only for now)
- We use a node package called "pkg" to build an executable 
- Run the npm exe script to generate the binaries 
    ```
    npm run exe
    ```
    This will take care of installing `pkg` and creating the executable in the `binaries` folder. This script will also copy the native dependency into the `binaries` folder.

- Zip up the binaries folder and upload it to the distribution channel
- Test your changes by extracting the zip file in a separate folder(not your current development folder) and running the exe.