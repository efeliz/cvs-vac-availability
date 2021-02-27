using System;

namespace CVS_Vac.Fetcher.Models
{
    public class City
    {
        public City(string title, bool isAvailable, bool subscribed = false)
        {
            Title = title;
            IsAvailable = isAvailable;
            Subscribed = subscribed;
        }

        public string Title { get; set; }
        public bool IsAvailable { get; set; }
        public bool Subscribed { get; set; }
    }
}
