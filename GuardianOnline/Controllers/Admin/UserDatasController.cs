using System;
using System.Collections.Generic;
using System.Data;
using System.Data.Entity;
using System.Linq;
using System.Net;
using System.Web;
using System.Web.Mvc;
using System.Collections;
using GuardianOnline.Infrastructure;

using System.IO;

namespace Guardian.Controllers.Admin
{
    public class UserDatasController : Controller
    {
        private MedInsuranceProEntities db = new MedInsuranceProEntities();

        // GET: UserDatas
        public ActionResult Index()
        {
            if ((Session["UserID"] == null) || (bool.Parse(Session["IsAdmin"].ToString()) == false))
            {
                return RedirectToAction("login", "ClaimForm");
            }
            var myvalues = (from values in db.ProviderUserDatas

                            select values.UserName).ToArray();

            ViewBag.li = myvalues;

            return View(/*db.UserDatas.ToList()*/);
        }
        public JsonResult Select_User(string id)
        {

            var recs = db.ProviderUserDatas.Where(d => d.UserName.Replace(" ", string.Empty) == id.Replace(" ", string.Empty)).ToList();


            return Json(recs, JsonRequestBehavior.AllowGet);

        }
        // GET: UserDatas/Details/5
        public ActionResult Details(int? id)
        {
            if ((Session["UserID"] == null) || (bool.Parse(Session["IsAdmin"].ToString()) == false))
            {
                return RedirectToAction("login", "ClaimForm");
            }
            if (id == null)
            {
                return new HttpStatusCodeResult(HttpStatusCode.BadRequest);
            }
            UserData userData = db.UserDatas.Find(id);
            if (userData == null)
            {
                return HttpNotFound();
            }
            return View(userData);
        }
        public JsonResult Get_Provider_Branch(string id)
        {

            var Provider_Branch = (from values in db.PVBranchDatas
                                   join item in db.PVProviderDatas on values.ProviderID equals item.ProviderID
                                   where item.ProviderName.Replace(" ", string.Empty) == id.Replace(" ", string.Empty)
                                   select values.Address).ToList();

            var Provider_Branchmain = (from values in db.PVProviderDatas
                                       where values.ProviderName.Replace(" ", string.Empty) == id.Replace(" ", string.Empty)
                                       select values.ProviderAddress).FirstOrDefault();
            Provider_Branch.Add(Provider_Branchmain);

            return Json(Provider_Branch, JsonRequestBehavior.AllowGet);
        }

