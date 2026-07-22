using DeezerStats.Domain.SeedWork;

namespace DeezerStats.Domain.Aggregates.UserAggregate
{
    /// <summary>
    /// Jeton de rafraîchissement permettant d'obtenir un nouvel access token sans ré-authentification
    /// complète. Stocké sous forme hachée (voir IRefreshTokenGenerator) : jamais la valeur brute.
    /// Aggregate root distinct de <see cref="User"/> plutôt qu'entité enfant, pour permettre une
    /// recherche directe par hash (POST /auth/refresh) sans devoir charger l'agrégat utilisateur.
    /// </summary>
    public class RefreshToken : Entity<Guid>, IAggregateRoot
    {
        public RefreshToken(Guid id, Guid userId, string tokenHash, DateTime expiresAt)
            : base(id)
        {
            if (userId == Guid.Empty)
            {
                throw new DomainException("Un refresh token doit être rattaché à un utilisateur.");
            }

            if (string.IsNullOrWhiteSpace(tokenHash))
            {
                throw new DomainException("Le hash du refresh token ne peut pas être vide.");
            }

            UserId = userId;
            TokenHash = tokenHash;
            ExpiresAt = expiresAt;
            CreatedAt = DateTime.UtcNow;
        }

        private RefreshToken()
        {
        }

        public Guid UserId { get; private set; }

        public string TokenHash { get; private set; } = default!;

        public DateTime ExpiresAt { get; private set; }

        public DateTime CreatedAt { get; private set; }

        public DateTime? RevokedAt { get; private set; }

        /// <summary>
        /// Obtient identifiant du token émis en remplacement de celui-ci lors d'une rotation (voir Revoke),
        /// pour tracer la chaîne de rotation en cas d'investigation (réutilisation d'un token révoqué).
        /// </summary>
        public Guid? ReplacedByTokenId { get; private set; }

        public bool IsExpired => DateTime.UtcNow >= ExpiresAt;

        public bool IsRevoked => RevokedAt.HasValue;

        public bool IsActive => !IsExpired && !IsRevoked;

        /// <summary>
        /// Révoque le token (logout explicite, rotation lors d'un refresh, ou réponse à une
        /// réutilisation détectée d'un token déjà révoqué). Idempotent : révoquer un token déjà
        /// révoqué ne fait rien, pour ne jamais écraser sa date de révocation d'origine.
        /// </summary>
        /// <param name="replacedByTokenId">
        /// Identifiant du nouveau token qui remplace celui-ci lors d'une rotation.
        /// Optionnel ; utilisé pour tracer la chaîne de remplacement.
        /// </param>
        public void Revoke(Guid? replacedByTokenId = null)
        {
            if (IsRevoked)
            {
                return;
            }

            RevokedAt = DateTime.UtcNow;
            ReplacedByTokenId = replacedByTokenId;
        }
    }
}
