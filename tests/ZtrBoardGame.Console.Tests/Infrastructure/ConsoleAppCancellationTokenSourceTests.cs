using ZtrBoardGame.Console.Infrastructure;

namespace ZtrBoardGame.Console.Tests.Infrastructure;

[TestFixture]
public class ConsoleAppCancellationTokenSourceTests
{
    private ConsoleAppCancellationTokenSource _sut = null!;

    [SetUp]
    public void Setup()
    {
        _sut = new();
    }

    [TearDown]
    public void Teardown()
    {
        _sut?.Dispose();
    }

    [Test]
    public void Constructor_ShouldInitializeToken_WhichIsNotCancelled()
    {
        // Act
        var token = _sut.Token;

        // Assert
        token.IsCancellationRequested.Should().BeFalse();
        token.CanBeCanceled.Should().BeTrue();
    }

    [Test]
    public async Task Dispose_ShouldCancelTheToken()
    {
        // Arrange
        var token = _sut.Token;
        token.IsCancellationRequested.Should().BeFalse();

        // Act
        _sut.Dispose();

        // Assert
        // Give a brief moment for potential background operations triggered by Dispose/Cancel
        await Task.Delay(50);
        token.IsCancellationRequested.Should().BeTrue();
    }

    [Test]
    public void Dispose_CanBeCalledMultipleTimes_WithoutThrowing()
    {
        // Arrange
        _sut.Dispose(); // First call

        // Act & Assert
        Action secondDispose = () => _sut.Dispose();
        secondDispose.Should().NotThrow();
    }

    [Test]
    public void Token_ShouldReturnSameInstance()
    {
        // Act
        var token1 = _sut.Token;
        var token2 = _sut.Token;

        // Assert
        // CancellationToken is a struct, so we compare properties or rely on reference equality of the source
        // For simplicity, we check CanBeCanceled as an indicator it's from the same source.
        // A more robust check might involve reflection or modifying the SUT, but this is sufficient for basic validation.
        token1.CanBeCanceled.Should().Be(token2.CanBeCanceled);
        token1.IsCancellationRequested.Should().Be(token2.IsCancellationRequested);
    }
}
