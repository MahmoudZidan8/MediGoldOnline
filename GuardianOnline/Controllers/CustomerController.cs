using Customer.Hubs;
using GuardianOnline.Infrastructure;
using System;
using System.Collections.Generic;
using System.Configuration;
using System.Data;
using System.Data.SqlClient;
using System.Linq;
using System.Threading.Tasks;
using System.Web;
using System.Web.Mvc;

namespace Customer.Controllers
{
    public class CustomerController : Controller
    {
        private MedInsuranceProEntities db = new MedInsuranceProEntities();

        // GET: Customer
        public ActionResult Index()
        {
            return View();
        }

        public async Task<JsonResult> Get()
        {
            try
            {
                await Task.Delay(1);

                using (var connection = new SqlConnection(ConfigurationManager.ConnectionStrings["CustomerConnection"].ConnectionString))
                {
                    connection.Open();
                    using (SqlCommand command = new SqlCommand(@"SELECT [ClaimFormNum],[CardNumber],[ProviderID],[BranchID] FROM [dbo].[AlertProviderData] where [ClaimFormNum]<>0 and [CardNumber] <>'0'", connection))
                    {
                        // Make sure the command object does not already have
                        // a notification object associated with it.
                        command.Notification = null;

                        SqlDependency dependency = new SqlDependency(command);
                        dependency.OnChange += new OnChangeEventHandler(dependency_OnChange);

                        if (connection.State == ConnectionState.Closed)
                            connection.Open();


                        SqlDataReader reader = command.ExecuteReader();

                        var listCus = reader.Cast<IDataRecord>()
                                .Select(x => new
                                {
                                    ClaimFormNum = (long)x["ClaimFormNum"],
                                    CardNumber = (string)x["CardNumber"],
                                    ProviderID = (int)x["ProviderID"],
                                    BranchID = (int)x["BranchID"],
                                }).ToList();



                        return Json(new { listCus = listCus }, JsonRequestBehavior.AllowGet);

                    }
                }

            }
            catch
            {
                List<int> listCus = new List<int>();
                return Json(new { listCus = listCus }, JsonRequestBehavior.AllowGet);
            }
        }

        public async Task<JsonResult> Getmessages()
        {
            try { 
            await Task.Delay(1);

            int providerid = int.Parse(Session["providerid"].ToString());
            int BranchID = int.Parse(Session["BranchID"].ToString());

            using (var connection1 = new SqlConnection(ConfigurationManager.ConnectionStrings["CustomerConnection"].ConnectionString))
            {
                connection1.Open();
                using (SqlCommand command1 = new SqlCommand(@"SELECT [ProviderName],[BranchName],[CMCName],[Message],[MessageDateTime],[IsProvider],[IsCMC] FROM [dbo].[Messanger] where [ProviderID]="+providerid+ "And [BranchID]="+BranchID, connection1))
                {
                    // Make sure the command object does not already have
                    // a notification object associated with it.
                    command1.Notification = null;

                    SqlDependency dependency1 = new SqlDependency(command1);
                    dependency1.OnChange += new OnChangeEventHandler(dependency_OnChange1);

                    if (connection1.State == ConnectionState.Closed)
                        connection1.Open();


                    SqlDataReader reader = command1.ExecuteReader();

                    var listCus = reader.Cast<IDataRecord>()
                            .Select(x => new
                            {
                                ProviderName = (string)x["ProviderName"],
                                BranchName = (string)x["BranchName"],
                                CMCName = (string)x["CMCName"],
                                Message = (string)x["Message"],
                                MessageDateTime = (DateTime)x["MessageDateTime"],
                                IsProvider = (bool)x["IsProvider"],
                                IsCMC = (bool)x["IsCMC"]
                            }).ToList();



                    return Json(new { listCus = listCus }, JsonRequestBehavior.AllowGet);

                }
            }
            }
            catch
            {
                List<int> listCus = new List<int>();
                return Json(new { listCus = listCus }, JsonRequestBehavior.AllowGet);
            }
        }
        public async Task<JsonResult> Get1()
        {
            try { 
            await Task.Delay(10);

            using (var connection5 = new SqlConnection(ConfigurationManager.ConnectionStrings["CustomerConnection"].ConnectionString))
            {
                connection5.Open();
                using (SqlCommand command5 = new SqlCommand(@"SELECT [CardNumber],[ProviderID],[BranchID] FROM [dbo].[AlertSmartCardData] where [CardNumber] <>'0'", connection5))
                {
                    // Make sure the command object does not already have
                    // a notification object associated with it.
                    command5.Notification = null;

                    SqlDependency dependency = new SqlDependency(command5);
                    dependency.OnChange += new OnChangeEventHandler(dependency_OnChange2);

                    if (connection5.State == ConnectionState.Closed)
                        connection5.Open();


                    SqlDataReader reader = command5.ExecuteReader();

                    var listCus = reader.Cast<IDataRecord>()
                            .Select(x => new
                            {
                                CardNumber = (string)x["CardNumber"],
                                ProviderID = (int)x["ProviderID"],
                                BranchID = (int)x["BranchID"],
                            }).ToList();


                    return Json(new { listCus = listCus }, JsonRequestBehavior.AllowGet);

                }
            }
            }
            catch
            {
                List<int> listCus = new List<int>();
                return Json(new { listCus = listCus }, JsonRequestBehavior.AllowGet);
            }
        }

        private async void dependency_OnChange(object sender, SqlNotificationEventArgs e)
        {
            await CusHub.Show1();
        }
        private async void dependency_OnChange1(object sender, SqlNotificationEventArgs e)
        {
            await CusHub.Show();
        }
        private async void dependency_OnChange2(object sender, SqlNotificationEventArgs e)
        {
            await CusHub.Show2();
        }
       
    }
}