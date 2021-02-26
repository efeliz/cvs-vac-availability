using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;

namespace CVS_Vac.Fetcher
{
    class Program
    {
        private static string VAC_ROOT_URL = "https://www.cvs.com/immunizations/covid-19-vaccine";

        private static string SENDGRID_API_KEY;
        private static List<string> SUBSCRIBER_EMAILS = new List<string>();
        private static int DAILY_NOTICE_LIMIT = 5;

        private static string SELECTED_STATE = "";
        private static List<string> SELECTED_CITIES = new List<string>();
        private static Dictionary<DateTime, int> DAILY_SENT_LOG = new Dictionary<DateTime, int>();


        static async Task Main(string[] args)
        {
            Console.WriteLine("Hi, thanks for using this CVS Covid-19 Vaccine Booking Status checker program.\n");
            Console.WriteLine("First we'll need to set some things set up before getting started.\n");

            Thread.Sleep(1000);

            // run initial setup
            await InitialSetup();
        }

        private static async Task InitialSetup()
        { 
            // retrieve SendGrid API Key
            Console.WriteLine("In order to notify you about any changes, I'll need to use Twilio's SendGrid API. Please paste your API key and press enter to continue:");
            SENDGRID_API_KEY = Console.ReadLine();

            // fetch subscriber emails
            Console.WriteLine("\nPlease enter a comma-separated list of the emails that would like to receive status updates (ex. person1@email.com,person2@email.com):");
            string subscriberListRaw = Console.ReadLine();
            string[] subscriberListSplit = subscriberListRaw.Split(",");
            foreach (string email in subscriberListSplit)
            {
                SUBSCRIBER_EMAILS.Add(email.Trim().ToLower());
            }

            // ask about daily limit count
            Console.WriteLine($"\nIn order to respect your inbox, please enter a daily limit (number) for how many times I could notify you about a new opening i.e. default is '{DAILY_NOTICE_LIMIT}':");
            string dailyLimitRaw = Console.ReadLine().Trim();
            if (dailyLimitRaw.Length > 0)
            {
                int dailyLimitParsed = Int32.Parse(dailyLimitRaw);
                DAILY_NOTICE_LIMIT = dailyLimitParsed;
            }

            // get available states/territories
        }
    }
}
