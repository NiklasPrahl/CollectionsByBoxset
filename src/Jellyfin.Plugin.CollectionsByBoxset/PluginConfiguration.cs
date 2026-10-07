// SPDX-License-Identifier: GPL-3.0-or-later
using System;
using System.Collections.Generic;
using MediaBrowser.Model.Plugins;

namespace Jellyfin.Plugin.CollectionsByBoxset;

public class PluginConfiguration : BasePluginConfiguration
{
    /// <summary>Allowed root folders. Empty = all library paths.</summary>
    public List<string> Roots { get; set; } = new();

    public List<string> Exclusions { get; set; } = new();

    public int MinMovies { get; set; } = 1;

    public bool RemoveMissing { get; set; } = true;

    public bool CopyImages { get; set; } = true;

    /// <summary>Collections created by this plugin. Only these are ever modified.</summary>
    public List<ManagedCollection> Managed { get; set; } = new();
}

public class ManagedCollection
{
    public string FolderPath { get; set; } = string.Empty;

    public Guid CollectionId { get; set; }
}
