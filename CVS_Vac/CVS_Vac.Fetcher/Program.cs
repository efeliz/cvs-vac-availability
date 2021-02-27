﻿using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using CVS_Vac.Fetcher.Models;
using PlaywrightSharp;
using System.Linq;

namespace CVS_Vac.Fetcher
{
    class Program
    {
        private static string VAC_ROOT_URL = "https://www.cvs.com/immunizations/covid-19-vaccine";
        private static IPlaywright PlaywrightRef;
        private static IBrowser BrowserRef;
        private static IPage FetcherPageRef;

        private static int FETCHER_MINUTES = 1;
        private static int FETCHER_INTERVAL = (60000 * FETCHER_MINUTES);

        private static string SENDGRID_API_KEY;
        private static List<string> SUBSCRIBER_EMAILS = new List<string>();
        private static int DAILY_NOTICE_LIMIT = 5;

        private static List<State> FETCHED_STATES = new List<State>();
        private static Dictionary<DateTime, int> DAILY_SENT_LOG = new Dictionary<DateTime, int>();


        static async Task Main(string[] args)
        {
            Console.WriteLine("Hi, thanks for using this CVS Covid-19 Vaccine Booking Status checker program.\n");
            Console.WriteLine("First we'll need to set some things set up before getting started. One moment please...\n");

            SetupScraper().Wait();

            // run initial setup
            InitialSetup().Wait();

            Task.Delay(1000).Wait();

            // begin automated fetcher process
            await StartFetcher();
        }

        private static async Task SetupScraper()
        {
            PlaywrightRef = await Playwright.CreateAsync();
            BrowserRef = await PlaywrightRef.Firefox.LaunchAsync(headless: false);
            FetcherPageRef = await BrowserRef.NewPageAsync();
            await FetcherPageRef.GoToAsync(VAC_ROOT_URL, waitUntil: LifecycleEvent.Networkidle);
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

            Console.Clear();
            Console.WriteLine("One moment please while I find which locations are currently offering vaccinations... (this could take a minute)");
            // get available states/territories
            FETCHED_STATES = await GetStates();

            // present state options to pick from (select 1 by number)
            var statesMidpoint = (int)Math.Floor((double)FETCHED_STATES.Count / 2.0);
            var statesCol1 = FETCHED_STATES.GetRange(0, statesMidpoint +1);
            var statesCol2 = FETCHED_STATES.GetRange(statesMidpoint+1, (FETCHED_STATES.Count - statesMidpoint)-1);
            Console.Clear();
            for (var s=0; s<statesCol1.Count; s++)
            {
                Console.Write($"{s+1}. {statesCol1[s].Title}\t");
                if (statesCol2.Count > s)
                {
                    Console.Write($"{(s+2) + statesMidpoint}. {statesCol2[s].Title}\n");
                }
                else
                {
                    Console.WriteLine();
                }
            }

            Console.WriteLine("\nPlease select the state/territory (number) you would like to subscribe to for notifications from above:");
            var selectedStateIndex = Math.Min(Int32.Parse(Console.ReadLine()) - 1, FETCHED_STATES.Count - 1);
            // select state
            FETCHED_STATES[selectedStateIndex].Subscribed = true;
            var selectedState = FETCHED_STATES[selectedStateIndex];

            // City Lister
            Console.Clear();
            // present city options to pick from 
            var citiesMidpoint = (int)Math.Floor((double)selectedState.Cities.Count / 2.0);
            var citiesCol1 = selectedState.Cities.GetRange(0, citiesMidpoint + 1);
            var citiesCol2 = selectedState.Cities.GetRange(citiesMidpoint + 1, (selectedState.Cities.Count - citiesMidpoint) - 1);
            Console.Clear();
            for (var c = 0; c < citiesCol1.Count; c++)
            {
                Console.Write($"{c + 1}. {citiesCol1[c].Title}\t\t");
                if (citiesCol2.Count > c)
                {
                    Console.Write($"{(c + 2) + citiesMidpoint}. {citiesCol2[c].Title}\n");
                }
                else
                {
                    Console.WriteLine();
                }
            }

            Console.WriteLine($"\nNow that you've selected your state ({selectedState.Title}) please enter a comma-separated list of the cities (numbers) you'd like to subscribe to:");
            string[] selectedCitiesRaw = Console.ReadLine().Trim().Replace(" ", "").Split(",");
            List<City> selectedCities = new List<City>();
            foreach(var cityNum in selectedCitiesRaw)
            {
                var cityIndex = Math.Max(Int32.Parse(cityNum) - 1, 0);
                selectedState.Cities[cityIndex].Subscribed = true;
                selectedCities.Add(selectedState.Cities[cityIndex]);
            }

            // completion message
            Console.Clear();
            Console.Write("Setup is now complete and you've selected the following:\n" +
                $"SendGridKey: {SENDGRID_API_KEY}\n" +
                $"Subscribers: {subscriberListRaw}\n" +
                $"Daily Notice Limit: {DAILY_NOTICE_LIMIT}\n" +
                $"Selected State: {selectedState.Title}\n" +
                $"Selected Cities: ");
            foreach(var city in selectedCities)
            {
                Console.Write($"{city.Title}; ");
            }
            Console.WriteLine("\n\n");
        }

