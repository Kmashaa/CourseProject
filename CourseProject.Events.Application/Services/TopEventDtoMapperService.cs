using CourseProject.Events.Application.Interfaces;
using CourseProject.Events.Application.Models;
using CourseProject.Events.Domain.Entities;
using System;
using System.Collections.Generic;
using System.Text;

namespace CourseProject.Events.Application.Services
{
    internal class TopEventDtoMapperService : ITopEventDtoMapperService
    {
        public TopEvent DtoToEntity(TopEventDto topEventDto)
        {
            TopEvent topEvent = new(topEventDto.Id, topEventDto.Title, topEventDto.Description, DateTime.SpecifyKind(topEventDto.StartAt, DateTimeKind.Utc), DateTime.SpecifyKind(topEventDto.EndAt, DateTimeKind.Utc), topEventDto.TotalSeats, topEventDto.AvailableSeats, topEventDto.SalesPercentage);

            return topEvent;
        }

        public TopEventDto EntityToDto(TopEvent topEvent)
        {
            TopEventDto topEventDto = new(topEvent.Id, topEvent.Title, topEvent.Description, topEvent.StartAt, topEvent.EndAt, topEvent.TotalSeats, topEvent.AvailableSeats, topEvent.SalesPercentage) { };
            return topEventDto;
        }

    }
}
