using System;
using System.ComponentModel;
using System.Runtime.CompilerServices;

namespace TrackStop.Models.Search
{
    public class SearchResultItem
    {
        public SearchItemType Type { get; set; }
        public object Item { get; set; }

        public string GroupTitle => Type switch
        {
            SearchItemType.Genre => "Жанры",
            SearchItemType.Artist => "Исполнители",
            SearchItemType.Album => "Альбомы",
            SearchItemType.Song => "Песни",
            _ => ""
        };
    }

}