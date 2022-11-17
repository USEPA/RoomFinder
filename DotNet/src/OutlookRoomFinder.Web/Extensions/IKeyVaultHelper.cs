using System.Threading.Tasks;

namespace OutlookRoomFinder.Web.Extensions
{
    public interface IKeyVaultHelper
    {
        /// <summary>
        /// Gets value from key vault by secret name
        /// </summary>
        /// <param name="secretName">name of secret to get</param>        
        Task<string> GetSecretAsync(string secretName);

        /// <summary>
        /// Sets value by secret name in key vault
        /// </summary>
        /// <param name="secretName">name of secret</param>
        /// <param name="secretValue">value of secret</param>        
        Task SetSecretAsync(string secretName, string secretValue);
    }
}
