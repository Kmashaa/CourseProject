using CourseProject.Entities;
using CourseProject.Interfaces;
namespace CourseProject.Data
{
    public class EventRepository : IEventRepository
    {
        private readonly List<Event> _events = new();

        public EventRepository()
        {
            //Имитация данных из БД
            _events.AddRange(new Event
                {
                    Id = 0,
                    Title = "Концерт",
                    Description = "Концерт популярного певца",
                    StartAt = new DateTime(2026, 9, 1, 18, 0, 0),
                    EndAt = new DateTime(2026, 9, 1, 21, 0, 0)

                },
                new Event
                {
                    Id = 1,
                    Title = "Шоу",
                    Description = "Гастроли популярного шоу",
                    StartAt = new DateTime(2026, 9, 14, 19, 0, 0),
                    EndAt = new DateTime(2026, 9, 14, 21, 30, 0)
                }
            );
        }

        public List<Event> GetAll()
        {
            return _events;
        }

        public Event? GetById(int id)
        {
            return _events.FirstOrDefault(o=>o.Id==id);
        }

        public void Create(Event newEvent)
        {
            _events.Add(newEvent);
        }

        public void Update(Event updatedEvent)
        {
            var index = _events.FindIndex(o => o.Id == updatedEvent.Id);
            if (index != -1)
            {
                _events[index] = updatedEvent;
            }

        }

        public void Delete(int id)
        {
            var eventFromList= _events.FirstOrDefault(o=>o.Id == id);

            if (eventFromList != null)
            {
                _events.Remove(eventFromList);
            }
        }

    }
}
