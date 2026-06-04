using System.ComponentModel;
using System.Runtime.CompilerServices;

namespace TrackStop.Models
{
    public class Genre : INotifyPropertyChanged
    {
        public int Id { get; set; }
        public string Name { get; set; }

        public event PropertyChangedEventHandler PropertyChanged;
        protected void OnPropertyChanged([CallerMemberName] string name = null) =>
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(name));
    }
} 