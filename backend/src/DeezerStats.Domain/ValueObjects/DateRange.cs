using DeezerStats.Domain.SeedWork;

namespace DeezerStats.Domain.ValueObjects
{
    public record DateRange
    {
        public DateRange(DateOnly? from, DateOnly? to)
        {
            if (from.HasValue && to.HasValue && from > to)
            {
                throw new DomainException("La date de début ne peut pas être postérieure à la date de fin.");
            }

            From = from;
            To = to;
        }

        public DateOnly? From { get; init; }

        public DateOnly? To { get; init; }

        public bool Contains(DateTime dateTime)
        {
            var date = DateOnly.FromDateTime(dateTime);
            if (From.HasValue && date < From.Value)
            {
                return false;
            }

            if (To.HasValue && date > To.Value)
            {
                return false;
            }

            return true;
        }
    }
}
