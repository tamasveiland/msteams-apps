// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license.
//
// Microsoft Bot Framework: http://botframework.com
// Microsoft Teams: https://dev.office.com/microsoft-teams
//
// Bot Builder SDK GitHub:
// https://github.com/Microsoft/BotBuilder
//
// Bot Builder SDK Extensions for Teams
// https://github.com/OfficeDev/BotBuilder-MicrosoftTeams
//
// Copyright (c) Microsoft Corporation
// All rights reserved.
//
// MIT License:
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

using ExcelDataReader;
using System;
using System.Collections.Generic;
using System.Data;
using System.IO;
using System.Linq;
using CrossVertical.PollingBot.Models;
using CrossVertical.PollingBot.Repository;

namespace CrossVertical.PollingBot.Helper
{
    /// <summary>
    /// Helper to Read excel file.
    /// </summary>
    public static class ExcelHelper
    {
        public static async System.Threading.Tasks.Task<string> GetAddSurveyDetailsAsync(string strFilePath)
        {
            var surveyDetailsList = new List<Question>();
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
                        int i = 1;
                        var questionBank = new QuestionBank()
                        {
                            Id = Guid.NewGuid().ToString(),
                            Type = Helper.Constants.QuestionBank,
                            Active=true


                        };

                        foreach (DataRow row in table.Rows)
                        {
                            // Skip the first row...
                            Question surveyDetails = new Question();
                            surveyDetails.Id = Guid.NewGuid().ToString();
                            surveyDetails.Title = row[0].ToString();
                            surveyDetails.Options = row[1].ToString().Split(new[] { ',' }, StringSplitOptions.RemoveEmptyEntries).Select(channel => channel.Trim()).ToList();
                            if(questionBank.EmailIds==null) questionBank.EmailIds= row[2].ToString().Split(new[] { ',' }, StringSplitOptions.RemoveEmptyEntries).Select(channel => channel.Trim()).ToList();
                            i = i + 1;
                            //surveyDetailsList.Add(surveyDetails);
                            questionBank.Questions.Add(surveyDetails);
                        }
                        var updateQuestionBank = await DocumentDBRepository.GetItemsAsync<QuestionBank>(l => l.Type.Contains(Helper.Constants.QuestionBank) && l.Active==true);
                        if (updateQuestionBank != null && updateQuestionBank.Count() > 0)
                        {
                            foreach (var updateq1 in updateQuestionBank)
                            {
                                updateq1.Active = false;
                                var InactiveQuestionBank = await DocumentDBRepository.UpdateItemAsync(updateq1.Id, updateq1);
                            }
                        }
                        var feedbackQuestionBank = await DocumentDBRepository.GetItemsAsync<FeedbackData>(l => l.Type.Contains(Helper.Constants.FeedBack) && l.Active==true);
                        if (feedbackQuestionBank != null && feedbackQuestionBank.Count() > 0)
                        {
                            foreach (var feedback in feedbackQuestionBank)
                            {
                                feedback.Active = false;
                                var InactiveFeedBack = await DocumentDBRepository.UpdateItemAsync(feedback.Id, feedback);
                            }
                        }
                        var InsertQuestionBank = await DocumentDBRepository.CreateItemAsync(questionBank);

                        return questionBank.Id;
                        // The result of each spreadsheet is in result.Tables
                    }
                }
            }
            catch (Exception)
            {
                // Send null if exception occurred.
                surveyDetailsList = null;
                
            }
            return null;
        }
    }
}