// SPDX-License-Identifier: GPL-3.0-or-later
using System;
using System.Collections.Generic;
using System.IO;

namespace Jellyfin.Plugin.CollectionsByBoxset.Services;

/// <summary>Finds the nearest ancestor folder ending in [boxset], within allowed roots.</summary>
public static class BoxsetFolderResolver
{
    public const string Marker = "[boxset]";

    private static StringComparison Comparison => OperatingSystem.IsWindows()
        ? StringComparison.OrdinalIgnoreCase
        : StringComparison.Ordinal;

    public static string? Resolve(string? moviePath, IEnumerable<string> roots, IEnumerable<string> exclusions)
    {
        if (string.IsNullOrWhiteSpace(moviePath) || !Path.IsPathFullyQualified(moviePath))
        {
            return null;
        }

        var path = Path.GetFullPath(moviePath);
        foreach (var excluded in exclusions)
        {
            if (IsWithin(path, excluded))
            {
                return null;
            }
        }

        var rootList = new List<string>(roots);
        if (rootList.Count == 0)
        {
            rootList.Add(Path.GetPathRoot(path) ?? string.Empty);
        }

        foreach (var root in rootList)
        {
            if (!IsWithin(path, root))
            {
                continue;
            }

            var boundary = Path.TrimEndingDirectorySeparator(Path.GetFullPath(root));
            var current = Path.GetDirectoryName(path);
            while (current is not null && IsWithin(current, boundary))
            {
                var trimmed = Path.TrimEndingDirectorySeparator(current);
                if (Path.GetFileName(trimmed).EndsWith(Marker, StringComparison.OrdinalIgnoreCase)
                    && GetCollectionName(trimmed).Length > 0)
                {
                    return trimmed;
                }

                if (string.Equals(trimmed, boundary, Comparison))
                {
                    break;
                }

                current = Path.GetDirectoryName(trimmed);
            }
        }

        return null;
    }

    public static string GetCollectionName(string folder)
    {
        var name = Path.GetFileName(Path.TrimEndingDirectorySeparator(folder));
        return name.EndsWith(Marker, StringComparison.OrdinalIgnoreCase)
            ? name[..^Marker.Length].Trim()
            : name;
    }

    private static bool IsWithin(string path, string root)
    {
        if (string.IsNullOrWhiteSpace(root) || !Path.IsPathFullyQualified(root))
        {
            return false;
        }

        var normalized = Path.TrimEndingDirectorySeparator(Path.GetFullPath(root));
        if (string.Equals(path, normalized, Comparison))
        {
            return true;
        }

        var prefix = Path.EndsInDirectorySeparator(normalized)
            ? normalized
            : normalized + Path.DirectorySeparatorChar;
        return path.StartsWith(prefix, Comparison);
    }
}
