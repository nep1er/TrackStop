using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace TrackStop.Models.Search
{
    public class GenreSearchItem
    {
        public Genre Genre { get; set; }
        public string DisplayName { get; set; }
        public string TypeIcon => "🎵";
    }
}