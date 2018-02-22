@echo off

cd bin\Debug

start ClusterServer.exe -p 8080 -n qqq -d 2500 -a-
start ClusterServer.exe -p 8081 -n qqq -d 2600 -a-
start ClusterServer.exe -p 8082 -n qqq -d 2700 -a-
start ClusterServer.exe -p 8083 -n qqq -d 800 -a-

cd ../..