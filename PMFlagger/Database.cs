using System;
using System.IO;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

/*
 * Text file format
 * proxy_ip:proxy_port:proxy_type:username:password
 * 
 * This database will likely not have many entries in it
 * so Save() will be called by Add()
 */

namespace PMFlagger
{
    public static class Database
    {
        public static bool Initialized = false;
        public static Dictionary<string, ChatangoAccount> db;
        private static Object dbLock = new Object();
        private static string dbName = "Accounts.db";

        public static Dictionary<string, ChatangoAccount> Get()
        {          

            lock(dbLock)
            {
                // access file, load db
                string[] accounts;

                if( Initialized )
                {
                    return db;
                }
                else
                {
                    db = new Dictionary<string, ChatangoAccount>();
                    Initialized = true;
                }
               
                try
                {
                    accounts = File.ReadAllLines(dbName);                 
                }
                catch(FileNotFoundException)
                {
                    File.Create(dbName);
                    return db.DeepClone();
                }

                foreach (string account in accounts)
                {
                    string[] items = account.Split(':');
                    string key = items[0] + ":" + items[1] + ":" + items[2];
                    ChatangoAccount chAccount = new ChatangoAccount(
                        items[3],
                        items[4],
                        new SocksProxy.Proxy(
                            items[0],
                            items[1],
                            (items[2] == "SOCKS5" ? SocksProxy.ProxyType.SOCKS5 : SocksProxy.ProxyType.SOCKS4)
                            )
                        );                    
                    db[key] = chAccount;
                }
            }           

            lock(dbLock)
            {
                return db.DeepClone();
            }            
        }       

        public static void Add(ChatangoAccount chAccount)
        {
            string key = chAccount.proxy.ToString();
            lock (dbLock)
            {
                db[key] = chAccount;
            }

            Save();
        }

        public static void Delete(ChatangoAccount chAccount)
        {
            string key = chAccount.proxy.ToString();
            lock (dbLock)
            {
                db.Remove(key);
            }

            Save();
        }

        private static void Save()
        {
            lock (dbLock)
            {
                File.Delete(dbName);
                List<string> lines = new List<string>();
                foreach (string key in db.Keys)
                {
                    ChatangoAccount chAccount = db[key];
                    string data =
                        chAccount.proxy.ToString() + ":" +
                        chAccount.username + ":" +
                        chAccount.password;
                    lines.Add(data);
                }

                File.WriteAllLines(dbName, lines.ToArray());
            }
        }

    }
}
