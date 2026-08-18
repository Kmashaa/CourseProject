using CourseProject.Application.Models;
using CourseProject.Domain.Entities;

namespace CourseProject.Application.Interfaces
{
    public interface IPaginatedResultDtoMapperService
    {
        PaginatedResult DtoToEntity(PaginatedResultDto paginatedResultDto);

        PaginatedResultDto EntityToDto(PaginatedResult paginatedResult);

    }
}
