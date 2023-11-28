using PuppeteerSharp;
using System;
using System.Net.Http;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
class Program
{
    private const string cssSelector = "header span>span";
    private const string baseUrl = "https://www.instagram.com/explore/tags/";

    static async Task Main(string[] args)
    {
        string[] hashtags;
        if (args.Length == 0)
        {
            Console.WriteLine("Enter hashtags with a whitespase");
            string input = Console.ReadLine();
            hashtags = input.Split(" ", StringSplitOptions.TrimEntries);
        }
        else
        {
            hashtags = args;
        }

        // Downloading browser if needed.
        using (var browserFetcher = new BrowserFetcher())
        {
            var browsers = browserFetcher.GetInstalledBrowsers();
            if (!browsers.Any())
            {
                await browserFetcher.DownloadAsync();
                Console.WriteLine("Browser downloaded.");
            }
        }

        Console.WriteLine("Programm is looking online with a hidden browser.");
        using (var browser = await Puppeteer.LaunchAsync(new LaunchOptions { Headless = true, DefaultViewport = null }))
        {
            List<Task> tasks = new();
            // Create tasks for each hashtag
            foreach (var hashtag in hashtags)
            {
                tasks.Add(FetchAndProcessHtmlAsync(hashtag, browser));
                // Basic simulation of user behavior.
                await Task.Delay(new Random().Next(500, 4000));
            }

            // Wait for all tasks to complete
            await Task.WhenAll(tasks);
        }
        Console.WriteLine("Press enter to close programm");
        Console.ReadLine();
    }

    static async Task FetchAndProcessHtmlAsync(string hashtag, IBrowser browser)
    {
        try
        {
            var page = await browser.NewPageAsync();
            await page.SetRequestInterceptionAsync(true);

            // Set up request interception handler to reduce traffic (hopefully).
            page.Request += async (sender, e) =>
            {
                if (e.Request.ResourceType == ResourceType.Image)
                {
                    // Block image requests
                    await e.Request.AbortAsync();
                }
                else
                {
                    // Allow other requests
                    await e.Request.ContinueAsync();
                }
            };

            var result = await page.GoToAsync(baseUrl + hashtag + "/");
            await page.WaitForSelectorAsync(cssSelector);
            var header = await page.QuerySelectorAsync(cssSelector);
            var innerHtmlProp = await header.GetPropertyAsync("innerHTML");
            string html = await innerHtmlProp.JsonValueAsync<string>();
            int number = ParseToInt(html);
            Console.WriteLine($"#{hashtag} {number} posts");
            await page.CloseAsync();
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Error for #{hashtag}: {ex.Message}");
        }
    }

    static int ParseToInt(string value)
    {
        // Use regular expression to remove non-numeric characters
        string cleanedString = Regex.Replace(value, "[^0-9]", "");

        try
        {
            // Parse the cleaned string into an integer
            int parsedNumber = int.Parse(cleanedString);
            return parsedNumber;
        }
        catch (FormatException)
        {
            Console.WriteLine("Invalid format");
            return 0;
        }
    }
}
