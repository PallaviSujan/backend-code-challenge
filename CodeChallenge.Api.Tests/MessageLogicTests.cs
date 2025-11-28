using System;
using System.Threading.Tasks;
using FluentAssertions;
using Moq;
using Xunit;
using CodeChallenge.Api.Logic;
using CodeChallenge.Api.Data;
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
            repoMock
                .Setup(r => r.FindByTitleAndOrganizationAsync(It.IsAny<string>(), It.IsAny<Guid>()))
                .ReturnsAsync((Message)null);
            repoMock
                .Setup(r => r.AddAsync(It.IsAny<Message>()))
                .Returns(Task.CompletedTask);

            var logic = new MessageLogic(repoMock.Object);

            var message = new Message
            {
                Title = "New Title",
                Content = new string('x', 20),
                OrganizationId = Guid.NewGuid()
            };

            // Act
            var result = await logic.CreateAsync(message);

            // Assert
            result.IsSuccess.Should().BeTrue();
            repoMock.Verify(r => r.AddAsync(It.Is<Message>(m =>
                m.Title == message.Title &&
                m.OrganizationId == message.OrganizationId &&
                m.IsActive &&
                m.CreatedAt != default &&
                m.UpdatedAt != default
            )), Times.Once);
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
                .Setup(r => r.FindByTitleAndOrganizationAsync("Duplicate", existing.OrganizationId))
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
                .Setup(r => r.FindByTitleAndOrganizationAsync(It.IsAny<string>(), It.IsAny<Guid>()))
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
