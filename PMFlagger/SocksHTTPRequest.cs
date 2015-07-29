/*
 * Created: July 26, 2015
 * Purpose: HTTP Request through a Socks5 proxy
 * 
 * NOTES: Only set the onCompletion callback.  It will be called once the request finishes
 */

using System;
using System.Net;
using System.Collections.Generic;
using System.Web;
using System.Text;


public class SocksHTTPRequest
{
    public class Post : SocksProxySocket.Proxy
    {
        private string host;
        private string raw;
        public delegate void onCompletion(bool success, string data);
        public onCompletion onCompletionCallback = null;

        public Post(string _host, SocksProxy.Proxy _proxy, onCompletion _onCompletionCallback)
            : base(_proxy)
        {
            host = _host;
            onCompletionCallback = _onCompletionCallback;
            // internal
            onReceiveCallback = myOnReceiveCallback;
            onConnectCallback = myOnConnectCallback;
            onCloseCallback = myOnCloseCallback;
        }

        // get some base headers: user agent, accept
        public Dictionary<string, string> GetHeaders()
        {
            Dictionary<string, string> headers = new Dictionary<string, string>();
            headers["User-Agent"] = "Mozilla/5.0 (Windows NT 6.1; WOW64; rv:38.0) Gecko/20100101 Firefox/38.0";
            headers["Accept"] = "text/html,application/xhtml+xml,application/xml;q=0.9,*/*;q=0.8";
            headers["Accept-Language"] = "en-US,en;q=0.5";
            headers["Accept-Encoding"] = "gzip, deflate";
            headers["Connection"] = "keep-alive";

            return headers;
        }

        public void StartRequest(string path, Dictionary<string, string> headers, Dictionary<string, string> postData, Dictionary<string, string> cookies = null)
        {
            raw = "POST /" + path + " HTTP/1.1\r\n";
            raw += "Host: " + host + "\r\n";
            foreach (string header in headers.Keys)
            {
                raw += header + ": " + headers[header] + "\r\n";
            }
            string cookiesRaw = "Cookie: ";
            if (cookies != null)
            {
                foreach (string cookie in cookies.Keys)
                {
                    cookiesRaw += cookie + "=" + cookies[cookie] + "; ";
                }
                cookiesRaw = cookiesRaw.Substring(0, cookiesRaw.Length - 2);
            }
            string postDataRaw = "";
            foreach (string post in postData.Keys)
            {
                postDataRaw += Uri.EscapeDataString(post) + "=" + Uri.EscapeDataString(postData[post]) + "&";
            }
            postDataRaw = postDataRaw.Substring(0, postDataRaw.Length - 1);

            // add user cookies
            if (cookies != null)
            {
                raw += cookiesRaw + "\r\n";
            }

            // add post data
            raw += "Content-Type: application/x-www-form-urlencoded\r\n";
            raw += "Content-Length: " + Convert.ToString(postDataRaw.Length) + "\r\n\r\n";
            raw += postDataRaw;
            raw += "\r\n\r\n";

            Connect(host, 80);
        }

        private void myOnReceiveCallback(SocksProxySocket.Proxy p, byte[] data)
        {
            Close(false);

            if (onCompletionCallback == null)
                return;

            if (data != null)
            {
                onCompletionCallback(true, Encoding.ASCII.GetString(data));
            }
            else
            {
                onCompletionCallback(false, null);
            }
        }

        private void myOnConnectCallback(SocksProxySocket.Proxy p)
        {
            p.Send(raw);
            p.Receive();
        }

        private void myOnCloseCallback()
        {
            if(onCompletionCallback != null)
                onCompletionCallback(false, null);
        }

    }
}