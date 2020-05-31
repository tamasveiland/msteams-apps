using CrossVertical.PollingBot.Repository;
using Microsoft.WindowsAzure.Storage;
using Microsoft.WindowsAzure.Storage.Table;
using Microsoft.WindowsAzure.Storage.Table.Protocol;
using System.Configuration;
using System.Threading.Tasks;
using System.Web.Mvc;

namespace CrossVertical.PollingBot.Controllers
{
    public class HomeController : Controller
    {
        [Route("")]
        public ActionResult Index()
        {
            return View();
        }

        [Route("hello")]
        public ActionResult Hello()
        {
            return View("Index");
        }

        [Route("first")]
        public ActionResult First()
        {
            return View();
        }

        [Route("second")]
        public ActionResult Second()
        {
            return View();
        }

        [Route("configure")]
        public ActionResult Configure()
        {
            return View();
        }

        [Route("CleanDatabase")]
        [HttpPost]
        public async Task<ActionResult> CleanDatabase()
        {
            var authRequestHeader = Request.Headers["AuthKey"];
            if (!string.IsNullOrEmpty(authRequestHeader) && ConfigurationManager.AppSettings["AuthKey"] == authRequestHeader)
            {
                // Cleanup up database
                await DocumentDBRepository.CleanUpAsync();

                // Cleanup TableStorage
                var storageAccountConnectionString = ConfigurationManager.AppSettings["AzureWebJobsStorage"];
                var storageAccount = CloudStorageAccount.Parse(storageAccountConnectionString);
                var tableClient = storageAccount.CreateCloudTableClient();
                var table = tableClient.GetTableReference("botdata");

                await table.DeleteIfExistsAsync();

                // Recreate DB Collection
                await DocumentDBRepository.InitializeAsync();

                await SafeCreateIfNotExists(table);

                return new HttpStatusCodeResult(System.Net.HttpStatusCode.OK);
            }
            else
                return new HttpStatusCodeResult(System.Net.HttpStatusCode.Forbidden, "Auth key does not match.");
        }

        public static async Task<bool> SafeCreateIfNotExists(CloudTable table, TableRequestOptions requestOptions = null, OperationContext operationContext = null)
        {
            do
            {
                try
                {
                    return table.CreateIfNotExists(requestOptions, operationContext);
                }
                catch (StorageException e)
                {
                    if ((e.RequestInformation.HttpStatusCode == 409) && (e.RequestInformation.ExtendedErrorInformation.ErrorCode.Equals(TableErrorCodeStrings.TableBeingDeleted)))
                        await Task.Delay(2000);// The table is currently being deleted. Try again until it works.
                    else
                        throw;
                }
            } while (true);
        }
    }
}