        // GET: UserDatas/Create
        public ActionResult Create()
        {
            if ((Session["UserID"] == null) || (bool.Parse(Session["IsAdmin"].ToString()) == false))
            {
                return RedirectToAction("login", "ClaimForm");
            }

            //List<string> UserType_Name = new List<string>();
            //UserType_Name.Add("Tristar");
            //UserType_Name.Add("Customer HR");
            //UserType_Name.Add("Insurance Company");
            //ViewBag.UserType_ID = UserType_Name;

            //List<string> UserTypeCustomer_Name = new List<string>();
            //ViewBag.UserTypeCustomer_ID = UserTypeCustomer_Name;
            var providerName = (from values in db.PVProviderDatas
                                where values.IsActive == true
                                select values.ProviderName).ToArray();

            ViewBag.providernames = providerName;

            List<string> branch_Name = new List<string>();
            ViewBag.branchNames = branch_Name;

            return View();
        }
        public JsonResult Get_User_Customer_Type(string id)
        {
            //var x = _context.CustomerDatas.ToList()[2];
            if (id == "" || id == "whatever")
            {

                return Json("", JsonRequestBehavior.AllowGet);

            }

            if (id == "Tristar")
            {
                List<string> UserTypeCustomer_Name = new List<string>();
                UserTypeCustomer_Name.Add("Tristar");
                return Json(UserTypeCustomer_Name, JsonRequestBehavior.AllowGet);
            }
            else if (id == "Customer HR")
            {
                var CustomerNames = (from values in db.CustomerDatas
                                     join item in db.Contracts on values.CustomerID equals item.CustomerID
                                     select values.CustomerName).Distinct().ToList();
                return Json(CustomerNames, JsonRequestBehavior.AllowGet);

            }
            else
            {
                var InsuranceCompanyNames = (from values in db.InsuranceCompanyDatas
                                             select values.InsuranceCompanyName).ToList();
                return Json(InsuranceCompanyNames, JsonRequestBehavior.AllowGet);

            }

        }
        // POST: UserDatas/Create
        // To protect from overposting attacks, please enable the specific properties you want to bind to, for 
        // more details see https://go.microsoft.com/fwlink/?LinkId=317598.
        [HttpPost]
        [ValidateAntiForgeryToken]
        public ActionResult Create([Bind(Include = "UserID,UserName,UserEmail,UserPassword,confUserPassword,IPAddress,CreateDate,LastLoginDate,FirstName,LastName,IsLockedOut,IsFirstLogin,InvalidPasswordAttempts,IsAdmin,UserImage,ProviderID,BranchID,ProviderAdmin")] ProviderUserData userData, string provider_names, string branch_Names, HttpPostedFileBase imgfile)
        {
            var providerName = (from values in db.PVProviderDatas
                                where values.IsActive == true
                                select values.ProviderName).ToArray();

            ViewBag.providernames = providerName;

            List<string> branch_Name = new List<string>();
            ViewBag.branchNames = branch_Name;
            if (provider_names == "" || provider_names == "whatever")
            {

                ViewBag.ErorMessage = "Select Provider Name is Required";
                return View(userData);
            }
            var Provider_id = (from values in db.PVProviderDatas
                                       where values.ProviderName.Replace(" ", string.Empty) == provider_names.Replace(" ", string.Empty)
                                       select values.ProviderID).FirstOrDefault();
            userData.ProviderID = Provider_id;

            if (branch_Names == "" || branch_Names == "whatever")
            {

                ViewBag.ErorMessage = "Select Branch Name is Required";
                return View(userData);
            }
            var Branch_id = (from values in db.PVBranchDatas
                               where values.Address.Replace(" ", string.Empty) == branch_Names.Replace(" ", string.Empty)
                               select values.BranchID).FirstOrDefault();
            if(Branch_id==0)
            {
                userData.BranchID = Provider_id;
            }
            else
            {
                userData.BranchID = Branch_id;
            }
            var recc1 = db.ProviderUserDatas.FirstOrDefault(d => d.UserName.Replace(" ", string.Empty) == userData.UserName.Replace(" ", string.Empty));
            if (recc1 != null)
            {
                ViewBag.ErorMessage = "User Name is Added from a while";
                return View(userData);
            }

            userData.IPAddress = "1.1.1.1";
            userData.CreateDate = DateTime.Now;
            userData.LastLoginDate = DateTime.Now;
            userData.IsLockedOut = false;
            userData.IsFirstLogin = true;
            userData.InvalidPasswordAttempts = 0;
            userData.UserTypeID = 0;
            userData.UserTypeCustomerID = 0;
            userData.IsAdmin = false;
            userData.ProviderAdmin = userData.ProviderAdmin;
            if (ModelState.IsValid)
            {
                string path = "~/Images/UserDefault.png";
                if (imgfile != null)
                {
                    if (imgfile.FileName.Length > 0)
                    {
                        path = "~/Images/" + Path.GetFileName(imgfile.FileName);
                        imgfile.SaveAs(Server.MapPath(path));
                    }
                }

                userData.UserImage = path;

                db.ProviderUserDatas.Add(userData);
                db.SaveChanges();
                return RedirectToAction("Index");
            }

            return View(userData);
        }

