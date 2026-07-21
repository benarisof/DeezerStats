namespace DeezerStats.Application.Ports.Security
{
    public interface IPasswordHasher
    {
        public string Hash(string plainTextPassword);

        public bool Verify(string plainTextPassword, string hashedPassword);
    }
}
