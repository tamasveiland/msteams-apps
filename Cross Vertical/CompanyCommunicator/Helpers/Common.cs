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
using CrossVertical.Announcement.Repository;
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace CrossVertical.Announcement.Helpers
{
    public class Common
    {
        public static string RemoveHashFromGuestUserUPN(string upn)
        {
            if (string.IsNullOrEmpty(upn) || !upn.Contains("#ext#"))
                return upn;

            // # can't be added in document DB as key.
            return upn.Replace("#", "_");
        }

        public static async Task CreateOrUpdateExistingGroups(List<Group> groupDetails, Tenant tenantData, bool deleteOldGroups = false)
        {
            List<Group> oldGroups = new List<Group>();
            List<string> nullGroups = new List<string>();
            foreach (var groupId in tenantData.Groups)
            {
                var group = await Cache.Groups.GetItemAsync(groupId);
                if (group != null)
                    oldGroups.Add(group);
                else
                    nullGroups.Add(groupId);
            }

            foreach (var groupDetail in groupDetails)
            {
                // Check if old group with same name exist, then replace user details.
                var oldGroup = oldGroups.FirstOrDefault(g => string.Equals(g.Name, groupDetail.Name, StringComparison.InvariantCultureIgnoreCase));
                if (oldGroup != null)
                {
                    groupDetail.Id = oldGroup.Id;
                    oldGroups.Remove(oldGroup);
                }

                await Cache.Groups.AddOrUpdateItemAsync(groupDetail.Id, groupDetail);
                if (!tenantData.Groups.Contains(groupDetail.Id))
                    tenantData.Groups.Add(groupDetail.Id);

            }

            // Clean the groups which are not added in new excel.
            if (deleteOldGroups)
                foreach (var oldGroup in oldGroups)
                {
                    await Cache.Groups.DeleteItemAsync(oldGroup.Id);
                    if (tenantData.Groups.Contains(oldGroup.Id))
                        tenantData.Groups.Remove(oldGroup.Id);
                }
            foreach (var nullGroup in nullGroups)
            {
                tenantData.Groups.Remove(nullGroup);
            }

            await Cache.Tenants.AddOrUpdateItemAsync(tenantData.Id, tenantData);
        }

        public static Role GetUserRole(string emailId, Tenant tenatInfo)
        {
            var role = Role.User;
            if (tenatInfo.Moderators.Contains(emailId))
                role = Role.Moderator;
            if (tenatInfo.Admin == emailId)
                role = Role.Admin;
            return role;
        }

        public static async Task<List<Campaign>> GetMyAnnouncements(string emailId, string tenantId)
        {
            var tenant = await Cache.Tenants.GetItemAsync(tenantId);
            Role role = GetUserRole(emailId, tenant);

            var myTenantAnnouncements = new List<Campaign>();
            foreach (var announcementId in tenant.Announcements)
            {
                var announcement = await Cache.Announcements.GetItemAsync(announcementId);
                if (announcement != null)
                {
                    if (role == Role.Moderator || role == Role.Admin)
                    {
                        myTenantAnnouncements.Add(announcement);
                    }
                    else if (announcement.Recipients.Channels.Any(c => c.Members.Contains(emailId))
                          || announcement.Recipients.Groups.Any(g => g.Users.Any(u => u.Id == emailId)))
                    {
                        // Validate if user is part of this announcement.
                        myTenantAnnouncements.Add(announcement);
                    }
                }
            }
            return myTenantAnnouncements;
        }

        internal static async Task<Tenant> CheckAndAddTenantDetails(string tenantId)
        {
            // Tenant not present in cached check DB
            var tenantData = await Cache.Tenants.GetItemAsync(tenantId);
            if (tenantData == null)
            {
                tenantData = new Tenant()
                {
                    Id = tenantId,
                };
                await Cache.Tenants.AddOrUpdateItemAsync(tenantData.Id, tenantData);
            }

            return tenantData;
        }

        public static Task ForEachAsync<T>(IEnumerable<T> source, int dop, Func<T, Task> body)
        {
            return Task.WhenAll(
                from partition in Partitioner.Create(source).GetPartitions(dop)
                select Task.Run(async delegate
                {
                    using (partition)
                        while (partition.MoveNext())
                            await body(partition.Current);
                }));
        }
    }
}