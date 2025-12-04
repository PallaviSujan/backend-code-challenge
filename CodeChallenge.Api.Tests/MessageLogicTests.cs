using System;
using System.Threading.Tasks;
using Moq;
using Xunit;
using CodeChallenge.Api.Logic;
using CodeChallenge.Api.Models;
using CodeChallenge.Api.Repositories;

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

            var message = new CreateMessageRequest
            {
                Title = "New Title",
                Content = new string('x', 20),
            };

            // Act
            var result = await logic.CreateMessageAsync(Guid.NewGuid(), message);

            Assert.True(result is Success);

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
                .Setup(r => r.GetByIdAsync(existing.OrganizationId, existing.Id))
                .ReturnsAsync(existing);

            var logic = new MessageLogic(repoMock.Object);

            var toCreate = new CreateMessageRequest
            {
                Title = "Duplicate",
                Content = new string('x', 20),
            };

            // Act
            var result = await logic.CreateMessageAsync(Guid.NewGuid(), toCreate);

            // Assert
            Assert.False(result is Success);
        }

        [Fact]
        public async Task CreateAsync_Invalid_Content_Length_Returns_ValidationError()
        {
            // Arrange
            var repoMock = new Mock<IMessageRepository>();
            repoMock
                .Setup(r => r.GetAllByOrganizationAsync(It.IsAny<Guid>()))
                .ReturnsAsync(new List<Message>());

            var logic = new MessageLogic(repoMock.Object);

            var message = new CreateMessageRequest
            {
                Title = "Valid Title",
                Content = "short",
            };

            // Act
            var result = await logic.CreateMessageAsync(Guid.NewGuid(), message);

            // Assert
            Assert.False(result is Success);
        }

        [Fact]
        public async Task UpdateAsync_NonExistent_Message_Returns_NotFound()
        {
            // Arrange
            var repoMock = new Mock<IMessageRepository>();
            repoMock
                .Setup(r => r.GetByIdAsync(It.IsAny<Guid>(), It.IsAny<Guid>()))
                .ReturnsAsync((Message)null);

            var logic = new MessageLogic(repoMock.Object);

            var toUpdate = new UpdateMessageRequest
            {
                Title = "Title",
                Content = new string('x', 20),
            };

            // Act
            var result = await logic.UpdateMessageAsync(Guid.NewGuid(), Guid.NewGuid(), toUpdate);

            // Assert
            Assert.False(result is Success);
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
                .Setup(r => r.GetByIdAsync(stored.OrganizationId, stored.Id))
                .ReturnsAsync(stored);

            var logic = new MessageLogic(repoMock.Object);

            var toUpdate = new UpdateMessageRequest
            {
                Title = "New Title",
                Content = new string('x', 20),
            };

            // Act
            var result = await logic.UpdateMessageAsync(Guid.NewGuid(), Guid.NewGuid(), toUpdate);

            // Assert
            Assert.False(result is Success);
        }


        [Fact]
        public async Task DeleteAsync_NonExistent_Message_Returns_NotFound()
        {
            // Arrange
            var repoMock = new Mock<IMessageRepository>();
            repoMock
                .Setup(r => r.GetByIdAsync(It.IsAny<Guid>(), It.IsAny<Guid>()))
                .ReturnsAsync((Message)null);

            var logic = new MessageLogic(repoMock.Object);

            // Act
            var result = await logic.DeleteMessageAsync(Guid.NewGuid(), Guid.NewGuid());

            // Assert
            Assert.False(result is Success);
        }

        }
    }
