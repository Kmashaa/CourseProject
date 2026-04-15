using CourseProject.Entities;
using System.Xml.Serialization;

namespace CourseProject.Interfaces
{
    public interface IEventRepository
    {
        List<Event> GetAll();

        Event? GetById(int id);

        void Create(Event @event);

        void Update(Event @event);

        void Delete(int id);
    }
}
