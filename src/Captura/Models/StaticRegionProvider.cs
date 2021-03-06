﻿using Screna;
using System.Drawing;

namespace Captura
{
    class StaticRegionProvider : IImageProvider
    {
        readonly RegionSelector _selector;

        public StaticRegionProvider(RegionSelector RegionSelector)
        {
            _selector = RegionSelector;

            var rect = _selector.SelectedRegion.Even();
            Height = rect.Height;
            Width = rect.Width;            
        }
        
        public Bitmap Capture() => ScreenShot.Capture(_selector.SelectedRegion.Even());

        public int Height { get; }

        public int Width { get; }
        
        public void Dispose() { }
    }
}
