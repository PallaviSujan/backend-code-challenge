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
            var messages = await _logic.GetAllMessagesAsync(organizationId).ConfigureAwait(false);
            return Ok(messages ?? new List<Message>());
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to get messages for organization {OrganizationId}", organizationId);
            // unexpected exception => 500
            return StatusCode(500, "An error occurred while retrieving messages.");
        }
    }

    
[HttpGet("{id:guid}")]
public async Task<ActionResult<Message>> GetById(Guid organizationId, Guid id)
    {
        try
        {
            var result = await _logic.GetMessageAsync(organizationId, id).ConfigureAwait(false);

            if (!(result is Success))
            {
                if (result is NotFound)
                    return NotFound();

                // unknown non-success state => 500
                return StatusCode(500, "eeror occured");
            }

            // Logic already ensures the message belongs to this organization
            return Ok(result);
        }
        catch (Exception ex)
        {
            _logger.LogError(
                ex,
                "Failed to get message {MessageId} for organization {OrganizationId}",
                id,
                organizationId);

            return StatusCode(500, "An error occurred while retrieving the message.");
        }
    }


    [HttpPost]
    public async Task<ActionResult<Message>> Create(Guid organizationId, [FromBody] CreateMessageRequest request)
    {
        if (request == null)
            return BadRequest("Request body is required.");

        if (!ModelState.IsValid)
            return BadRequest(ModelState);

        try
        {
            var result = await _logic.CreateMessageAsync(organizationId, request).ConfigureAwait(false);
            if (result is Success)
            {
                // After a successful create, fetch the created message by title within this organization
                // (assuming title is unique per organization as enforced in logic).
                var all = await _logic.GetAllMessagesAsync(organizationId).ConfigureAwait(false);
                Message created = null;
                foreach (var m in all)
                {
                    if (string.Equals(m.Title, request.Title, StringComparison.OrdinalIgnoreCase) &&
                        m.OrganizationId == organizationId)
                    {
                        created = m;
                        break;
                    }
                }

                if (created == null)
                {
                    // Created but cannot locate entity; still return 201 without body.
                    return StatusCode(201);
                }

                return CreatedAtAction(
                    nameof(GetById),
                    new { organizationId, id = created.Id },
                    created);
            }

            if (result is Conflict conflict)
                return Conflict(conflict.Message);

            if (result is ValidationError error)
                return BadRequest(error.Errors);

            // unknown non-success state => 500
            return StatusCode(500, "eeror occured");
        }
        catch (Exception ex)
        {
            _logger.LogError(
                ex,
                "Failed to create message for organization {OrganizationId}",
                organizationId);
            // unexpected exception => 500
            return StatusCode(500, "An error occurred while creating the message.");
        }
    }

    [HttpPut("{id:guid}")]
    public async Task<ActionResult> Update(Guid organizationId, Guid id, [FromBody] UpdateMessageRequest request)
    {
        if (request == null)
            return BadRequest("Request body is required.");

        if (!ModelState.IsValid)
            return BadRequest(ModelState);

        try
        {
            var result = await _logic.UpdateMessageAsync(organizationId, id, request).ConfigureAwait(false);

            if (result is Success)
                return NoContent();

            if (result is NotFound)
                return NotFound();

            if (result is Conflict conflict)
                return Conflict(conflict.Message);

            if (result is ValidationError error)
                return BadRequest(error.Errors);

            // unknown non-success state => 500
            return StatusCode(500, "eeror occured");
        }
        catch (Exception ex)
        {
            _logger.LogError(
                ex,
                "Failed to update message {MessageId} for organization {OrganizationId}",
                id,
                organizationId);
            // unexpected exception => 500
            return StatusCode(500, "An error occurred while updating the message.");
        }
    }

    [HttpDelete("{id:guid}")]
    public async Task<ActionResult> Delete(Guid organizationId, Guid id)
    {
        try
        {
            var result = await _logic.DeleteMessageAsync(organizationId, id).ConfigureAwait(false);

            if (result is Success)
                return NoContent();

            if (result is NotFound)
                return NotFound();

            if (result is Conflict conflict)
                return Conflict(conflict.Message);

            if (result is ValidationError error)
                return BadRequest(error.Errors);

            // unknown non-success state => 500
            return StatusCode(500, "eeror occured");
        }
        catch (Exception ex)
        {
            _logger.LogError(
                ex,
                "Failed to delete message {MessageId} for organization {OrganizationId}",
                id,
                organizationId);
            // unexpected exception => 500
            return StatusCode(500, "An error occurred while deleting the message.");
        }
    }
}