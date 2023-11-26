using PuppeteerSharp;
using System;
using System.Net.Http;
using System.Text.RegularExpressions;
using System.Threading.Tasks;

class Program
{
	private const string cssSelector = "header span>span";
	static async Task Main(string[] args)
	{
		// Get HTML content for a given URL
		const string url = "https://www.instagram.com/explore/tags/собаки/";
		string htmlContent = await FetchHtmlAsync(url);

		// Display the HTML content
		Console.WriteLine(htmlContent);
		int number = ParseToInt(htmlContent);
		Console.WriteLine($"number is {number}");
	}

	static async Task<string> FetchHtmlAsync(string url)
	{
		try
		{
			using (var browserFetcher = new BrowserFetcher())
			{
				await browserFetcher.DownloadAsync();
				Console.WriteLine("Browser downloaded.");
			}
			using (
				var browser = await Puppeteer.LaunchAsync(new LaunchOptions
				{
					Headless = false,
					DefaultViewport = null
				})
				)
			{
				var page = await browser.NewPageAsync();
				await page.SetRequestInterceptionAsync(true);

				// Set up request interception handler
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

				var result = await page.GoToAsync(url);
				await page.WaitForSelectorAsync(cssSelector);
				var header = await page.QuerySelectorAsync(cssSelector);
				var innerHtmlProp = await header.GetPropertyAsync("innerHTML");
				string html = await innerHtmlProp.JsonValueAsync<string>();
				return html;
			}
		}
		catch (Exception ex)
		{
			// Handle any exceptions that may occur during the request
			Console.WriteLine($"Error: {ex.Message}");
			return string.Empty;
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