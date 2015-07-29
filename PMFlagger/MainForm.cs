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
using System.Diagnostics;
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
        private List<ChatangoSocksClient> clients = new List<ChatangoSocksClient>();
        private Dictionary<String, ChatangoAccount> db;

        public MainForm()
        {
            InitializeComponent();
            
        }
        
        private void MainForm_Load(object sender, EventArgs e)
        {
            LoadProxies();
            db = Database.Get();
            
            foreach(SocksProxy.Proxy p in proxyList)
            {
                ChatangoAccount newAccount;

                if (db.ContainsKey(p.ToString()))
                {
                    newAccount = db[p.ToString()];
                    clients.Add(new ChatangoSocksClient(newAccount));
                    Console.WriteLine("Loaded account from db: " + newAccount.username);
                }
                else
                {
                    newAccount = new ChatangoAccount(null, null, p);
                    clients.Add(new ChatangoSocksClient(newAccount));
                    Console.WriteLine("Trying to create account: " + newAccount.username);
                }
            }

            listBox1.DataSource = clients;
            this.Show();

        }

        private void listBox1_DrawItem(object sender, DrawItemEventArgs e)
        {
            if (e.Index == -1)
                return;

            e.DrawBackground();
            Graphics g = e.Graphics;

            ChatangoSocksClient current = (ChatangoSocksClient)listBox1.Items[e.Index];
            bool selected = ((e.State & DrawItemState.Selected) == DrawItemState.Selected);

            if (selected)
                g.FillRectangle(new SolidBrush(Color.FromKnownColor(KnownColor.Highlight)), e.Bounds);
            else if (current.chatangoState == ChatangoSocksClient.CHATANGO_STATE.READY)
                g.FillRectangle(new SolidBrush(Color.Green), e.Bounds);
            else if (current.chatangoState == ChatangoSocksClient.CHATANGO_STATE.FLAGGED)
                g.FillRectangle(new SolidBrush(Color.Blue), e.Bounds);
            else if (current.chatangoState == ChatangoSocksClient.CHATANGO_STATE.CLOSED)
                g.FillRectangle(new SolidBrush(Color.Red), e.Bounds);
            else
                g.FillRectangle(new SolidBrush(Color.Gray), e.Bounds);

            g.DrawString(current.ToString(), e.Font, (selected ? new SolidBrush(Color.White) : new SolidBrush(Color.Black)), listBox1.GetItemRectangle(e.Index));

            e.DrawFocusRectangle();

            
        }

        private void button1_Click(object sender, EventArgs e)
        {
            List<ChatangoSocksClient> usable = clients.Where(x => x.chatangoState == ChatangoSocksClient.CHATANGO_STATE.READY).Take((int)numericUpDown1.Value).ToList();

            foreach (ChatangoSocksClient c in usable)
                c.FlagUser(textBox1.Text);

        }

        void LoadProxies()
        {
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
        }

        private void listBox1_SelectedIndexChanged(object sender, EventArgs e)
        {

        }

        private void timer1_Tick(object sender, EventArgs e)
        {
            numericUpDown1.Maximum = clients.Where(x => x.chatangoState == ChatangoSocksClient.CHATANGO_STATE.READY).Count();
        }

        private void button2_Click(object sender, EventArgs e)
        {
            int index = listBox1.SelectedIndex;
            Point offset = listBox1.AutoScrollOffset;
            clients = clients.OrderByDescending(x => x.chatangoState).ToList();
            listBox1.DataSource = null;
            listBox1.DataSource = clients;
            listBox1.SelectedIndex = index;
            listBox1.AutoScrollOffset = offset;

        }
    }
}
