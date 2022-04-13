#!/bin/bash

theVal=`ps -aux | grep python3 | grep run_logger | awk '{print $2 ": " $11 " " $12}'`
echo $theVal

theVal=`ps -aux | grep dotnet | grep VSCapture | awk '{print $2 ": " $11 " " $12}'`
echo $theVal

