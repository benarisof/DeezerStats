namespace DeezerStats.Domain.SeedWork
{
    public abstract class Entity<TId> : IEquatable<Entity<TId>>
    {
        protected Entity()
        {
        }

        protected Entity(TId id)
        {
            if (EqualityComparer<TId>.Default.Equals(id, default))
            {
                throw new ArgumentException("L'ID de l'entité ne peut pas être vide.", nameof(id));
            }

            Id = id;
        }

        public TId Id { get; protected set; } = default!;

        public static bool operator ==(Entity<TId>? left, Entity<TId>? right)
        {
            if (left is null && right is null)
            {
                return true;
            }

            if (left is null || right is null)
            {
                return false;
            }

            return left.Equals(right);
        }

        public static bool operator !=(Entity<TId>? left, Entity<TId>? right)
        {
            return !(left == right);
        }

        public override bool Equals(object? obj)
        {
            if (obj is null)
            {
                return false;
            }

            if (ReferenceEquals(this, obj))
            {
                return true;
            }

            if (obj.GetType() != GetType())
            {
                return false;
            }

            return Equals((Entity<TId>)obj);
        }

        public bool Equals(Entity<TId>? other)
        {
            if (other is null)
            {
                return false;
            }

            if (ReferenceEquals(this, other))
            {
                return true;
            }

            return EqualityComparer<TId>.Default.Equals(Id, other.Id);
        }

        public override int GetHashCode() => EqualityComparer<TId>.Default.GetHashCode(Id!);
    }
}
