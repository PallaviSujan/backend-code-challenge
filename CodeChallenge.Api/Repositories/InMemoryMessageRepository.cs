using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using CodeChallenge.Api.Models;

namespace CodeChallenge.Api.Repositories
{
    /// <summary>
    /// In-memory implementation of IMessageRepository
    /// </summary>
    public class InMemoryMessageRepository : IMessageRepository
    {
        private readonly Dictionary<Guid, Message> _messages = new();
        private readonly object _lock = new();

        public Task<Message?> GetByIdAsync(Guid organizationId, Guid id)
        {
            lock (_lock)
            {
                if (_messages.TryGetValue(id, out var message) &&
                    message.OrganizationId == organizationId)
                {
                    return Task.FromResult<Message?>(message);
                }

                return Task.FromResult<Message?>(null);
            }
        }

        public Task<IEnumerable<Message>> GetAllByOrganizationAsync(Guid organizationId)
        {
            lock (_lock)
            {
                var messages = _messages.Values
                    .Where(m => m.OrganizationId == organizationId)
                    .OrderByDescending(m => m.CreatedAt)
                    .ToList();

                return Task.FromResult<IEnumerable<Message>>(messages);
            }
        }

        public Task<Message?> GetByTitleAsync(Guid organizationId, string title)
        {
            lock (_lock)
            {
                var message = _messages.Values
                    .FirstOrDefault(m =>
                        m.OrganizationId == organizationId &&
                        m.Title.Equals(title, StringComparison.OrdinalIgnoreCase));

                return Task.FromResult(message);
            }
        }

        public Task<Message> CreateAsync(Message message)
        {
            lock (_lock)
            {
                message.Id = Guid.NewGuid();
                message.CreatedAt = DateTime.UtcNow;
                message.UpdatedAt = message.CreatedAt;

                _messages[message.Id] = message;

                return Task.FromResult(message);
            }
        }

        // Validates organization via existing stored message
        public Task<Message?> UpdateAsync(Message message)
        {
            lock (_lock)
            {
                if (!_messages.TryGetValue(message.Id, out var existing) ||
                    existing.OrganizationId != message.OrganizationId)
                {
                    return Task.FromResult<Message?>(null);
                }

                message.UpdatedAt = DateTime.UtcNow;
                _messages[message.Id] = message;

                return Task.FromResult<Message?>(message);
            }
        }

        // Validates organizationId before deleting
        public Task<bool> DeleteAsync(Guid organizationId, Guid id)
        {
            lock (_lock)
            {
                if (_messages.TryGetValue(id, out var message) &&
                    message.OrganizationId == organizationId)
                {
                    _messages.Remove(id);
                    return Task.FromResult(true);
                }

                return Task.FromResult(false);
            }
        }
    }
}
