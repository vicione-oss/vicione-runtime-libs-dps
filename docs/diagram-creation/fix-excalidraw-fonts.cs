#!/usr/bin/env dotnet

// Make Excalidraw SVG exports render with local fonts only.
//
// .NET 10 file-based app — run with:
//   dotnet run docs/diagram-creation/fix-excalidraw-fonts.cs [folder ...]
//
// Excalidraw SVG exports (e.g. from the IntelliJ plugin) declare @font-face
// rules that download Virgil/Cascadia/Assistant from excalidraw.com. Browsers
// block external requests inside SVGs rendered as images (which is how markdown
// embeds them), so text silently falls back to the browser default font.
//
// This script rewrites each export to be self-contained and offline:
//   - the <style> block is replaced by a single @font-face that resolves
//     Virgil from the locally installed font ("Virgil 3 YOFF" / "Virgil3YOFF"),
//     with no remote URL
//   - Excalidraw's class="style-fonts" on that block is dropped: the exports
//     declare version="1.1", and SVG 1.1 does not allow class on <style>
//     (IDE schema validation flags it); nothing references the class
//   - text font stacks become "Virgil, Helvetica" so machines without the font
//     get a clean sans-serif instead of an emoji/default font
//
// The font names are configurable in one place — see the constants at the top
// of ExcalidrawFonts.
//
// Idempotent — run it after every SVG (re-)export. Without arguments it
// processes all diagrams folders anywhere under docs/. Only files carrying
// the "svg-source:excalidraw" marker are touched.

using System.Runtime.CompilerServices;
using System.Text;
using System.Text.RegularExpressions;

var repoRoot = ExcalidrawFonts.RepoRoot();

// Positional args are the folders to scan; none means use the defaults.
var folders = args.Length > 0
    ? args.Select(arg => new DirectoryInfo(arg))
    : ExcalidrawFonts.DefaultFolders(repoRoot);

int fixedCount = 0, alreadyFixed = 0, skipped = 0;
foreach (var folder in folders)
{
    if (!folder.Exists)
        continue;

    foreach (var svg in folder.GetFiles("*.svg").OrderBy(file => file.FullName, StringComparer.Ordinal))
    {
        var rel = Path.GetRelativePath(repoRoot.FullName, svg.FullName);
        var fontAdjustmentResult = ExcalidrawFonts.AdjustFont(svg);
        switch (fontAdjustmentResult)
        {
            case FontAdjustmentResult.NotExcalidraw:
                skipped++;
                Console.WriteLine($"skipped (not an excalidraw export): {rel}");
                break;
            case FontAdjustmentResult.Fixed:
                fixedCount++;
                Console.WriteLine($"fixed: {rel}");
                break;
            case FontAdjustmentResult.AlreadyFixed:
                alreadyFixed++;
                break;
        }
    }
}

Console.WriteLine($"done: {fixedCount} fixed, {alreadyFixed} already fixed, {skipped} skipped");
return 0;


// What AdjustFont did with one SVG.
internal enum FontAdjustmentResult { NotExcalidraw, Fixed, AlreadyFixed }


// Locating the diagrams, rewriting one SVG, and the constant replacement text.
internal static partial class ExcalidrawFonts
{
    // --- Configuration -------------------------------------------------------
    // The Excalidraw drawing font: the @font-face family name, the primary entry
    // in every text element's font stack, and the marker that decides which
    // font-family attributes get rewritten.
    private const string PrimaryFont = "Virgil";

    // The locally-installed font names that resolve PrimaryFont offline, written
    // verbatim into the @font-face "src: local(...)" list (try order preserved).
    private const string LocalSources = "local(\"Virgil 3 YOFF\"), local(\"Virgil3YOFF\")";

    // The sans-serif shown on machines that don't have PrimaryFont installed.
    private const string FallbackFont = "Helvetica";

    // The marker Excalidraw embeds in every export; files without it are skipped.
    private const string ExcalidrawMarker = "svg-source:excalidraw";
    // -------------------------------------------------------------------------

