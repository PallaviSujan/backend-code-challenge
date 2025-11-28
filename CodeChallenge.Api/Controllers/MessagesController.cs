using CodeChallenge.Api.Models;
using CodeChallenge.Api.Repositories;
using Microsoft.AspNetCore.Mvc;

namespace CodeChallenge.Api.Controllers;

[ApiController]
[Route("api/v1/organizations/{organizationId}/messages")]
public class MessagesController : ControllerBase
{
    private readonly IMessageRepository _repository;
    private readonly ILogger<MessagesController> _logger;

    public MessagesController(IMessageRepository repository, ILogger<MessagesController> logger)
    {
        _repository = repository;
        _logger = logger;
    }

    [HttpGet]
    public async Task<ActionResult<IEnumerable<Message>>> GetAll(Guid organizationId)
    {
        try
        {
            var messages = await _repository.GetAllAsync(organizationId).ConfigureAwait(false);
            return Ok(messages ?? new List<Message>());
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to get messages for organization {OrganizationId}", organizationId);
            return StatusCode(500, "An error occurred while retrieving messages.");
        }
    }

    [HttpGet("{id}")]
    public async Task<ActionResult<Message>> GetById(Guid organizationId, Guid id)
    {
        try
            {
                var message = await _repository.GetByIdAsync(organizationId, id).ConfigureAwait(false);
                if (message == null)
                {
                    return NotFound();
                }

                return Ok(message);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to get message {MessageId} for organization {OrganizationId}", id, organizationId);
                return StatusCode(500, "An error occurred while retrieving the message.");
            }
    }

    [HttpPost]
    public async Task<ActionResult<Message>> Create(Guid organizationId, [FromBody] CreateMessageRequest request)
    {
        if (request == null)
            {
                return BadRequest("Request body is required.");
            }

            if (!ModelState.IsValid)
            {
                return BadRequest(ModelState);
            }

            try
            {
                var message = new Message
                {
                    Id = Guid.NewGuid(),
                    OrganizationId = organizationId,
                    CreatedAt = DateTime.UtcNow,
                    Title = request.Title,
                    Content = request.Content
                };

                await _repository.CreateAsync(organizationId, message).ConfigureAwait(false);

                return CreatedAtAction(nameof(GetById), new { organizationId = organizationId, id = message.Id }, message);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to create message for organization {OrganizationId}", organizationId);
                return StatusCode(500, "An error occurred while creating the message.");
            }
        }

    [HttpPut("{id}")]
    public async Task<ActionResult> Update(Guid organizationId, Guid id, [FromBody] UpdateMessageRequest request)
    {
        if (request == null)
            {
                return BadRequest("Request body is required.");
            }

            if (!ModelState.IsValid)
            {
                return BadRequest(ModelState);
            }

            try
            {
                var existing = await _repository.GetByIdAsync(organizationId, id).ConfigureAwait(false);
                if (existing == null)
                {
                    return NotFound();
                }

                // Map updatable fields from the request
                existing.Title = request.Title;
                existing.Content = request.Content;
                existing.UpdatedAt = DateTime.UtcNow;

                await _repository.UpdateAsync(organizationId, existing).ConfigureAwait(false);

                return NoContent();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to update message {MessageId} for organization {OrganizationId}", id, organizationId);
                return StatusCode(500, "An error occurred while updating the message.");
            }
    }

    [HttpDelete("{id}")]
     public async Task<ActionResult> Delete(Guid organizationId, Guid id)
     {
         try
         {
             var existing = await _repository.GetByIdAsync(organizationId, id).ConfigureAwait(false);
             if (existing == null)
             {
                 return NotFound();
             }

             await _repository.DeleteAsync(organizationId, id).ConfigureAwait(false);

             return NoContent();
         }
         catch (Exception ex)
         {
             _logger.LogError(ex, "Failed to delete message {MessageId} for organization {OrganizationId}", id, organizationId);
             return StatusCode(500, "An error occurred while deleting the message.");
         }
     }
}