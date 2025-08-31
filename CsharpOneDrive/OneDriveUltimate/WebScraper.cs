using System.Collections.Generic;
using HtmlAgilityPack;
using System.Collections.Concurrent;
using System.Net;

public static class WebScraper
{

    public static async Task<List<VersionInfo>> ScrapeHtmlAsync(string url)
    {
        Utils.Log("Fetching version data...");

        var versions = new List<VersionInfo>();

        using HttpClient client = new HttpClient();
        var html = await client.GetStringAsync(url);

        var doc = new HtmlDocument();
        doc.LoadHtml(html);

        var rows = doc.DocumentNode.SelectNodes("//table//tr");
        if (rows != null)
        {
            foreach (var row in rows)
            {
                var cells = row.SelectNodes("td");
                if (cells != null && cells.Count > 1)
                {
                    versions.Add(new VersionInfo
                    {
                        VersionDate = cells[0].InnerText.Trim(),
                        Version = cells[1].InnerText.Trim()

                    });



                }
            }
        }
        // Example: wrap the raw HTML in a list for now
        Utils.Log("returning...");
        return versions;
    }

    public static async Task<List<VersionInfo>> GetListOfHiddenVersions()
    {
        Utils.Log("function");
        //var AllVersions = StorageManager.GetStoredVersions();
        //test list
        var AllVersions = StorageManager.GetStoredVersions(Config.VersionFile);
        int hiddenItemsSearchLimit = Config.MaxSubVersionCheck;

        var hiddenVersions = new List<VersionInfo>();

        using var httpClient = new HttpClient();
        foreach (var version in AllVersions)
        {
            Utils.Log("looping each");
            string versionNumberStr = version.Version; // e.g. "19.002.0107.0008"

            // split version into prefix + subversion
            int lastDotIndex = versionNumberStr.LastIndexOf('.');
            if (lastDotIndex == -1) continue;

            string prefix = versionNumberStr.Substring(0, lastDotIndex + 1); // "19.002.0107."
            string lastPart = versionNumberStr.Substring(lastDotIndex + 1);   // "0008"

            if (!int.TryParse(lastPart, out int baseSubVersion))
                continue;

            for (int i = 1; i <= hiddenItemsSearchLimit; i++)
            {
                Utils.Log("looping versions");
                string candidateSubVersion = i.ToString("D4"); // pad with leading zeros (0001, 0002...)
                string candidateVersion = prefix + candidateSubVersion;

                // Skip if already exists in our stored versions
                if (AllVersions.Any(v => v.Version == candidateVersion))
                    continue;

                // Build both URLs
                string url64 = $"https://oneclient.sfx.ms/Win/Installers/{candidateVersion}/amd64/OneDriveSetup.exe";
                string url32 = $"https://oneclient.sfx.ms/Win/Installers/{candidateVersion}/OneDriveSetup.exe";

                // Check availability
                if (await UrlExistsAsync(httpClient, url64) || await UrlExistsAsync(httpClient, url32))
                {
                    hiddenVersions.Add(new VersionInfo
                    {
                        Version = candidateVersion,
                        VersionDate = version.VersionDate, // you might need to decide how to handle date

                    });
                    Utils.Log("versoin added");
                }
            }
        }
        Utils.Log("returning all hidden done");

        return hiddenVersions;
    }

public static async Task<List<VersionInfo>> GetListOfHiddenVersionsParallel()
{

    var AllVersions = StorageManager.GetStoredVersions(Config.VersionFile);
    int hiddenItemsSearchLimit = Config.MaxSubVersionCheck;

    var hiddenVersions = new ConcurrentBag<VersionInfo>(); // thread-safe

    using var httpClient = new HttpClient();

    foreach (var version in AllVersions)
    {
        Utils.Log($"Checking base version: {version.Version}");

        string versionNumberStr = version.Version;

        int lastDotIndex = versionNumberStr.LastIndexOf('.');
        if (lastDotIndex == -1) continue;

        string prefix = versionNumberStr.Substring(0, lastDotIndex + 1);
        string lastPart = versionNumberStr.Substring(lastDotIndex + 1);

        if (!int.TryParse(lastPart, out int baseSubVersion))
            continue;

        // Generate candidate versions
        var candidateTasks = Enumerable.Range(1, hiddenItemsSearchLimit)
            .Select(async i =>
            {
                string candidateSubVersion = i.ToString("D4");
                string candidateVersion = prefix + candidateSubVersion;

                if (AllVersions.Any(v => v.Version == candidateVersion))
                    return; // skip existing

                string url64 = $"https://oneclient.sfx.ms/Win/Installers/{candidateVersion}/amd64/OneDriveSetup.exe";
                string url32 = $"https://oneclient.sfx.ms/Win/Installers/{candidateVersion}/OneDriveSetup.exe";

                if (await UrlExistsAsync(httpClient, url64) || await UrlExistsAsync(httpClient, url32))
                {
                    hiddenVersions.Add(new VersionInfo
                    {
                        Version = candidateVersion,
                        VersionDate = version.VersionDate
                    });
                    Utils.Log($"Hidden version found: {candidateVersion}");
                }
            });

        // Run all candidate checks for this version concurrently
        await Task.WhenAll(candidateTasks);
    }

    Utils.Log("returning all hidden done");
    return hiddenVersions.ToList();
}

    private static async Task<bool> UrlExistsAsync(HttpClient client, string url)
    {
        Utils.Log("urlTest");
        try
        {
            using var response = await client.SendAsync(
                new HttpRequestMessage(HttpMethod.Head, url),
                HttpCompletionOption.ResponseHeadersRead);

            return response.IsSuccessStatusCode;
        }
        catch
        {
            return false;
        }
    }

}