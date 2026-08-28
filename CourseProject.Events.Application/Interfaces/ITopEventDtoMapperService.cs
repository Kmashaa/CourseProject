using CourseProject.Events.Application.Models;
using CourseProject.Events.Domain.Entities;
using System;
using System.Collections.Generic;
using System.Text;

namespace CourseProject.Events.Application.Interfaces
{
    public interface ITopEventDtoMapperService
    {
        TopEvent DtoToEntity(TopEventDto topEventDto);

        TopEventDto EntityToDto(TopEvent topEvent);

    }
}
