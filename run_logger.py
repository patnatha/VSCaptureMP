import time
import os
import subprocess
import signal
import sys
import datetime
import requests

the_target = None
the_or = None
the_token = None
def load_token():
    global the_token, the_or, the_target
    try:
        curFilePath = os.path.dirname(os.path.abspath(__file__))
        tokenFilePath = os.path.join(curFilePath, "bin/Debug/net6.0/token.auth")
        f = open(tokenFilePath)
        for linenum, line in enumerate(f):
            if(linenum == 2):
                the_token = line.strip("\n")
            elif(linenum == 0):
                the_or = line.strip("\n")
            elif(linenum == 1):
                the_target = line.strip("\n")
        f.close()
    except Exception as err:
        print("ERROR, no token file", err)

load_token()

def time_since_most_recent():
    global the_token, the_or, the_target
    try:
        datetime5Min = datetime.datetime.now() - datetime.timedelta(minutes=5)
        datetime5Min = datetime5Min.strftime("%Y-%m-%d %H:%M:%S")
        print(datetime5Min) 
        data = {
        'token': the_token,
        'content': 'record',
        'action': 'export',
        'format': 'json',
        'type': 'flat',
        'csvDelimiter': '',
        'rawOrLabel': 'raw',
        'rawOrLabelHeaders': 'raw',
        'exportCheckboxLabel': 'false',
        'exportSurveyFields': 'false',
        'exportDataAccessGroups': 'false',
        'returnFormat': 'json',
        'filterLogic': '[or] = "' + the_or + '" AND [datetime] > "' + datetime5Min + '"'
        }
        r = requests.post(the_target,data=data)
        print('HTTP Status: ' + str(r.status_code))
        print(r.json())
    except Exception as err:
        print("Query REDCap Error", err)

time_since_most_recent()

intellivue_lan = None
intellivue_default_port = 24105
def scan_lan():
    devices = []
    for device in os.popen('fping -a -g 192.168.8.0/24 2> /dev/null'): devices.append(device)
    print("Scanning LAN, found " + str(len(devices)) + " devices")

    for ipaddr in devices:
        ipaddr = ipaddr.strip("\n")

        for line in os.popen('nc -v -u -w 3 ' + ipaddr + ' ' + 
                             str(intellivue_default_port) + ' 2>&1'):
            if("failed" in line or "timed out" in line):
                print(ipaddr, "Failed")
                continue
            elif("succeeded" in line):
                response = subprocess.run(["ping", "-c", "1", "-w2", 
                                            ipaddr], 
                                stdout=subprocess.DEVNULL).returncode
                if(response == 0):
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

