using System;
using Dsw2026Tpi.Application.Services;
using Xunit;

namespace Dsw2026Tpi.Tests;

public class ValidateRangeTests
{
    [Fact]
    public void ValidateRange_CuandoElRangoEsValido_NoDeberiaLanzarExcepcion()
    {
        // Arrange
        var startTime = new TimeSpan(8, 0, 0);
        var endTime = new TimeSpan(9, 0, 0);

        // Act
        var exception = Record.Exception(() =>
            AvailabilityGenerator.ValidateRange(startTime, endTime));

        // Assert
        Assert.Null(exception);
    }
}
