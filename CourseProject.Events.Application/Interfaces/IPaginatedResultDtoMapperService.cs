using CourseProject.Events.Domain.Entities;
using CourseProject.Events.Application.Models;

namespace CourseProject.Events.Application.Interfaces
{
    public interface IPaginatedResultDtoMapperService
    {
        PaginatedResult DtoToEntity(PaginatedResultDto paginatedResultDto);

        PaginatedResultDto EntityToDto(PaginatedResult paginatedResult);

    }
}
