using Memphis.API.Extensions;
using Memphis.API.Services;
using Memphis.Shared.Dtos.Vatusa;
using Memphis.Shared.Models;
using Memphis.Shared.Utils;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Memphis.API.Controllers;

[ApiController]
[Route("[controller]")]
[Produces("application/json")]
public class TransferRequestsController : ControllerBase
{
    private readonly RedisService _redisService;
    private readonly VatusaService _vatusaService;
    private readonly IHub _sentryHub;
    private readonly ILogger<TransferRequestsController> _logger;

    public TransferRequestsController(RedisService redisService, VatusaService vatusaService,
        IHub sentryHub, ILogger<TransferRequestsController> logger)
    {
        _redisService = redisService;
        _vatusaService = vatusaService;
        _sentryHub = sentryHub;
        _logger = logger;
    }

    [HttpGet]
    [Authorize(Roles = Constants.SeniorStaff)]
    [ProducesResponseType(typeof(Response<IList<TransferRequest>>), 200)]
    [ProducesResponseType(typeof(Response<string?>), 400)]
    [ProducesResponseType(401)]
    [ProducesResponseType(403)]
    [ProducesResponseType(typeof(Response<int>), 404)]
    [ProducesResponseType(typeof(Response<string?>), 500)]
    public async Task<ActionResult<Response<IList<TransferRequest>>>> GetTransferRequests()
    {
        try
        {
            if (!await _redisService.ValidateRoles(Request.HttpContext.User, Constants.SeniorStaffList))
                return StatusCode(401);

            var result = new List<TransferRequest>();
            var transferRequests = await _vatusaService.GetTransferRequests();
            if (transferRequests != null && transferRequests.Count > 0)
            {
                foreach (var entry in transferRequests)
                {
                    var transferHistory = await _vatusaService.GetTransferHistory(entry.Cid);
                    var transferHistoryList = new List<TransferRequestHistory>();

                    if (transferHistory != null && transferHistory.Count > 0)
                    {
                        foreach (var history in transferHistory)
                        {
                            transferHistoryList.Add(new TransferRequestHistory
                            {
                                From = history.From,
                                To = history.To,
                                Status = history.Status,
                                Reason = history.Reason,
                                Created = history.Created
                            });
                        }
                    }

                    result.Add(new TransferRequest
                    {
                        Id = entry.Id,
                        Cid = entry.Cid,
                        FirstName = entry.FirstName,
                        LastName = entry.LastName,
                        Email = entry.Email,
                        From = entry.FromFacility.Name,
                        Reason = entry.Reason,
                        TransferHistory = transferHistoryList,
                        Submitted = entry.Date
                    });
                }
            }
            return Ok(new Response<IList<TransferRequest>>
            {
                StatusCode = 200,
                Message = $"Got {result.Count} transfer requests",
                Data = result
            });
        }
        catch (Exception ex)
        {
            _logger.LogError("GetTransferRequests error '{Message}'\n{StackTrace}", ex.Message, ex.StackTrace);
            return _sentryHub.CaptureException(ex).ReturnActionResult();
        }
    }

    [HttpPut("{transferId}")]
    [Authorize(Roles = $"{Constants.SeniorStaff},")]
    [ProducesResponseType(typeof(Response<string?>), 200)]
    [ProducesResponseType(typeof(Response<string?>), 400)]
    [ProducesResponseType(401)]
    [ProducesResponseType(403)]
    [ProducesResponseType(typeof(Response<int>), 404)]
    [ProducesResponseType(typeof(Response<string?>), 500)]
    public async Task<ActionResult<Response<string?>>> ProcessTransferRequest(int transferId, TransferRequestActionDto payload)
    {
        try
        {
            if (!await _redisService.ValidateRoles(Request.HttpContext.User, Constants.SeniorStaffList))
                return StatusCode(401);

            if (payload.Action != "accept" && payload.Action != "reject")
            {
                return BadRequest(new Response<string?>
                {
                    StatusCode = 400,
                    Message = "Invalid action, options are 'approve' or 'reject'"
                });
            }

            var cid = Request.HttpContext.GetCid();
            if (cid == null)
            {
                return BadRequest(new Response<string?>
                {
                    StatusCode = 404,
                    Message = "Submitting user not found"
                });
            }
            var result = await _vatusaService.ActionTransferRequest(transferId, payload.Action, cid.ToString()!, payload.Reason);
            if (result.Success)
            {
                return Ok(new Response<string?>
                {
                    StatusCode = 200,
                    Message = $"Successfully {payload.Action}ed transfer request {transferId}"
                });
            }
            return BadRequest(new Response<string?>
            {
                StatusCode = 400,
                Message = result.Message ?? "Failed to process transfer request"
            });
        }
        catch (Exception ex)
        {
            _logger.LogError("ProcessTransferRequest error '{Message}'\n{StackTrace}", ex.Message, ex.StackTrace);
            return _sentryHub.CaptureException(ex).ReturnActionResult();
        }
    }
}
