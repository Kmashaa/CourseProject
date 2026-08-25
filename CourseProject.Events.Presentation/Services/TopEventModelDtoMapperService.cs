using CourseProject.Events.Application.Models;
using CourseProject.Events.Domain.Entities;
using CourseProject.Events.Presentation.Interfaces;
using CourseProject.Events.Presentation.Models;

namespace CourseProject.Events.Presentation.Services
{
    public class TopEventModelDtoMapperService : ITopEventModelDtoMapperService
    {
        public TopEventDto ModelToDto(TopEventModel topEventModel)
        {
            TopEventDto topEvent = new(topEventModel.Id, topEventModel.Title, topEventModel.Description, DateTime.SpecifyKind(topEventModel.StartAt, DateTimeKind.Utc), DateTime.SpecifyKind(topEventModel.EndAt, DateTimeKind.Utc), topEventModel.TotalSeats, topEventModel.AvailableSeats, topEventModel.SalesPercentage);

            return topEvent;
        }

        public TopEventModel DtoToModel(TopEventDto topEventDto)
        {
            TopEventModel topEventModel = new(topEventDto.Id, topEventDto.Title, topEventDto.Description, topEventDto.StartAt, topEventDto.EndAt, topEventDto.TotalSeats, topEventDto.AvailableSeats, topEventDto.SalesPercentage) { };
            return topEventModel;
        }
    }
}
