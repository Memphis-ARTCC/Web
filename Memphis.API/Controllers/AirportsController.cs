﻿using FluentValidation;
using FluentValidation.Results;
using Memphis.API.Data;
using Memphis.API.Extensions;
using Memphis.API.Services;
using Memphis.Shared.Dtos;
using Memphis.Shared.Models;
using Memphis.Shared.Utils;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Newtonsoft.Json;
using Constants = Memphis.Shared.Utils.Constants;

namespace Memphis.API.Controllers;

[ApiController]
[Route("[controller]")]
[Produces("application/json")]
public class AirportsController : ControllerBase
{
    private readonly DatabaseContext _context;
    private readonly RedisService _redisService;
    private readonly LoggingService _loggingService;
    private readonly IValidator<AirportDto> _validator;
    private readonly ISentryClient _sentryHub;
    private readonly ILogger<AirportsController> _logger;

    public AirportsController(DatabaseContext context, RedisService redisService, LoggingService loggingService,
        IValidator<AirportDto> validator, ISentryClient sentryHub, ILogger<AirportsController> logger)
    {
        _context = context;
        _redisService = redisService;
        _loggingService = loggingService;
        _validator = validator;
        _sentryHub = sentryHub;
        _logger = logger;
    }


    [HttpPost]
    [Authorize(Roles = Constants.CanAirports)]
    [ProducesResponseType(typeof(Response<Airport>), 201)]
    [ProducesResponseType(typeof(Response<IList<ValidationFailure>>), 400)]
    [ProducesResponseType(401)]
    [ProducesResponseType(403)]
    [ProducesResponseType(typeof(Response<string?>), 500)]
    public async Task<ActionResult<Response<Airport>>> CreateAirport(AirportDto payload)
    {
        try
        {
            if (!await _redisService.ValidateRoles(Request.HttpContext.User, Constants.CanAirportsList))
            {
                return StatusCode(401);
            }

            ValidationResult validation = await _validator.ValidateAsync(payload);
            if (!validation.IsValid)
            {
                return BadRequest(new Response<IList<ValidationFailure>>
                {
                    StatusCode = 400,
                    Message = "Validation failure",
                    Data = validation.Errors
                });
            }

            Microsoft.EntityFrameworkCore.ChangeTracking.EntityEntry<Airport> result = await _context.Airports.AddAsync(new Airport
            {
                Name = payload.Name,
                Icao = payload.Icao,
            });
            await _context.SaveChangesAsync();
            string newData = JsonConvert.SerializeObject(result.Entity);
            await _loggingService.AddWebsiteLog(Request, $"Created airport {result.Entity.Id}", string.Empty, newData);

            return StatusCode(201, new Response<Airport>
            {
                StatusCode = 201,
                Message = $"Created airport '{result.Entity.Id}'",
                Data = result.Entity
            });
        }
        catch (Exception ex)
        {
            _logger.LogError("CreateAirport error '{Message}'\n{StackTrace}", ex.Message, ex.StackTrace);
            return _sentryHub.CaptureException(ex).ReturnActionResult();
        }
    }

    [HttpGet]
    [ProducesResponseType(typeof(Response<IList<Airport>>), 200)]
    [ProducesResponseType(typeof(Response<string?>), 500)]
    public async Task<ActionResult<Response<IList<Airport>>>> GetAirports()
    {
        try
        {
            List<Airport> result = await _context.Airports.ToListAsync();
            return Ok(new Response<IList<Airport>>
            {
                StatusCode = 200,
                Message = $"Got {result.Count} airports",
                Data = result
            });
        }
        catch (Exception ex)
        {
            _logger.LogError("GetAirports error '{Message}'\n{StackTrace}", ex.Message, ex.StackTrace);
            return _sentryHub.CaptureException(ex).ReturnActionResult();
        }
    }

