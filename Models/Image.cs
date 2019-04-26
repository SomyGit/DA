using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;

namespace DA.Models
{

    public class Image
    {
        public Image()
        {
            // hard-coded to a single thumbnail at 200 x 300 for now
            Thumbnails = new List<Thumbnail> { new Thumbnail { Width = 200, Height = 300 } };
        }
        public string Name { get; set; }
        public string ContentType { get; set; }
        public byte[] Data { get; set; }

        public string FileName { get; set; }
        public string outputText { get; set; }
        public string Url { get; set; }
        public List<Thumbnail> Thumbnails { get; set; }

        public Stream PhotoStream { get; set; }

        public string audioSrc { get; set; }

        public string desc { get; set; }
    }
}

