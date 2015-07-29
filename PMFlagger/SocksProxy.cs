/*
 * Created: July 26, 2015
 * Purpose: Asynch Socks4/5 proxy client definitions
 */

using System;
using System.Net;
using System.Collections.Generic;

public static class SocksProxy
{

    public enum ProxyType
    {
        SOCKS4,
        SOCKS5,
    }

    public class Proxy : ICloneable
    {
        public IPAddress ip { get; set; }
        public int port { get; set; }
        public ProxyType proxyType { get; set; }
        public bool isAlive { get; set; }


        public Proxy(string _ip, string _port, ProxyType _proxyType)
        {
            ip = IPAddress.Parse(_ip);
            port = Int32.Parse(_port);
            proxyType = _proxyType;
            // assume dead until checked
            isAlive = false;
        }

        public Proxy(IPAddress _ip, int _port, ProxyType _proxyType)
        {
            ip = _ip;
            port = _port;
            proxyType = _proxyType;
            isAlive = false;
        }

        // format for db
        public override string ToString()
        {
            return string.Format("{0}:{1}:{2}", ip.ToString(), port, proxyType.ToString());
        }     
  
        public object Copy()
        {
            return this;
        }

        public object Clone()
        {
            return new Proxy(this.ip, this.port, this.proxyType);            
        }
    }

    public class ProxyComparer : IEqualityComparer<Proxy>
    {
        public bool Equals(Proxy a, Proxy b)
        {
            return (a.ip.Equals(b.ip) && a.port == b.port && a.proxyType == b.proxyType);
        }

        public int GetHashCode(Proxy a)
        {
            return a.ip.ToString().GetHashCode() ^ a.port;
        }
    }
}