This is the home for a modifation of the VSCaptyreMP by xenofusion (https://github.com/xeonfusion/VSCaptureMP). We are using this program which was originally developed using C# .NET6 and we modified using MAC OSX and then deployed to a Raspberry PI 4 for contunous data siphoning from an intellivue MP 90 machine and real time logging to our instiutions RedCap Instance.

In the home directoy of thte executable (bin/Debug/net6.0) the program will look for a "token.auth" file for how to connect to the RedCap database. The format of the file is as follows.
First Line: OR identified, we use 1/2/3
Second Line: RedCap API Token
Third Line: url to end point for POST commands
