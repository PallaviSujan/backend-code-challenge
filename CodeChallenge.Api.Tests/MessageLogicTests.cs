using System;
using System.Threading.Tasks;
using FluentAssertions;
using Moq;
using Xunit;
using CodeChallenge.Api.Logic;
using CodeChallenge.Api.Models;

namespace CodeChallenge.Tests
{
    public class MessageLogicTests
    {
        [Fact]
        public async Task CreateAsync_Should_Create_Message_Successfully()
        {
            // Arrange
            var repoMock = new Mock<IMessageRepository>();

            var logic = new MessageLogic(repoMock.Object);

            var message = new Message
            {
                Title = "New Title",
                Content = new string('x', 20),
                OrganizationId = Guid.NewGuid()
            };

            // Act
            var result = await logic.CreateMessageAsync(message.OrganizationId, message);

            // Assert
            result.IsSuccess.Should().BeTrue();
            result.Value.Title.Should().Be("New Title");
        }

        [Fact]
        public async Task CreateAsync_Duplicate_Title_Returns_Conflict()
        {
            // Arrange
            var existing = new Message
            {
                Id = Guid.NewGuid(),
                Title = "Duplicate",
                OrganizationId = Guid.NewGuid()
            };

            var repoMock = new Mock<IMessageRepository>();
            repoMock
                .Setup(r => r.GetAllMessagesAsync(existing.OrganizationId))
                .ReturnsAsync(existing);

            var logic = new MessageLogic(repoMock.Object);

            var toCreate = new Message
            {
                Title = "Duplicate",
                Content = new string('x', 20),
                OrganizationId = existing.OrganizationId
            };

            // Act
            var result = await logic.CreateAsync(toCreate);

            // Assert
            result.IsSuccess.Should().BeFalse();
            result.Error.Should().Contain("already exists", StringComparison.OrdinalIgnoreCase);
        }

        [Fact]
        public async Task CreateAsync_Invalid_Content_Length_Returns_ValidationError()
        {
            // Arrange
            var repoMock = new Mock<IMessageRepository>();
            repoMock
                .Setup(r => r.GetAllMessagesAsync(It.IsAny<Guid>()))
                .ReturnsAsync((Message)null);

            var logic = new MessageLogic(repoMock.Object);

            var message = new Message
            {
                Title = "Valid Title",
                Content = "short",
                OrganizationId = Guid.NewGuid()
            };

            // Act
            var result = await logic.CreateAsync(message);

            // Assert
            result.IsSuccess.Should().BeFalse();
            result.Error.Should().Contain("Content must be between 10 and 1000", StringComparison.OrdinalIgnoreCase);
        }

        [Fact]
        public async Task UpdateAsync_NonExistent_Message_Returns_NotFound()
        {
            // Arrange
            var repoMock = new Mock<IMessageRepository>();
            repoMock
                .Setup(r => r.GetByIdAsync(It.IsAny<Guid>()))
                .ReturnsAsync((Message)null);

            var logic = new MessageLogic(repoMock.Object);

            var toUpdate = new Message
            {
                Id = Guid.NewGuid(),
                Title = "Title",
                Content = new string('x', 20),
                OrganizationId = Guid.NewGuid()
            };

            // Act
            var result = await logic.UpdateAsync(toUpdate);

            // Assert
            result.IsSuccess.Should().BeFalse();
            result.Error.Should().Contain("not found", StringComparison.OrdinalIgnoreCase);
        }

        [Fact]
        public async Task UpdateAsync_Inactive_Message_Returns_ValidationError()
        {
            // Arrange
            var stored = new Message
            {
                Id = Guid.NewGuid(),
                Title = "Old",
                Content = new string('x', 20),
                OrganizationId = Guid.NewGuid(),
                IsActive = false
            };

            var repoMock = new Mock<IMessageRepository>();
            repoMock
                .Setup(r => r.GetByIdAsync(stored.Id))
                .ReturnsAsync(stored);

            var logic = new MessageLogic(repoMock.Object);

            var toUpdate = new Message
            {
                Id = stored.Id,
                Title = "New Title",
                Content = new string('x', 20),
                OrganizationId = stored.OrganizationId
            };

            // Act
            var result = await logic.UpdateAsync(toUpdate);

            // Assert
            result.IsSuccess.Should().BeFalse();
            result.Error.Should().Contain("inactive", StringComparison.OrdinalIgnoreCase);
        }

        [Fact]
        public async Task DeleteAsync_NonExistent_Message_Returns_NotFound()
        {
            // Arrange
            var repoMock = new Mock<IMessageRepository>();
            repoMock
                .Setup(r => r.GetByIdAsync(It.IsAny<Guid>()))
                .ReturnsAsync((Message)null);

            var logic = new MessageLogic(repoMock.Object);

            // Act
            var result = await logic.DeleteAsync(Guid.NewGuid());

            // Assert
            result.IsSuccess.Should().BeFalse();
            result.Error.Should().Contain("not found", StringComparison.OrdinalIgnoreCase);
        }
    }
}
