using CourseProject.Entities;
using System.Xml.Serialization;

namespace CourseProject.Interfaces
{
    public interface IEventRepository
    {
        List<Event> GetAll();

        Event? GetById(int id);

        void Create(Event newEvent);

        void Update(Event updatedEvent);

        void Delete(int id);
    }
}
