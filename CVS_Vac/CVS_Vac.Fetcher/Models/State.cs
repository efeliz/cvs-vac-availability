using System;
using System.Collections.Generic;

namespace CVS_Vac.Fetcher.Models
{
    public class State
    {
        public State(string title, List<City> cities, bool subscribed = false)
        {
            Title = title;
            Cities = cities;
            Subscribed = subscribed;
        }

        public string Title { get; set; }
        public List<City> Cities { get; set; }
        public bool Subscribed { get; set; }
    }
}
