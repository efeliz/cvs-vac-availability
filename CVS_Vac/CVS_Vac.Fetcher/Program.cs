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

            // get available states/territories
            FETCHED_STATES = await GetStates();
            foreach (var state in FETCHED_STATES)
            {
                Console.WriteLine($"State: {state.Title}\nCities:{state.Cities.Count}");
            }

            // run initial setup
            //await InitialSetup();
        }

        private static async Task SetupScraper()
        {
            PlaywrightRef = await Playwright.CreateAsync();
            BrowserRef = await PlaywrightRef.Firefox.LaunchAsync(headless: false);
            FetcherPageRef = await BrowserRef.NewPageAsync();
            await FetcherPageRef.GoToAsync(VAC_ROOT_URL, waitUntil: LifecycleEvent.Networkidle);

            await Task.Delay(5000);
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
            List<State> foundStates = await GetStates();
            foreach (var state in foundStates)
            {
                Console.WriteLine($"State: {state.Title}\nCities:{state.Cities.Count}");
            }
        }

        private static async Task<List<State>> GetStates()
        {
            List<State> states = new List<State>();

            // await FetcherPageRef.ReloadAsync();

            // parse state list
            var statesAccordian = (await FetcherPageRef.QuerySelectorAllAsync(".container .accordian__container .accordian__elemcontent")).First();
            var stateAnchorElements = await statesAccordian.QuerySelectorAllAsync(".link__column li.link__row a");
            foreach (var anchElem in stateAnchorElements)
            {
                var stateName = (await anchElem.GetAttributeAsync("data-analytics-name")).Trim();
                var fetchedCities = await GetCities(anchElem);

                // create state obj
                var foundState = new State(
                    title: stateName,
                    cities: fetchedCities
                );

                states.Add(foundState);

                // close modal
                await FetcherPageRef.ClickAsync(".modal__box.modal--active .modal__inner button");
                await FetcherPageRef.WaitForSelectorAsync(".modal__box modal--active", state: WaitForState.Hidden);
            }

            await Task.Delay(5000);

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
    }
}
