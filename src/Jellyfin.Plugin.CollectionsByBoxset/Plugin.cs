// SPDX-License-Identifier: GPL-3.0-or-later
using System;
using System.Collections.Generic;
using MediaBrowser.Common.Configuration;
using MediaBrowser.Common.Plugins;
using MediaBrowser.Model.Plugins;
using MediaBrowser.Model.Serialization;

namespace Jellyfin.Plugin.CollectionsByBoxset;

/// <summary>Creates Jellyfin collections from folders whose name ends with [boxset].</summary>
public class Plugin : BasePlugin<PluginConfiguration>, IHasWebPages
{
    public Plugin(IApplicationPaths applicationPaths, IXmlSerializer xmlSerializer)
        : base(applicationPaths, xmlSerializer)
    {
        Instance = this;
    }

    public static Plugin? Instance { get; private set; }

    public override string Name => "Collections by Boxset";

    public override string Description =>
        "Erstellt Sammlungen aus Ordnern, deren Name auf [boxset] endet.";

    public override Guid Id => Guid.Parse("92e7bf59-1970-45c9-8df1-714f7eb80f55");

    public IEnumerable<PluginPageInfo> GetPages()
    {
        yield return new PluginPageInfo
        {
            Name = "CollectionsByBoxset",
            EmbeddedResourcePath = GetType().Namespace + ".Web.config.html"
        };
    }
}
