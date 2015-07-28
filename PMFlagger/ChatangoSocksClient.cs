/*
 * Created: July 26, 2015
 * Purpose: Chatango class
 */

using System;
using System.Net;
using System.Collections.Generic;
using System.Threading;
using System.Diagnostics;
using System.Text;
using System.Text.RegularExpressions;
using PMFlagger;

public class ChatangoSocksClient : SocksProxySocket.Proxy
{
    public enum CHATANGO_STATE
    {
        UNAUTHED,
        AUTHED,
        LOGGEDIN
    }

    private string authid = string.Empty;
    private ChatangoAccount account;
    private SocksHTTPRequest.Post authRequest;
    public CHATANGO_STATE state = CHATANGO_STATE.UNAUTHED;

    public ChatangoSocksClient(ChatangoAccount account) : base(account.proxy)
    {
        this.account = account;

        onReceiveCallback = OnRecieve;
        onConnectCallback = OnConnect;

        GetAuthID();
    }

    private void GetAuthID()
    {
        Dictionary<string, string> postData = new Dictionary<string, string>();
        Dictionary<string, string> headers;

        postData["user_id"] = this.account.username;
        postData["password"] = this.account.password;
        postData["storecookie"] = "on";
        postData["checkerrors"] = "yes";

        this.authRequest = new SocksHTTPRequest.Post("chatango.com", this.account.proxy, new SocksHTTPRequest.Post.onCompletion(OnRecieveAuth));

        headers = authRequest.GetHeaders();
        headers["Cache-Control"] = "max-age=0";
        headers["Origin"] = "http://chatango.com";
        headers["Referer"] = "http://chatango.com/login";

        this.authRequest.StartRequest("login", headers, postData);
    }

    private void OnRecieveAuth(bool success, string data)
    {
        if(!success || data == null)
        {
            Console.WriteLine("The login POST request for the account " + this.account.ToString() + " failed.");
            return;
        }

        this.authid = Regex.Match(data, @"auth.chatango.com=(.*?);").Groups[1].Value;

        if(authid != null && authid != string.Empty)
        {
            this.state = CHATANGO_STATE.AUTHED;
            this.Connect("c1.chatango.com", 5222);
        }
        else
        {
            Console.WriteLine("The attempt to get an AuthID for the account " + this.account.ToString() + " failed.");
            return;
        }
    }

    private void OnRecieve(SocksProxySocket.Proxy p, byte[] data)
    {
        Debug.Print("DATA: " + ASCIIEncoding.ASCII.GetString(data));
    }

    private void OnConnect(SocksProxySocket.Proxy p)
    {
        state = CHATANGO_STATE.LOGGEDIN;
        this.Send("tlogin:" + authid + ":2\0");
    }


}
