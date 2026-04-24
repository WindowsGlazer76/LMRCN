using System.Collections.ObjectModel;
using System.Net;
using System.Net.NetworkInformation;
using System.Net.Sockets;
using CommunityToolkit.Mvvm.ComponentModel;
#if ANDROID
using Android.App;
using Android.Media;
using Android.Net.Wifi;
#endif

namespace LMRCN.Models
{

    public class Model
    {

    }



    public partial class Endgerät : ObservableObject
    {
        [ObservableProperty]
        private string devIP;

        public string DevSubnet { get; set; }
        public int DevPort { get; set; }

        [ObservableProperty]
        public string devName;
        [ObservableProperty]
        public string devPlatform = "Unknown";
        [ObservableProperty]
        public bool isWindows = false;
        [ObservableProperty]
        public bool isAndroid = false;

        public IPEndPoint DevEndPoint { get; set; } = null;
        public UdpClient DevClient { get; set; } = null;

        [ObservableProperty]
        public bool isReachable = false;
        [ObservableProperty]
        public bool isNotReachable = true;
        [ObservableProperty]
        public string statColor = "DarkRed";
        [ObservableProperty]
        public string stat = "Not contacted";
        [ObservableProperty]
        public bool isFindingDevice = false;
        [ObservableProperty]
        public string findDeviceButtonText = "Find Device";
        [ObservableProperty]
        public string findDeviceButtonColor = "Green";

        public Endgerät(string ip, string subnet, int port, string name)
        {
            DevIP = ip;
            DevSubnet = subnet;
            DevPort = port;
            DevName = name;
        }

    }





    public class Netzwerk
    {

        public ObservableCollection<Endgerät> DeviceList { get; set; } = new();
        public ObservableCollection<string> ServerRequests { get; set; } = new();
        public ObservableCollection<string> ClientResponses { get; set; } = new();
        public Endgerät ThisDevice { get; set; }
        public UdpClient _server = null;
        public CancellationTokenSource _cts = new();



        public Netzwerk()
        {
            ThisDevice = DefThisDev();
        }



        public async Task StartServer()
        {
            if (_server != null)
                return;

            try
            {
                _server = new UdpClient();
                _server.Client.SetSocketOption(SocketOptionLevel.Socket, SocketOptionName.ReuseAddress, true);
                _server.Client.Bind(ThisDevice.DevEndPoint);

                while (!_cts.Token.IsCancellationRequested)
                {
                    try
                    {
                        UdpReceiveResult result = await _server.ReceiveAsync(_cts.Token);
                        byte[] buffer = result.Buffer;
                        IPEndPoint remoteEndPoint = result.RemoteEndPoint;
                        string incomingMessage = System.Text.Encoding.UTF8.GetString(buffer).Trim();
                        byte[] response;

                        if (incomingMessage == ThisDevice.DevName + "_getPlatform")
                        {
                            response = System.Text.Encoding.UTF8.GetBytes(ThisDevice.DevName + ":" + ThisDevice.DevPlatform);
                            await _server.SendAsync(response, response.Length, remoteEndPoint);
                        }

                        if (incomingMessage == ThisDevice.DevName + "_shutdown")
                        {
                            response = System.Text.Encoding.UTF8.GetBytes(incomingMessage + ":valid");
                            await _server.SendAsync(response, response.Length, remoteEndPoint);
                            _ = Funktionen.Cmd("/c shutdown /s /f /t 0", true);
                        }
                        if (incomingMessage == ThisDevice.DevName + "_restart")
                        {
                            response = System.Text.Encoding.UTF8.GetBytes(incomingMessage + ":valid");
                            await _server.SendAsync(response, response.Length, remoteEndPoint);
                            _ = Funktionen.Cmd("/c shutdown /r /f /t 0", true);
                        }
                        if (incomingMessage == ThisDevice.DevName + "_hibernate")
                        {
                            response = System.Text.Encoding.UTF8.GetBytes(incomingMessage + ":valid");
                            await _server.SendAsync(response, response.Length, remoteEndPoint);
                            _ = Funktionen.Cmd("/c shutdown /h /f", true);
                        }
                        if (incomingMessage == ThisDevice.DevName + "_logoff")
                        {
                            response = System.Text.Encoding.UTF8.GetBytes(incomingMessage + ":valid");
                            await _server.SendAsync(response, response.Length, remoteEndPoint);
                            _ = Funktionen.Cmd("/c shutdown /l /f", true);
                        }
                        if (incomingMessage.Contains(':'))
                        {
                            if (incomingMessage.Substring(0, incomingMessage.IndexOf(':')) == ThisDevice.DevName + "_customCommand")
                            {
                                response = System.Text.Encoding.UTF8.GetBytes(incomingMessage + ":valid");
                                await _server.SendAsync(response, response.Length, remoteEndPoint);
                                _ = Funktionen.Cmd(incomingMessage.Substring(incomingMessage.IndexOf(':') + 1), false);
                            }
                        }

                        ServerRequests.Add(incomingMessage);

                    }
                    catch (Exception ex)
                    {
                        //logging
                    }
                }
            }
            catch (Exception ex)
            {
                //logging
            }
        }


