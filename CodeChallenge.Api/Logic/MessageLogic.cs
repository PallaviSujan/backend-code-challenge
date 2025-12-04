using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using CodeChallenge.Api.Models;
using CodeChallenge.Api.Repositories;
using static System.Runtime.InteropServices.JavaScript.JSType;

namespace CodeChallenge.Api.Logic
{
    public class MessageLogic : IMessageLogic
    {
        private readonly IMessageRepository _repository;

        public MessageLogic(IMessageRepository repository)
        {
            _repository = repository ?? throw new ArgumentNullException(nameof(repository));
        }

        public async Task<Result> CreateMessageAsync(Guid organizationId, CreateMessageRequest request)
        {
            var errors = new Dictionary<string, string[]>();

            if (request == null)
            {
                errors.Add("Request", new[] { "Request is required." });
                return new ValidationError(errors);
            }

            ValidateTitle(request.Title, out var titleError);
            if (titleError != null)
            {
                errors.Add("TitleError", new[] { "Title is invalid." });
                return new ValidationError(errors);
            }

            ValidateContent(request.Content, out var contentError);
            if (titleError != null)
            {
                errors.Add("ContentError", new[] { "Content is invalid." });
                return new ValidationError(errors);
            }

            // use GetByTitleAsync from IMessageRepository
            var existing = await _repository
                .GetByTitleAsync(organizationId, request.Title)
                .ConfigureAwait(false);

            if (existing != null)
            {
                errors.Add("ExistingConflict", new[] { "A message with the same title already exists for this organization." });
                return new ValidationError(errors);
            }
                    
            var message = new Message
            {
                Id = Guid.NewGuid(),
                Title = request.Title,
                Content = request.Content,
                OrganizationId = organizationId,
                CreatedAt = DateTime.UtcNow,
                UpdatedAt = DateTime.UtcNow,
                IsActive = true
            };

            await _repository.CreateAsync(message).ConfigureAwait(false);
            return new Success();
        }

        public async Task<Result> UpdateMessageAsync(Guid organizationId, Guid id, UpdateMessageRequest request)
        {
            var errors = new Dictionary<string, string[]>();

            if (request == null)
            {
                errors.Add("Request", new[] { "Request is required." });
                return new ValidationError(errors);
            }

            var stored = await _repository
                .GetByIdAsync(organizationId, id)
                .ConfigureAwait(false);

            if (stored == null)
                return new NotFound("Message not found.");
            if (!stored.IsActive)
            {
                errors.Add("Stored", new[] { "Cannot update an inactive message." });
                return new ValidationError(errors);
            }

            
            ValidateTitle(request.Title, out var titleError);
            if (titleError != null)
            {
                errors.Add("TitleError", new[] { titleError });
                return new ValidationError(errors);
            }

            
            ValidateContent(request.Content, out var contentError);
            if (contentError != null)
            {
                errors.Add("ContentError", new[] { contentError });
                return new ValidationError(errors);
            }

            var duplicate = await _repository
                .GetByTitleAsync(organizationId, request.Title)
                .ConfigureAwait(false);

            if (duplicate != null && duplicate.Id != stored.Id)
                return new Conflict(
                    "A message with the same title already exists for this organization.");

            stored.Title = request.Title;
            stored.Content = request.Content;
            stored.UpdatedAt = DateTime.UtcNow;

            var updated = await _repository.UpdateAsync(stored).ConfigureAwait(false);
            if (updated == null)
                return new NotFound("Message not found.");

            return new Success();
        }

        public async Task<Result> DeleteMessageAsync(Guid organizationId, Guid id)
        {
            var existing = await _repository
                .GetByIdAsync(organizationId, id)
                .ConfigureAwait(false);

            if (existing == null)
                return new NotFound("Message not found.");

            var deleted = await _repository
                .DeleteAsync(organizationId, id)
                .ConfigureAwait(false);

            if (!deleted)
                return new NotFound("Message not found.");

            return new Success();
        }

        public async Task<Message?> GetMessageAsync(Guid organizationId, Guid id)
        {
            return await _repository.GetByIdAsync(organizationId, id).ConfigureAwait(false);
        }

        public async Task<Result> GetByIdAsync(Guid organizationId, Guid id)
        {
            var stored = await _repository.GetByIdAsync(organizationId, id).ConfigureAwait(false);
            if (stored == null)
                return new NotFound("Message not found.");

            return new Success();
        }

        public async Task<IEnumerable<Message>> GetAllMessagesAsync(Guid organizationId)
        {
            return await _repository
                .GetAllByOrganizationAsync(organizationId)
                .ConfigureAwait(false);
        }

        private void ValidateTitle(string title, out string error)
        {
            error = null;
            if (string.IsNullOrWhiteSpace(title))
            {
                error = "Title is required.";
                return;
            }

            if (title.Length < 3 || title.Length > 200)
                error = "Title must be between 3 and 200 characters.";
        }

        private void ValidateContent(string content, out string error)
        {
            error = null;
            if (content == null)
            {
                error = "Content is required.";
                return;
            }

            if (content.Length < 10 || content.Length > 1000)
                error = "Content must be between 10 and 1000 characters.";
        }
    }
}
