using TaskEngineAPI.DTO;

namespace TaskEngineAPI.Interfaces
{ 
        public interface IJwtService
        {
       
            string GenerateJwtToken(string userName, string? email, string? avatar, string? type, int TenantID, out DateTime expiry);
            string GenerateRefreshToken();
            Task<User> GetUserFromRefreshToken(string refreshToken);
            Task<bool> SaveRefreshTokenToDatabase(string userId, string refreshToken, DateTime expiration);
        }
    }

