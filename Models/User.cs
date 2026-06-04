using System.ComponentModel;
using System.Runtime.CompilerServices;

namespace TrackStop.Models
{
    public class User : INotifyPropertyChanged
    {
        private int _userId;
        private string _login;
        private string _password;
        private string _role;

        public int UserId
        {
            get => _userId;
            set
            {
                _userId = value;
                OnPropertyChanged();
            }
        }

        public string Login
        {
            get => _login;
            set
            {
                _login = value;
                OnPropertyChanged();
            }
        }

        public string Password
        {
            get => _password;
            set
            {
                _password = value;
                OnPropertyChanged();
            }
        }

        public string Role
        {
            get => _role;
            set
            {
                _role = value;
                OnPropertyChanged();
            }
        }

        public bool IsArtist => Role == "artist";
        public bool IsAdmin => Role == "admin";
        public bool IsRegularUser => Role == "user";

        public event PropertyChangedEventHandler PropertyChanged;
        protected void OnPropertyChanged([CallerMemberName] string name = null) =>
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(name));
    }
}