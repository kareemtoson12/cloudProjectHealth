using Microsoft.AspNetCore.Mvc;
using PatientManagement.API.Controllers;
using Xunit;

namespace PatientManagement.API.Tests;

public class PatientControllerTests
{
    [Fact]
    public void Get_ReturnsOkResult()
    {
        // Arrange
        var controller = new PatientController();

        // Act
        var result = controller.Get();

        // Assert
        Assert.IsType<OkObjectResult>(result);
    }
} 