using System.Collections.Generic;
using System.Windows;

namespace  AutoFilterPresets.Models
{
    public class FilterImages : ObservableObject
    {
        public string Name;
        private string image;
        public string Image { get => image; set => SetValue(ref image, value); }

        public string OriginalImage;

        private string background;
        public string Background { get => background; set => SetValue(ref background, value); }

        public string OriginalBackground;

        public FilterImages(string Name)
        {
            this.Name = Name;
        }
    }
}