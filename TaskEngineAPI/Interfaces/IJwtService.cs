using TaskEngineAPI.DTO;

namespace TaskEngineAPI.Interfaces
{ 
        public interface IJwtService
        {
       
            string GenerateJwtToken(string userName, out DateTime expiry);

            string GenerateRefreshToken();

            Task<User> GetUserFromRefreshToken(string refreshToken);

            Task<bool> SaveRefreshTokenToDatabase(string userId, string refreshToken, DateTime expiration);
        }
    }

