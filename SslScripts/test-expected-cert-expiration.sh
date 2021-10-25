#!/usr/bin/env bash

# This script repeatedly connects to a host and check presented SSL certificate. If expiration does not match an expected
# expiration, it saves the certificate in a file

while true
do
    output=`echo | openssl s_client -showcerts -servername api.nuget.org -connect az320820.vo.msecnd.net:443 2>/dev/null`
    text=`echo "$output" | openssl x509 -inform pem -noout -text`
    if [[ $text =~ "Not After : Jul 29 22:49:43 2022 GMT" ]]
    then
        echo -n .
    else
        filename=`date +cert-%Y-%m-%d-%H-%M-%S.txt`
        echo "$output" >$filename
        echo "$filename"
    fi
    #sleep 15
done