        public async Task ScanNetwork()
        {
            string localIP = ThisDevice.DevIP;
            string subnet = ThisDevice.DevSubnet;

            var semaphore = new SemaphoreSlim(85);
            var tasks = new List<Task>();

            for (int i = 2; i < 255; i++)
            {
                string ip = subnet + i;
                tasks.Add(ScanIpAsync(ip, i, localIP, semaphore));
            }

            await Task.WhenAll(tasks);
        }
        private async Task ScanIpAsync(string ip, int timeout, string localIP, SemaphoreSlim semaphore)
        {
                #if ANDROID
                    await semaphore.WaitAsync();
                #endif

            try
            {
                #if ANDROID
                    await Task.Delay(timeout);
                #endif

                using var ping = new Ping();
                var reply = await ping.SendPingAsync(ip);

                var deviceIPs = DeviceIPList();

                if (reply.Status == IPStatus.Success && ip != localIP && !deviceIPs.Contains(ip))
                {
                    MainThread.BeginInvokeOnMainThread(() =>
                    {
                        lock (DeviceList)
                        {
                            DeviceList.Add(DefDevice(ip));
                        }
                    });
                }
                else if (reply.Status != IPStatus.Success && deviceIPs.Contains(ip))
                {
                    MainThread.BeginInvokeOnMainThread(() =>
                    {
                        lock (DeviceList)
                        {
                            var idx = DeviceIPList().IndexOf(ip);
                            if (idx >= 0)
                                DeviceList.RemoveAt(idx);
                        }
                    });
                }
            }
            catch (TimeoutException)
            {

            }
            catch (Exception ex)
            {
                //logging
            }
            finally
            {
                #if ANDROID
                    semaphore.Release();
                #endif
            }
        }



        public async Task<bool> SendCommand(Endgerät device, string command)
        {
            CancellationTokenSource ctc = new CancellationTokenSource();
            CancellationToken ct = ctc.Token;

            try
            {
                byte[] commandBytes = System.Text.Encoding.UTF8.GetBytes(device.DevName + command);
                await device.DevClient.SendAsync(commandBytes, commandBytes.Length, device.DevEndPoint);
                var receiveTask = WaitForResponse(device, ct);
                var completedTask = await Task.WhenAny(receiveTask, Task.Delay(2000));

                if (completedTask == receiveTask)
                {
                    var result = await receiveTask;
                    byte[] buffer = result.Buffer;
                    string msg = System.Text.Encoding.UTF8.GetString(buffer).Trim();
                    ClientResponses.Add(msg);

                    if (command == "_getPlatform")
                    {
                        if (msg == device.DevName + ":Windows")
                        {
                            device.IsWindows = true;
                            device.DevPlatform = "Windows";
                            device.IsReachable = true;
                            device.IsNotReachable = false;
                            device.StatColor = "DarkGreen";
                            device.Stat = "Online";
                            return true;
                        }
                        else if (msg == device.DevName + ":Android")
                        {
                            device.DevPlatform = "Android";
                            device.IsAndroid = true;
                            device.IsReachable = true;
                            device.IsNotReachable = false;
                            device.StatColor = "DarkGreen";
                            device.Stat = "Online";
                            return true;
                        }
                        else
                        {
                            throw new Exception("invalid platform declaration!");
                        }

                    }
                    else if (command == "_findDevice")
                    {
                        switch (device.IsFindingDevice)
                        {
                            case true:
                                device.IsFindingDevice = false;
                                device.FindDeviceButtonColor = "Green";
                                device.FindDeviceButtonText = "Find Device";
                                break;

                            case false:
                                device.IsFindingDevice = true;
                                device.FindDeviceButtonColor = "DarkRed";
                                device.FindDeviceButtonText = "Stop";
                                break;
                        }
                    }

                    if (msg == device.DevName + command + ":valid")
                    {
                        return true;
                    }
                    else if (msg == device.DevName + command + ":invalid")
                    {
                        //logic coming soon
                        return false;
                    }
                    else
                    {
                        //logic coming soon
                        return false;
                    }
                }
                else
                {
                    ctc.Cancel();
                    device.IsReachable = false;
                    device.IsNotReachable = true;
                    device.StatColor = "DarkRed";
                    device.Stat = "Offline";
                    return false;
                }
            }
            catch
            {
                // logging
                return false;
            }
        }
        public static async Task<UdpReceiveResult> WaitForResponse(Endgerät device, CancellationToken token)
        {
            UdpReceiveResult result = await device.DevClient.ReceiveAsync(token);
            return result;
        }



