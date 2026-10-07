// SPDX-License-Identifier: GPL-3.0-or-later
using Jellyfin.Plugin.CollectionsByBoxset.Services;
using Xunit;

namespace Jellyfin.Plugin.CollectionsByBoxset.Tests;

public class BoxsetFolderResolverTests
{
    private static readonly string[] Roots = { "/media/Filme" };
    private static readonly string[] None = Array.Empty<string>();

    [Fact]
    public void Finds_marked_parent_for_nested_movie()
    {
        var r = BoxsetFolderResolver.Resolve("/media/Filme/Alien [boxset]/Alien (1979)/Alien.mkv", Roots, None);
        Assert.Equal("/media/Filme/Alien [boxset]", r);
    }

    [Fact]
    public void Finds_marked_parent_for_flat_movie()
    {
        var r = BoxsetFolderResolver.Resolve("/media/Filme/Alien [boxset]/Alien.mkv", Roots, None);
        Assert.Equal("/media/Filme/Alien [boxset]", r);
    }

    [Fact]
    public void Unmarked_folder_is_ignored()
    {
        Assert.Null(BoxsetFolderResolver.Resolve("/media/Filme/Alien/Alien.mkv", Roots, None));
    }

    [Fact]
    public void Nearest_marker_wins()
    {
        var r = BoxsetFolderResolver.Resolve("/media/Filme/A [boxset]/B [boxset]/x/x.mkv", Roots, None);
        Assert.Equal("/media/Filme/A [boxset]/B [boxset]", r);
    }

    [Fact]
    public void Sibling_path_prefix_is_not_inside_root()
    {
        Assert.Null(BoxsetFolderResolver.Resolve("/media/FilmeBackup/Alien [boxset]/a.mkv", Roots, None));
    }

    [Fact]
    public void Exclusion_wins()
    {
        var r = BoxsetFolderResolver.Resolve(
            "/media/Filme/Alien [boxset]/a.mkv", Roots, new[] { "/media/Filme/Alien [boxset]" });
        Assert.Null(r);
    }

    [Fact]
    public void Empty_roots_allow_any_path()
    {
        var r = BoxsetFolderResolver.Resolve("/x/Reihe [boxset]/f/f.mkv", None, None);
        Assert.Equal("/x/Reihe [boxset]", r);
    }

    [Fact]
    public void Marker_is_removed_from_name_only()
    {
        Assert.Equal("Alien", BoxsetFolderResolver.GetCollectionName("/media/Filme/Alien [boxset]"));
        Assert.Equal("Alien", BoxsetFolderResolver.GetCollectionName("/media/Filme/Alien [BOXSET]"));
    }

    [Fact]
    public void Folder_consisting_only_of_marker_is_ignored()
    {
        Assert.Null(BoxsetFolderResolver.Resolve("/media/Filme/[boxset]/a.mkv", Roots, None));
    }
}
