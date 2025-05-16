using Microsoft.AspNetCore.Mvc;
using AppointmentScheduling.API.Controllers;
using Xunit;

namespace AppointmentScheduling.API.Tests;

public class AppointmentControllerTests
{
    [Fact]
    public void Get_ReturnsOkResult()
    {
        // Arrange
        var controller = new AppointmentController();

        // Act
        var result = controller.Get();

        // Assert
        Assert.IsType<OkObjectResult>(result);
    }
} 