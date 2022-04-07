import os
devices = []
for device in os.popen('arp -a'): devices.append(device)

for dev in devices:
    parseLine = dev.split(" ")
    if(len(parseLine) > 2):
        ipaddr = parseLine[1].strip(")").strip("(")
        #print(ipaddr)

        for line in os.popen('nc -vz -G 3 ' + ipaddr + ' 24501 2>&1'):
            if("Sucess" in line):
                print(ipaddr + ": Port Open")
            else:
                print(line)