        // GET: UserDatas/Edit/5
        public ActionResult Edit(int? id)
        {
            if ((Session["UserID"] == null) || (bool.Parse(Session["IsAdmin"].ToString()) == false))
            {
                return RedirectToAction("login", "ClaimForm");
            }
            //List<string> UserType_Name = new List<string>();
            //UserType_Name.Add("Tristar");
            //UserType_Name.Add("Customer HR");
            //UserType_Name.Add("Insurance Company");
            //ViewBag.UserType_ID = UserType_Name;




            if (id == null)
            {
                return new HttpStatusCodeResult(HttpStatusCode.BadRequest);
            }
            ProviderUserData userData = db.ProviderUserDatas.Find(id);
            if (userData == null)
            {
                return HttpNotFound();
            }
            int? aaa = userData.UserID;
            var recaa = db.ProviderUserDatas.FirstOrDefault(d => d.UserID == aaa);
            if (recaa != null)
            {

                int Provider_ID = recaa.ProviderID;
                int Branch_ID = recaa.BranchID;


                var providerName = (from values in db.PVProviderDatas
                                    where values.IsActive == true
                                    select values.ProviderName).ToArray();
                ViewBag.providernames = providerName;

                var Provider_name = (from values in db.PVProviderDatas
                                   where values.ProviderID == Provider_ID
                                     select values.ProviderName).FirstOrDefault();
                ViewBag.qqq = Provider_name;

                var Provider_Branch = (from values in db.PVBranchDatas
                                       join item in db.PVProviderDatas on values.ProviderID equals item.ProviderID
                                       where item.ProviderID == Provider_ID
                                       select values.Address).ToList();

                var Provider_Branchmain = (from values in db.PVProviderDatas
                                           where values.ProviderID == Provider_ID
                                           select values.ProviderAddress).FirstOrDefault();
                Provider_Branch.Add(Provider_Branchmain);
                ViewBag.branchNames = Provider_Branch;

                var Branch_name = (from values in db.PVBranchDatas
                                 where values.ProviderID == Provider_ID && values.BranchID == Branch_ID
                                 select values.Address).FirstOrDefault();
                if (Branch_name == null)
                {
                    ViewBag.q = Provider_Branchmain;
                }
                else
                {
                    ViewBag.q = Branch_name;
                }

              
            }
            else
            {
                ViewBag.qqq = "";
                ViewBag.q = "";
                List<string> branch_Name = new List<string>();
                ViewBag.branchNames = branch_Name;
                ViewBag.providernames = branch_Name;
            }


            return View(userData);
        }