    [HttpGet("{airportId:int}")]
    [ProducesResponseType(typeof(Response<Airport>), 200)]
    [ProducesResponseType(typeof(Response<int>), 404)]
    [ProducesResponseType(typeof(Response<string?>), 500)]
    public async Task<ActionResult<Response<IList<Airport>>>> GetAirport(int airportId)
    {
        try
        {
            Airport? result = await _context.Airports.FindAsync(airportId);
            if (result == null)
            {
                return NotFound(new Response<int>
                {
                    StatusCode = 404,
                    Message = $"Airport '{airportId}' not found",
                    Data = airportId
                });
            }

            return Ok(new Response<Airport>
            {
                StatusCode = 200,
                Message = $"Got airport '{result.Id}'",
                Data = result
            });
        }
        catch (Exception ex)
        {
            _logger.LogError("GetAirport error '{Message}'\n{StackTrace}", ex.Message, ex.StackTrace);
            return _sentryHub.CaptureException(ex).ReturnActionResult();
        }
    }

    [HttpPut]
    [Authorize(Roles = Constants.CanAirports)]
    [ProducesResponseType(typeof(Response<Airport>), 200)]
    [ProducesResponseType(typeof(Response<IList<ValidationFailure>>), 400)]
    [ProducesResponseType(401)]
    [ProducesResponseType(403)]
    [ProducesResponseType(typeof(Response<string?>), 404)]
    [ProducesResponseType(typeof(Response<string?>), 500)]
    public async Task<ActionResult<Response<Airport>>> UpdateAirport(AirportDto payload)
    {
        try
        {
            if (!await _redisService.ValidateRoles(Request.HttpContext.User, Constants.CanAirportsList))
            {
                return StatusCode(401);
            }

            ValidationResult validation = await _validator.ValidateAsync(payload);
            if (!validation.IsValid)
            {
                return BadRequest(new Response<IList<ValidationFailure>>
                {
                    StatusCode = 400,
                    Message = "Validation failure",
                    Data = validation.Errors
                });
            }

            Airport? airport = await _context.Airports.FindAsync();
            if (airport == null)
            {
                return NotFound(new Response<string?>
                {
                    StatusCode = 404,
                    Message = $"Airport '{payload.Id}' not found",
                });
            }

            string oldData = JsonConvert.SerializeObject(airport);
            airport.Name = payload.Name;
            airport.Icao = payload.Icao;
            airport.Updated = DateTimeOffset.UtcNow;
            await _context.SaveChangesAsync();
            string newData = JsonConvert.SerializeObject(airport);

            await _loggingService.AddWebsiteLog(Request, $"Updated airport '{airport.Id}'", oldData, newData);

            return Ok(new Response<Airport>
            {
                StatusCode = 200,
                Message = $"Updated airport '{airport.Id}'",
                Data = airport
            });
        }
        catch (Exception ex)
        {
            _logger.LogError("UpdateAirport error '{Message}'\n{StackTrace}", ex.Message, ex.StackTrace);
            return _sentryHub.CaptureException(ex).ReturnActionResult();
        }
    }

    [HttpDelete("{airportId:int}")]
    [Authorize(Roles = Constants.CanAirports)]
    [ProducesResponseType(typeof(Response<string?>), 200)]
    [ProducesResponseType(401)]
    [ProducesResponseType(403)]
    [ProducesResponseType(typeof(Response<int>), 404)]
    [ProducesResponseType(typeof(Response<string?>), 500)]
    public async Task<ActionResult<Response<string>>> DeleteAirport(int airportId)
    {
        try
        {
            if (!await _redisService.ValidateRoles(Request.HttpContext.User, Constants.CanAirportsList))
            {
                return StatusCode(401);
            }

            Airport? airport = await _context.Airports.FindAsync(airportId);
            if (airport == null)
            {
                return NotFound(new Response<int>
                {
                    StatusCode = 404,
                    Message = $"Airport '{airportId}' not found",
                    Data = airportId
                });
            }

            string oldData = JsonConvert.SerializeObject(airport);
            _context.Airports.Remove(airport);
            await _context.SaveChangesAsync();

            await _loggingService.AddWebsiteLog(Request, $"Deleted airport '{airportId}'", oldData, string.Empty);

            await _redisService.RemoveCached("airports");
            return Ok(new Response<string?>
            {
                StatusCode = 200,
                Message = $"Deleted airport '{airportId}'"
            });
        }
        catch (Exception ex)
        {
            _logger.LogError("DeleteAirport error '{Message}'\n{StackTrace}", ex.Message, ex.StackTrace);
            return _sentryHub.CaptureException(ex).ReturnActionResult();
        }
    }
}