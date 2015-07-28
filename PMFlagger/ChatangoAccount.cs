using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace PMFlagger
{
    public class ChatangoAccount
    {
        public string username;
        public string password;
        public SocksProxy.Proxy proxy;
        public ChatangoSocksClient client;

        public ChatangoAccount(string username, string password, SocksProxy.Proxy proxy)
        {
            this.username = (username == string.Empty ? GenerateUsername() : username);
            this.password = (username == string.Empty ? GeneratePassword() : password);
            this.proxy = proxy;
            this.client = new ChatangoSocksClient(this);
        }

        public override string ToString()
        {
            return string.Format("{0}:{1}@({2})", new object[] { this.username , this.password, this.proxy.ToString() });
        }

        private string GenerateUsername()
        {
            string[] names = {"Jackson", "Aiden", "Liam", "Lucas", "Noah", "Mason", "Ethan", "Caden", "Jacob", "Logan", "Jayden", "Elijah", "Jack", "Luke", "Michael", "Benjamin", "Alexander", "James", "Jayce", "Caleb", "Connor", "William", "Carter", "Ryan", "Oliver", "Matthew", "Daniel", "Gabriel", "Henry", "Owen", "Grayson", "Dylan", "Landon", "Isaac", "Nicholas", "Wyatt", "Nathan", "Andrew", "Cameron", "Dominic", "Joshua", "Eli", "Sebastian", "Hunter", "Brayden", "David", "Samuel", "Evan", "Gavin", "Christian", "Max", "Anthony", "Joseph", "Julian", "John", "Colton", "Levi", "Muhammad", "Isaiah", "Aaron", "Tyler", "Charlie", "Adam", "Parker", "Austin", "Thomas", "Zachary", "Nolan", "Alex", "Ian", "Jonathan", "Christopher", "Cooper", "Hudson", "Miles", "Adrian", "Leo", "Blake", "Lincoln", "Jordan", "Tristan", "Jason", "Josiah", "Xavier", "Camden", "Chase", "Declan", "Carson", "Colin", "Brody", "Asher", "Jeremiah", "Micah", "Easton", "Xander", "Ryder", "Nathaniel", "Elliot", "Sean", "Cole"};
            Random rand = new Random();
            int index = rand.Next(names.Length);

            return names[index].ToLower() + rand.Next(100, 999);
        }

        private string GeneratePassword()
        {
            string alphanum = "ABCDEFGHIJKLMNOPQRSTUVWXYZabcdefghijklmnopqrstuvwxyz1234567890";
            Random rand = new Random();
            return new string(alphanum.OrderBy(x => rand.Next()).Take(8).ToArray());
        }
    }
}
