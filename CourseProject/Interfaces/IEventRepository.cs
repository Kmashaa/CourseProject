using CourseProject.Entities;
using System.Xml.Serialization;

namespace CourseProject.Interfaces
{
    public interface IEventRepository
    {
        List<Event> GetAll();

        Event? GetById(Guid id);

        Event Create(Event @event);

        Event Update(Event @event);

        Guid Delete(Guid id);
    }
}
