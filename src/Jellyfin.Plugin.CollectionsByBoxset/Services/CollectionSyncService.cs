// SPDX-License-Identifier: GPL-3.0-or-later
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Jellyfin.Data.Enums;
using MediaBrowser.Controller.Collections;
using MediaBrowser.Controller.Entities;
using MediaBrowser.Controller.Entities.Movies;
using MediaBrowser.Controller.Library;
using Microsoft.Extensions.Logging;

namespace Jellyfin.Plugin.CollectionsByBoxset.Services;

public sealed record SyncResult(int Created, int Updated, int Skipped);

public sealed class CollectionSyncService
{
    private static readonly SemaphoreSlim Gate = new(1, 1);
    private static readonly string[] ImageStems =
        { "poster", "folder", "cover", "backdrop", "fanart", "logo", "banner", "thumb", "landscape" };
    private static readonly string[] ImageExtensions = { ".jpg", ".jpeg", ".png", ".webp" };

    private readonly ILibraryManager _library;
    private readonly ICollectionManager _collections;
    private readonly ILogger _log;

    public CollectionSyncService(ILibraryManager library, ICollectionManager collections, ILogger log)
    {
        _library = library;
        _collections = collections;
        _log = log;
    }

    public async Task<SyncResult> RunAsync(PluginConfiguration cfg, CancellationToken ct)
    {
        if (!await Gate.WaitAsync(0, ct).ConfigureAwait(false))
        {
            throw new InvalidOperationException("Ein Scan laeuft bereits.");
        }

        try
        {
            return await RunCoreAsync(cfg, ct).ConfigureAwait(false);
        }
        finally
        {
            Gate.Release();
        }
    }

    private async Task<SyncResult> RunCoreAsync(PluginConfiguration cfg, CancellationToken ct)
    {
        var roots = cfg.Roots.Where(r => !string.IsNullOrWhiteSpace(r)).ToList();
        var exclusions = cfg.Exclusions.Where(r => !string.IsNullOrWhiteSpace(r)).ToList();
        var min = Math.Max(1, cfg.MinMovies);

        var movies = _library.GetItemList(new InternalItemsQuery
        {
            IncludeItemTypes = new[] { BaseItemKind.Movie },
            Recursive = true
        }).OfType<Movie>().ToList();

        var folders = new Dictionary<string, List<Guid>>(StringComparer.OrdinalIgnoreCase);
        foreach (var movie in movies)
        {
            var folder = BoxsetFolderResolver.Resolve(movie.Path, roots, exclusions);
            if (folder is null)
            {
                continue;
            }

            if (!folders.TryGetValue(folder, out var ids))
            {
                ids = new List<Guid>();
                folders[folder] = ids;
            }

            ids.Add(movie.Id);
        }

        var nameConflicts = folders.Keys
            .GroupBy(f => BoxsetFolderResolver.GetCollectionName(f), StringComparer.OrdinalIgnoreCase)
            .Where(g => g.Count() > 1)
            .ToList();
        var conflictedFolders = new HashSet<string>(
            nameConflicts.SelectMany(g => g), StringComparer.OrdinalIgnoreCase);
        foreach (var group in nameConflicts)
        {
            _log.LogWarning("[CBB] Namenskonflikt '{Name}': mehrere Ordner, uebersprungen: {Folders}",
                group.Key, string.Join(" | ", group));
        }

        int created = 0, updated = 0, skipped = conflictedFolders.Count;
        foreach (var (folder, ids) in folders)
        {
            ct.ThrowIfCancellationRequested();
            if (conflictedFolders.Contains(folder) || ids.Count < min)
            {
                continue;
            }

            var name = BoxsetFolderResolver.GetCollectionName(folder);
            var desired = ids.Distinct().ToHashSet();
            var entry = cfg.Managed.FirstOrDefault(
                e => string.Equals(e.FolderPath, folder, StringComparison.OrdinalIgnoreCase));
            var box = entry is null ? null : _library.GetItemById(entry.CollectionId) as BoxSet;

            if (box is null)
            {
                var sameName = _library.GetItemList(new InternalItemsQuery
                {
                    IncludeItemTypes = new[] { BaseItemKind.BoxSet },
                    Name = name
                }).OfType<BoxSet>().FirstOrDefault();
                if (sameName is not null)
                {
                    _log.LogWarning("[CBB] Sammlung '{Name}' existiert bereits und wird nicht vom Plugin verwaltet, uebersprungen.", name);
                    skipped++;
                    continue;
                }

                box = await _collections.CreateCollectionAsync(new CollectionCreationOptions { Name = name })
                    .ConfigureAwait(false);
                if (entry is null)
                {
                    cfg.Managed.Add(new ManagedCollection { FolderPath = folder, CollectionId = box.Id });
                }
                else
                {
                    entry.CollectionId = box.Id;
                }

                created++;
                _log.LogInformation("[CBB] Sammlung '{Name}' erstellt ({Count} Filme)", name, desired.Count);
            }

            var linkedMovies = box.GetLinkedChildren().OfType<Movie>().Select(m => m.Id).ToHashSet();
            var toAdd = desired.Except(linkedMovies).ToList();
            var toRemove = cfg.RemoveMissing ? linkedMovies.Except(desired).ToList() : new List<Guid>();

            if (toAdd.Count > 0)
            {
                await _collections.AddToCollectionAsync(box.Id, toAdd).ConfigureAwait(false);
            }

            if (toRemove.Count > 0)
            {
                await _collections.RemoveFromCollectionAsync(box.Id, toRemove).ConfigureAwait(false);
            }

            if (toAdd.Count > 0 || toRemove.Count > 0)
            {
                updated++;
            }

            if (cfg.CopyImages)
            {
                CopyImages(folder, box);
            }
        }

        Plugin.Instance?.UpdateConfiguration(cfg);
        _log.LogInformation("[CBB] Scan fertig: erstellt={C} aktualisiert={U} uebersprungen={S}", created, updated, skipped);
        return new SyncResult(created, updated, skipped);
    }

    private void CopyImages(string sourceFolder, BoxSet box)
    {
        try
        {
            var target = box.Path;
            if (string.IsNullOrWhiteSpace(target) || !Directory.Exists(target) || !Directory.Exists(sourceFolder))
            {
                return;
            }

            var targetFiles = Directory.GetFiles(target);
            foreach (var file in Directory.GetFiles(sourceFolder))
            {
                var stem = Path.GetFileNameWithoutExtension(file).ToLowerInvariant();
                var ext = Path.GetExtension(file).ToLowerInvariant();
                if (!ImageStems.Contains(stem) || !ImageExtensions.Contains(ext))
                {
                    continue;
                }

                var exists = targetFiles.Any(t =>
                    string.Equals(Path.GetFileNameWithoutExtension(t), stem, StringComparison.OrdinalIgnoreCase));
                if (!exists)
                {
                    File.Copy(file, Path.Combine(target, Path.GetFileName(file)), overwrite: false);
                }
            }
        }
        catch (Exception ex) when (ex is IOException or UnauthorizedAccessException)
        {
            _log.LogWarning(ex, "[CBB] Bilder fuer '{Name}' konnten nicht kopiert werden", box.Name);
        }
    }
}
