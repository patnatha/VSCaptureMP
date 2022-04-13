import time
import os
import subprocess
import signal
import sys
import datetime
import requests

#Variables for checking against redcap database
last_start = None #time since last start
time_delay = 1.5 * 60 #1.5 minutes to seconds
the_target = None #redcap endpoint
the_or = None #the or from configuration file
the_token = None #the redcap token
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

#load the tokens
load_token()

#Check to see if any records been posted in the recent past
def time_since_most_recent():
    global the_token, the_or, the_target
    try:
        datetimebackMin = datetime.datetime.now() - datetime.timedelta(seconds=time_delay)
        datetimebackMin = datetimebackMin.strftime("%Y-%m-%d %H:%M:%S")
        #print(datetimebackMin)
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
        'filterLogic': '[or] = "' + the_or + '" AND [datetime] > "' 
            + datetimebackMin + '"'
        }
        r = requests.post(the_target,data=data)
        #print(r.status_code)
        #print(r.json())
        if(r.status_code == 200):
            #Parse the response if the server is working
            if(len(r.json()) == 0):
                return True
            else:
                return False
        else:
            return False
    except Exception as err:
        #Defaults to true
        print("Query REDCap Error", err)
        return False

#Scan the lan and find the intellivue
intellivue_lan = None
intellivue_default_port = 24105
def scan_lan():
    devices = ['192.168.8.217'] #hard code and set on the managed router
    print("Scanning LAN, found " + str(len(devices)) + " devices")

    for ipaddr in devices:
        ipaddr = ipaddr.strip("\n")

        #Scan for the open port with netcat
        for line in os.popen('nc -v -u -w 3 ' + ipaddr + ' ' + 
                             str(intellivue_default_port) + ' 2>&1'):
            if("failed" in line or "timed out" in line):
                print("\t", ipaddr, "Failed")
                continue
            elif("succeeded" in line):
                response = subprocess.run(["ping", "-c", "1", "-w2", 
                                            ipaddr], 
                                stdout=subprocess.DEVNULL).returncode
                if(response == 0):
                    print("\t", ipaddr, "Success") 
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
    global last_start
    last_start = datetime.datetime.now()
    print("\tStarting Logger")
    os.system("cd " + command_path + "; " + commandPre + 
              intellivue_lan + commandPost + "&")

while True:
    #Print the current time 
    print("=====", datetime.datetime.now().strftime("%Y-%m-%d %H:%M:%S"), "=====")
    
    #Get the intellivue_lan port
    intellivue_lan = scan_lan()

    #If monitor is up then start logger
    if(intellivue_lan != None):
        print("Host is up")
        if(not is_logger_alive()):
            start_logger()
        else:
            print("\tLogger is Alive")
            
            #Check the database to make sure it is still posting records
            if(time_since_most_recent() and last_start != None and
                    (datetime.datetime.now() - last_start).seconds > time_delay):
                print("\tBut hasn't recorded anything :(")
                stop_logger()
                start_logger()

    #If monitor is down then stop logger
    elif(intellivue_lan == None):
        print("Host is down")
        if(is_logger_alive()):
            stop_logger()
        else:
            print("\tLogger already dead")

    #Sleep for a bit
    time.sleep(30)

