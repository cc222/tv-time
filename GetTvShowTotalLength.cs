using System;
using System.Linq;
using System.Net.Http;
using System.Text.Json;
using System.Threading.Tasks;

class GetTvShowTotalLength
{
    static async Task<int> Main(string[] args)
    {
        if (args.Length == 0)
        {
            Console.Error.WriteLine("Please provide a TV show name.");
            return 1;
        }

        string showName = args[0];
        try
        {
            int totalRuntime = await GetMostRecentShowTotalRuntime(showName);
            Console.WriteLine(totalRuntime);
            return 0;
        }
        catch (ShowNotFoundException)
        {
            return 10;
        }
        catch (Exception ex)
        {
            Console.Error.WriteLine($"Error: {ex.Message}");
            return 1;
        }
    }

    private static async Task<int> GetMostRecentShowTotalRuntime(string showName)
    {
        using HttpClient client = new HttpClient();
        var searchResponse = await client.GetStringAsync($"https://api.tvmaze.com/search/shows?q={Uri.EscapeDataString(showName)}");

        JsonDocument searchResults = JsonDocument.Parse(searchResponse);

        //if there's more than one show by that name the program outputs the info on the most recent one


        var shows = searchResults.RootElement.EnumerateArray()
            .Select(e => e.GetProperty("show"))
            .Where(show => string.Equals(show.GetProperty("name").GetString(), showName, StringComparison.OrdinalIgnoreCase)) // Filter for exact name match
            //api should provide this data no need to check
            .OrderByDescending(show => DateTime.Parse(show.GetProperty("premiered").GetString()))
            .ToList();
        if (shows.Count < 1)
        {
            throw new ShowNotFoundException();
        }
        JsonElement mostRecentShow = shows.First();
        int totalRuntime = await GetShowEpisodesRuntime(mostRecentShow.GetProperty("id").GetInt32());

        return totalRuntime;
    }

    private static async Task<int> GetShowEpisodesRuntime(int showId)
    {
        using HttpClient client = new HttpClient();
        var response = await client.GetStringAsync($"https://api.tvmaze.com/shows/{showId}?embed=episodes");

        JsonDocument showData = JsonDocument.Parse(response);

        if (!showData.RootElement.TryGetProperty("_embedded", out JsonElement embedded) ||
            !embedded.TryGetProperty("episodes", out JsonElement episodes))
        {
            throw new ShowNotFoundException();
        }

        int totalRuntime = 0;
        foreach (JsonElement episode in episodes.EnumerateArray())
        {
            if (episode.TryGetProperty("runtime", out JsonElement runtimeElement) &&
                runtimeElement.TryGetInt32(out int runtime))
            {
                totalRuntime += runtime;
            }
        }

        return totalRuntime;
    }
}
