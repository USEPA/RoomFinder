using OutlookRoomFinder.Core.Models;
using System.Collections;
using System.Collections.Generic;
using System.Linq;

namespace OutlookRoomFinder.Core
{
    /// <summary>
    /// Dynamic assorted list controls the key/value/pairs
    /// </summary>
    public class NodeSortedList : SortedList<string, dynamic>
    {
        public void Add(IEnumerable<string> keys, ResourceItem item)
        {
            string key = keys.First();
            if (keys.Count() > 1)
            {
                if (!this.ContainsKey(key))
                {
                    this.Add(key, new NodeSortedList());
                }
                this[key].Add(keys.Skip(1), item);
            }
            else if (!string.IsNullOrEmpty(key))
            {
                if (!this.ContainsKey(key))
                {
                    this.Add(key, new ArrayList());
                }

                this[key].Add(item);
            }
            else
            {
                // nothing can happen here
            }
        }
    }
}