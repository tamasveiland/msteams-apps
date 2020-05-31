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
using ExcelDataReader;
using System;
using System.Collections.Generic;
using System.Data;
using System.IO;
using System.Linq;

namespace CrossVertical.Announcement.Helpers
{
    /// <summary>
    /// Helper to Read excel file.
    /// </summary>
    public static class ExcelHelper
    {
        public static List<Group> GetAddTeamDetails(string strFilePath)
        {
            var groups = new List<Group>();
            try
            {
                using (var stream = File.Open(strFilePath, FileMode.Open, FileAccess.Read))
                {
                    using (var reader = ExcelReaderFactory.CreateReader(stream))
                    {
                        // 2. Use the AsDataSet extension method
                        var result = reader.AsDataSet();
                        var table = result.Tables[0]; //get first table from Dataset
                        table.Rows.RemoveAt(0);// Remvoe Excel Titles
                        foreach (DataRow row in table.Rows)
                        {
                            // Skip the first row...
                            Group groupDetails = new Group();
                            groupDetails.Id = Guid.NewGuid().ToString();
                            groupDetails.Name = row[0].ToString();
                            var users = row[1].ToString().Split(new[] { ',' }, StringSplitOptions.RemoveEmptyEntries).Select(user => user.ToLower().Trim()).ToList();
                            groupDetails.Users = users;

                            if (table.Columns.Count>2)
                            {
                                // Check if DL is passed
                                var distributionLists = row[2].ToString().Split(new[] { ',' }, StringSplitOptions.RemoveEmptyEntries).Select(dl => dl.ToLower().Trim()).ToList();
                                groupDetails.DistributionLists = distributionLists;
                            }
                            groups.Add(groupDetails);
                        }
                        // The result of each spreadsheet is in result.Tables
                    }
                }
            }
            catch (Exception)
            {
                // Send null if exception occurred.
                groups = null;
            }
            return groups;
        }
    }
}