        private string GetLocalIPAddress()
        {
#if ANDROID
                var wifiManager = (WifiManager)Android.App.Application.Context.GetSystemService(Service.WifiService);
                int ipaddress = wifiManager.ConnectionInfo.IpAddress;

                byte[] bytes = BitConverter.GetBytes(ipaddress);

                var ipAddr = new IPAddress(bytes);
                return ipAddr.ToString();

#else
                var host = Dns.GetHostEntry(Dns.GetHostName());
                foreach (var ip in host.AddressList)
                {
                    if (ip.AddressFamily == AddressFamily.InterNetwork)
                    {
                        return ip.ToString();
                    }
                }
                throw new Exception("Keine IPv4-Adresse gefunden!");
#endif
        }

        private Endgerät DefThisDev()
        {
            string localIP = GetLocalIPAddress();
            string subnet = localIP.Substring(0, localIP.LastIndexOf('.') + 1);
            int port = 43000 + int.Parse(localIP.Substring(localIP.LastIndexOf('.') + 1));
            IPEndPoint localEP = new IPEndPoint(IPAddress.Parse(localIP), port);
            string hostName = Dns.GetHostName();
            string platform = DeviceInfo.Current.Platform.ToString();
            Endgerät thisDevice = new Endgerät(localIP, subnet, port, hostName);
            thisDevice.DevEndPoint = localEP;
            switch (platform)
            {
                case "WinUI":
                    thisDevice.DevPlatform = "Windows";
                    thisDevice.IsWindows = true;
                    break;
                case "Android":
                    thisDevice.DevPlatform = "Android";
                    thisDevice.IsAndroid = true;
                    break;
            }
            return thisDevice;
        }

        private Endgerät DefDevice(string ip)
        {
            string subnet = ip.Substring(0, ip.LastIndexOf('.') + 1);
            int port = 43000 + int.Parse(ip.Substring(ip.LastIndexOf('.') + 1));
            string devicename = Dns.GetHostEntry(ip).HostName;
            IPEndPoint localEP = new IPEndPoint(IPAddress.Parse(GetLocalIPAddress()), port);
            IPEndPoint ep = new IPEndPoint(IPAddress.Parse(ip), port);
            UdpClient client = new UdpClient(localEP);
            Endgerät clientDevice = new Endgerät(ip, subnet, port, devicename);
            clientDevice.DevEndPoint = ep;
            clientDevice.DevClient = client;
            return clientDevice;
        }

        private List<string> DeviceIPList()
        {
            List<string> clientIPs = new List<string>();
            foreach (Endgerät client in DeviceList)
            {
                clientIPs.Add(client.DevIP);
            }
            return clientIPs;
        }
    }





    public static class Funktionen
    {
        public static async Task Cmd(string command, bool createWindow)
        {
            System.Diagnostics.Process process = new System.Diagnostics.Process();
            process.StartInfo.FileName = "cmd.exe";
            process.StartInfo.Arguments = command;
            process.StartInfo.CreateNoWindow = createWindow;
            process.Start();
        }
    }
}
