/*
 * Created: July 26, 2015
 * Purpose: Load a list of socks4/5 proxies from the web
 */

using System;
using System.Net;
using System.IO;
using System.Linq;
using System.Collections.Generic;
using System.Text.RegularExpressions;
using System.Net.Sockets;

public class GetProxies
{
    private IPHostEntry google = null;

    private List<SocksProxy.Proxy> GetSocksProxyProxies()
    {
        List<SocksProxy.Proxy> proxies = new List<SocksProxy.Proxy>();
        string url = "http://www.socks-proxy.net/";
        string data = (new HTTP()).HttpGet(url);
        Regex regex = new Regex(@"<tr><td>(.+?)<\/td><td>(.+?)<\/td><td>.+?<\/td><td>.+?<\/td><td>(.+?)<\/td>");
        foreach (Match m in regex.Matches(data))
        {
            string ip = m.Groups[1].Value;
            string port = m.Groups[2].Value;
            SocksProxy.ProxyType proxyType;
            if (m.Groups[3].Value.Equals("Socks5"))
                proxyType = SocksProxy.ProxyType.SOCKS5;
            else
                proxyType = SocksProxy.ProxyType.SOCKS4;
            proxies.Add(new SocksProxy.Proxy(ip, port, proxyType));
        }
        return proxies;
    }

    private List<SocksProxy.Proxy> GetXroxyProxies(int page = 0)
    {
        List<SocksProxy.Proxy> proxies = new List<SocksProxy.Proxy>();
        string url = "http://www.xroxy.com/proxylist.php?port=&type=Socks5&ssl=&country=&latency=&reliability=&sort=reliability&desc=true&pnum=";
        url += page.ToString();
        string data = (new HTTP()).HttpGet(url);
        Regex regex = new Regex("proxy:name=XROXY proxy&host=(.+?)&port=(.+?)&isSocks=true");
        foreach (Match m in regex.Matches(data))
        {
            string ip = m.Groups[1].Value;
            string port = m.Groups[2].Value;
            // all xroxy socks are socks5
            SocksProxy.ProxyType proxyType = SocksProxy.ProxyType.SOCKS5;
            proxies.Add(new SocksProxy.Proxy(ip, port, proxyType));
        }
        if (proxies.Count > 0)
        {
            proxies.AddRange(GetXroxyProxies(++page));
        }

        return proxies;
    }

    public List<SocksProxy.Proxy> LoadProxies()
    {
        List<SocksProxy.Proxy> proxies = new List<SocksProxy.Proxy>();
        proxies.AddRange(GetSocksProxyProxies());
        proxies.AddRange(GetXroxyProxies());
        return proxies.Distinct(new SocksProxy.ProxyComparer()).ToList<SocksProxy.Proxy>();
    }

    public bool IsProxyAlive(SocksProxy.Proxy proxy, int timeout = 1000)
    {
        try
        {
            Socket clientSocket = new Socket(AddressFamily.InterNetwork, SocketType.Stream, ProtocolType.Tcp);
            IAsyncResult result = clientSocket.BeginConnect(proxy.ip, proxy.port, null, null);
            result.AsyncWaitHandle.WaitOne(timeout, true);
            clientSocket.ReceiveTimeout = timeout;
            clientSocket.SendTimeout = timeout;
            if (clientSocket.Connected)
            {
                if (proxy.proxyType == SocksProxy.ProxyType.SOCKS5)
                {
                    // socks5, no authentication required
                    byte[] handshake = { 5, 1, 0 };
                    byte[] buffer = new byte[32];
                    clientSocket.Send(handshake);
                    clientSocket.Receive(buffer);
                    if (buffer[0] == 5 && buffer[1] == 0)
                    {
                        clientSocket.Close();
                        return true;
                    }
                }
                else
                {
                    if (google == null)
                    {
                        google = Dns.GetHostEntry("www.google.com");
                    }
                    //SOCKS4
                    byte[] handshake = { 4, 1, 0, 80, 0, 0, 0, 0, (byte)'C', (byte)'H', (byte)'A', (byte)'T', (byte)'A', (byte)'N', (byte)'G', (byte)'O', 0 };
                    int _ip = BitConverter.ToInt32(google.AddressList[0].GetAddressBytes(), 0);
                    byte[] ip = BitConverter.GetBytes(_ip);
                    if( BitConverter.IsLittleEndian)
                    {
                        Array.Reverse(ip);
                    }
                    handshake[4] = ip[0];
                    handshake[5] = ip[1];
                    handshake[6] = ip[2];
                    handshake[7] = ip[3];
                    clientSocket.Send(handshake);
                    byte[] buffer = new byte[32];
                    clientSocket.Receive(buffer);
                    if (buffer[0] == 4 && buffer[1] == 0x5A)
                    {
                        clientSocket.Close();
                        return true;
                    }
                }
            }            
            clientSocket.Close();

        }
        catch (Exception)
        {
        }

        return false;
    }

    public int CountAlive(List<SocksProxy.Proxy> proxies)
    {
        int count = 0;

        foreach (SocksProxy.Proxy p in proxies)
        {
            if (p.isAlive)
            {
                count++;
            }
        }

        return count;
    }

}
