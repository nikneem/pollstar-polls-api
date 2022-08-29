using Microsoft.AspNetCore.Mvc;
using PollStar.Polls.Abstractions.DataTransferObjects;
using PollStar.Polls.Abstractions.Services;
using PollStar.Polls.ErrorCodes;
using PollStar.Polls.Exceptions;

namespace PollStar.API.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class PollsController : ControllerBase
    {
        private readonly IPollStarPollsService _service;

        [HttpGet]
        public async Task<IActionResult> List([FromQuery] Guid sessionId)
        {
            try
            {
                var service = await _service.GetPollsListAsync(sessionId);
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

        [HttpGet("{id}")]
        public async Task<IActionResult> Get(Guid id)
        {
            try
            {
                var service = await _service.GetPollAsync(id);
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

        //[HttpPut("{id}")]
        //public async Task<IActionResult> Put(Guid id,CreatePollDto dto)
        //{
        //    try
        //    {
        //        var service = await _service.(dto);
        //        return Ok(service);
        //    }
        //    catch (PollStarPollException psEx)
        //    {
        //        if (psEx.ErrorCode == PollStarPollErrorCode.PollNotFound)
        //        {
        //            return new NotFoundResult();
        //        }
        //    }

        //    return BadRequest();
        //}

        public PollsController(IPollStarPollsService service)
        {
            _service = service;
        }

    }
}
