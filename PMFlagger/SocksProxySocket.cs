/*
 * Created: July 26, 2015
 * Purpose: Asynch Socks4/5 proxy client with callbacks
 */

using System;
using System.IO;
using System.Net;
using System.Net.Sockets;
using System.Text;
using System.Threading;

public class SocksProxySocket
{
    private class StateObject
    {
        public const int BufferSize = 4096;
        public byte[] buffer = new byte[BufferSize];
    }

    public enum SocketState
    {
        HANDSHAKING,
        CONNECTING,
        CONNECTED,
        CLOSED,
    }

    public class Proxy
    {
        private SocksProxy.Proxy proxy { get; set; }
        private Socket clientSocket = null;
        private HostStruct hs = null;
        public bool isConnected { get; set; }
        public SocketState state { get; set; }

        // delegates
        public delegate void onConnect(Proxy p);
        public delegate void onReceive(Proxy p, byte[] data);
        public delegate void onClose();

        // callbacks
        public onConnect onConnectCallback = null;
        public onReceive onReceiveCallback = null;
        public onClose onCloseCallback = null;

        // host struct for connect
        private class HostStruct
        {
            public string host;
            public int port;
        }

        public Proxy(SocksProxy.Proxy _proxy)
        {
            proxy = _proxy;
            isConnected = false;
        }

        private byte GetVersion()
        {
            if (proxy.proxyType == SocksProxy.ProxyType.SOCKS4)
                return 4;
            else
                return 5;
        }

        private void ConnectCallback(IAsyncResult ar)
        {
            try
            {
                clientSocket.EndConnect(ar);
                isConnected = false;

                // do handshake ; no auth      
                if (proxy.proxyType == SocksProxy.ProxyType.SOCKS4)
                {
                    // SOCKS4 handshake
                    state = SocketState.CONNECTING;
                    byte[] handshake = { 4, 1, 0, 0, 0, 0, 0, 0, (byte)'C', (byte)'H', (byte)'A', (byte)'T', (byte)'A', (byte)'N', (byte)'G', (byte)'O', (byte)'1', 0 };
                    byte[] port = BitConverter.GetBytes(hs.port);
                    IPAddress _ip;
                    switch (Uri.CheckHostName(hs.host))
                    {
                        case UriHostNameType.Dns:
                            _ip = Dns.GetHostEntry(hs.host).AddressList[0];
                            break;
                        case UriHostNameType.IPv4:
                            _ip = IPAddress.Parse(hs.host);
                            break;
                        default:
                            Console.WriteLine("Neither DNS nor IPv4");
                            Close();
                            return;
                    }
                    int i_ip = BitConverter.ToInt32(_ip.GetAddressBytes(), 0);
                    byte[] ip = BitConverter.GetBytes(i_ip);

                    if (BitConverter.IsLittleEndian)
                    {
                        Array.Reverse(port);
                        Array.Reverse(ip);
                    }

                    handshake[2] = port[0];
                    handshake[3] = port[1];
                    handshake[4] = ip[0];
                    handshake[5] = ip[1];
                    handshake[6] = ip[2];
                    handshake[7] = ip[3];
                    Send(handshake);
                }
                else
                {
                    // SOCKS5 handshake
                    byte[] handshake = { 5, 1, 0 };
                    Send(handshake);
                }

                Receive();
            }
            catch (Exception e)
            {
                Close();
                Console.WriteLine(e.ToString());
            }

        }

        public bool Connect(string host, int port)
        {
            hs = new HostStruct();
            hs.host = host;
            hs.port = port;
            state = SocketState.HANDSHAKING;

            try
            {
                IPEndPoint ipEnd = new IPEndPoint(proxy.ip, proxy.port);
                Socket client = new Socket(AddressFamily.InterNetwork, SocketType.Stream, ProtocolType.Tcp);
                clientSocket = client;
                client.BeginConnect(ipEnd, new AsyncCallback(ConnectCallback), hs);
            }

            catch (Exception e)
            {
                Console.WriteLine(e.ToString());
            }

            return isConnected;
        }