        // POST: UserDatas/Edit/5
        // To protect from overposting attacks, please enable the specific properties you want to bind to, for 
        // more details see https://go.microsoft.com/fwlink/?LinkId=317598.
        [HttpPost]
        [ValidateAntiForgeryToken]
        public ActionResult Edit([Bind(Include = "UserID,UserName,UserEmail,UserPassword,confUserPassword,IPAddress,CreateDate,LastLoginDate,FirstName,LastName,IsLockedOut,IsFirstLogin,InvalidPasswordAttempts,IsAdmin,UserImage,ProviderID,BranchID,ProviderAdmin")] ProviderUserData userData, string provider_names, string branch_Names, HttpPostedFileBase imgfile)
        {
            
            int? aaa = userData.UserID;
            var recaa = db.ProviderUserDatas.FirstOrDefault(d => d.UserID == aaa);
            if (recaa != null)
            {

                int Provider_ID = recaa.ProviderID;
                int Branch_ID = recaa.BranchID;


                var providerName = (from values in db.PVProviderDatas
                                    where values.IsActive == true
                                    select values.ProviderName).ToArray();
                ViewBag.providernames = providerName;

                var Provider_name = (from values in db.PVProviderDatas
                                     where values.ProviderID == Provider_ID
                                     select values.ProviderName).FirstOrDefault();
                ViewBag.qqq = Provider_name;

                var Provider_Branch = (from values in db.PVBranchDatas
                                       join item in db.PVProviderDatas on values.ProviderID equals item.ProviderID
                                       where item.ProviderID == Provider_ID
                                       select values.Address).ToList();

                var Provider_Branchmain = (from values in db.PVProviderDatas
                                           where values.ProviderID == Provider_ID
                                           select values.ProviderAddress).FirstOrDefault();
                Provider_Branch.Add(Provider_Branchmain);
                ViewBag.branchNames = Provider_Branch;

                var Branch_name = (from values in db.PVBranchDatas
                                   where values.ProviderID == Provider_ID && values.BranchID == Branch_ID
                                   select values.Address).FirstOrDefault();
                if (Branch_name == null)
                {
                    ViewBag.q = Provider_Branchmain;
                }
                else
                {
                    ViewBag.q = Branch_name;
                }


            }
            else
            {
                ViewBag.qqq = "";
                ViewBag.q = "";
                List<string> branch_Name = new List<string>();
                ViewBag.branchNames = branch_Name;
                ViewBag.providernames = branch_Name;
            }

            if (provider_names == "" || provider_names == "whatever")
            {

                ViewBag.ErorMessage = "Select Provider Name is Required";
                return View(userData);
            }
            var Provider_id = (from values in db.PVProviderDatas
                               where values.ProviderName.Replace(" ", string.Empty) == provider_names.Replace(" ", string.Empty)
                               select values.ProviderID).FirstOrDefault();
            userData.ProviderID = Provider_id;

            if (branch_Names == "" || branch_Names == "whatever")
            {

                ViewBag.ErorMessage = "Select Branch Name is Required";
                return View(userData);
            }
            var Branch_id = (from values in db.PVBranchDatas
                             where values.Address.Replace(" ", string.Empty) == branch_Names.Replace(" ", string.Empty)
                             select values.BranchID).FirstOrDefault();
            if (Branch_id == 0)
            {
                userData.BranchID = Provider_id;
            }
            else
            {
                userData.BranchID = Branch_id;
            }

            var recc1 = db.ProviderUserDatas.FirstOrDefault(d => d.UserName.Replace(" ", string.Empty) == userData.UserName.Replace(" ", string.Empty) && d.UserID != userData.UserID);
            if (recc1 != null)
            {
                ViewBag.ErorMessage = "User Name is Added from a while Please Enter Different User Name";
                return View(userData);
            }

            var rec = db.ProviderUserDatas.FirstOrDefault(d => d.UserID == userData.UserID);
            userData.UserPassword = rec.UserPassword;
            userData.confUserPassword = rec.UserPassword;
            userData.IPAddress = rec.IPAddress;
            userData.CreateDate = rec.CreateDate;
            userData.LastLoginDate = rec.LastLoginDate;
            userData.IsFirstLogin = rec.IsFirstLogin;
            userData.InvalidPasswordAttempts = rec.InvalidPasswordAttempts;
            userData.UserTypeID = rec.UserTypeID;
            userData.UserTypeCustomerID = rec.UserTypeCustomerID;
            //userData.ProviderAdmin = rec.ProviderAdmin;
            string userImg = rec.UserImage;

            if (ModelState.IsValid)
            {

                string path = userImg;
                if (imgfile != null)
                {
                    if (imgfile.FileName.Length > 0)
                    {
                        path = "~/Images/" + Path.GetFileName(imgfile.FileName);
                        imgfile.SaveAs(Server.MapPath(path));
                    }
                }
                //userData.IsAdmin=false;
                userData.UserImage = path;

                using (var database = new MedInsuranceProEntities())
                {
                    var foo = database.ProviderUserDatas.Where(f => f.UserID == userData.UserID).FirstOrDefault();
                    foo.ProviderID = userData.ProviderID;
                    foo.BranchID = userData.BranchID;
                    foo.UserTypeID = userData.UserTypeID;
                    foo.UserTypeCustomerID = userData.UserTypeCustomerID;
                    foo.UserName = userData.UserName;
                    foo.FirstName = userData.FirstName;
                    foo.LastName = userData.LastName;
                    foo.UserEmail = userData.UserEmail;
                    foo.IsLockedOut = userData.IsLockedOut;
                    foo.UserImage = userData.UserImage;
                    foo.IsAdmin = userData.IsAdmin;
                    foo.confUserPassword = foo.UserPassword;
                    foo.ProviderAdmin = userData.ProviderAdmin;
                    database.SaveChanges();
                }
               
                return RedirectToAction("Index");
            }
            return View(userData);
        }
        [HttpGet]
        public ActionResult Add_User_Roles()
        {
            if ((Session["UserID"] == null) || (bool.Parse(Session["IsAdmin"].ToString()) == false))
            {
                return RedirectToAction("login", "ClaimForm");
            }
            var myvalues = (from values in db.ProviderUserDatas
                            join item in db.ProviderUserRolesDatas on values.UserID equals item.UserID into finalGroup0
                            from finalItem0 in finalGroup0.DefaultIfEmpty()
                            where finalItem0.UserID != values.UserID
                            select values.UserName).ToArray();

            ViewBag.li = myvalues;

            return View();
        }
        public JsonResult Add_User_Roless(string User_list, bool UploadMember_Claim, bool Medical_Services, bool ClaimForm_Approved,bool Approval, bool Expanded_Services, bool Payment_Inquiry, bool Invoices_Audit, bool Doctor_ClaimForm)
        {
            int insertedRecords = 0;
            var arrlis = new ArrayList();


            if ((User_list == "") || (User_list == "whatever"))
            {
                insertedRecords = 1;
                arrlis.Add(insertedRecords);
                var ErorMessage = "Select Users Name is Required";
                arrlis.Add(ErorMessage);
                return Json(arrlis, JsonRequestBehavior.AllowGet);
            }

            var user_Data_selected = db.ProviderUserDatas.FirstOrDefault(d => d.UserName.Replace(" ", string.Empty) == User_list.Replace(" ", string.Empty));
            var user_id = user_Data_selected.UserID;
            int UserRole_ID = 1;

            if (db.ProviderUserRolesDatas.Any())
            {
                var maxValu = db.ProviderUserRolesDatas.Max(o => o.UserRoleID);
                var resul = db.ProviderUserRolesDatas.First(o => o.UserRoleID == maxValu);
                UserRole_ID = (resul.UserRoleID) + 1;
            }
            List<ProviderUserRolesData> userRolesData = new List<ProviderUserRolesData>();
            var Active_status = 0;
            if (UploadMember_Claim == true)
            { Active_status = 1; }
            ProviderUserRolesData newdataa = new ProviderUserRolesData()
            {
                UserRoleID = UserRole_ID,
                UserID = user_id,
                RoleID = 1,
                ActiveStatus = Active_status
            };
            userRolesData.Add(newdataa);

            UserRole_ID++;
            var Active_status1 = 0;
            if (Medical_Services == true)
            { Active_status1 = 1; }
            ProviderUserRolesData newdataa1 = new ProviderUserRolesData()
            {
                UserRoleID = UserRole_ID,
                UserID = user_id,
                RoleID = 2,
                ActiveStatus = Active_status1
            };
            userRolesData.Add(newdataa1);

            UserRole_ID++;
            var Active_status2 = 0;
            if (ClaimForm_Approved == true)
            { Active_status2 = 1; }
            ProviderUserRolesData newdataa2 = new ProviderUserRolesData()
            {
                UserRoleID = UserRole_ID,
                UserID = user_id,
                RoleID = 3,
                ActiveStatus = Active_status2
            };
            userRolesData.Add(newdataa2);

            UserRole_ID++;
            var Active_status3 = 0;
            if (Expanded_Services == true)
            { Active_status3 = 1; }
            ProviderUserRolesData newdataa3 = new ProviderUserRolesData()
            {
                UserRoleID = UserRole_ID,
                UserID = user_id,
                RoleID = 4,
                ActiveStatus = Active_status3
            };
            userRolesData.Add(newdataa3);
            
            UserRole_ID++;
            var Active_status4 = 0;
            if (Payment_Inquiry == true)
            { Active_status4 = 1; }
            ProviderUserRolesData newdataa4 = new ProviderUserRolesData()
            {
                UserRoleID = UserRole_ID,
                UserID = user_id,
                RoleID = 5,
                ActiveStatus = Active_status4
            };
            userRolesData.Add(newdataa4);
            
            UserRole_ID++;
            var Active_status5 = 0;
            if (Invoices_Audit == true)
            { Active_status5 = 1; }
            ProviderUserRolesData newdataa5 = new ProviderUserRolesData()
            {
                UserRoleID = UserRole_ID,
                UserID = user_id,
                RoleID = 6,
                ActiveStatus = Active_status5
            };
            userRolesData.Add(newdataa5);
             
            UserRole_ID++;
            var Active_status6 = 0;
            if (Approval == true)
            { Active_status6 = 1; }
            ProviderUserRolesData newdataa6 = new ProviderUserRolesData()
            {
                UserRoleID = UserRole_ID,
                UserID = user_id,
                RoleID = 7,
                ActiveStatus = Active_status6
            };
            userRolesData.Add(newdataa6);

            UserRole_ID++;
            var Active_status7 = 0;
            if (Doctor_ClaimForm == true)
            { Active_status7 = 1; }
            ProviderUserRolesData newdataa7 = new ProviderUserRolesData()
            {
                UserRoleID = UserRole_ID,
                UserID = user_id,
                RoleID = 8,
                ActiveStatus = Active_status7
            };
            userRolesData.Add(newdataa7);

            using (var context = new MedInsuranceProEntities())
            {
                context.ProviderUserRolesDatas.AddRange(userRolesData);
                context.SaveChanges();
            }


            insertedRecords = 0;
            arrlis.Add(insertedRecords);

            var myvalues = (from values in db.ProviderUserDatas
                            join item in db.ProviderUserRolesDatas on values.UserID equals item.UserID into finalGroup0
                            from finalItem0 in finalGroup0.DefaultIfEmpty()
                            where finalItem0.UserID != values.UserID
                            select values.UserName).ToList();

            arrlis.Add(myvalues);

            return Json(arrlis, JsonRequestBehavior.AllowGet);
        }