        private static async Task<List<State>> GetStates(bool refreshPage = false)
        {
            List<State> states = new List<State>();

            if (refreshPage)
            {
                await FetcherPageRef.ReloadAsync();
                await FetcherPageRef.WaitForLoadStateAsync(LifecycleEvent.Networkidle);
            }
            
            // parse state list
            var statesAccordian = (await FetcherPageRef.QuerySelectorAllAsync(".container .accordian__container .accordian__elemcontent")).First();
            var stateAnchorElements = await statesAccordian.QuerySelectorAllAsync(".link__column li.link__row a");

            foreach (var anchElem in stateAnchorElements)
            {
                var stateName = (await anchElem.GetAttributeAsync("data-analytics-name")).Trim();
                var fetchedCities = await GetCities(anchElem);

                string scheduleURL = (await (await FetcherPageRef.QuerySelectorAsync(".modal__box.modal--active .modal__inner .link__text:nth-child(1)")).GetAttributeAsync("href")).Trim();
                if (!scheduleURL.Contains("https://www.cvs.com")){
                    scheduleURL = $"https://www.cvs.com{scheduleURL}";
                }
                string lastUpdatedRaw = (await (await FetcherPageRef.QuerySelectorAsync(".modal__box.modal--active .modal__inner div[data-id='timestamp'] p")).GetInnerTextAsync());
                var lastUpdated = lastUpdatedRaw.Replace("Status as of", "").Replace("Availability can change quickly based on demand.", "").Trim();


                // create state obj
                var foundState = new State(
                    title: stateName,
                    cities: fetchedCities,
                    scheduleLink: (scheduleURL == null || scheduleURL.Length < 1) ? VAC_ROOT_URL : scheduleURL,
                    lastUpdated: lastUpdated
                );

                states.Add(foundState);

                // close modal
                await FetcherPageRef.ClickAsync(".modal__box.modal--active .modal__inner button");
                await FetcherPageRef.WaitForSelectorAsync(".modal__box.modal--active", state: WaitForState.Hidden);
            }

            return states;
        }

        private static async Task<List<City>> GetCities(IElementHandle stateLinkElem)
        {
            List<City> cities = new List<City>();

            // click state link
            await stateLinkElem.ClickAsync();

            await Task.Delay(1000);

            await FetcherPageRef.WaitForSelectorAsync(".modal__box.modal--active", state: WaitForState.Visible);

            var cityElements = await FetcherPageRef.QuerySelectorAllAsync(".modal__box.modal--active .covid-status table tbody tr");

            foreach (var city in cityElements)
            {
                var cityNameElem = await city.QuerySelectorAsync("td:nth-child(1) span");
                var cityName = (await cityNameElem.GetInnerTextAsync()).Trim();

                var cityStatusElem = await city.QuerySelectorAsync("td:nth-child(2) span");
                var cityStatus = (await cityStatusElem.GetInnerTextAsync()).Trim();

                //await Task.Delay(500);

                //Console.WriteLine($"City Name: {cityName}\nCity Status: {cityStatus}\n");

                // create city obj
                City foundCity = new City(
                    title: cityName,
                    isAvailable: cityStatus == "Available" ? true : false
                );

                cities.Add(foundCity);
            }

            return cities;
        }

        private static async Task StartFetcher()
        {
            await Task.Run(() =>
            {
                while (true)
                {
                   UpdateData().Wait();

                    Console.WriteLine("I'm sleeping...");
                    Thread.Sleep(FETCHER_INTERVAL);
                }
            });
        }

        private static async Task UpdateData()
        {
            /*List<State> subbedStates = new List<State>();
            List<City> subbedCities = new List<City>();

            // check which states user is subscribed to
            for (var s=0; s<FETCHED_STATES.Count; s++)
            {
                if (FETCHED_STATES[s].Subscribed)
                {
                    subbedStates.Add(FETCHED_STATES[s]);
                    for (var c=0; c<FETCHED_STATES[s].Cities.Count; c++)
                    {
                        if (FETCHED_STATES[s].Cities[c].Subscribed)
                        {
                            subbedCities.Add(FETCHED_STATES[s].Cities[c]);
                        }
                    }
                }
            }*/

            Console.WriteLine("Checking for changes in availability... (this may take a moment)");
            List<State> newStates = await GetStates(refreshPage: true);

            for (var s = 0; s < newStates.Count; s++)
            {
                var oldState = FETCHED_STATES[s];
                var newState = newStates[s];

                for (var c = 0; c < newState.Cities.Count; c++)
                {
                    // check if state was subscribed to
                    if (oldState.Subscribed)
                    {
                        // check if city was subscribed to
                        if (oldState.Cities[c].Subscribed)
                        {
                            if (oldState.Cities[c].IsAvailable == false && newState.Cities[c].IsAvailable)
                            {
                                Console.WriteLine($"Notifying ({SUBSCRIBER_EMAILS[0]}) that {newState.Title} has a new vaccine opening in: {newState.Cities[c].Title}");
                            }
                        }
                    }

                    // update old city value
                    oldState.Cities[c].IsAvailable = newState.Cities[c].IsAvailable;
                }

                // update old state value
                oldState.LastUpdated = newState.LastUpdated;
                oldState.ScheduleLink = newState.ScheduleLink;
            }
        }
    }
}