        private void SendCallback(IAsyncResult ar)
        {
            try
            {
                clientSocket.EndSend(ar);
            }
            catch (Exception e)
            {
                Console.WriteLine(e.ToString());
                Close();
            }
        }

        public void Send(string data)
        {
            Send(Encoding.ASCII.GetBytes(data));
        }

        public void Send(byte[] data)
        {
            clientSocket.BeginSend(data, 0, data.Length, 0, new AsyncCallback(SendCallback), null);
        }

        private void HandleRecv(byte[] data)
        {
            switch (state)
            {
                case SocketState.HANDSHAKING:
                    // garbage collector will get rid of this, no need for Close()
                    MemoryStream request = new MemoryStream();
                    // check response
                    if (data[0] != GetVersion() && data[1] != 0)
                    {
                        // bad
                        Close();
                    }
                    request.Append(GetVersion());
                    // connect
                    request.Append(1);
                    // reserved
                    request.Append(0);
                    // dns or ipv4?
                    switch (Uri.CheckHostName(hs.host))
                    {
                        case UriHostNameType.Dns:
                            request.Append(3);
                            request.Append((byte)hs.host.Length);
                            break;
                        case UriHostNameType.IPv4:
                            request.Append(1);
                            break;
                        default:
                            // not supported
                            Console.WriteLine("Neither DNS nor IPv4");
                            return;
                    }
                    request.Append(Encoding.ASCII.GetBytes(hs.host));
                    byte[] bPort = BitConverter.GetBytes((UInt16)hs.port);
                    if (BitConverter.IsLittleEndian)
                    {
                        // network byte order is big endian
                        Array.Reverse(bPort);
                    }
                    request.Append(bPort);
                    byte[] buffer = request.ToArray();
                    state = SocketState.CONNECTING;
                    Send(buffer);
                    Receive();
                    break;

                case SocketState.CONNECTING:
                    if (data[0] == 5 && data[1] == 0)
                    {
                        isConnected = true;
                        state = SocketState.CONNECTED;
                        // connect callback
                        if (onConnectCallback != null)
                        {
                            onConnectCallback(this);
                        }
                    }
                    else if(data[0] == 4 && data[1] == 0x5A)
                    {
                        isConnected = true;
                        state = SocketState.CONNECTED;
                        // connect callback
                        if (onConnectCallback != null)
                        {
                            onConnectCallback(this);
                        }
                    }
                    else
                    {
                        Close();
                        Console.WriteLine("Bad socks response while connecting");
                    }
                    break;

                case SocketState.CONNECTED:
                    // packets are from proxied host
                    if (onReceiveCallback != null)
                    {
                        onReceiveCallback(this, data);
                    }
                    break;
            }
        }

        private void ReceiveCallback(IAsyncResult ar)
        {
            try
            {
                StateObject state = (StateObject)ar.AsyncState;
                int bytesRead = clientSocket.EndReceive(ar);
                if (bytesRead > 0)
                {
                    byte[] data = new byte[bytesRead];
                    Array.Copy(state.buffer, data, bytesRead);
                    HandleRecv(data);
                }
                else if (bytesRead == 0)
                {
                    // connection closed                   
                    Close();
                }

            }
            catch (Exception e)
            {
                Console.WriteLine(e.ToString());
            }
        }

        public void Receive()
        {
            try
            {
                StateObject state = new StateObject();
                clientSocket.BeginReceive(state.buffer, 0, StateObject.BufferSize, 0, new AsyncCallback(ReceiveCallback), state);
            }
            catch (Exception e)
            {
                Console.WriteLine(e.ToString());
            }
        }

        public void Close(bool doCallback = true)
        {
            try
            {
                clientSocket.Close();
                if (doCallback && onCloseCallback != null)
                    onCloseCallback();
            }
            catch (Exception)
            {
            }

            clientSocket = null;
            isConnected = false;
            state = SocketState.CLOSED;
        }
    }
}