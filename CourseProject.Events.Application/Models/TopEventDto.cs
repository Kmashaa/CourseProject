using System;
using System.Collections.Generic;
using System.Text;

namespace CourseProject.Events.Application.Models
{
    public class TopEventDto
    {
        public TopEventDto() { }
        public Guid Id { get; init; }

        public string Title { get; set; }

        public string? Description { get; set; }

        public DateTime StartAt { get; set; }

        public DateTime EndAt { get; set; }

        public int TotalSeats { get; set; }

        public int AvailableSeats { get; set; }

        public decimal SalesPercentage { get; set; }

        public TopEventDto(Guid id, string title, string? description, DateTime startAt, DateTime endAt, int totalSeats, int availableSeats, decimal salesPercentage)
        {
            Id = id;
            Title = title;
            Description = description;
            StartAt = startAt;
            EndAt = endAt;
            TotalSeats = totalSeats;
            AvailableSeats = availableSeats;
            SalesPercentage = salesPercentage;
        }
    }
}
