using MainLedger.Application.Contact.Commands.SubmitContactMessage;
using MainLedger.Contracts.Contact;
using MediatR;
using Microsoft.AspNetCore.Mvc;

namespace MainLedger.API.Controllers;

[ApiController]
[Route("api/contact")]
public class ContactController : ControllerBase
{
    private readonly ISender _sender;

    public ContactController(ISender sender)
    {
        _sender = sender;
    }

    /// <summary>
    /// Submit a contact form message
    /// </summary>
    [HttpPost]
    [ProducesResponseType(typeof(SubmitContactMessageResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    public async Task<IActionResult> SubmitContactMessage(
        [FromBody] SubmitContactMessageRequest request
    )
    {
        var command = new SubmitContactMessageCommand(
            request.FirstName,
            request.LastName,
            request.Email,
            request.Subject,
            request.Message
        );

        await _sender.Send(command);

        return Ok(
            new SubmitContactMessageResponse
            {
                Message = "Thank you for contacting us! We'll get back to you soon.",
            }
        );
    }
}
