using Airline.FlightTeamCreation.Web.Models;
using Airline.FlightTeamCreation.Web.Repository;
using System;
using System.Net;
using System.Net.Http;
using System.Threading.Tasks;
using System.Web.Http;

namespace Airline.FlightTeamCreation.Web.Controllers
{
    public class AuthenticationController : ApiController
    {
        [Route("Auth/Simple")]
        [HttpGet]
        public async Task<IHttpActionResult> RedirectURL()
        {
            var requ = Request.Content;
            var queryDictionary = System.Web.HttpUtility.ParseQueryString(Request.RequestUri.Query);
            //return Request.CreateResponse(HttpStatusCode.OK);
            return Redirect(new Uri("/composeExtensionSettings.html", UriKind.Relative));
        }

        [Route("setuserinfo")]
        public HttpResponseMessage SetUserInfo(UserInfo userInfo)
        {

            UserInfoRepository.AddUserInfo(userInfo);
            return Request.CreateResponse(HttpStatusCode.OK, "Success");
        }

        [HttpGet]
        [Route("Auth/TabToken")]
        public HttpResponseMessage Token()
        {
            return Request.CreateResponse(HttpStatusCode.OK, "Success");
        }

        [HttpGet]
        [Route("Auth/Logout")]
        public HttpResponseMessage Logout()
        {
            return Request.CreateResponse(HttpStatusCode.OK, "Success");

        }
    }
}