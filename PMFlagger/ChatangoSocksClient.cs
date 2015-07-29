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
    private string incompletePacketData;
    public CHATANGO_STATE chatangoState = CHATANGO_STATE.UNAUTHED;

    public ChatangoSocksClient(ChatangoAccount account) : base(account.proxy)
    {
        this.incompletePacketData = "";
        this.account = account;
        this.authRequest = new SocksHTTPRequest.Post("chatango.com", this.account.proxy, new SocksHTTPRequest.Post.onCompletion(OnRecieveAuth));

        onReceiveCallback = ChOnRecieve;
        onConnectCallback = ChOnConnect;
        onCloseCallback = ChOnClose;

        if (this.account.exists)
        {
            GetAuthID();
        }
        else
        {
            CreateAccount();
        }
    }

    private void GetAuthID()
    {
        Dictionary<string, string> postData = new Dictionary<string, string>();
        Dictionary<string, string> headers;

        postData["user_id"] = this.account.username;
        postData["password"] = this.account.password;
        postData["storecookie"] = "on";
        postData["checkerrors"] = "yes";

        headers = authRequest.GetHeaders();
        headers["Cache-Control"] = "max-age=0";
        headers["Origin"] = "http://chatango.com";
        headers["Referer"] = "http://chatango.com/login";

        this.authRequest.StartRequest("login", headers, postData);
    }

    private void CreateAccount()
    {
        Dictionary<string, string> postData = new Dictionary<string,string>();
        Dictionary<string, string> headers = authRequest.GetHeaders();

        string email = this.account.GenerateEmail();        

        postData["email"] = email;
        postData["login"] = this.account.username;
        postData["password"] = this.account.password;
        postData["password_confirm"] = this.account.password;
        postData["storecookie"] = "on";
        postData["signupsubmit"] = "Sign up";
        postData["checkerrors"] = "yes";

        this.authRequest.StartRequest("signupdir", headers, postData);
    }

    private void OnRecieveAuth(bool success, string data)
    {
        if(!success || data == null)
        {
            Console.WriteLine("The login POST request for the account " + this.account.ToString() + " failed.");
            return;
        }

        this.authid = Regex.Match(data, @"auth.chatango.com=(.*?);").Groups[1].Value;

        if(authid != string.Empty)
        {
            this.chatangoState = CHATANGO_STATE.AUTHED;
            this.Connect("c1.chatango.com", 443);
        }
        else
        {
            Console.WriteLine("The attempt to get an AuthID for the account " + this.account.ToString() + " failed.");
            return;
        }
    }       

    private void ChSendPacket(params string[] packets)
    {
        string data = "";
        string terminator = "\r\n\0";
        if (packets[0].Equals("tlogin"))
        {
            terminator = "\0";
        }
        foreach(string packet in packets)
        {
            data += packet + ":";
        }

        data = data.Substring(0, data.Length);
        this.Send(data + terminator);
        this.Receive();
    }

    private void ChHandlePacket(string data)
    {
        // empty packets are heartbeats, reply with \r\n\0 every minute to stay
        // connected
        string[] packet = data.Split(':');
        Debug.Print("PACKET: " + packet[0] + " => " + data);        
    }

    private void ChOnRecieve(SocksProxySocket.Proxy p, byte[] data)    
    {        
        string packetString = Encoding.ASCII.GetString(data);
        if (this.incompletePacketData != "")
        {
            packetString = this.incompletePacketData + packetString;
        }
        string[] packets = packetString.Split('\0');
        if (packets[packets.Length - 1] != "")
        {
            // we are still waiting for more data
            for (int i = 0; i < packets.Length - 1; i++)
            {
                ChHandlePacket(packets[i].Replace("\r\n", ""));
            }

            this.incompletePacketData = packets[packets.Length - 1];
            this.Receive();
        }
        else
        {
            // we are not waiting for more data
            this.incompletePacketData = "";
            for (int i = 0; i < packets.Length - 1; i++)
            {
                ChHandlePacket(packets[i].Replace("\r\n", ""));
            }
        }

    }

    private void ChOnConnect(SocksProxySocket.Proxy p)
    {
        Debug.Print("CONNECTED TO CHATANGO SERVER WITH " + p.ToString());
        chatangoState = CHATANGO_STATE.LOGGEDIN;       
        this.ChSendPacket("tlogin", authid, "2");
    }

    private void ChOnClose()
    {
        Debug.Print("CLOSED CONNECTION TO CHATANGO SERVER");
    }
}
