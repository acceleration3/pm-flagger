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
        CLOSED,
        UNAUTHED,
        READY,
        FLAGGED,
    }

    private string authid = string.Empty;
    private ChatangoAccount account;
    private SocksHTTPRequest.Post httpPost;
    private string incompletePacketData;
    public CHATANGO_STATE chatangoState = CHATANGO_STATE.UNAUTHED;

    public ChatangoSocksClient(ChatangoAccount account) : base(account.proxy)
    {
        this.incompletePacketData = "";
        this.account = account;
        
        onReceiveCallback = ChOnRecieve;
        //onConnectCallback = ChOnConnect;
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

    public override string ToString()
    {
        return string.Format("[{1}]{0}", new object[] { this.account.username, this.chatangoState.ToString() });
    }

    private void GetAuthID()
    {
        this.httpPost = new SocksHTTPRequest.Post("chatango.com", this.account.proxy, new SocksHTTPRequest.Post.onCompletion(OnRecieveAuth));

        Dictionary<string, string> postData = new Dictionary<string, string>();
        Dictionary<string, string> headers = httpPost.GetHeaders();

        postData["user_id"] = this.account.username;
        postData["password"] = this.account.password;
        postData["storecookie"] = "on";
        postData["checkerrors"] = "yes";

        headers["Cache-Control"] = "max-age=0";
        headers["Origin"] = "http://chatango.com";
        headers["Referer"] = "http://chatango.com/login";

        this.httpPost.StartRequest("login", headers, postData);
    }

    private void CreateAccount()
    {
        this.httpPost = new SocksHTTPRequest.Post("chatango.com", this.account.proxy, OnAccountCreate);
        Dictionary<string, string> postData = new Dictionary<string,string>();
        Dictionary<string, string> headers = httpPost.GetHeaders();

        string email = this.account.GenerateEmail();        

        postData["email"] = email;
        postData["login"] = this.account.username;
        postData["password"] = this.account.password;
        postData["password_confirm"] = this.account.password;
        postData["storecookie"] = "on";
        postData["signupsubmit"] = "Sign up";
        postData["checkerrors"] = "yes";

        this.httpPost.StartRequest("signupdir", headers, postData);
    }

    public void FlagUser(string username)
    {
        this.httpPost = new SocksHTTPRequest.Post("chatango.com", this.account.proxy, new SocksHTTPRequest.Post.onCompletion(OnFlag));
        Dictionary<string, string> postData = new Dictionary<string, string>();
        Dictionary<string, string> headers = httpPost.GetHeaders();
        Dictionary<string, string> cookies = new Dictionary<string,string>();

        postData["t"] = this.authid;
        postData["sid"] = username;
        
        cookies["auth.chatango.com"] = this.authid;
        cookies["cookies_enabled.chatango.com"] = "yes";
        cookies["id.chatango.com"] = this.account.username;
        cookies["fph.chatango.com"] = "http";

        headers["Referer"] = "http://st.chatango.com/flash/SellersApp.swf";
        headers["Origin"] = "http://st.chatango.com";

        this.httpPost.StartRequest("iflag", headers, postData, cookies);
    }

    private void OnFlag(bool success, string data)
    {
        if (!success || data == null)
        {
            Console.WriteLine("The flag POST request for the account " + this.account.ToString() + " failed.");
            return;
        }

        if(data.Contains("flagged"))
        {
            Console.WriteLine(this.account.ToString() + " successfully reported the account.");
            this.chatangoState = CHATANGO_STATE.FLAGGED;
        }
        else
        {
            Console.WriteLine("The attempt to flag with the account " + this.account.ToString() + " failed.");
            return;
        }
    }

    private void OnAccountCreate(bool success, string data)
    {
        if (!this.account.exists)
        {
            // it exists now
            this.account.exists = true;
            Database.Add(this.account);
            Console.WriteLine("Created account successfully... " + this.account.username);
            GetAuthID();
        }
    }

    private void OnRecieveAuth(bool success, string data)
    {
        if(!success || data == null)
        {
            Console.WriteLine("The login POST request for the account " + this.account.ToString() + " failed.");
            return;
        }

        this.authid = Regex.Match(data, @"auth.chatango.com=(.*?);").Groups[1].Value;

        if(this.authid != string.Empty)
        {
            //No need to login ?
            this.chatangoState = CHATANGO_STATE.READY;
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

    /*
    private void ChOnConnect(SocksProxySocket.Proxy p)
    {
        Debug.Print("CONNECTED TO CHATANGO SERVER WITH " + p.ToString());
        chatangoState = CHATANGO_STATE;       
        this.ChSendPacket("tlogin", authid, "2");
    }
    */

    private void ChOnClose()
    {
        Debug.Print("CLOSED CONNECTION TO CHATANGO SERVER");
        this.chatangoState = CHATANGO_STATE.CLOSED;
    }
}
