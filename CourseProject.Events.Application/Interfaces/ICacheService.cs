using CourseProject.Events.Domain.Entities;
using System;
using System.Collections.Generic;
using System.Text;
using System.Text.Json;

namespace CourseProject.Events.Application.Interfaces
{
    public interface ICacheService
    {
        Task<Event?> GetById(Guid id);

        Task<Event?> SetById(Guid id, Event @event);

        Task DeleteById(Guid id);
    }
}
