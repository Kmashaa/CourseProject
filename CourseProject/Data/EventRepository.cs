using CourseProject.Entities;
using CourseProject.Interfaces;
using System.Diagnostics.Metrics;
namespace CourseProject.Data
{
    public class EventRepository : IEventRepository
    {
        private readonly List<Event> _events = new();
        private static int counter = 0;
        public EventRepository()
        {
            //Имитация данных из БД
            _events.AddRange(
                new Event
                {
                    Id = counter++,
                    Title = "Концерт",
                    Description = "Концерт популярного певца",
                    StartAt = new DateTime(2026, 9, 1, 18, 0, 0),
                    EndAt = new DateTime(2026, 9, 1, 21, 0, 0)

                },
                new Event
                {
                    Id = counter++,
                    Title = "Шоу",
                    Description = "Гастроли популярного шоу",
                    StartAt = new DateTime(2026, 9, 14, 19, 0, 0),
                    EndAt = new DateTime(2026, 9, 14, 21, 30, 0)
                },
                new Event
                {
                    Id = counter++,
                    Title = "Stand Up",
                    Description = "Stand Up ТНТ",
                    StartAt = new DateTime(2026, 9, 2, 18, 0, 0),
                    EndAt = new DateTime(2026, 9, 2, 21, 0, 0)

                },
                new Event
                {
                    Id = counter++,
                    Title = "Мероприятие",
                    Description = "Какое-то мероприятие",
                    StartAt = new DateTime(2026, 9, 25, 19, 0, 0),
                    EndAt = new DateTime(2026, 9, 25, 21, 30, 0)
                }
            );
        }

        public List<Event> GetAll()
        {
            return _events;
        }

        public Event? GetById(int id)
        {
            return _events.FirstOrDefault(o => o.Id == id);
        }

        public void Create(Event @event)
        {
            @event.Id=counter++;
            _events.Add(@event);
        }

        public void Update(Event @event)
        {
            var index = _events.FindIndex(o => o.Id == @event.Id);
            if (index != -1)
            {
                _events[index] = @event;
            }

        }

        public void Delete(int id)
        {
            var eventFromList = _events.FirstOrDefault(o => o.Id == id);

            if (eventFromList != null)
            {
                _events.Remove(eventFromList);
            }
        }

    }
}
