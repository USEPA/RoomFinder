using System.Collections.Generic;
using System.Linq;

namespace EPA.Office365.Extensions
{
    /// <summary>
    /// Helper methods for the linq queries.
    /// </summary>
    public static class LinqExtensions
    {
        /// <summary>
        /// Return collection of objects
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="source"></param>
        /// <param name="chunkSize"></param>
        /// <returns></returns>
        public static IEnumerable<List<T>> ChunkBy<T>(this List<T> source, int chunkSize = 50)
        {
            return source
                .Select((x, i) => new { Index = i, Value = x })
                .GroupBy(x => x.Index / chunkSize)
                .Select(x => x.Select(v => v.Value).ToList());
        }

        /// <summary>
        /// Chunks a collection into an enumerable
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="source"></param>
        /// <param name="chunkSize"></param>
        /// <returns></returns>
        public static IEnumerable<List<T>> ChunkBy<T>(this ICollection<T> source, int chunkSize = 50)
        {
            return source
                .Select((x, i) => new { Index = i, Value = x })
                .GroupBy(x => x.Index / chunkSize)
                .Select(x => x.Select(v => v.Value).ToList());
        }
    }
}
