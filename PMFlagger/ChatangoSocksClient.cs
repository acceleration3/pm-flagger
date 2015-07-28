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
    private string _username = string.Empty;
    private string _password = string.Empty;
    private string _authid = string.Empty;
    private SocksProxy.Proxy _proxy;
    private SocksHTTPRequest.Post authRequest;

    public ChatangoSocksClient(ChatangoAccount account) : base(account.proxy)
    {
        this._username = account.username;
        this._password = account.password;
        this._proxy = account.proxy;

        onReceiveCallback = OnRecieve;
        onConnectCallback = OnConnect;

        GetAuthID();
    }

    private void GetAuthID()
    {
        //user_id=acceleration3&password=-snip-&storecookie=on&checkerrors=yes
        Dictionary<string, string> postData = new Dictionary<string, string>();
        Dictionary<string, string> headers;

        postData["user_id"] = this._username;
        postData["password"] = this._password;
        postData["storecookie"] = "on";
        postData["checkerrors"] = "yes";

        this.authRequest = new SocksHTTPRequest.Post("chatango.com", _proxy, new SocksHTTPRequest.Post.onCompletion(OnRecieveAuth));

        headers = authRequest.GetHeaders();
        headers["Cache-Control"] = "max-age=0";
        headers["Origin"] = "http://chatango.com";
        headers["Referer"] = "http://chatango.com/login";

        this.authRequest.StartRequest("login", headers, postData);
    }

    private void OnRecieveAuth(bool success, string data)
    {
        this._authid = Regex.Match(data, @"auth.chatango.com=(.*?);").Value;
    }

    private void OnRecieve(SocksProxySocket.Proxy p, byte[] data)
    {

    }

    private void OnConnect(SocksProxySocket.Proxy p)
    {

    }

}
