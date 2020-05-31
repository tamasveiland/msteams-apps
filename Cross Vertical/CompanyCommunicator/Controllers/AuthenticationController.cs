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
using CrossVertical.Announcement.Helper;
using CrossVertical.Announcement.Helpers;
using CrossVertical.Announcement.Models;
using CrossVertical.Announcement.Repository;
using Microsoft.Bot.Connector;
using Newtonsoft.Json;
using System.Threading.Tasks;
using System.Web;
using System.Web.Mvc;

namespace CrossVertical.Announcement.Controllers
{
    public class AuthenticationController : Controller
    {
        // GET: Authentication
        [Route("adminconsent")]
        public async Task<ActionResult> ConsentPage(string tenant, string admin_consent, string state)
        {

            if (string.IsNullOrEmpty(tenant))
            {
                return HttpNotFound();
            }

            var adminUserDetails = JsonConvert.DeserializeObject<AdminUserDetails>(HttpUtility.UrlDecode(state));
            var tenantDetails = await Cache.Tenants.GetItemAsync(tenant);
            tenantDetails.IsAdminConsented = true;
            tenantDetails.Admin = adminUserDetails.UserEmailId;
            await Cache.Tenants.AddOrUpdateItemAsync(tenantDetails.Id, tenantDetails);

            var userDetails = await Cache.Users.GetItemAsync(adminUserDetails.UserEmailId);

            await ProactiveMessageHelper.SendPersonalNotification(adminUserDetails.ServiceUrl, tenant, userDetails, "Your app consent is successfully granted. Please go ahead and set groups & moderators.", null);

            await ProactiveMessageHelper.SendPersonalNotification(adminUserDetails.ServiceUrl, tenant, userDetails, null, CardHelper.GetAdminPanelCard(string.Join(",", tenantDetails.Moderators)));

            return View();
        }

        // GET: Authentication
        [Route("test")]
        public async Task<ActionResult> Test(string tasks)
        {
            var token = await GraphHelper.GetAccessToken("0d9b645f-597b-41f0-a2a3-ef103fbd91bb", ApplicationSettings.AppId, ApplicationSettings.AppSecret);
            GraphHelper helper = new GraphHelper(token);
            var photo = await helper.GetUserProfilePhoto("0d9b645f-597b-41f0-a2a3-ef103fbd91bb", "mungo@blrdev.onmicrosoft.com");
            return View();
        }
    }
}