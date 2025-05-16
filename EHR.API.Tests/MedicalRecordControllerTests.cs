using Microsoft.AspNetCore.Mvc;
using EHR.API.Controllers;
using Xunit;

namespace EHR.API.Tests;

public class MedicalRecordControllerTests
{
    [Fact]
    public void Get_ReturnsOkResult()
    {
        // Arrange
        var controller = new MedicalRecordController();

        // Act
        var result = controller.Get();

        // Assert
        Assert.IsType<OkObjectResult>(result);
    }
} 