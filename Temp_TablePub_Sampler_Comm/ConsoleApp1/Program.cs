// See https://aka.ms/new-console-template for more information
using Microsoft.VisualStudio.TestPlatform.CommunicationUtilities;
using System.Collections;
using System.Net.NetworkInformation;
using System.Text;
using Udp.Communication.ByteArray.ServerClient;
using Xunit.Abstractions;

Console.WriteLine("Hello, World!");

foreach (NetworkInterface ni in NetworkInterface.GetAllNetworkInterfaces())
    Console.WriteLine($"{ni.Name} - Mtu - {ni.GetIPProperties().GetIPv4Properties().Mtu}");

    /*
    var a = new TablePubSubWithRealCommTests.TablePubSubWithRealCommTests(new Con());

    a.PubSub_N_Records_UpdateAsInsertForLowLatency();

    //Thread.Sleep(100_000);

    public class Con : ITestOutputHelper
    {
        public void WriteLine(string message)
        {
            Console.WriteLine(message);
        }

        public void WriteLine(string format, params object[] args)
        {
            Console.WriteLine(format, args);
        }
    }
    */

var udpServer = new UdpByteArrayServer(15442, 1400, 20_000, 5 * 1024*1024, 10);
udpServer.StartAsync();

udpServer.ClientDisconnected += UdpServer_ClientDisconnected;
udpServer.ClientConnected += UdpServer_ClientConnected;
udpServer.MessageReceived += UdpServer_MessageReceived;

void UdpServer_MessageReceived(System.Net.IPEndPoint arg1, byte[] arg2, int arg3)
{
    Console.WriteLine($"[{DateTime.Now}][Server] - Message - {System.Text.Encoding.ASCII.GetString(arg2,0, arg3)}, {arg3}");
    ConsoleApp1.Class1.temp1.AddRange(arg2.Take(arg3));
}


void UdpServer_ClientConnected(System.Net.IPEndPoint obj)
{
    Console.WriteLine($"[{DateTime.Now}][Server] - Client Conncted");
}

void UdpServer_ClientDisconnected(System.Net.IPEndPoint obj)
{
    Console.WriteLine($"[{DateTime.Now}][Server] - Client Disconncted");
}

var udpClient = new UdpByteArrayClient(
    "127.0.0.1", 
    15442, 1400, 5 * 1024 * 1024);
udpClient.StartAsync();
udpClient.MessageReceived += UdpClient_MessageReceived;

void UdpClient_MessageReceived(System.Net.IPEndPoint arg1, byte[] arg2, int arg3)
{
    Console.WriteLine($"[{DateTime.Now}][Client] - Message - {System.Text.Encoding.ASCII.GetString(arg2, 0, arg3)}");
};

//Thread.Sleep(100);

var msg1 = System.Text.Encoding.ASCII.GetBytes("Client-To-Server-Ran1");
udpClient.SendMessage(msg1, msg1.Length);
//udpClient.SendMessage(msg1, msg1.Length);
//udpClient.SendMessage(msg1, msg1.Length);
Thread.Sleep(100);
var msg2 = System.Text.Encoding.ASCII.GetBytes("Client-To-Server-Ran2");
udpClient.SendMessage(msg2, msg2.Length);
Thread.Sleep(100);
var msg3 = System.Text.Encoding.ASCII.GetBytes("Client-To-Server-Ran3");
udpClient.SendMessage(msg3, msg3.Length);
Thread.Sleep(100);
var msg4 = System.Text.Encoding.ASCII.GetBytes("Client-To-Server-Ran4");
udpClient.SendMessage(msg4, msg4.Length);
Thread.Sleep(100);
var msg5 = System.Text.Encoding.ASCII.GetBytes("Client-To-Server-Ran5");
udpClient.SendMessage(msg5, msg5.Length);
Thread.Sleep(100);
var msg6 = System.Text.Encoding.ASCII.GetBytes("Client-To-Server-Ran6");
udpClient.SendMessage(msg6, msg6.Length);

var msg7 = System.Text.Encoding.ASCII.GetBytes("Server-To-Client-Ran7");
var firstClient = udpServer.GetConnectedClients().FirstOrDefault();
udpServer.SendMessage(firstClient, msg7, msg7.Length);

var msg8 = System.Text.Encoding.ASCII.GetBytes("Server-To-Client-Ran8");
firstClient = udpServer.GetConnectedClients().FirstOrDefault();
udpServer.SendMessage(firstClient, msg8, msg8.Length);

Thread.Sleep(50_000);
