using CourseProject.Events.Presentation.Interfaces;
using CourseProject.Events.Presentation.Models;
using CourseProject.Events.Application.Models;

namespace CourseProject.Events.Presentation.Services
{
    public class EventModelDtoMapperService : IEventModelDtoMapperService
    {
        public EventDto ModelToDto(EventModel eventModel)
        {
            EventDto @event = new(eventModel.Id, eventModel.Title, DateTime.SpecifyKind(eventModel.StartAt, DateTimeKind.Utc), DateTime.SpecifyKind(eventModel.EndAt, DateTimeKind.Utc), (int)eventModel.TotalSeats, eventModel.Description);

            return @event;
        }

        public EventModel DtoToModel(EventDto deventDto)
        {
            EventModel eventDto = new()
            {
                Id = deventDto.Id,
                Title = deventDto.Title,
                Description = deventDto.Description,
                StartAt = deventDto.StartAt,
                EndAt = deventDto.EndAt,
                TotalSeats = deventDto.TotalSeats,
                AvailableSeats = deventDto.AvailableSeats
            };
            return eventDto;
        }

    }
}
