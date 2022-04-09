import time
import os
import subprocess
import signal
import sys

intellivue_lan = "192.168.8.217"

#these are constants to run the program
command_path = "/home/pi/Documents/VSCaptureMP/bin/Debug/net6.0"
command = "dotnet VSCaptureMP.dll -mode 1 -port " + intellivue_lan +" -interval 3 -waveset 7 -scale 2 -export 2"

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
    os.system("cd " + command_path + "; " + command + "&")

while True:
    #Check to see if monitor is on
    response = subprocess.run(["ping", "-c", "1", "-w2", intellivue_lan], 
                stdout=subprocess.DEVNULL).returncode
    if(response == 0):
        print("Host is up")
        if(not is_logger_alive()):
            start_logger()
        else:
            print("\tLogger is Alive")
    elif(response == 1):
        print("Host is down")
        if(is_logger_alive()):
            stop_logger()
        else:
            print("\tLogger already dead")

    #Sleep for one minute
    time.sleep(60)

