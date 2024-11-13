﻿using AutoMapper;
using Microsoft.AspNetCore.Mvc;
using Ticketron.Dto.TicketDto;
using Ticketron.Interfaces;
using Ticketron.Models;

namespace Ticketron.Controllers

{
    [Route("api/[controller]")]
    [ApiController]
    public class TicketController : Controller
    {
        private readonly ITicketRepository _ticketRepository;
        private readonly IMapper _mapper;
        private readonly IBookingRepository _bookingRepository;
        private readonly IUserContextService _userContextService;
        private readonly IParticipantRepository _participantRepository;
        private readonly IUserRepository _userRepository;


        public TicketController(ITicketRepository ticketRepository, IMapper imapper, IBookingRepository bookingRepository, IUserContextService userContextService, IParticipantRepository participantRepository, IUserRepository userRepository)
        {
            _ticketRepository = ticketRepository;
            _mapper = imapper;
            _bookingRepository = bookingRepository;
            _userContextService = userContextService;
            _participantRepository = participantRepository;
            _userRepository = userRepository;
        }

        [HttpGet("{ticketId}")]
        [ProducesResponseType(200, Type = typeof(TicketDto))]
        [ProducesResponseType(400)]
        [ProducesResponseType(404)]
        public async Task<IActionResult> GetTicket(Guid ticketId)
        {
            if (!await _ticketRepository.TicketExistsAsync(ticketId))
                return NotFound();

            var ticket = _mapper.Map<TicketDto>(_ticketRepository.GetTicketAsync(ticketId));

            return Ok(ticket);
        }

        [HttpGet("booking/{bookingId}")]
        [ProducesResponseType(200, Type = typeof(IEnumerable<TicketDto>))]
        [ProducesResponseType(400)]
        public async Task<IActionResult> GetTickets(Guid bookingId)
        {
            var tickets = _mapper.Map<List<TicketDto>>(await _ticketRepository.GetTicketsAsync(bookingId));

            return Ok(tickets);
        }

        [HttpPost("create")]
        [ProducesResponseType(201, Type = typeof(TicketDto))]
        [ProducesResponseType(400)]
        [ProducesResponseType(401)]
        [ProducesResponseType(404)]
        [ProducesResponseType(500)]
        public async Task<IActionResult> CreateTicket([FromBody] TicketCreateDto newTicket)
        {
            if (!ModelState.IsValid)
                return BadRequest();

            if (newTicket == null)
                return BadRequest();

            var booking = await _bookingRepository.GetBookingAsync(newTicket.BookingId);
            if (booking == null)
                return NotFound("Booking not found");

            if (newTicket.ParticipantId == null)
            {
                Guid currentUserId;
                try
                {
                    currentUserId = _userContextService.GetUserObjectId();
                }
                catch (UnauthorizedAccessException ex)
                {
                    return Unauthorized(ex.Message);
                }

                newTicket.ParticipantId = currentUserId;
            }

            // Create participant of user if not exists

            var participant = await _participantRepository.GetParticipantAsync(newTicket.ParticipantId.Value);

            if (participant == null)
            {
                participant = new Participant
                {
                    Id = newTicket.ParticipantId.Value,
                    CreatedBy = newTicket.ParticipantId.Value,
                    Booking = booking,
                    User = await _userRepository.GetUserByIdAsync(newTicket.ParticipantId.Value),
                    IsUser = true
                };
                if (!await _participantRepository.CreateParticipantAsync(participant))
                    return Problem();
            }

            string? imageUrl = null;
            //if (newTicket.Image != null)
            //{
            //    blobName = await _blobService.UploadImageAsync(newTicket.Image, newTicket.BookingId);
            //}

            var ticketMap = _mapper.Map<Ticket>(newTicket);
            ticketMap.Booking = booking;
            ticketMap.Participant = participant;
            ticketMap.ImageUrl = imageUrl;

            if (!await _ticketRepository.CreateTicketAsync(ticketMap))
                return StatusCode(500);

            var createdTicketDto = _mapper.Map<TicketDto>(ticketMap);

            return Ok(createdTicketDto);
        }

        [HttpPut("update")]
        [ProducesResponseType(204)]
        [ProducesResponseType(400)]
        [ProducesResponseType(404)]
        [ProducesResponseType(500)]
        public async Task<IActionResult> UpdateTicket([FromBody] TicketUpdateDto updateTicket)
        {
            if (UpdateTicket == null)
                return BadRequest();

            if (!ModelState.IsValid)
                return BadRequest();

            var existingTicket = await _ticketRepository.GetTicketAsync(updateTicket.Id);

            if (existingTicket == null)
                return NotFound("Booking not found");

            var ticketMap = _mapper.Map(updateTicket, existingTicket);

            if (!await _ticketRepository.UpdateTicketAsync(ticketMap))
                return StatusCode(500);

            return Ok(ticketMap);
        }

        [HttpDelete("{ticketId}")]
        [ProducesResponseType(204)]
        [ProducesResponseType(404)]
        [ProducesResponseType(500)]
        public async Task<IActionResult> DeleteTicket(Guid ticketId)
        {
            var ticket = await _ticketRepository.GetTicketAsync(ticketId);

            if (ticket == null)
                return NotFound();

            if (!await _ticketRepository.DeleteTicketAsync(ticket))
                return StatusCode(500);

            return NoContent();
        }
    }
}
