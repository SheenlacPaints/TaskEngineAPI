using Microsoft.Extensions.Configuration;
using Microsoft.IdentityModel.Tokens;
using System.Data.SqlClient;
using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Security.Cryptography;
using System.Text;
using TaskEngineAPI.DTO;
using TaskEngineAPI.Interfaces;

namespace TaskEngineAPI.Services
{

    public class JwtService : IJwtService
    {
        private readonly IConfiguration _config;
        public JwtService(IConfiguration config) => _config = config;

       
     
        public string GenerateJwtToken(string username, out DateTime expires)
        {
            var audience = _config.GetSection("Jwt:Audience").Value;
            var issuer = _config.GetSection("Jwt:Issuer").Value;
            var key = Encoding.ASCII.GetBytes(_config.GetSection("Jwt:Key").Value);
            expires = DateTime.Now.AddHours(1);
            var tokenDescriptor = new SecurityTokenDescriptor
            {
                Subject = new ClaimsIdentity(new[]
                {
                    new Claim("Id", Guid.NewGuid().ToString()),
                    new Claim(JwtRegisteredClaimNames.Sub, username),
                    new Claim(JwtRegisteredClaimNames.Jti, Guid.NewGuid().ToString())
                }),
                Expires = expires,
                Issuer = issuer,
                Audience = audience,
                SigningCredentials = new SigningCredentials(new SymmetricSecurityKey(key), SecurityAlgorithms.HmacSha512Signature)
            };
            var tokenHandler = new JwtSecurityTokenHandler();
            var token = tokenHandler.CreateToken(tokenDescriptor);
            return tokenHandler.WriteToken(token);
        }


        public string GenerateRefreshToken()
        {
            return Convert.ToBase64String(Guid.NewGuid().ToByteArray());
        }


        public async Task<User> GetUserFromRefreshToken(string refreshToken)
        {
            using (SqlConnection conn = new SqlConnection(_config.GetConnectionString("Database")))
            {
                await conn.OpenAsync();
                string query = @"SELECT u.* FROM AdminUsers u 
                               INNER JOIN RefreshToken rt ON u.UserName = rt.UserId 
                               WHERE rt.Token = @Token AND rt.Expiration > @CurrentTime";

                using (SqlCommand cmd = new SqlCommand(query, conn))
                {
                    cmd.Parameters.AddWithValue("@Token", refreshToken);
                    cmd.Parameters.AddWithValue("@CurrentTime", DateTime.Now);

                    using (SqlDataReader reader = await cmd.ExecuteReaderAsync())
                    {
                        if (await reader.ReadAsync())
                        {
                            return new User
                            {
                                userName = reader.GetString(reader.GetOrdinal("UserName")),

                            };
                        }
                    }
                }
            }
            return null;
        }


        public async Task<bool> SaveRefreshTokenToDatabase(string userId, string refreshToken, DateTime expiration)
        {
            try
            {
                await using (SqlConnection conn = new SqlConnection(_config.GetConnectionString("Database")))
                {
                    await conn.OpenAsync();

                    string checkQuery = "SELECT COUNT(*) FROM RefreshToken WHERE UserId = @UserId";
                    using (SqlCommand checkCmd = new SqlCommand(checkQuery, conn))
                    {
                        checkCmd.Parameters.AddWithValue("@UserId", userId);
                        int count = (int) await checkCmd.ExecuteScalarAsync();

                        if (count > 0)
                        {
                            string updateQuery = @"UPDATE RefreshToken SET Token = @Token,Expiration = @Expiration,ModifiedAt = @ModifiedAt WHERE UserId = @UserId";

                            using (SqlCommand updateCmd = new SqlCommand(updateQuery, conn))
                            {
                                updateCmd.Parameters.AddWithValue("@Token", refreshToken);
                                updateCmd.Parameters.AddWithValue("@Expiration", expiration);
                                updateCmd.Parameters.AddWithValue("@ModifiedAt", DateTime.Now);
                                updateCmd.Parameters.AddWithValue("@UserId", userId);
                                await updateCmd.ExecuteNonQueryAsync();
                            }
                        }
                        else
                        {

                            string insertQuery = @"INSERT INTO RefreshToken 
                                                (Token, UserId, Expiration, CreatedAt) 
                                                VALUES (@Token, @UserId, @Expiration, @CreatedAt)";

                            using (SqlCommand insertCmd = new SqlCommand(insertQuery, conn))
                            {
                                insertCmd.Parameters.AddWithValue("@Token", refreshToken);
                                insertCmd.Parameters.AddWithValue("@UserId", userId);
                                insertCmd.Parameters.AddWithValue("@Expiration", expiration);
                                insertCmd.Parameters.AddWithValue("@CreatedAt", DateTime.Now);
                                await insertCmd.ExecuteNonQueryAsync();
                            }
                        }
                    }
                }
                return true;
            }
            catch (Exception ex)
            {

                return false;
            }
        }
    }
}
