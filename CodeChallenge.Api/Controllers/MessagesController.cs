using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using CodeChallenge.Api.Logic;
using CodeChallenge.Api.Models;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;

namespace CodeChallenge.Api.Controllers;

[ApiController]
[Route("api/v1/organizations/{organizationId}/messages")]
public class MessagesController : ControllerBase
{
    private readonly IMessageLogic _logic;
    private readonly ILogger<MessagesController> _logger;

    public MessagesController(IMessageLogic logic, ILogger<MessagesController> logger)
    {
        _logic = logic ?? throw new ArgumentNullException(nameof(logic));
        _logger = logger;
    }

    [HttpGet]
    public async Task<ActionResult<IEnumerable<Message>>> GetAll(Guid organizationId)
    {
        try
        {
            var result = await _logic.GetAllAsync(organizationId).ConfigureAwait(false);
            if (result.IsSuccess)
            {
                return Ok(result.Value ?? new List<Message>());
            }

            // treat failures as server/bad request depending on message
            return StatusCode(500, result.Error);
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
            var result = await _logic.GetByIdAsync(organizationId, id).ConfigureAwait(false);
            if (result.IsSuccess)
                return Ok(result.Value);

            if (!string.IsNullOrEmpty(result.Error) && result.Error.Contains("not found", StringComparison.OrdinalIgnoreCase))
                return NotFound();

            return BadRequest(result.Error);
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
                Title = request.Title,
                Content = request.Content
            };

            var result = await _logic.CreateAsync(organizationId, message).ConfigureAwait(false);
            if (result.IsSuccess)
            {
                return CreatedAtAction(nameof(GetById), new { organizationId = organizationId, id = result.Value.Id }, result.Value);
            }

            return BadRequest(result.Error);
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
            var message = new Message
            {
                Id = id,
                OrganizationId = organizationId,
                Title = request.Title,
                Content = request.Content
            };

            var result = await _logic.UpdateAsync(organizationId, message).ConfigureAwait(false);
            if (result.IsSuccess)
                return NoContent();

            if (!string.IsNullOrEmpty(result.Error) && result.Error.Contains("not found", StringComparison.OrdinalIgnoreCase))
                return NotFound();

            return BadRequest(result.Error);
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
            var result = await _logic.DeleteAsync(organizationId, id).ConfigureAwait(false);
            if (result.IsSuccess)
                return NoContent();

            if (!string.IsNullOrEmpty(result.Error) && result.Error.Contains("not found", StringComparison.OrdinalIgnoreCase))
                return NotFound();

            return BadRequest(result.Error);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to delete message {MessageId} for organization {OrganizationId}", id, organizationId);
            return StatusCode(500, "An error occurred while deleting the message.");
        }
    }
}
