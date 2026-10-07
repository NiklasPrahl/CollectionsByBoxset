// SPDX-License-Identifier: GPL-3.0-or-later
using System;
using System.Threading;
using System.Threading.Tasks;
using Jellyfin.Plugin.CollectionsByBoxset.Services;
using MediaBrowser.Controller.Collections;
using MediaBrowser.Controller.Library;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;

namespace Jellyfin.Plugin.CollectionsByBoxset.Api;

[ApiController]
[Route("Plugins/CollectionsByBoxset")]
[Authorize(Policy = "RequiresElevation")]
public sealed class CollectionsByBoxsetController : ControllerBase
{
    private readonly ILibraryManager _library;
    private readonly ICollectionManager _collections;
    private readonly ILoggerFactory _loggerFactory;

    public CollectionsByBoxsetController(
        ILibraryManager library, ICollectionManager collections, ILoggerFactory loggerFactory)
    {
        _library = library;
        _collections = collections;
        _loggerFactory = loggerFactory;
    }

    [HttpPost("Scan")]
    public async Task<ActionResult<SyncResult>> Scan(CancellationToken cancellationToken)
    {
        var cfg = Plugin.Instance?.Configuration ?? new PluginConfiguration();
        var service = new CollectionSyncService(
            _library, _collections, _loggerFactory.CreateLogger<CollectionsByBoxsetController>());
        try
        {
            return Ok(await service.RunAsync(cfg, cancellationToken).ConfigureAwait(false));
        }
        catch (InvalidOperationException ex)
        {
            return Conflict(ex.Message);
        }
    }
}
