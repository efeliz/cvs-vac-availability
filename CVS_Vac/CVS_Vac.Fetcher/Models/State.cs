using System;
using System.Collections.Generic;

namespace CVS_Vac.Fetcher.Models
{
    public class State
    {
        public State(string title, List<City> cities, string scheduleLink, string lastUpdated, bool subscribed = false)
        {
            Title = title;
            Cities = cities;
            Subscribed = subscribed;
            ScheduleLink = scheduleLink;
            LastUpdated = lastUpdated;
        }

        public string Title { get; set; }
        public List<City> Cities { get; set; }
        public bool Subscribed { get; set; }
        public string ScheduleLink { get; set; }
        public string LastUpdated { get; set; }
    }
}
