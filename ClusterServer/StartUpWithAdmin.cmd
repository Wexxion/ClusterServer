﻿@echo off

cd bin\Debug

start ClusterServer.exe -p 8080 -n qqq -d 700 -a+
start ClusterServer.exe -p 8081 -n qqq -d 800 -a+
start ClusterServer.exe -p 8082 -n qqq -d 900 -a+
start ClusterServer.exe -p 8083 -n qqq -d 1000 -a+

cd ../..