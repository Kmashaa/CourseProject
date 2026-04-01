using CourseProject.Entities;
using CourseProject.Interfaces;

namespace CourseProject.Services
{
    public class EventService: IEventService
    {
        private readonly IEventRepository _repository;

        public EventService(IEventRepository repository)
        {
            _repository = repository;
        }

        public List<Event> GetAllEvents()
        {
            return _repository.GetAll();
        }

        public Event? GetEventById(int id)
        {
            return _repository.GetById(id);
        }

        public void CreateEvent(Event newEvent)
        {
            _repository.Create(newEvent);
        }

        public void UpdateEvent(Event updatedEvent)
        {
            _repository.Update(updatedEvent);
        }

        public void DeleteEvent(int id)
        {
            _repository.Delete(id);
        }

    }
}
