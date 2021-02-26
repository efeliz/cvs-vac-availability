using System;
using System.Collections.Generic;

namespace CVS_Vac.Fetcher.Models
{
    public class State
    {
        public string Title { get; set; }
        public List<City> Cities { get; set; }
        public bool Subscribed { get; set; }
    }
}
