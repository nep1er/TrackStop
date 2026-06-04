using System.Windows;
using System.Windows.Controls;

namespace TrackStop.Models.Search
{
    public class SearchResultsTemplateSelector : DataTemplateSelector
    {
        public DataTemplate SectionHeaderTemplate { get; set; }
        public DataTemplate SeparatorTemplate { get; set; }
        public DataTemplate NoResultsTemplate { get; set; }
        public DataTemplate GenreTemplate { get; set; }
        public DataTemplate ArtistTemplate { get; set; }
        public DataTemplate AlbumTemplate { get; set; }
        public DataTemplate SongTemplate { get; set; }

        public override DataTemplate SelectTemplate(object item, DependencyObject container)
        {
            return item switch
            {
                SectionHeaderItem => SectionHeaderTemplate,
                SeparatorItem => SeparatorTemplate,
                NoResultsItem => NoResultsTemplate,
                GenreSearchItem => GenreTemplate,
                Artist => ArtistTemplate,
                Album => AlbumTemplate,
                Song => SongTemplate,
                _ => base.SelectTemplate(item, container)
            };
        }
    }
}