using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace PMFlagger
{
    public class ChatangoAccount : ICloneable
    {
        public string username;
        public string password;
        public SocksProxy.Proxy proxy;
        public ChatangoSocksClient client;
        public bool exists;

        public ChatangoAccount(string username, string password, SocksProxy.Proxy proxy)
        {            
            this.proxy = proxy;            
            // assume the account exists 
            this.exists = true;
            this.username = username;
            this.password = password;

            if (username == null)
            {
                this.password = GeneratePassword();
                this.username = GenerateUsername();
                this.exists = false;
            }
        }

        public override string ToString()
        {
            return string.Format("{0}:{1}@({2})", new object[] { this.username , this.password, this.proxy.ToString() });
        }

        private string GenerateRandomString(int n, bool onlyAlpha = false)
        {
            string data = "abcdefghijklmnopqrstuvwxyz";
            if (!onlyAlpha)
                data += "0123456789";
            Random rand = new Random();
            return new string(data.OrderBy(x => rand.Next()).Take(n).ToArray());
        }

        private string GenerateUsername()
        {
            string[] names = {"Jackson", "Aiden", "Liam", "Lucas", "Noah", "Mason", "Ethan", "Caden", "Jacob", "Logan", "Jayden", "Elijah", "Jack", "Luke", "Michael", "Benjamin", "Alexander", "James", "Jayce", "Caleb", "Connor", "William", "Carter", "Ryan", "Oliver", "Matthew", "Daniel", "Gabriel", "Henry", "Owen", "Grayson", "Dylan", "Landon", "Isaac", "Nicholas", "Wyatt", "Nathan", "Andrew", "Cameron", "Dominic", "Joshua", "Eli", "Sebastian", "Hunter", "Brayden", "David", "Samuel", "Evan", "Gavin", "Christian", "Max", "Anthony", "Joseph", "Julian", "John", "Colton", "Levi", "Muhammad", "Isaiah", "Aaron", "Tyler", "Charlie", "Adam", "Parker", "Austin", "Thomas", "Zachary", "Nolan", "Alex", "Ian", "Jonathan", "Christopher", "Cooper", "Hudson", "Miles", "Adrian", "Leo", "Blake", "Lincoln", "Jordan", "Tristan", "Jason", "Josiah", "Xavier", "Camden", "Chase", "Declan", "Carson", "Colin", "Brody", "Asher", "Jeremiah", "Micah", "Easton", "Xander", "Ryder", "Nathaniel", "Elliot", "Sean", "Cole"};
            Random rand = new Random();
            int index = rand.Next(names.Length);           
            return names[index].ToLower() + GenerateRandomString(rand.Next(5, 7));
        }

        private string GeneratePassword()
        {
            return GenerateRandomString(8);
        }

        public string GenerateEmail()
        {
            return GenerateRandomString(16, true) + "@" + GenerateRandomString(8, true) + "." + GenerateRandomString(8, true);
        }

        public object Copy()
        {
            return this;
        }

        public object Clone()
        {
            return new ChatangoAccount(username,
                password,
                (SocksProxy.Proxy)this.proxy.Clone());
        }
    }
}
