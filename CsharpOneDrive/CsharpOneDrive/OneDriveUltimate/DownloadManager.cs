using System.Text.Json;

public static class DownloadManager
{
    private static async Task DownloadFile(HttpClient client,string url, string filePath)
    {
        Utils.Log($"Downloading {url}...");
        
        using (var response = await client.GetAsync(url, HttpCompletionOption.ResponseHeadersRead))
        {
            response.EnsureSuccessStatusCode();
            
            using (var streamToReadFrom = await response.Content.ReadAsStreamAsync())
            using (var streamToWriteTo = File.Create(filePath))
            {
                await streamToReadFrom.CopyToAsync(streamToWriteTo);
            }
        }
        
        Utils.Log($"Saved to {filePath}");
        Utils.Log("++++++++++++++++++++++++++++++++++++++++");
    }
    
    //##  ##  ## ##  
   public async static Task<List<string>> DownloadNewVersions(List<VersionInfo> versions)
    {
        var downloadedFiles = new List<string>();
        
        using (var client = new HttpClient())
        {
            foreach (var version in versions)
            {
                string version64Url = $"https://oneclient.sfx.ms/Win/Installers/{version.Version}/amd64/OneDriveSetup.exe";
                string version32Url = $"https://oneclient.sfx.ms/Win/Installers/{version.Version}/OneDriveSetup.exe";
                
                //string downloadsFolder = Config.DownloadPath; // Use configured download path
                string downloadPath = Path.Combine(Config.DownloadPath, version.Version);
                Directory.CreateDirectory(downloadPath);
                switch (Config.Architecture)
                { 
                    case "x64":
                         // Download 64-bit version
                        string path64 = Path.Combine(downloadPath, "OneDriveSetup_x64.exe");
                        if (!File.Exists(path64))
                        {
                            try
                            {
                                await DownloadFile(client, version64Url, path64);
                                downloadedFiles.Add(path64);
                            }
                            catch (Exception ex)
                            {
                                Utils.Log($"Failed to download 64-bit version {version.Version}: {ex.Message}", "ERROR");
                                // Continue to next file/version
                            }
                        }
                        else
                        {
                            Utils.Log($"64-bit version {version.Version} already exists at {path64}, skipping download.");
                        }
                        break;

                    case "x86":
                        // Download 32-bit version
                        string path32 = Path.Combine(downloadPath, "OneDriveSetup_x86.exe");
                        if (!File.Exists(path32))
                        {
                            try
                            {
                                await DownloadFile(client, version32Url, path32);
                                downloadedFiles.Add(path32);
                            }
                            catch (Exception ex)
                            {
                                Utils.Log($"Failed to download 32-bit version {version.Version}: {ex.Message}", "ERROR");
                                // Continue to next file/version
                            }
                        }
                        break;

                    case "ARM64":
                    default:
                        break;
                }
               

                
            }
        }

        return downloadedFiles;
    }

}