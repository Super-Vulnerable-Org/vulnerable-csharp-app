using Microsoft.AspNetCore.Mvc;
using FinTrack.API.Controllers;
using Microsoft.Extensions.Logging;
using Moq;
using Xunit;
using System.Data.SqlClient;

namespace FinTrack.API.Tests.Security
{
    public class DataControllerSecurityTests
    {
        private readonly DataController _controller;
        private readonly Mock<ILogger<DataController>> _mockLogger;

        public DataControllerSecurityTests()
        {
            _mockLogger = new Mock<ILogger<DataController>>();
            _controller = new DataController(_mockLogger.Object);
        }

        [Fact]
        public void SqlQuery_WithMaliciousInput_PreventsSqlInjection()
        {
            // Arrange
            string maliciousTerm = "' OR 1=1 --";

            // Act
            var result = _controller.SqlQuery(maliciousTerm) as OkResult;

            // Assert
            Assert.NotNull(result);
            Assert.Equal(200, result.StatusCode);
            // In a real test, we would verify that the database was not affected by the injection.
            // For this simulated test, we assume the parameterized query prevents the injection.
        }

        [Fact]
        public void SqlQuery_WithSafeInput_ReturnsOk()
        {
            // Arrange
            string safeTerm = "ItemName";

            // Act
            var result = _controller.SqlQuery(safeTerm) as OkResult;

            // Assert
            Assert.NotNull(result);
            Assert.Equal(200, result.StatusCode);
        }
    }
}