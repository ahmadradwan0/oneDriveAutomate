// See https://aka.ms/new-console-template for more information

class Program
{
    public async static Task<List<VersionInfo>> InitWebScrapping()
    {
        Utils.Log("getting versions from website table ...");

        // Step 1: Scrape versions from web
        var webVersions = await WebScraper.ScrapeHtmlAsync("https://hansbrender.com/all-onedrive-versions-windows/");

        return webVersions;
        /*
        Utils.Log("Number of versions from Json File ");
        Utils.Log(StorageManager.GetStoredVersions().Count.ToString());
        //StorageManager.SaveVersionsOverwrite(webVersions);


        Utils.Log("Number of versions from web");
        Utils.Log(webVersions.Count.ToString());

        List<VersionInfo>? newItems =  StorageManager.CompareByItem(webVersions);

        int totalDuplicateCount = webVersions
            .GroupBy(x => new { x.Version, x.VersionDate })
            .Where(g => g.Count() > 1)
            .Sum(g => g.Count() -1 );


        Utils.Log(totalDuplicateCount.ToString());


        Utils.Log("Getting hidden versions");
        var HiddenWebVersions = await WebScraper.GetListOfHiddenVersions();
        Utils.Log(HiddenWebVersions.Count.ToString());
        Utils.Log("Init finish ...");*/
    }
    

    public async static Task<List<VersionInfo>> LocalStorageScrapping()
    {
        Utils.Log("Json File versions List ");
        //StorageManager.SaveVersionsOverwrite(webVersions);
        return StorageManager.GetStoredVersions(Config.VersionFile);
    }

    public async static Task<List<VersionInfo>> HiddenInitWebScrapping()
    { 
        Utils.Log("Getting hidden versions from web");
        List<VersionInfo>? NewHiddenVersions = await WebScraper.GetListOfHiddenVersionsParallel();
        return NewHiddenVersions;
    }

    static async Task Main(string[] args)
    {
        var websiteList = await InitWebScrapping();
        Utils.Log("Website items found: " + websiteList.Count.ToString());

        var JsonList = await LocalStorageScrapping();
        Utils.Log("Json items found: " + JsonList.Count.ToString());

        var newItems = StorageManager.CompareByItem(websiteList, JsonList);
        Utils.Log("New items found From Website: " + newItems.Count.ToString());

        var hiddenItems = await HiddenInitWebScrapping();
        Utils.Log("Hidden items found : " + hiddenItems.Count.ToString());

        if (hiddenItems.Count > 0)
        { 
            var combinedList = StorageManager.CombineTwoLists(JsonList, hiddenItems);
            Utils.Log("Combined total items: " + combinedList.Count.ToString());

            StorageManager.SaveVersionsOverwrite(combinedList);
            Utils.Log("NewList saved to file. T");
        }


        Utils.Log("Exiting...");
    }
}