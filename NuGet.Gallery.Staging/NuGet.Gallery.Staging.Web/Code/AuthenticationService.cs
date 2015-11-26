using System.Data.SqlClient;
using System.Threading.Tasks;

namespace NuGet.Gallery.Staging.Web.Code
{
    public class AuthenticationService
    {
        private readonly string _connectionString;

        public AuthenticationService(string connectionString)
        {
            _connectionString = connectionString;
        }

        public async Task<User> AuthenticateAsync(string username, string password)
        {
            using (var connection = new SqlConnection(_connectionString))
            {
                await connection.OpenAsync();

                var command = new SqlCommand(
                    " SELECT TOP 1 U.[Username], U.[EmailAddress], U.[ApiKey], C.[Type] AS [CredentialType], C.[Value] AS [CredentialValue]" +
                    " FROM [dbo].[Users] AS U" +
                    " INNER JOIN [dbo].[Credentials] AS C ON C.[UserKey] = U.[Key]" +
                    " WHERE (C.[Type] IN ('password.pbkdf2', 'password.sha512', 'password.sha1'))" +
                    "   AND (Username = @UsernameOrEmail OR EmailAddress = @UsernameOrEmail)",
                        connection);

                command.Parameters.AddWithValue("@UsernameOrEmail", username);

                var reader = await command.ExecuteReaderAsync();
                if (await reader.ReadAsync())
                {
                    // Validate credential
                    var validCredential = false;
                    var credentialType = reader["CredentialType"].ToString();
                    var credentialValue = reader["CredentialValue"].ToString();
                    if (credentialType == "password.pbkdf2")
                    {
                        validCredential = CryptographyService.ValidateSaltedHash(credentialValue, password, Constants.PBKDF2HashAlgorithmId);
                    }
                    else if (credentialType == "password.sha512")
                    {
                        validCredential = CryptographyService.ValidateSaltedHash(credentialValue, password, Constants.Sha512HashAlgorithmId);
                    }
                    else if (credentialType == "password.sha1")
                    {
                        validCredential = CryptographyService.ValidateSaltedHash(credentialValue, password, Constants.Sha1HashAlgorithmId);
                    }

                    // Valid? If so, return a user instance
                    if (validCredential)
                    {
                        return new User
                        {
                            Username = reader["Username"].ToString(),
                            EmailAddress = reader["EmailAddress"].ToString(),
                            ApiKey = reader["ApiKey"].ToString()
                        };
                    }
                }
            }

            return null;
        }
    }
}