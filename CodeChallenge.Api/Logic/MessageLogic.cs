using System;
using System.Threading.Tasks;
using CodeChallenge.Api.Data; // assumed repository namespace
using CodeChallenge.Api.Models; // assumed Message model namespace
using CodeChallenge.Api.Logic.Results; // assumed Results namespace

namespace CodeChallenge.Api.Logic
{
    public class MessageLogic : IMessageLogic
    {
        private readonly IMessageRepository _repository;

        public MessageLogic(IMessageRepository repository)
        {
            _repository = repository ?? throw new ArgumentNullException(nameof(repository));
        }

        public async Task<Result<Message>> CreateAsync(Message message)
        {
            if (message == null) return Result<Message>.Failure("Message is required.");

            ValidateTitle(message.Title, out var titleError);
            if (titleError != null) return Result<Message>.Failure(titleError);

            ValidateContent(message.Content, out var contentError);
            if (contentError != null) return Result<Message>.Failure(contentError);

            var existing = await _repository.FindByTitleAndOrganizationAsync(message.Title, message.OrganizationId);
            if (existing != null)
                return Result<Message>.Failure("A message with the same title already exists for this organization.");

            message.Id = message.Id == Guid.Empty ? Guid.NewGuid() : message.Id;
            message.CreatedAt = DateTime.UtcNow;
            message.UpdatedAt = DateTime.UtcNow;
            message.IsActive = true;

            await _repository.AddAsync(message);
            return Result<Message>.Success(message);
        }

        public async Task<Result<Message>> UpdateAsync(Message message)
        {
            if (message == null) return Result<Message>.Failure("Message is required.");

            var stored = await _repository.GetByIdAsync(message.Id);
            if (stored == null) return Result<Message>.Failure("Message not found.");
            if (!stored.IsActive) return Result<Message>.Failure("Cannot update an inactive message.");

            ValidateTitle(message.Title, out var titleError);
            if (titleError != null) return Result<Message>.Failure(titleError);

            ValidateContent(message.Content, out var contentError);
            if (contentError != null) return Result<Message>.Failure(contentError);

            var duplicate = await _repository.FindByTitleAndOrganizationAsync(message.Title, message.OrganizationId);
            if (duplicate != null && duplicate.Id != message.Id)
                return Result<Message>.Failure("A message with the same title already exists for this organization.");

            // Apply allowed changes and update UpdatedAt automatically
            stored.Title = message.Title;
            stored.Content = message.Content;
            stored.UpdatedAt = DateTime.UtcNow;

            await _repository.UpdateAsync(stored);
            return Result<Message>.Success(stored);
        }

        public async Task<Result> DeleteAsync(Guid id)
        {
            var stored = await _repository.GetByIdAsync(id);
            if (stored == null) return Result.Failure("Message not found.");
            if (!stored.IsActive) return Result.Failure("Cannot delete an inactive message.");

            await _repository.DeleteAsync(id);
            return Result.Success();
        }

        public async Task<Result<Message>> GetByIdAsync(Guid id)
        {
            var stored = await _repository.GetByIdAsync(id);
            if (stored == null) return Result<Message>.Failure("Message not found.");
            return Result<Message>.Success(stored);
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
            {
                error = "Title must be between 3 and 200 characters.";
            }
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
            {
                error = "Content must be between 10 and 1000 characters.";
            }
        }
    }
}