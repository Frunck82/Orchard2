﻿using System.Collections.Generic;
using System.IO;
using System.Linq;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.ApplicationParts;
using Microsoft.AspNetCore.Mvc.Razor;
using Microsoft.AspNetCore.Mvc.Razor.Compilation;
using Microsoft.Extensions.FileSystemGlobbing;
using Microsoft.Extensions.FileSystemGlobbing.Abstractions;
using Microsoft.Extensions.Options;
using Orchard.Environment.Extensions;

namespace Orchard.DisplayManagement.Fluid
{
    public class FluidViewsFeatureProvider : IApplicationFeatureProvider<ViewsFeature>
    {
        private static List<string> _paths;
        private static object _synLock = new object();

        private readonly string _rootPath;
        private readonly ExtensionExpanderOptions _expanderOptions;

        public FluidViewsFeatureProvider(
            IHostingEnvironment hostingEnvironment,
            IOptions<ExtensionExpanderOptions> expanderOptionsAccessor)
        {
            _rootPath = hostingEnvironment.ContentRootPath;
            _expanderOptions = expanderOptionsAccessor.Value;

            if (_paths != null)
            {
                return;
            }

            lock (_synLock)
            {
                if (_paths == null)
                {
                    var matcher = new Matcher();
                    foreach (var option in _expanderOptions.Options)
                    {
                        matcher.AddInclude(option.SearchPath.Replace("\\", "/").TrimEnd('/') + "/**/*" + FluidView.ViewExtension);
                    }

                    var matches = matcher.Execute(new DirectoryInfoWrapper(new DirectoryInfo(_rootPath)));
                    _paths = new List<string>(matches.Files.Select(f => '/' + f.Path));
                }
            }
        }

        public void PopulateFeature(IEnumerable<ApplicationPart> parts, ViewsFeature feature)
        {
            foreach (var path in _paths)
            {
                feature.Views[path.Replace(FluidView.ViewExtension, RazorViewEngine.ViewExtension)] = typeof(FluidView);
            }
        }
    }
}
