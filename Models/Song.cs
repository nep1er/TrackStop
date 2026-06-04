using System.ComponentModel;
using System.Runtime.CompilerServices;

public class Song : INotifyPropertyChanged
{
    public int Id { get; set; }
    public string Title { get; set; }
    public string Artist { get; set; }
    public string Album { get; set; }
    public string Duration { get; set; }
    public string FilePath { get; set; }
    public string CoverPath { get; set; }
    public int GenreId { get; set; }
    public string Genre { get; set; }

    private bool _isFavorite;
    public int AlbumId { get; set; }

    public bool IsFavorite
    {
        get => _isFavorite;
        set
        {
            _isFavorite = value;
            OnPropertyChanged();
        }
    }

    public event PropertyChangedEventHandler PropertyChanged;
    protected void OnPropertyChanged([CallerMemberName] string name = null) =>
        PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(name));
}