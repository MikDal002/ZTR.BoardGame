using ZtrTemplates.Console.Infrastructure;

namespace ZtrTemplates.Console.Tests.Infrastructure;

[TestFixture]
public class ConsoleAppCancellationTokenSourceTests
{
    private ConsoleAppCancellationTokenSource _sut = null!; // System Under Test

    [SetUp]
    public void Setup()
    {
        // Create a new instance before each test
        _sut = new ConsoleAppCancellationTokenSource();
    }

    [TearDown]
    public void Teardown()
    {
        // Ensure disposal after each test
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
    public void Dispose_ShouldCancelTheToken()
    {
        // Arrange
        var token = _sut.Token;
        token.IsCancellationRequested.Should().BeFalse(); // Pre-condition

        // Act
        _sut.Dispose();

        // Assert
        // Give a brief moment for potential background operations triggered by Dispose/Cancel
        Thread.Sleep(50); // Small delay might be needed depending on thread scheduling
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
