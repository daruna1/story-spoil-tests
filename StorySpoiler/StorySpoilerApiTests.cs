using System.Net;
using System.Text.Json;
using System.Threading.Tasks;
using NUnit.Framework;
using RestSharp;
using RestSharp.Authenticators;
using StorySpoiler.Models;

namespace StorySpoiler.Tests;

[TestFixture]
public class StorySpoilerApiTests
{
    private const string BaseUrl = "https://d3s5nxhwblsjbi.cloudfront.net";
    private static RestClient? _client;
    private static string? _storyId;

    private static string Username =>
        System.Environment.GetEnvironmentVariable("STORY_USER") ?? "viara1";
    private static string Password =>
        System.Environment.GetEnvironmentVariable("STORY_PASS") ?? "viara1viara1";

    [OneTimeSetUp]
    public async Task OneTimeSetUp()
    {
        var manualToken = System.Environment.GetEnvironmentVariable("STORY_TOKEN");
        string token;

        if (!string.IsNullOrWhiteSpace(manualToken))
        {
            token = manualToken!;
        }
        else
        {
            using var authClient = new RestClient(BaseUrl);
            var login = new RestRequest("api/User/Authentication", Method.Post)
                .AddJsonBody(new AuthRequestDto { UserName = Username, Password = Password });

            var loginResponse = await authClient.ExecuteAsync<AuthResponseDto>(login);
            Assert.That(loginResponse.StatusCode, Is.EqualTo(HttpStatusCode.OK), "Login failed");

            token = loginResponse.Data?.AccessToken ?? "";
            Assert.That(string.IsNullOrWhiteSpace(token), Is.False, "No access token from login.");
        }

        var options = new RestClientOptions(BaseUrl)
        {
            Authenticator = new JwtAuthenticator(token)
        };
        _client = new RestClient(options);
    }

    [OneTimeTearDown]
    public void OneTimeTearDown()
    {
        _client?.Dispose();
    }

    [Test, Order(1)]
    public async Task Create_Story_Should_Return_201_And_Id_And_Message()
    {
        var story = new StoryDto
        {
            Title = $"Test Story {System.Guid.NewGuid():N}".Substring(0, 18),
            Description = "Created from automated test",
            Url = ""
        };

        var req = new RestRequest("api/Story/Create", Method.Post).AddJsonBody(story);
        var resp = await _client!.ExecuteAsync<ApiResponseDto>(req);

        Assert.That(resp.StatusCode, Is.EqualTo(HttpStatusCode.Created));
        Assert.That(resp.Data?.StoryId, Is.Not.Null.And.Not.Empty);
        Assert.That(resp.Data?.Msg, Is.EqualTo("Successfully created!"));

        _storyId = resp.Data!.StoryId!;
        TestContext.WriteLine($"Created StoryId = {_storyId}");
    }

    [Test, Order(2)]
    public async Task Edit_Story_Should_Return_200_And_Message()
    {
        Assert.That(_storyId, Is.Not.Null, "StoryId not set from create test.");

        var updated = new StoryDto
        {
            Title = "Edited Title",
            Description = "Edited Description",
            Url = ""
        };

        var req = new RestRequest($"api/Story/Edit/{_storyId}", Method.Put).AddJsonBody(updated);
        var resp = await _client!.ExecuteAsync<ApiResponseDto>(req);

        Assert.That(resp.StatusCode, Is.EqualTo(HttpStatusCode.OK));
        Assert.That(resp.Data?.Msg, Is.EqualTo("Successfully edited"));
    }

    [Test, Order(3)]
    public async Task Get_All_Should_Return_200_And_NonEmpty_Array()
    {
        var req = new RestRequest("api/Story/All", Method.Get);
        var resp = await _client!.ExecuteAsync(req);

        Assert.That(resp.StatusCode, Is.EqualTo(HttpStatusCode.OK));

        var options = new JsonSerializerOptions { PropertyNameCaseInsensitive = true };
        var list = JsonSerializer.Deserialize<System.Collections.Generic.List<object>>(resp.Content ?? "[]", options);

        Assert.That(list, Is.Not.Null);
        Assert.That(list!.Count, Is.GreaterThan(0));
    }

    [Test, Order(4)]
    public async Task Delete_Story_Should_Return_200_And_Message()
    {
        Assert.That(_storyId, Is.Not.Null, "StoryId not set from create test.");

        var req = new RestRequest($"api/Story/Delete/{_storyId}", Method.Delete);
        var resp = await _client!.ExecuteAsync<ApiResponseDto>(req);

        Assert.That(resp.StatusCode, Is.EqualTo(HttpStatusCode.OK));
        Assert.That(resp.Data?.Msg, Is.EqualTo("Deleted successfully!"));
    }


    [Test, Order(5)]
    public async Task Create_Without_Required_Fields_Should_Return_400()
    {
        var invalid = new StoryDto { Title = "", Description = "" };

        var req = new RestRequest("api/Story/Create", Method.Post).AddJsonBody(invalid);
        var resp = await _client!.ExecuteAsync<ApiResponseDto>(req);

        Assert.That(resp.StatusCode, Is.EqualTo(HttpStatusCode.BadRequest));
    }

    [Test, Order(6)]
    public async Task Edit_NonExisting_Should_Return_404_Or_400_And_Message()
    {
        var fakeId = System.Guid.NewGuid().ToString();
        var updated = new StoryDto { Title = "X", Description = "Y", Url = "" };

        var req = new RestRequest($"api/Story/Edit/{fakeId}", Method.Put).AddJsonBody(updated);
        var resp = await _client!.ExecuteAsync<ApiResponseDto>(req);

        Assert.That(
            resp.StatusCode == HttpStatusCode.NotFound || resp.StatusCode == HttpStatusCode.BadRequest,
            $"Expected 404 or 400, but was: {(int)resp.StatusCode} {resp.StatusCode}. Content: {resp.Content}"
        );

        var body = resp.Data?.Msg ?? resp.Content ?? "";
        if (!string.IsNullOrWhiteSpace(body))
        {
            StringAssert.Contains("No spoilers", body);
        }
    }

    [Test, Order(7)]
    public async Task Delete_NonExisting_Should_Return_400_And_Message()
    {
        var fakeId = System.Guid.NewGuid().ToString();

        var req = new RestRequest($"api/Story/Delete/{fakeId}", Method.Delete);
        var resp = await _client!.ExecuteAsync<ApiResponseDto>(req);

        Assert.That(resp.StatusCode, Is.EqualTo(HttpStatusCode.BadRequest));

        var body = resp.Data?.Msg ?? resp.Content ?? "";
        if (!string.IsNullOrWhiteSpace(body))
        {
            StringAssert.Contains("Unable to delete this story spoiler!", body);
        }
    }
}
