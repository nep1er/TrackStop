using System.ComponentModel;
using System.Runtime.CompilerServices;

namespace TrackStop.Models
{
    public class Artist : INotifyPropertyChanged
    {
        public int Id { get; set; }
        public string Name { get; set; }
        public string Bio { get; set; }
        public string PhotoPath { get; set; }
        public int Subscribers { get; set; }

        public int? UserId { get; set; }

        private bool _isFavorite;
        public bool IsFavorite
        {
            get => _isFavorite;
            set
            {
                _isFavorite = value;
                OnPropertyChanged();
            }
        }

        public string FormattedSubscribers => FormatSubscribers(Subscribers);

        private string FormatSubscribers(int subscribers)
        {
            if (subscribers < 1000)
                return subscribers.ToString();

            if (subscribers < 1000000)
            {
                double kSubscribers = subscribers / 1000.0;
                return kSubscribers % 1 == 0 ? $"{kSubscribers:0}k" : $"{kSubscribers:0.#}k";
            }

            double mSubscribers = subscribers / 1000000.0;
            return mSubscribers % 1 == 0 ? $"{mSubscribers:0}M" : $"{mSubscribers:0.#}M";
        }



        public event PropertyChangedEventHandler PropertyChanged;
        protected void OnPropertyChanged([CallerMemberName] string name = null) =>
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(name));
    }
}