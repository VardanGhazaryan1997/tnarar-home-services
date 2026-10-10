using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using System.Text.Json.Serialization;
using HomeServices.Api.IntegrationTests.Infrastructure;
using HomeServices.Application.Estimates;
using HomeServices.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;

namespace HomeServices.Api.IntegrationTests;

[Collection(ApiCollection.Name)]
public class EstimatesEndpointTests(ApiFactory factory)
{
    private const string Url = "/api/v1/estimates/measure";

    private static readonly JsonSerializerOptions Json = new(JsonSerializerDefaults.Web) { Converters = { new JsonStringEnumConverter() } };

    [Fact]
    public async Task Anyone_can_measure_rooms_with_seeded_work()
    {
        Guid plastering;
        await using (var scope = factory.Services.CreateAsyncScope())
        {
            plastering = (await scope.ServiceProvider.GetRequiredService<AppDbContext>().WorkItems.SingleAsync(w => w.Slug == "wall-plastering")).Id;
        }

        var client = factory.CreateClient();
        client.DefaultRequestHeaders.Add("Accept-Language", "en");
        var response = await client.PostAsJsonAsync(Url, new
        {
            rooms = new[]
            {
                new
                {
                    type = "Bedroom",
                    length = 4m,
                    width = 3m,
                    height = 2.7m,
                    openings = new[] { new { kind = "Door", width = 0.9m, height = 2.1m, count = 1 } },
                    lines = new[] { new { workItemId = plastering } },
                },
            },
        });

        response.StatusCode.ShouldBe(HttpStatusCode.OK);
        var result = (await response.Content.ReadFromJsonAsync<EstimateMeasurementDto>(Json))!;
        var room = result.Rooms.ShouldHaveSingleItem();
        room.FloorArea.ShouldBe(12m);
        room.Lines.ShouldHaveSingleItem().Quantity.ShouldBe(35.91m); // 14 × 2.7 − 0.9 × 2.1
        room.Lines[0].Name.ShouldBe("Wall plastering");
    }

    [Fact]
    public async Task Impossible_rooms_are_refused()
    {
        var client = factory.CreateClient();

        var empty = await client.PostAsJsonAsync(Url, new { rooms = Array.Empty<object>() });
        empty.StatusCode.ShouldBe(HttpStatusCode.BadRequest);

        var tooBig = await client.PostAsJsonAsync(Url, new { rooms = new[] { new { type = "Other", length = 500m, width = 3m } } });
        tooBig.StatusCode.ShouldBe(HttpStatusCode.UnprocessableEntity);
        (await tooBig.Content.ReadAsStringAsync()).ShouldContain("estimate.size_invalid");
    }
}
