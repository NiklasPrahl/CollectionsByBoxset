// SPDX-License-Identifier: GPL-3.0-or-later
using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Jellyfin.Plugin.CollectionsByBoxset.Services;
using MediaBrowser.Controller.Collections;
using MediaBrowser.Controller.Library;
using MediaBrowser.Model.Tasks;
using Microsoft.Extensions.Logging;

namespace Jellyfin.Plugin.CollectionsByBoxset.Tasks;

public sealed class BoxsetSyncTask : IScheduledTask
{
    private readonly ILibraryManager _library;
    private readonly ICollectionManager _collections;
    private readonly ILoggerFactory _loggerFactory;

    public BoxsetSyncTask(ILibraryManager library, ICollectionManager collections, ILoggerFactory loggerFactory)
    {
        _library = library;
        _collections = collections;
        _loggerFactory = loggerFactory;
    }

    public string Name => "Sammlungen aus [boxset]-Ordnern aktualisieren";

    public string Key => "CollectionsByBoxsetSync";

    public string Description => "Erstellt und aktualisiert Sammlungen aus Ordnern mit [boxset] im Namen.";

    public string Category => "Library";

    public Task ExecuteAsync(IProgress<double> progress, CancellationToken cancellationToken)
    {
        var cfg = Plugin.Instance?.Configuration ?? new PluginConfiguration();
        var service = new CollectionSyncService(_library, _collections, _loggerFactory.CreateLogger<BoxsetSyncTask>());
        return RunAsync(service, cfg, progress, cancellationToken);
    }

    public IEnumerable<TaskTriggerInfo> GetDefaultTriggers() => Array.Empty<TaskTriggerInfo>();

    private static async Task RunAsync(
        CollectionSyncService service, PluginConfiguration cfg, IProgress<double> progress, CancellationToken ct)
    {
        progress.Report(0);
        await service.RunAsync(cfg, ct).ConfigureAwait(false);
        progress.Report(100);
    }
}
