import time
import os
import subprocess
import signal
import sys

intellivue_lan = None
intellivue_default_port = 24105
def scan_lan():
    devices = []
    for device in os.popen('arp -a'): devices.append(device)
    print("Scanning LAN, found " + str(len(devices)) + " devices")

    for dev in devices:
        parseLine = dev.split(" ")
        if(len(parseLine) > 2):
            ipaddr = parseLine[1].strip(")").strip("(")
            
            for line in os.popen('nc -v -u -w 3 ' + ipaddr + ' ' + 
                                 str(intellivue_default_port) + ' 2>&1'):
                if("failed" in line or "timed out" in line):
                    print(ipaddr, "Failed")
                    continue
                elif("succeeded" in line):
                    print(ipaddr, "Success")
                    return ipaddr
                else:
                    print(ipaddr, "UNKNOWN RESP", line)

#these are constants to run the program
command_path = "/home/pi/Documents/VSCaptureMP/bin/Debug/net6.0"
commandPre = "dotnet VSCaptureMP.dll -mode 1 -port " 
commandPost = " -interval 3 -waveset 7 -scale 2 -export 2"

def is_logger_alive():
    result = subprocess.run(['ps','-aux'], stdout=subprocess.PIPE)
    for line in result.stdout.decode('utf-8').split('\n'):
        if("VSCaptureMP" in line and "run_logger.py" not in line):
            return True
    return False

def stop_logger():
    print("\tStopping Logger")
    os.system("ps -aux | grep dotnet | grep VSCapture | awk '{print $2}' | sudo xargs kill -9")

#Die with grace
def sighandler(signum, stack):
    print("Program Shutting Down")
    stop_logger()
    sys.exit(0)
signal.signal(signal.SIGINT, sighandler)
signal.signal(signal.SIGTERM, sighandler)

def start_logger():
    print("\tStarting Logger")
    os.system("cd " + command_path + "; " + commandPre + 
              intellivue_lan + commandPost + "&")

while True:
    #Check to see if monitor is up on the LAN
    intellivue_lan = scan_lan()

    #If monitor is up then start logger
    if(intellivue_lan != None):
        print("Host is up")
        if(not is_logger_alive()):
            start_logger()
        else:
            print("\tLogger is Alive")
    #If monitor is down then stop logger
    elif(intellivue_lan == None):
        print("Host is down")
        if(is_logger_alive()):
            stop_logger()
        else:
            print("\tLogger already dead")

    #Sleep for one minute
    time.sleep(60)