        [HttpGet]
        public ActionResult Edit_User_Roles()
        {
            if ((Session["UserID"] == null) || (bool.Parse(Session["IsAdmin"].ToString()) == false))
            {
                return RedirectToAction("login", "ClaimForm");
            }
            var myvalues = (from values in db.ProviderUserDatas
                            join item in db.ProviderUserRolesDatas on values.UserID equals item.UserID into finalGroup0
                            from finalItem0 in finalGroup0.DefaultIfEmpty()
                            where values.UserID == finalItem0.UserID
                            select values.UserName).Distinct().ToArray();

            ViewBag.li = myvalues;

            return View();
        }

        public JsonResult Get_User_Roless(string id)
        {
            int insertedRecords = 0;
            var arrlis = new ArrayList();


            if ((id == "") || (id == "whatever"))
            {
                insertedRecords = 1;
                arrlis.Add(insertedRecords);
                var ErorMessage = "Select Users Name is Required";
                arrlis.Add(ErorMessage);
                return Json(arrlis, JsonRequestBehavior.AllowGet);
            }

            var myvalues = (from values in db.ProviderUserDatas
                            join item in db.ProviderUserRolesDatas on values.UserID equals item.UserID into finalGroup0
                            from finalItem0 in finalGroup0.DefaultIfEmpty()
                            where values.UserName.Replace(" ", string.Empty) == id.Replace(" ", string.Empty)
                            select finalItem0.ActiveStatus).ToList();


            insertedRecords = 0;
            arrlis.Add(insertedRecords);
            arrlis.Add(myvalues);

            return Json(arrlis, JsonRequestBehavior.AllowGet);
        }

