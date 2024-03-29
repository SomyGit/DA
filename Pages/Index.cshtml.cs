﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.Extensions.FileProviders;

namespace DA.Pages
{
    public class IndexModel : PageModel
    {
        private readonly IFileProvider _fileProvider;

        public IndexModel(IFileProvider fileProvider)
        {
            _fileProvider = fileProvider;
        }

        public IDirectoryContents DirectoryContents { get; private set; }

        public void OnGet()
        {
            DirectoryContents = _fileProvider.GetDirectoryContents(string.Empty);
        }
    }
}
