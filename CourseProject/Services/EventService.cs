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

        public void CreateEvent(Event @event)
        {
            _repository.Create(@event);
        }

        public void UpdateEvent(Event @event)
        {
            _repository.Update(@event);
        }

        public void DeleteEvent(int id)
        {
            _repository.Delete(id);
        }

    }
}