        public JsonResult Edit_User_Roless(string User_list, bool UploadMember_Claim, bool Medical_Services, bool ClaimForm_Approved, bool Approval, bool Expanded_Services, bool Payment_Inquiry, bool Invoices_Audit, bool Doctor_ClaimForm)
        {
            int insertedRecords = 0;
            var arrlis = new ArrayList();


            if ((User_list == "") || (User_list == "whatever"))
            {
                insertedRecords = 1;
                arrlis.Add(insertedRecords);
                var ErorMessage = "Select Users Name is Required";
                arrlis.Add(ErorMessage);
                return Json(arrlis, JsonRequestBehavior.AllowGet);
            }

            var user_Data_selected = db.ProviderUserDatas.FirstOrDefault(d => d.UserName.Replace(" ", string.Empty) == User_list.Replace(" ", string.Empty));
            var user_id = user_Data_selected.UserID;

            var UserRolesDatas = db.ProviderUserRolesDatas.Where(f => f.UserID == user_id).ToList();


            var Active_status = 0;
            if (UploadMember_Claim == true)
            { Active_status = 1; }

            var role1 = UserRolesDatas.FirstOrDefault(dd => dd.RoleID == 1);
            var ActiveStatus1 = role1.ActiveStatus;
            if (ActiveStatus1 != Active_status)
            {
                using (var database = new MedInsuranceProEntities())
                {
                    var foo = database.ProviderUserRolesDatas.Where(f => f.UserID == user_id && f.RoleID == 1).FirstOrDefault();
                    foo.ActiveStatus = Active_status;
                    database.SaveChanges();
                }
            }
            var Active_status1 = 0;
            if (Medical_Services == true)
            { Active_status1 = 1; }
            var role2 = UserRolesDatas.FirstOrDefault(dd => dd.RoleID == 2);
            var ActiveStatus2 = role2.ActiveStatus;
            if (ActiveStatus2 != Active_status1)
            {
                using (var database = new MedInsuranceProEntities())
                {
                    var foo = database.ProviderUserRolesDatas.Where(f => f.UserID == user_id && f.RoleID == 2).FirstOrDefault();
                    foo.ActiveStatus = Active_status1;
                    database.SaveChanges();
                }
            }
            var Active_status2 = 0;
            if (ClaimForm_Approved == true)
            { Active_status2 = 1; }
            var role3 = UserRolesDatas.FirstOrDefault(dd => dd.RoleID == 3);
            var ActiveStatus3 = role3.ActiveStatus;
            if (ActiveStatus3 != Active_status2)
            {
                using (var database = new MedInsuranceProEntities())
                {
                    var foo = database.ProviderUserRolesDatas.Where(f => f.UserID == user_id && f.RoleID == 3).FirstOrDefault();
                    foo.ActiveStatus = Active_status2;
                    database.SaveChanges();
                }
            }

            var Active_status3 = 0;
            if (Expanded_Services == true)
            { Active_status3 = 1; }
            var role4 = UserRolesDatas.FirstOrDefault(dd => dd.RoleID == 4);
            var ActiveStatus4 = role4.ActiveStatus;
            if (ActiveStatus4 != Active_status3)
            {
                using (var database = new MedInsuranceProEntities())
                {
                    var foo = database.ProviderUserRolesDatas.Where(f => f.UserID == user_id && f.RoleID == 4).FirstOrDefault();
                    foo.ActiveStatus = Active_status3;
                    database.SaveChanges();
                }
            }

            var Active_status5 = 0;
            if (Payment_Inquiry == true)
            { Active_status5 = 1; }
            var role5 = UserRolesDatas.FirstOrDefault(dd => dd.RoleID == 5);
            var ActiveStatus6 = role5.ActiveStatus;
            if (ActiveStatus6 != Active_status5)
            {
                using (var database = new MedInsuranceProEntities())
                {
                    var foo = database.ProviderUserRolesDatas.Where(f => f.UserID == user_id && f.RoleID == 5).FirstOrDefault();
                    foo.ActiveStatus = Active_status5;
                    database.SaveChanges();
                }
            }

            var Active_status6 = 0;
            if (Invoices_Audit == true)
            { Active_status6 = 1; }
            var role6 = UserRolesDatas.FirstOrDefault(dd => dd.RoleID == 6);
            var ActiveStatus7 = role6.ActiveStatus;
            if (ActiveStatus7 != Active_status6)
            {
                using (var database = new MedInsuranceProEntities())
                {
                    var foo = database.ProviderUserRolesDatas.Where(f => f.UserID == user_id && f.RoleID == 6).FirstOrDefault();
                    foo.ActiveStatus = Active_status6;
                    database.SaveChanges();
                }
            }
               
            var Active_status7 = 0;
            if (Approval == true)
            { Active_status7 = 1; }
            var role7 = UserRolesDatas.FirstOrDefault(dd => dd.RoleID == 7);
            var ActiveStatus8 = role7.ActiveStatus;
            if (ActiveStatus8 != Active_status7)
            {
                using (var database = new MedInsuranceProEntities())
                {
                    var foo = database.ProviderUserRolesDatas.Where(f => f.UserID == user_id && f.RoleID == 7).FirstOrDefault();
                    foo.ActiveStatus = Active_status7;
                    database.SaveChanges();
                }
            }


            var Active_status8 = 0;
            if (Doctor_ClaimForm == true)
            { Active_status8 = 1; }
            var role8 = UserRolesDatas.FirstOrDefault(dd => dd.RoleID == 8);
            var ActiveStatus9 = role8.ActiveStatus;
            if (ActiveStatus9 != Active_status8)
            {
                using (var database = new MedInsuranceProEntities())
                {
                    var foo = database.ProviderUserRolesDatas.Where(f => f.UserID == user_id && f.RoleID == 8).FirstOrDefault();
                    foo.ActiveStatus = Active_status8;
                    database.SaveChanges();
                }
            }

            insertedRecords = 0;
            arrlis.Add(insertedRecords);

            return Json(arrlis, JsonRequestBehavior.AllowGet);
        }

