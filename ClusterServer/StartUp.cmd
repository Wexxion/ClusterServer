@echo off

cd bin\Debug

start ClusterServer.exe -p 8080 -n qqq -d 0 -a+
start ClusterServer.exe -p 8081 -n qqq -d 1000 -a+
start ClusterServer.exe -p 8082 -n qqq -d 2900 -a+
start ClusterServer.exe -p 8083 -n qqq -d 3100 -a+

cd ../..