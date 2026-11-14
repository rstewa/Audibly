using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;

namespace Audibly.App.Helpers;

/// <summary>
///     Chapter titles for audiobooks made of several files. A file without chapter marks becomes one
///     chapter named after its title tag; when those tags are missing or repeat (often the book title
///     on every file), the chapters cannot be told apart, so the file names are used instead.
/// </summary>
public static class FileChapterTitles
{
    /// <summary>
    ///     Returns the title to show for each chapter, in the order given.
    /// </summary>
    /// <param name="chapters">Each chapter's source file index and current title.</param>
    /// <param name="sourcePaths">The audiobook's files, by source file index.</param>
    public static string[] Resolve(IReadOnlyList<(int SourceIndex, string? Title)> chapters,
        IReadOnlyList<string> sourcePaths)
    {
        var titles = chapters.Select(chapter => chapter.Title ?? string.Empty).ToArray();
        if (sourcePaths.Count < 2) return titles;

        // Only a file's single chapter stands for the whole file; real chapter marks keep their names.
        var perFile = chapters.GroupBy(chapter => chapter.SourceIndex).ToDictionary(g => g.Key, g => g.Count());
        var wholeFile = Enumerable.Range(0, chapters.Count)
            .Where(i => perFile[chapters[i].SourceIndex] == 1 &&
                        chapters[i].SourceIndex >= 0 && chapters[i].SourceIndex < sourcePaths.Count)
            .ToArray();

        var tagTitles = wholeFile.Select(i => titles[i].Trim()).ToArray();
        var distinguishable = tagTitles.All(title => title.Length > 0) &&
                              tagTitles.Distinct(StringComparer.OrdinalIgnoreCase).Count() == tagTitles.Length;
        if (distinguishable) return titles;

        foreach (var i in wholeFile)
        {
            var name = Path.GetFileNameWithoutExtension(sourcePaths[chapters[i].SourceIndex]);
            if (!string.IsNullOrWhiteSpace(name)) titles[i] = name;
        }

        return titles;
    }
}
