// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license.
//
// MIT License
// Permission is hereby granted, free of charge, to any person obtaining
// a copy of this software and associated documentation files (the
// "Software"), to deal in the Software without restriction, including
// without limitation the rights to use, copy, modify, merge, publish,
// distribute, sublicense, and/or sell copies of the Software, and to
// permit persons to whom the Software is furnished to do so, subject to
// the following conditions:
//
// The above copyright notice and this permission notice shall be
// included in all copies or substantial portions of the Software.
//
// THE SOFTWARE IS PROVIDED ""AS IS"", WITHOUT WARRANTY OF ANY KIND,
// EXPRESS OR IMPLIED, INCLUDING BUT NOT LIMITED TO THE WARRANTIES OF
// MERCHANTABILITY, FITNESS FOR A PARTICULAR PURPOSE AND
// NONINFRINGEMENT. IN NO EVENT SHALL THE AUTHORS OR COPYRIGHT HOLDERS BE
// LIABLE FOR ANY CLAIM, DAMAGES OR OTHER LIABILITY, WHETHER IN AN ACTION
// OF CONTRACT, TORT OR OTHERWISE, ARISING FROM, OUT OF OR IN CONNECTION
// WITH THE SOFTWARE OR THE USE OR OTHER DEALINGS IN THE SOFTWARE. 
using CrossVertical.Announcement.Models;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;
using System.Threading.Tasks;

namespace CrossVertical.Announcement.Repository
{
    public static class Cache
    {
        public static DBCache<Tenant> Tenants { get; set; } = new DBCache<Tenant>();
        public static DBCache<Group> Groups { get; set; } = new DBCache<Group>();
        public static DBCache<Team> Teams { get; set; } = new DBCache<Team>();
        public static DBCache<User> Users { get; set; } = new DBCache<User>();
        public static DBCache<Campaign> Announcements { get; set; } = new DBCache<Campaign>();

        public static void Clear()
        {
            Tenants = new DBCache<Tenant>();
            Groups = new DBCache<Group>();
            Teams = new DBCache<Team>();
            Users = new DBCache<User>();
            Announcements = new DBCache<Campaign>();
        }
    }

    public class DBCache<T> where T : DatabaseItem
    {
        private Dictionary<string, T> CachedItems { get; set; } = new Dictionary<string, T>();

        public async Task<List<T>> GetAllItemsAsync()
        {
            if (CachedItems.Count == 0) // Try to fetch from DB.
            {
                var items = await DocumentDBRepository.GetItemsAsync<T>(u => u.Type == typeof(T).Name);
                foreach (T item in items)
                {
                    CachedItems.Add(item.Id, item);
                }
            }
            return CachedItems.Values.ToList();
        }

        public async Task<List<T>> GetAllItemsAsync(Expression<Func<T, bool>> predicate)
        {
            var items = await DocumentDBRepository.GetItemsAsync<T>(predicate);
            foreach (T item in items)
            {
                if (!CachedItems.ContainsKey(item.Id))
                    CachedItems.Add(item.Id, item);
            }
            return items.ToList();
        }

        public async Task<T> GetItemAsync(string id)
        {
            if (string.IsNullOrEmpty(id))
                return null;

            if (CachedItems.ContainsKey(id))
                return CachedItems[id];
            else
            {
                var tenant = await DocumentDBRepository.GetItemAsync<T>(id);
                if (tenant != null)
                {
                    try
                    {
                        CachedItems.Add(id, tenant);
                    }
                    catch (System.Exception ex)
                    {
                        Helpers.ErrorLogService.LogError(ex);
                    }
                    return tenant;
                }
                return null; // Not found in DB.
            }
        }

        public async Task AddOrUpdateItemAsync(string id, T item)
        {
            var existingItem = await GetItemAsync(id);
            if (existingItem != null)
            {
                // Update Existing
                await DocumentDBRepository.UpdateItemAsync(id, item);
                CachedItems[id] = item;
            }
            else
            {
                try
                {
                    await DocumentDBRepository.CreateItemAsync(item);
                    CachedItems.Add(id, item);
                }
                catch (System.Exception ex)
                {
                    Helpers.ErrorLogService.LogError(ex);
                }
            }
        }

        public async Task DeleteItemAsync(string id)
        {
            if (CachedItems.ContainsKey(id))
            {
                // Update Existing
                await DocumentDBRepository.DeleteItemAsync(id);
                CachedItems.Remove(id);
            }
            else
            {
                await DocumentDBRepository.DeleteItemAsync(id);
            }
        }
    }
}