    // The single offline @font-face that replaces the <style> block's body.
    // Whitespace matches the IntelliJ export so an already-fixed file rewrites to
    // itself (no diff).
    private const string LocalFonts =
        "\n" +
        "      @font-face {\n" +
        $"        font-family: \"{PrimaryFont}\";\n" +
        $"        src: {LocalSources};\n" +
        "      }\n" +
        "    ";

    // Substitution for StyleBlock: a bare <style> tag (no class attribute) around
    // the offline body.
    private const string StyleReplacement = "<style>" + LocalFonts + "</style>";

    // The font stack written onto every PrimaryFont text element.
    private const string FontStack = $"font-family=\"{PrimaryFont}, {FallbackFont}\"";

    // UTF-8 without a BOM, matching the on-disk SVG encoding on read & write.
    private static readonly UTF8Encoding s_utf8NoBom = new(encoderShouldEmitUTF8Identifier: false);

    // The export's single <style> block carries the remote @font-face rules;
    // matched whole (Singleline so "." spans the newlines) regardless of
    // attributes, so both fresh exports (class="style-fonts") and already-fixed
    // files (bare <style>) match. Safe because only files carrying the
    // ExcalidrawMarker are processed and those contain exactly one <style>.
    [GeneratedRegex("""<style[^>]*>.*?</style>""", RegexOptions.Singleline)]
    private static partial Regex StyleBlock();

    // Every text element's font stack, e.g. font-family="Virgil, Segoe UI Emoji".
    // The value is captured so only PrimaryFont stacks get rewritten — keeping the
    // pattern font-independent (a compile-time literal the source generator likes).
    // The closing quote is spelled ["] because a raw string literal cannot end
    // with a bare ". The pattern must consume that quote: without it the
    // replacement appends a second quote to every attribute on every run.
    [GeneratedRegex("""font-family="([^"]*)["]""")]
    private static partial Regex FontFamilyAttribute();

    // This file may live at any depth (e.g. docs/diagram-creation/); walk up from
    // it to the repo root, identified by the solution file, rather than assuming a
    // fixed directory depth.
    public static DirectoryInfo RepoRoot([CallerFilePath] string sourcePath = "")
    {
        var directory = new FileInfo(sourcePath).Directory;
        while (directory is not null && !File.Exists(Path.Combine(directory.FullName, "dps.slnx")))
            directory = directory.Parent;
        return directory ?? throw new DirectoryNotFoundException(
            $"Could not locate the repo root (no dps.slnx found above {sourcePath}).");
    }

    // Every diagrams folder under docs, the glob equivalent of docs/**/diagrams
    // (the docs tree nests areas, e.g. docs/DataPort.Extensions/reference/api/diagrams).
    public static IEnumerable<DirectoryInfo> DefaultFolders(DirectoryInfo repoRoot)
    {
        var docs = repoRoot.SubDirectory("docs");
        if (!docs.Exists)
            return [];

        return docs.GetDirectories("diagrams", SearchOption.AllDirectories)
            .OrderBy(diagrams => diagrams.FullName, StringComparer.Ordinal);
    }

    // Rewrite one SVG in place. Line endings are normalized to LF so a re-export's
    // CRLF doesn't churn the diff.
    public static FontAdjustmentResult AdjustFont(FileInfo svg)
    {
        var content = File.ReadAllText(svg.FullName, s_utf8NoBom).ReplaceLineEndings("\n");
        if (!content.Contains(ExcalidrawMarker, StringComparison.Ordinal))
            return FontAdjustmentResult.NotExcalidraw;

        var updated = StyleBlock().Replace(content, StyleReplacement);
        updated = FontFamilyAttribute().Replace(updated, match =>
            match.Groups[1].Value.StartsWith(PrimaryFont, StringComparison.Ordinal) ? FontStack : match.Value);
        if (updated == content)
            return FontAdjustmentResult.AlreadyFixed;

        File.WriteAllText(svg.FullName, updated, s_utf8NoBom);
        return FontAdjustmentResult.Fixed;
    }

    // pathlib-style builder for a typed subdirectory (not required to exist).
    extension(DirectoryInfo directory)
    {
        private DirectoryInfo SubDirectory(string name) =>
            new(Path.Combine(directory.FullName, name));
    }
}
