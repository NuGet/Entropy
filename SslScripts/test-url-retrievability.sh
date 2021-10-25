#!/usr/bin/env bash

# This script repeatedly requests a URL and logs failures with full curl verbose output

#url="https://api.nuget.org/v3/registration5-gz-semver2/system.io.packaging/5.0.0.json"
url="https://dist.nuget.org/tools.json"

while true
do
        output=`curl -fvso /dev/null --connect-to dist.nuget.org:443:az320820.vo.msecnd.net:443 $url 2>&1`
        if [ "$?" -ne "0" ]
        then
                filename=`date +%Y-%m-%d-%H-%M-%S.txt`
                echo "Failed. Writing $filename file"
                echo "$output" >$filename
                echo >>$filename
                #echo | openssl s_client -showcerts -servername api.nuget.org -connect api.nuget.org:443 >>$filename
        else
                echo -n "."
        fi

        #sleep 15
done
