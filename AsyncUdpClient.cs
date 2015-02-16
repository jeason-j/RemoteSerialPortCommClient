using System;
using System.Net;
using System.Net.Sockets;
using System.Text;
using System.Threading;

namespace RemoteSerialPortCommClient
{
    public class UdpState
    {
        public UdpClient udpClient = null;
        public IPEndPoint ipEndPoint = null;
        public const int BufferSize = 1024;
        public byte[] buffer = new byte[BufferSize];
        public int counter = 0;
    }

    // 异步UDP类
    public class AsyncUdpClient
    {
        public static bool messageSent = false;
        // Receive a message and write it to the console.
        // 定义端口
        private const int listenPort = 1101;
        private const int remotePort = 1100;
        // 定义节点
        private IPEndPoint localEP = null;
        private IPEndPoint remoteEP = null;
        // 定义UDP发送和接收
        private UdpClient udpReceive = null;
        private UdpClient udpSend = null;
        private UdpState udpSendState = null;
        private UdpState udpReceiveState = null;
        private int counter = 0;
        // 异步状态同步
        private ManualResetEvent sendDone = new ManualResetEvent(false);
        private ManualResetEvent receiveDone = new ManualResetEvent(false);
        // 定义套接字
        //private Socket receiveSocket;
        //private Socket sendSocket;

        public AsyncUdpClient()
        {
            // 本机节点
            localEP = new IPEndPoint(IPAddress.Any, listenPort);
            // 远程节点
            remoteEP = new IPEndPoint(Dns.GetHostAddresses(Dns.GetHostName())[0], remotePort);
            // 实例化
            udpReceive = new UdpClient(localEP);
            udpSend = new UdpClient();

            // 分别实例化udpSendState、udpReceiveState
            udpSendState = new UdpState { ipEndPoint = remoteEP, udpClient = udpSend };

            udpReceiveState = new UdpState { ipEndPoint = remoteEP, udpClient = udpReceive };

            //receiveSocket = new Socket(AddressFamily.InterNetwork, SocketType.Dgram, ProtocolType.Udp);
            //receiveSocket.Bind(localEP);

            //sendSocket = new Socket(AddressFamily.InterNetwork, SocketType.Dgram, ProtocolType.Udp);
            //sendSocket.Bind(remoteEP);
        }

        // 发送函数
        public void SendMsg()
        {
            udpSend.Connect(remoteEP);

            //Thread t = new Thread(new ThreadStart(ReceiveMessages));
            //t.Start();
            Byte[] sendBytes;
            string message;
            while (true)
            {
                message = "Client" + (counter++).ToString();
                lock (this)
                {
                    sendBytes = Encoding.ASCII.GetBytes(message);
                    udpSendState.counter = counter;
                    // 调用发送回调函数
                    udpSend.BeginSend(sendBytes, sendBytes.Length, new AsyncCallback(SendCallback), udpSendState);
                    sendDone.WaitOne();
                    Thread.Sleep(200);
                    ReceiveMessages();
                }
            }
        }

        // 发送回调函数
        public void SendCallback(IAsyncResult iar)
        {
            UdpState udpState = iar.AsyncState as UdpState;
            if (iar.IsCompleted)
            {
                Console.WriteLine("第{0}个发送完毕！", udpState.counter);
                Console.WriteLine("number of bytes sent: {0}", udpState.udpClient.EndSend(iar));
                //if (udpState.counter == 10)
                //{
                //    udpState.udpClient.Close();
                //}
                sendDone.Set();
            }
        }

        // 接收函数
        public void ReceiveMessages()
        {
            lock (this)
            {
                udpReceive.BeginReceive(new AsyncCallback(ReceiveCallback), udpReceiveState);
                receiveDone.WaitOne();
                Thread.Sleep(100);
            }
        }

        // 接收回调函数
        public void ReceiveCallback(IAsyncResult iar)
        {
            UdpState udpState = iar.AsyncState as UdpState;
            if (iar.IsCompleted)
            {
                Byte[] receiveBytes = udpState.udpClient.EndReceive(iar, ref udpReceiveState.ipEndPoint);
                string receiveString = Encoding.Unicode.GetString(receiveBytes);
                Console.WriteLine("Received: {0}", receiveString);
                receiveDone.Set();
            }
        }
    }
}
