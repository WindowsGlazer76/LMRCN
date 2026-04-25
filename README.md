# LMRCN
### Local Multiplatform Remote Control Network
**This is my first public .NET MAUI project designed to remote control Windows devices with other Windows or Android Devices within the same local network.**
I'm planing to implement remote controllability for Android devices in the future.
The reason why this feature doesn't exist yet, is that I ran into problems running the command server on Android, the command client on the other hand works flawlessly for some reason.
I've tried to run the command server as a foreground service but that also didn't work for me, if you have any idea on how to fix this issue, feel free to contact me!


## Important notes!
* All communications between the udp command server and client are currently unencypted!
* devices are currently only reachable within the same subnet
* The comunication between your devices within your local network might not work if you have a proxy set up
* If your Windows device doesn't show up after the subnet scan, you might have to activate "File and Printer Sharing (Echo Request - ICMPv4-In)" in your Windows Defender Firewall


## Minimal reqiured platform OS versions
* Windows: 10.0.17763.0 (Oktober 2018 Update)
* Android: Android 7.0


## Usage
In Order to use this application you'll have to download the latest release of the LMRCN.exe for Windows or com.companyname.lmrcn-Signed.apk for Android devices.
First of all, open the app and press "GO! / Refresh". This will initiate a complete scan of the subnet that your current device is connected to.
From there on, you'll see all devices, including their device names and IPv4-addresses that are connected to that particular subnet.
Press the corresponding button of the device that you want to send commands to. Make sure this app is currently running on that device.
To get the device platform and to make it's availlable commands visible, press "Contact device"


* **Windows commands:**
  * "Power Off" --> forceful shutdown --> "shutdown /s /f /t 0"
  * "Restart" --> forceful restart --> "shutdown /r /f /t 0"
  * "Hibernate" --> forceful hibernation --> "shutdown /h /f"
  * "Sign Out" --> forceful log off --> "shutdown /l /f /t 0"
  * *Customizable cmd command line entry*

* **Android commands:**
  * *coming soon*


## Contributions
I appreciate anyone who wants to contribute to this small project!
Feel free to open an issue if you run into any problems or open a pull request if you have any Idea on how to optimize this project.
Pull requests of additional features will be taken into consideration if I consider them useful.


## Contact 
Email: max_knaus76@yahoo.com