        public ActionResult AccountData()
        {
            if ((Session["UserID"] == null))
            {
                return RedirectToAction("login", "Home");
            }

            int? id = int.Parse(Session["UserID"].ToString());
            ProviderUserData userData = db.ProviderUserDatas.Find(id);
            if (userData == null)
            {
                return HttpNotFound();
            }
            userData.confUserPassword = userData.UserPassword;
            return View(userData);
        }


        [HttpPost]
        [ValidateAntiForgeryToken]
        public ActionResult AccountData([Bind(Include = "UserID,UserName,UserEmail,UserPassword,confUserPassword,IPAddress,CreateDate,LastLoginDate,FirstName,LastName,IsLockedOut,IsFirstLogin,InvalidPasswordAttempts,IsAdmin,UserImage,ProviderID,BranchID")] ProviderUserData userData, HttpPostedFileBase imgfile)
        {


            var recc1 = db.ProviderUserDatas.FirstOrDefault(d => d.UserName.Replace(" ", string.Empty) == userData.UserName.Replace(" ", string.Empty) && d.UserID != userData.UserID);
            if (recc1 != null)
            {
                ViewBag.ErorMessage = "User Name is Added from a while Please Enter Different User Name";
                return View(userData);
            }

            var rec = db.ProviderUserDatas.FirstOrDefault(d => d.UserID == userData.UserID);

            userData.IPAddress = rec.IPAddress;
            userData.CreateDate = rec.CreateDate;
            userData.LastLoginDate = rec.LastLoginDate;
            userData.IsFirstLogin = rec.IsFirstLogin;
            userData.InvalidPasswordAttempts = rec.InvalidPasswordAttempts;
            userData.UserTypeID = rec.UserTypeID;
            userData.UserTypeCustomerID = rec.UserTypeCustomerID;
            userData.IsLockedOut = rec.IsLockedOut;

            //userData.UserPassword = rec.UserPassword;
            //userData.confUserPassword = rec.UserPassword;

            string userImg = rec.UserImage;

            if (ModelState.IsValid)
            {

                string path = userImg;
                if (imgfile != null)
                {
                    if (imgfile.FileName.Length > 0)
                    {
                        path = "~/Images/" + Path.GetFileName(imgfile.FileName);
                        imgfile.SaveAs(Server.MapPath(path));
                    }
                }

                userData.UserImage = path;

                using (var database = new MedInsuranceProEntities())
                {
                    var foo = database.ProviderUserDatas.Where(f => f.UserID == userData.UserID).FirstOrDefault();
                    foo.UserTypeID = userData.UserTypeID;
                    foo.UserTypeCustomerID = userData.UserTypeCustomerID;
                    foo.UserName = userData.UserName;
                    foo.FirstName = userData.FirstName;
                    foo.LastName = userData.LastName;
                    foo.UserEmail = userData.UserEmail;
                    foo.IsLockedOut = userData.IsLockedOut;
                    foo.UserImage = userData.UserImage;
                    foo.IsAdmin = userData.IsAdmin;
                    foo.UserPassword = userData.UserPassword;
                    foo.confUserPassword = foo.UserPassword;
                    database.SaveChanges();
                }
                //db.Entry(userData).State = EntityState.Modified;
                //db.SaveChanges();
                //return RedirectToAction("Index");
                return RedirectToAction("login", "ClaimForm");
            }
            return View(userData);
        }

        // GET: UserDatas/Delete/5
        public ActionResult Delete(int? id)
        {
            if ((Session["UserID"] == null) || (bool.Parse(Session["IsAdmin"].ToString()) == false))
            {
                return RedirectToAction("login", "ClaimForm");
            }
            if (id == null)
            {
                return new HttpStatusCodeResult(HttpStatusCode.BadRequest);
            }
            UserData userData = db.UserDatas.Find(id);
            if (userData == null)
            {
                return HttpNotFound();
            }
            return View(userData);
        }

        // POST: UserDatas/Delete/5
        [HttpPost, ActionName("Delete")]
        [ValidateAntiForgeryToken]
        public ActionResult DeleteConfirmed(int id)
        {
            UserData userData = db.UserDatas.Find(id);
            db.UserDatas.Remove(userData);
            db.SaveChanges();
            return RedirectToAction("Index");
        }

        protected override void Dispose(bool disposing)
        {
            if (disposing)
            {
                db.Dispose();
            }
            base.Dispose(disposing);
        }
    }
}
