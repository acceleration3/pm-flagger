/*
 * TODO: 
 * ChatangoSocksClient.cs
 *      add method for signing up
 *      add method for logging in
 *      add method for flagging accounts
 *      add chatango pm class
 * Other
 *      add database class, ability to save/load proxies
 *      save working proxies/port/proxytype in database with created account
 */

using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace PMFlagger
{
    public partial class MainForm : Form
    {
        private List<SocksProxy.Proxy> proxyList;
        private List<ChatangoAccount> accounts = new List<ChatangoAccount>();

        public MainForm()
        {
            InitializeComponent();
        }


        private void MainForm_Load(object sender, EventArgs e)
        {
            /*
            Console.WriteLine("Please wait while proxies are loaded and checked....");
            GetProxies getProxies = new GetProxies();
            proxyList = getProxies.LoadProxies();
            int count = proxyList.Count;
            int i = 1;
            foreach (var p in proxyList)
            {
                if (getProxies.IsProxyAlive(p))
                {
                    Console.WriteLine("[{0}/{1}] " + p.ToString() + " is alive", i, count);
                    p.isAlive = true;
                }
                else
                {
                    Console.WriteLine("[{0}/{1}] " + p.ToString() + " is dead", i, count);
                }
                ++i;
            }
            
            Console.WriteLine("[Finished Loading] {0} proxies are alive.", getProxies.CountAlive(proxyList));
            */
            SocksProxy.Proxy p = new SocksProxy.Proxy("59.58.162.141", "2699", SocksProxy.ProxyType.SOCKS5);
            ChatangoAccount testacc = new ChatangoAccount(null, null, p);
            ChatangoSocksClient testclient = new ChatangoSocksClient(testacc);
        }
    }
}
