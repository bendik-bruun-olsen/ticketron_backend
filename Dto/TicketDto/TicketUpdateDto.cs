﻿namespace Ticketron.Dto.TicketDto
{
    public class TicketUpdateDto
    {
        public required Guid Id { get; set; }
        public string? Title { get; set; }
        public DateTimeOffset? StartDate { get; set; }
        public DateTimeOffset? EndDate { get; set; }
        public int? Price { get; set; }
        public DateTimeOffset? PurchaseDate { get; set; }
        public string? Category { get; set; }
        public string? ImageUrl { get; set; }
        public Guid? ParticipantId { get; set; }

    }

}
