using Microsoft.AspNetCore.Mvc;
using PollStar.Polls.Abstractions.DataTransferObjects;
using PollStar.Polls.Abstractions.Services;
using PollStar.Polls.ErrorCodes;
using PollStar.Polls.Exceptions;

namespace PollStar.Polls.Api.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class PollsController : ControllerBase
    {
        private readonly IPollStarPollsService _service;

        [HttpGet]
        public async Task<IActionResult> List()
        {
            try
            {
                var sessionIdValue = Request.Query["session-id"];
                if (sessionIdValue.Count == 1 && Guid.TryParse(sessionIdValue.ToString(), out Guid sessionId))
                {
                    var service = await _service.GetPollsListAsync(sessionId);
                    return Ok(service);
                }
            }
            catch (PollStarPollException psEx)
            {
                if (psEx.ErrorCode == PollStarPollErrorCode.PollNotFound)
                {
                    return new NotFoundResult();
                }
            }

            return BadRequest();
        }

        [HttpGet("{id}")]
        public async Task<IActionResult> Get(Guid id)
        {
            try
            {
                var service = await _service.GetPollDetailsAsync(id);
                return Ok(service);
            }
            catch (PollStarPollException psEx)
            {
                if (psEx.ErrorCode == PollStarPollErrorCode.PollNotFound)
                {
                    return new NotFoundResult();
                }
            }

            return BadRequest();
        }

        [HttpPost]
        public async Task<IActionResult> Post(CreatePollDto dto)
        {
            try
            {
                var service = await _service.CreatePollAsync(dto);
                return Ok(service);
            }
            catch (PollStarPollException psEx)
            {
                if (psEx.ErrorCode == PollStarPollErrorCode.PollNotFound)
                {
                    return new NotFoundResult();
                }
            }

            return BadRequest();
        }

        [HttpPut("{id:guid}")]
        public async Task<IActionResult> Put(Guid id, PollDto dto)
        {
            try
            {
                var service = await _service.UpdatePollAsync(id, dto);
                return Ok(service);
            }
            catch (PollStarPollException psEx)
            {
                if (psEx.ErrorCode == PollStarPollErrorCode.PollNotFound)
                {
                    return new NotFoundResult();
                }
            }

            return BadRequest();
        }

        [HttpDelete("{id:guid}")]
        public async Task<IActionResult> Delete(Guid id)
        {
            try
            {
                var deleted = await _service.DeletePollAsync(id);
                return deleted ? Ok() : BadRequest();
            }
            catch (PollStarPollException psEx)
            {
                if (psEx.ErrorCode == PollStarPollErrorCode.PollNotFound)
                {
                    return new NotFoundResult();
                }
            }

            return BadRequest();
        }

        [HttpGet("{id}/activate")]
        public async Task<IActionResult> Activate(Guid id)
        {
            try
            {
                var service = await _service.ActivatePollAsync(id);
                return Ok(service);
            }
            catch (PollStarPollException psEx)
            {
                if (psEx.ErrorCode == PollStarPollErrorCode.PollNotFound)
                {
                    return new NotFoundResult();
                }
            }

            return BadRequest();
        }

        public PollsController(IPollStarPollsService service)
        {
            _service = service;
        }

    }
}
