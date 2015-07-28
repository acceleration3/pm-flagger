/*
 * Created: July 26, 2015
 * Purpose: Non-proxied HTTP requests
 */

using System;
using System.Net;
using System.IO;
using System.Collections.Generic;

public class HTTP
{
    public string HttpGet(string url, int timeout = 5000)
    {
        string reply = null;

        try
        {
            WebRequest request = WebRequest.Create(url);
            request.Timeout = timeout;
            request.Proxy = null;
            request.Method = "GET";
            using (WebResponse response = request.GetResponse())
            {
                using (StreamReader webReader = new StreamReader(response.GetResponseStream()))
                {
                    reply = webReader.ReadToEnd().Trim();
                }
            }
        }
        catch (Exception e)
        {
            Console.WriteLine(e.ToString());
        }

        return reply;
    }
}