using Application.Abstractions;
using Application.Contracts;
using Application.Features.Aggregation;
using Application.Models;
using Domain.Models;
using FluentAssertions;
using Moq;

namespace Application.Tests.Aggregation;

public sealed class GetAggregatedDataHandlerTests
{
    [Fact]
    public async Task Handle_MergesItems_FromSuccessfulProviders()
    {
        var req = new AggregationRequest { Q = null };

        var p1 = new Mock<IExternalApiProvider>();
       p1.Setup(x => x.FetchAsync(It.IsAny<AggregationRequest>(), It.IsAny<CancellationToken>()))
  .ReturnsAsync(new ProviderResult
  {
      Source = "P1",
      IsSuccess = true,
      Items = new[]
      {
          new AggregatedItem("P1", "A", null, null, null),
          new AggregatedItem("P1", "B", null, null, null),
      }
  });
        var p2 = new Mock<IExternalApiProvider>();
       p2.Setup(x => x.FetchAsync(It.IsAny<AggregationRequest>(), It.IsAny<CancellationToken>()))
  .ReturnsAsync(new ProviderResult
  {
      Source = "P2",
      IsSuccess = true,
      Items = new[]
      {
          new AggregatedItem("P2", "C", null, null, null),
      }
  });

        var cache = new Mock<IAggregationCache>();
        AggregationResponse dummy;
        cache.Setup(x => x.TryGet(It.IsAny<string>(), out dummy!)).Returns(false);

        var handler = new GetAggregatedDataHandler(new[] { p1.Object, p2.Object }, cache.Object);

        var result = await handler.Handle(new GetAggregatedDataQuery(req), CancellationToken.None);

        result.Items.Should().HaveCount(3);
        result.PartialFailures.Should().BeEmpty();
        p1.Verify(x => x.FetchAsync(It.IsAny<AggregationRequest>(), It.IsAny<CancellationToken>()), Times.Once);
p2.Verify(x => x.FetchAsync(It.IsAny<AggregationRequest>(), It.IsAny<CancellationToken>()), Times.Once);

    }

    [Fact]
    public async Task Handle_ReturnsPartialFailures_WhenAProviderFails()
    {
        var req = new AggregationRequest { Q = null };

        var ok = new Mock<IExternalApiProvider>();
        ok.SetupGet(x => x.Name).Returns("OK");
      ok.Setup(x => x.FetchAsync(It.IsAny<AggregationRequest>(), It.IsAny<CancellationToken>()))
  .ReturnsAsync(new ProviderResult
  {
      Source = "OK",
      IsSuccess = true,
      Items = new[]
      {
          new AggregatedItem("OK", "A", null, null, null),
      }
  });

        var bad = new Mock<IExternalApiProvider>();
        bad.SetupGet(x => x.Name).Returns("BAD");
       bad.Setup(x => x.FetchAsync(It.IsAny<AggregationRequest>(), It.IsAny<CancellationToken>()))
   .ReturnsAsync(new ProviderResult
   {
       Source = "BAD",
       IsSuccess = false,
       Items = Array.Empty<AggregatedItem>(),
       Error = "boom"
   });


        var cache = new Mock<IAggregationCache>();
        AggregationResponse dummy;
        cache.Setup(x => x.TryGet(It.IsAny<string>(), out dummy!)).Returns(false);

        var handler = new GetAggregatedDataHandler(new[] { ok.Object, bad.Object }, cache.Object);

        var result = await handler.Handle(new GetAggregatedDataQuery(req), CancellationToken.None);

        result.Items.Should().HaveCount(1);
        result.PartialFailures.Should().ContainSingle(x => x.Source == "BAD");
    }

    [Fact]
    public async Task Handle_UsesCache_WhenAvailable()
    {
        var req = new AggregationRequest { Q = null};

        var provider = new Mock<IExternalApiProvider>();
        provider.SetupGet(x => x.Name).Returns("P1");

        var cached = new AggregationResponse
        {
            Items = new[] { new AggregatedItem("cache", "CACHED", null, null, null) },
            PartialFailures = Array.Empty<AggregationFailure>()
        };

        var cache = new Mock<IAggregationCache>();
        cache.Setup(x => x.TryGet(It.IsAny<string>(), out cached)).Returns(true);

        var handler = new GetAggregatedDataHandler(new[] { provider.Object }, cache.Object);

        var result = await handler.Handle(new GetAggregatedDataQuery(req), CancellationToken.None);

        result.Items.Should().ContainSingle(x => x.Title == "CACHED");
        provider.Verify(x => x.FetchAsync(It.IsAny<AggregationRequest>(), It.IsAny<CancellationToken>()), Times.Never);
    }

    [Fact]
public async Task Handle_FiltersByQuery_OnTitle()
{
    var req = new AggregationRequest { Q = null};

    var p1 = new Mock<IExternalApiProvider>();
    p1.SetupGet(x => x.Name).Returns("P1");
    p1.Setup(x => x.FetchAsync(It.IsAny<AggregationRequest>(), It.IsAny<CancellationToken>()))
      .ReturnsAsync(ProviderResult.Success("P1", new[]
      {
          new AggregatedItem("P1", "dotnet runtime", null, null, null),
          new AggregatedItem("P1", "java", null, null, null),
      }));

    var cache = new Mock<IAggregationCache>();
    AggregationResponse dummy;
    cache.Setup(x => x.TryGet(It.IsAny<string>(), out dummy!)).Returns(false);

    var handler = new GetAggregatedDataHandler(new[] { p1.Object }, cache.Object);

    var result = await handler.Handle(new GetAggregatedDataQuery(req), CancellationToken.None);

    result.Items.Should().ContainSingle(x => x.Title.Contains("dotnet", StringComparison.OrdinalIgnoreCase));
}

}
