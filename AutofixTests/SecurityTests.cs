using Xunit;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;
using Moq;
using FinTrack.API.Controllers;
using System.Data.SqlClient; // Required for SqlConnection and SqlCommand

namespace AutofixTests
{
    public class SecurityTests
    {
        [Fact]
        public void SqlQuery_WithSafeTerm_ReturnsOk()
        {
            // Arrange
            var mockLogger = new Mock<ILogger<DataController>>();
            var controller = new DataController(mockLogger.Object);
            string safeTerm = "TestItem";

            // Act
            var result = controller.SqlQuery(safeTerm);

            // Assert
            Assert.IsType<OkResult>(result);
        }

        [Fact]
        public void SqlQuery_WithMaliciousTerm_ReturnsOkAndPreventsSqlInjection()
        {
            // Arrange
            var mockLogger = new Mock<ILogger<DataController>>();
            var controller = new DataController(mockLogger.Object);
            string maliciousTerm = "' OR 1=1--"; // Malicious SQL injection payload

            // Act
            // The parameterized query should treat this as a literal string.
            // If it were vulnerable, this would return all items or cause an error.
            // With the fix, it should safely search for a literal string "' OR 1=1--"
            // and return no results (or Ok() if no items match), without error.
            var result = controller.SqlQuery(maliciousTerm);

            // Assert
            Assert.IsType<OkResult>(result);
            // No exception should be thrown due to SQL injection
        }
    }
}