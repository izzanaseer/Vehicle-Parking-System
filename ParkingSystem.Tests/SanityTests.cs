using NUnit.Framework;
namespace ParkingSystem.Tests;

[TestFixture]
public class SanityTests
{
    [Test]
    public void Addition_TwoPlusTwo_ReturnsFour()
    {
        // Arrange
        int a = 2, b = 2;

        // Act
        int result = a + b;

        // Assert
        Assert.That(result, Is.EqualTo(4));
    }
}