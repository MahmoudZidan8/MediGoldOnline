using System;
using System.Collections.Generic;
using System.Data;
using System.Data.Entity;
using System.Linq;
using System.Web;
using System.Web.Mvc;
using GuardianOnline.Infrastructure;
using System.Collections;
using System.Threading.Tasks;
using System.IO;
using Microsoft.Reporting.WebForms;
using System.Globalization;

using PdfSharp;
using PdfSharp.Pdf;
using PdfSharp.Pdf.IO;
using System.IO.Compression;

namespace Guardian.Controllers.Claims
{
    public class ClaimFormController : Controller
    {

        private MedInsuranceProEntities db = new MedInsuranceProEntities();

        // GET: ClaimForm
        public ActionResult Index()
        {
            if ((Session["UserID"] == null) || (int.Parse(Session["IsAdmin"].ToString()) == 0))
            {
                return RedirectToAction("login", "ClaimForm");
            }
            return View();
        }

        [HttpGet]
        public ActionResult login()
        {
            Session["providerid"] = null;
            Session["BranchID"] = null;
            Session["UserID"] = null;
            Session["FirstName"] = null;
            Session["LastName"] = null;
            Session["UserImage"] = null;
            Session["IsAdmin"] = null;
            Session["Providername"] =null;
            Session["branchName"] = null;
            Session["branchCode"] = null;
            Session["ProviderAdmin"] = null;
            Session["UserqrImage"] = null;


            Session["UploadMember_Claim"] = null;
            Session["Medical_Services"] = null;
            Session["ClaimForm_Approved"] = null;
            Session["Approval"] = null;
            Session["Expanded_Services"] = null;
            Session["Payment_Inquiry"] = null;
            Session["Invoices_Audit"] = null;
            Session["Doctor_ClaimForm"] = null;

            return View();
        }
        [HttpPost]
        public ActionResult login(PVProviderData log)
        {

            var rec = db.PVProviderDatas.Where(x => x.ProviderCode == log.ProviderCode /*&& x.ProviderCode == log.ProviderPassword*/).ToList().FirstOrDefault();

            if (rec != null)
            {

                Session["providerid"] = rec.ProviderID;
                //Session["Loginenterid"] = rec.LoginEnterId;
                return RedirectToAction("Claimsform");
            }
            else
            {
                ViewBag.error = "invalid user";
                return View(log);

            }

        }

        public ActionResult LoginPost(string user, string password)
        {
            var rec = db.ProviderUserDatas.Where(x => x.UserName == user && x.UserPassword == password && x.IsLockedOut==false).FirstOrDefault();
            if (rec != null)
            {
                int Provider_ID = rec.ProviderID;
                int Branch_ID = rec.BranchID;

                var Provider_name = (from values in db.PVProviderDatas
                                     where values.ProviderID == Provider_ID
                                     select new { values.ProviderName,values.ProviderAddress ,values.ProviderCode,values.QRcodePath }).FirstOrDefault();


                var Branch_name = (from values in db.PVBranchDatas
                                   where values.ProviderID == Provider_ID && values.BranchID == Branch_ID
                                   select new { values.Address ,values.BranchCode,values.QRcodePath}).FirstOrDefault();
                var branchNamee = "";
                var branchCodee = "";
                var branchQRCodee = "";
                if (Branch_name == null)
                {
                    branchNamee = Provider_name.ProviderAddress;
                    branchCodee = Provider_name.ProviderCode;
                    branchQRCodee = Provider_name.QRcodePath;
                }
                else
                {
                    branchNamee = Branch_name.Address;
                    branchCodee = Branch_name.BranchCode;
                    branchQRCodee = Branch_name.QRcodePath;
                }

                Session["Providername"] = Provider_name.ProviderName;
                Session["branchName"] = branchNamee;
                Session["branchCode"] = branchCodee;
                string UserQRImag = branchQRCodee;
                string UserQRImage = UserQRImag.Replace("~", "");
                Session["UserqrImage"] = UserQRImage;


                Session["providerid"] = rec.ProviderID;

                Session["BranchID"] = rec.BranchID;
                Session["UserID"] = rec.UserID;
                Session["FirstName"] = rec.FirstName;
                Session["LastName"] = rec.LastName;
                string UserImag = rec.UserImage;
                string UserImage = UserImag.Replace("~", "");
                Session["UserImage"] = UserImage;

                Session["IsAdmin"] = rec.IsAdmin;
                Session["ProviderAdmin"] = rec.ProviderAdmin;

                var user_roles = db.ProviderUserRolesDatas.Where(x => x.UserID == rec.UserID).OrderBy(d=>d.UserRoleID).ToList();
                if (user_roles.Count != 0)
                {
                    Session["UploadMember_Claim"] = user_roles[0].ActiveStatus;
                    Session["Medical_Services"] = user_roles[1].ActiveStatus;
                    Session["ClaimForm_Approved"] = user_roles[2].ActiveStatus;
                    Session["Expanded_Services"] = user_roles[3].ActiveStatus;
                    Session["Payment_Inquiry"] = user_roles[4].ActiveStatus;
                    Session["Invoices_Audit"] = user_roles[5].ActiveStatus;
                    Session["Approval"] = user_roles[6].ActiveStatus;
                    Session["Doctor_ClaimForm"] = user_roles[7].ActiveStatus;

                }
                return Json("1", JsonRequestBehavior.AllowGet);
            }
            else
            {
                return Json("", JsonRequestBehavior.AllowGet);

            }

        }
        public ActionResult CheckIfSessionValid()
        {
            if (Session["UserID"] == null)
            {
                return Json("False");
            }

            return Json("True");
        }

        public ActionResult DoctorHome()
        {
            if ((Session["UserID"] == null))
            {
                return RedirectToAction("login", "ClaimForm");
            }

            //int providerid = int.Parse(Session["providerid"].ToString());
            //var rec = db.PVProviderDatas.FirstOrDefault(d => d.ProviderID == providerid);
            //if (rec != null)
            //{
            //    var provider_name = rec.ProviderName;
            //    ViewBag.scn = provider_name;
            //}
            //else
            //{
            //    ViewBag.scn = "";

            //}

            return View();
        }
        [HttpGet]
        public ActionResult UploadMemberClaim(string Card_numbere)
        {
            ViewBag.card_numberqq = Card_numbere;
            //if ((Session["UserID"] == null) || (int.Parse(Session["UploadMember_Claim"].ToString()) == 0))
            //{
            //    return RedirectToAction("login", "ClaimForm");
            //}
            return View();
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public ActionResult UploadMemberClaim([Bind(Include = "MemberClaimformID,ClaimFormImage,ClaimFormNumber,CardNumber,EntryDate,EntryID,MemberClaimformStatus")] MemberClaimForm memberClaimForm, HttpPostedFileBase imgfile)
        {

            long claimformnumber = memberClaimForm.ClaimFormNumber;
            var providernam = (from values in db.PVProviderClaimBooks
                               join o in db.PVProviderDatas on values.ProviderID equals o.ProviderID
                               where values.FromSerial <= claimformnumber && values.ToSerial >= claimformnumber
                               orderby values.FromSerial descending
                               select new { o.ProviderName, o.ProviderID }).ToList();
            if (providernam.Count == 0)
            {
                var approvalselcted = db.CLMApprovals.FirstOrDefault(n => n.ApprovalCode == claimformnumber && n.ApprovalStatus == 1);
                if (approvalselcted == null)
                {
                    ViewBag.ErorMessage = "Claim Form Number is not Correct";
                    return View(memberClaimForm);
                }
            }

            int providerid = int.Parse(Session["providerid"].ToString());
            memberClaimForm.EntryID = providerid;

            memberClaimForm.EntryDate = DateTime.Now;

            var providerdataa = db.PVProviderDatas.FirstOrDefault(d => d.ProviderID == providerid);
            var providercatt = providerdataa.ProviderCatID;
            //int? provcatid = 0;
            //if ((providercatt == 1) || (providercatt == 5) || (providercatt == 6) || (providercatt == 7) || (providercatt == 8) || (providercatt == 9))
            //{
            //    provcatid = 1;
            //}
            //else
            //{
            //    provcatid = providercatt;
            //}
            memberClaimForm.ProviderCat = providercatt;

            var a = db.MemberClaimForms.FirstOrDefault(n => n.ClaimFormNumber == claimformnumber /*&&n.ProviderCat== provcatid*/);
            if (a != null)
            {
                ViewBag.ErorMessage = "Claim Form Number Already enterd from A While";
                return View(memberClaimForm);

            }

            string card_number = memberClaimForm.CardNumber;

            var result1 = (
from item1 in db.ContractMembers
join item2 in db.Contracts on item1.ContractID equals item2.ContractID into finalGroup0
from finalItem0 in finalGroup0.DefaultIfEmpty()
where finalItem0.IsActive == true && item1.CardNumber == card_number && item1.MemberActivityStatusID == 1
select new { item1.CardNumber, item1.ContractMemberID }).OrderByDescending(d => d.ContractMemberID).FirstOrDefault();

            if (result1 == null)
            {
                ViewBag.ErorMessage = "Card Number is not Correct";
                return View(memberClaimForm);

            }
            memberClaimForm.MemberClaimformStatus = 1;
            if (ModelState.IsValid)
            {
                string path = "";
                if (imgfile.FileName.Length > 0)
                {

                    path = "~/Images/" + Path.GetFileName(claimformnumber+imgfile.FileName);
                    imgfile.SaveAs(Server.MapPath(path));
                    string saveDirectory = @"C:\Publish\Guardian\Images\";
                //string saveDirectory = @"E:\PubishGuardianFinal\Guardian\Images\";
               
                    string secondpath = saveDirectory + Path.GetFileName(claimformnumber+imgfile.FileName);
                    //aaa= Server.MapPath(path);
                   
                imgfile.SaveAs(secondpath);
                }
                memberClaimForm.ClaimFormImage = path;



                db.MemberClaimForms.Add(memberClaimForm);
                db.SaveChanges();

                //ViewBag.error = aaa;
                var lastmemberclaimform = db.MemberClaimForms.Where(d => d.ClaimFormNumber == memberClaimForm.ClaimFormNumber && d.EntryID == memberClaimForm.EntryID && d.CardNumber == memberClaimForm.CardNumber).OrderByDescending(d=>d.MemberClaimformID).FirstOrDefault();
                var lastmemberclaimformid = lastmemberclaimform.MemberClaimformID;
                List<ProviderClaimRole> providerClaimRole = new List<ProviderClaimRole>();

                for(int i=1;i<=9;i++)
                {
                    ProviderClaimRole PCR = new ProviderClaimRole();
                    PCR.MemberClaimformID = lastmemberclaimformid;
                    PCR.ProviderCatId = i;
                    PCR.ISActive = true;
                    providerClaimRole.Add(PCR);
                }
                using (var context = new MedInsuranceProEntities())
                {
                    context.ProviderClaimRoles.AddRange(providerClaimRole);
                    context.SaveChanges();
                }

                int ProviderId = int.Parse(Session["providerid"].ToString());
                int BranchId = int.Parse(Session["BranchID"].ToString());

                var Lastmemberrequestdate = db.SmartCardRequestDatas.Where(d => d.CardNumber == card_number && d.SmartCardRequestStatus == 1 && d.ProviderID == ProviderId && d.BranchID == BranchId).OrderByDescending(d => d.SmartCardRequestID).FirstOrDefault();
                if (Lastmemberrequestdate != null)
                {
                    var lastDateRequest = Lastmemberrequestdate.EntryDate;
                    var datetimeActive = DateTime.Now.AddDays(-7);
                    if (lastDateRequest > datetimeActive)
                    {
                        var Member_Requests = (from values in db.SmartCardRequestDatas
                                                where values.CardNumber == card_number && values.SmartCardRequestStatus == 1 && values.ProviderID == ProviderId && values.BranchID == BranchId 
                                                orderby values.SmartCardRequestID descending
                                                select values.CardNumber + " ## " + values.EntryDate.ToString() + " ## " + values.SmartCardRequestID).FirstOrDefault();

                        //var Member_Requests = Lastmemberrequestdate.CardNumber + " ## " + Lastmemberrequestdate.EntryDate.ToString() + " ## " + Lastmemberrequestdate.SmartCardRequestID;
                        return RedirectToAction("MemberSmartCardRequests", "ClaimForm", new { Member_Request = Member_Requests, claim_formnumber = claimformnumber });

                    }
                }

                    return RedirectToAction("ClaimServices", "ClaimForm", new { card_number = card_number, claim_formnumber= claimformnumber } );
               // return RedirectToAction("DoctorHome");
                //return View(memberClaimForm);

            }

            return View(memberClaimForm);
        }
        public ActionResult Get_ClaimFormImage(string id, int CmcTriger)
        {
            try
            {
                long claimformnumber = 0;

                MemberClaimForm claimform_member = new MemberClaimForm();
                if (CmcTriger == 2)
                {
                    claimformnumber = long.Parse(id);
                    //int providerid1 = int.Parse(Session["providerid"].ToString());
                    //var providerdataa = db.PVProviderDatas.FirstOrDefault(d => d.ProviderID == providerid1);
                    //var providercatt = providerdataa.ProviderCatID;
                    //int? provcatid = 0;
                    //if ((providercatt == 1) || (providercatt == 5) || (providercatt == 6) || (providercatt == 7) || (providercatt == 8) || (providercatt == 9))
                    //{
                    //    provcatid = 1;
                    //}
                    //else
                    //{
                    //    provcatid = providercatt;
                    //}
                    claimform_member = db.MemberClaimForms.FirstOrDefault(d => d.ClaimFormNumber == claimformnumber && d.MemberClaimformStatus == 1 /*&& d.ProviderCat == provcatid*/);

                }
                else if (CmcTriger == 4)
                {
                    claimformnumber = long.Parse(id);
                    claimform_member = db.MemberClaimForms.FirstOrDefault(d => d.ClaimFormNumber == claimformnumber && d.MemberClaimformStatus == 1);

                }
                else if (CmcTriger == 5)
                {
                    claimformnumber = long.Parse(id);
                    claimform_member = db.MemberClaimForms.FirstOrDefault(d => d.ClaimFormNumber == claimformnumber);

                }
                else if (CmcTriger == 3)
                {

                    String[] spearator = { " ## ", " ## " };
                    Int32 count = 3;
                    String[] strlist = id.Split(spearator, count,
                           StringSplitOptions.RemoveEmptyEntries);
                    long claimformnum = long.Parse(strlist[0]);
                    string cardnumber = strlist[1];
                    int claimid = int.Parse(strlist[2]);
                    claimformnumber = claimformnum;


                    //var providerdataa = db.CLMClaimsONLINEs.FirstOrDefault(d => d.ClaimID == claimid);
                    //int? providerid1 = providerdataa.ProviderID;
                    //var providercatt = providerdataa.ProviderCatID;
                    //int? provcatid = 0;
                    //if ((providercatt == 1) || (providercatt == 5) || (providercatt == 6) || (providercatt == 7) || (providercatt == 8) || (providercatt == 9))
                    //{
                    //    provcatid = 1;
                    //}
                    //else
                    //{
                    //    provcatid = providercatt;
                    //}

                    claimform_member = db.MemberClaimForms.FirstOrDefault(d => d.ClaimFormNumber == claimformnumber && d.MemberClaimformStatus == 1 /*&& d.ProviderCat == provcatid*/);
                }
                else
                {

                    String[] spearator = { " ## ", " ## ", " ## " };
                    Int32 count = 4;
                    String[] strlist = id.Split(spearator, count,
                           StringSplitOptions.RemoveEmptyEntries);
                    long claimformnum = long.Parse(strlist[0]);
                    string cardnumber = strlist[1];
                    int claimid = int.Parse(strlist[2]);
                    string Provider_Name = strlist[3];
                    claimformnumber = claimformnum;


                    //var providerdataa = db.CLMClaimsONLINEs.FirstOrDefault(d => d.ClaimID == claimid);
                    //int? providerid1 = providerdataa.ProviderID;
                    //var providercatt = providerdataa.ProviderCatID;
                    //int? provcatid = 0;
                    //if ((providercatt == 1) || (providercatt == 5) || (providercatt == 6) || (providercatt == 7) || (providercatt == 8) || (providercatt == 9))
                    //{
                    //    provcatid = 1;
                    //}
                    //else
                    //{
                    //    provcatid = providercatt;
                    //}

                    claimform_member = db.MemberClaimForms.FirstOrDefault(d => d.ClaimFormNumber == claimformnumber && d.MemberClaimformStatus == 1 /*&& d.ProviderCat == provcatid*/);
                }
                ArrayList arr_lis = new ArrayList();
                var ClaimIDDDD = 0;

                if (claimform_member == null)
                {
                    arr_lis.Add("WithoutImage");

                    //return Json("", JsonRequestBehavior.AllowGet);
                }
                else
                {
                    string claimform_image = claimform_member.ClaimFormImage;
                    string n = claimform_image.Replace("~", "..");
                    arr_lis.Add(n);
                }
                if (CmcTriger == 2)
                {
                    int providerid = int.Parse(Session["providerid"].ToString());
                    var pvprovData = db.PVProviderDatas.FirstOrDefault(d => d.ProviderID == providerid);
                    int? provCatId = pvprovData.ProviderCatID;

                    //var res= (from values in db.CLMClaims
                    //          join item1 in db.CLMClaimItems on values.ClaimID equals item1.ClaimID
                    //          join item in db.PVProviderDatas on values.ProviderID equals item.ProviderID
                    //          where values.ClaimFormNum == claimformnumber && item.ProviderCatID== provCatId
                    //          select new { item1.ServiceName, item1.ServiceQnt }).ToList();

                    //int? provcatid = 0;
                    //if ((provCatId == 1) || (provCatId == 5) || (provCatId == 6) || (provCatId == 7) || (provCatId == 8) || (provCatId == 9))
                    //{
                    //    provcatid = 1;
                    //}
                    //else
                    //{
                    //    provcatid = provCatId;
                    //}
                    var claimitems_Active = (from values in db.CLMClaimsONLINEs
                                             join item1 in db.CLMClaimsItemsONLINEs on values.ClaimID equals item1.ClaimID
                                             where values.ClaimFormNum == claimformnumber && item1.ClaimItemStatusID == 1 && values.ProviderCatID == provCatId/*values.ProviderID == providerid*/
                                             select new { item1.ServiceName, item1.ServiceQnt, item1.ClaimItemID }).OrderBy(d => d.ClaimItemID).ToList();

                    arr_lis.Add(claimitems_Active);

                    var claimitems_Stoped = (from values in db.CLMClaimsONLINEs
                                             join item1 in db.CLMClaimsItemsONLINEs on values.ClaimID equals item1.ClaimID
                                             where values.ClaimFormNum == claimformnumber && item1.ClaimItemStatusID == 2 && values.ProviderCatID == provCatId/*values.ProviderID == providerid*/
                                             select new { item1.ServiceName, item1.ServiceQnt, item1.Refuse_Note, item1.ClaimItemID }).OrderBy(d => d.ClaimItemID).ToList();

                    arr_lis.Add(claimitems_Stoped);

                    var claimitems_Done = (from values in db.CLMClaimsONLINEs
                                           join item1 in db.CLMClaimsItemsONLINEs on values.ClaimID equals item1.ClaimID
                                           where values.ClaimFormNum == claimformnumber && item1.ClaimItemStatusID == 3 && values.ProviderCatID == provCatId/*values.ProviderID == providerid*/
                                           select new { item1.ServiceName, item1.ServiceQnt, item1.Refuse_Note, item1.ClaimItemID }).OrderBy(d => d.ClaimItemID).ToList();

                    arr_lis.Add(claimitems_Done);

                }
                else if (CmcTriger == 4)
                {

                    var claimitems_Active = (from values in db.CLMClaimsONLINEs
                                             join item1 in db.CLMClaimsItemsONLINEs on values.ClaimID equals item1.ClaimID
                                             where values.ClaimFormNum == claimformnumber && item1.ClaimItemStatusID == 1
                                             select new { item1.ServiceName, item1.ServiceQnt, item1.Refuse_Note, item1.ClaimItemID }).OrderBy(d => d.ClaimItemID).ToList();

                    arr_lis.Add(claimitems_Active);

                    var claimitems_Stoped = (from values in db.CLMClaimsONLINEs
                                             join item1 in db.CLMClaimsItemsONLINEs on values.ClaimID equals item1.ClaimID
                                             where values.ClaimFormNum == claimformnumber && item1.ClaimItemStatusID == 2
                                             select new { item1.ServiceName, item1.ServiceQnt, item1.Refuse_Note, item1.ClaimItemID }).OrderBy(d => d.ClaimItemID).ToList();

                    arr_lis.Add(claimitems_Stoped);

                    var claimitems_Done = (from values in db.CLMClaimsONLINEs
                                           join item1 in db.CLMClaimsItemsONLINEs on values.ClaimID equals item1.ClaimID
                                           where values.ClaimFormNum == claimformnumber && item1.ClaimItemStatusID == 3
                                           select new { item1.ServiceName, item1.ServiceQnt, item1.Refuse_Note, item1.ClaimItemID }).OrderBy(d => d.ClaimItemID).ToList();

                    arr_lis.Add(claimitems_Done);

                }
                else if (CmcTriger == 5)
                {

                    var claimitems_Active = (from values in db.CLMClaimsONLINEs
                                             join item1 in db.CLMClaimsItemsONLINEs on values.ClaimID equals item1.ClaimID
                                             where values.ClaimFormNum == claimformnumber && item1.ClaimItemStatusID == 1
                                             select new { item1.ServiceName, item1.ServiceQnt, item1.Refuse_Note, item1.ServicePrice, item1.ServiceAmt, item1.ClaimItemID }).OrderBy(d => d.ClaimItemID).ToList();

                    arr_lis.Add(claimitems_Active);

                    var claimitems_Stoped = (from values in db.CLMClaimsONLINEs
                                             join item1 in db.CLMClaimsItemsONLINEs on values.ClaimID equals item1.ClaimID
                                             where values.ClaimFormNum == claimformnumber && item1.ClaimItemStatusID == 2
                                             select new { item1.ServiceName, item1.ServiceQnt, item1.Refuse_Note, item1.ServicePrice, item1.ServiceAmt, item1.ClaimItemID }).OrderBy(d => d.ClaimItemID).ToList();

                    arr_lis.Add(claimitems_Stoped);

                    var claimitems_Done = (from values in db.CLMClaimsONLINEs
                                           join item1 in db.CLMClaimsItemsONLINEs on values.ClaimID equals item1.ClaimID
                                           where values.ClaimFormNum == claimformnumber && item1.ClaimItemStatusID == 3
                                           select new { item1.ServiceName, item1.ServiceQnt, item1.Refuse_Note, item1.ServicePrice, item1.ServiceAmt, item1.ClaimItemID }).OrderBy(d => d.ClaimItemID).ToList();

                    arr_lis.Add(claimitems_Done);

                }
                else if (CmcTriger == 3)
                {

                    String[] spearator = { " ## ", " ## " };
                    Int32 count = 3;
                    String[] strlist = id.Split(spearator, count,
                           StringSplitOptions.RemoveEmptyEntries);
                    long claimformnum = long.Parse(strlist[0]);
                    string cardnumber = strlist[1];
                    int claimid = int.Parse(strlist[2]);
                    claimformnumber = claimformnum;
                    ClaimIDDDD = claimid;

                    //var claimitems_Active = (from values in db.CLMClaimsONLINEs
                    //                         join item1 in db.CLMClaimsItemsONLINEs on values.ClaimID equals item1.ClaimID
                    //                         where values.ClaimID == claimid && item1.ClaimItemStatusID == 1/*values.ProviderID == providerid*/
                    //                         select new { item1.ServiceName, item1.ServiceQnt, item1.ClaimItemID }).OrderBy(d => d.ClaimItemID).ToList();

                    //arr_lis.Add(claimitems_Active);

                    var claimitems_Stoped = (from values in db.CLMClaimsONLINEs
                                             join item1 in db.CLMClaimsItemsONLINEs on values.ClaimID equals item1.ClaimID
                                             where values.ClaimID == claimid && item1.ClaimItemStatusID == 2/*values.ProviderID == providerid*/
                                             select new { item1.ServiceName, item1.ServiceQnt, item1.ServicePrice, item1.ServiceAmt, item1.Refuse_Note, item1.ClaimItemID , item1.RequestedPrice }).OrderBy(d => d.ClaimItemID).ToList();

                    arr_lis.Add(claimitems_Stoped);

                    var claimitems_Done = (from values in db.CLMClaimsONLINEs
                                           join item1 in db.CLMClaimsItemsONLINEs on values.ClaimID equals item1.ClaimID
                                           where values.ClaimID == claimid && item1.ClaimItemStatusID == 3/*values.ProviderID == providerid*/
                                           select new { item1.ServiceName, item1.ServiceQnt, item1.ServicePrice, item1.ServiceAmt, item1.Refuse_Note, item1.ClaimItemID, item1.RequestedPrice }).OrderBy(d => d.ClaimItemID).ToList();

                    arr_lis.Add(claimitems_Done);
                }
                else
                {

                    String[] spearator = { " ## ", " ## ", " ## " };
                    Int32 count = 4;
                    String[] strlist = id.Split(spearator, count,
                           StringSplitOptions.RemoveEmptyEntries);
                    long claimformnum = long.Parse(strlist[0]);
                    string cardnumber = strlist[1];
                    int claimid = int.Parse(strlist[2]);
                    string Provider_Name = strlist[3];
                    claimformnumber = claimformnum;

                    var claimitems_Active = (from values in db.CLMClaimsONLINEs
                                             join item1 in db.CLMClaimsItemsONLINEs on values.ClaimID equals item1.ClaimID
                                             where values.ClaimID == claimid && item1.ClaimItemStatusID == 1/*values.ProviderID == providerid*/
                                             select new { item1.ServiceName, item1.ServiceQnt, item1.ServicePrice, item1.ServiceAmt, item1.ClaimItemID }).OrderBy(d => d.ClaimItemID).ToList();

                    arr_lis.Add(claimitems_Active);

                    var claimitems_Stoped = (from values in db.CLMClaimsONLINEs
                                             join item1 in db.CLMClaimsItemsONLINEs on values.ClaimID equals item1.ClaimID
                                             where values.ClaimFormNum == claimformnumber && item1.ClaimItemStatusID == 2/*values.ProviderID == providerid*/
                                             select new { item1.ServiceName, item1.ServiceQnt, item1.Refuse_Note, item1.ClaimItemID }).OrderBy(d => d.ClaimItemID).ToList();

                    arr_lis.Add(claimitems_Stoped);

                    var claimitems_Done = (from values in db.CLMClaimsONLINEs
                                           join item1 in db.CLMClaimsItemsONLINEs on values.ClaimID equals item1.ClaimID
                                           where values.ClaimFormNum == claimformnumber && item1.ClaimItemStatusID == 3/*values.ProviderID == providerid*/
                                           select new { item1.ServiceName, item1.ServiceQnt, item1.Refuse_Note, item1.ClaimItemID }).OrderBy(d => d.ClaimItemID).ToList();

                    arr_lis.Add(claimitems_Done);
                }
                var claimform_providerCat = db.CLMClaimsONLINEs.Where(d => d.ClaimID == ClaimIDDDD).FirstOrDefault();
                if (claimform_providerCat != null)
                {
                    var F_Diagnose = claimform_providerCat.Diagnosis;
                    var F_Icd = claimform_providerCat.ICDCode;
                    var s_Diagnose = claimform_providerCat.SecondDiagnosis;
                    var s_Icd = claimform_providerCat.SecondICDCode;
                    var t_Diagnose = claimform_providerCat.ThirdDiagnosis;
                    var t_Icd = claimform_providerCat.ThirdICDCode;
                    arr_lis.Add(F_Diagnose);
                    arr_lis.Add(F_Icd);
                    arr_lis.Add(s_Diagnose);
                    arr_lis.Add(s_Icd);
                    arr_lis.Add(t_Diagnose);
                    arr_lis.Add(t_Icd);
                }
                else
                {
                    arr_lis.Add("");
                    arr_lis.Add("");
                    arr_lis.Add("");
                    arr_lis.Add("");
                    arr_lis.Add("");
                    arr_lis.Add("");
                }
                return Json(arr_lis, JsonRequestBehavior.AllowGet);
            }
            catch
            {
                return Json("", JsonRequestBehavior.AllowGet);

            }

        }

        public JsonResult Get_Deductable(string id)
        {
            try
            {
                String[] spearator = { " ## ", " ## " };
                Int32 count = 3;
                String[] strlist = id.Split(spearator, count,
                       StringSplitOptions.RemoveEmptyEntries);
                long claimformnum = long.Parse(strlist[0]);
                string cardnumber = strlist[1];
                int claimid = int.Parse(strlist[2]);

                var claimitems_Done = (from values in db.CLMClaimsONLINEs
                                       join item1 in db.CLMClaimsItemsONLINEs on values.ClaimID equals item1.ClaimID
                                       where values.ClaimID == claimid && item1.ClaimItemStatusID == 3
                                       select item1.RequestedPrice).ToList().Sum();
                                       //select item1.ServiceAmt).ToList().Sum();

                var claimsdata = db.CLMClaimsONLINEs.FirstOrDefault(d => d.ClaimID == claimid);
                var claimservice_id = claimsdata.ClaimServiceID;
                var DatProcedure = claimsdata.ProcedureDate;
                var card_num = claimsdata.CardNumber;

                var cont_num = (
    from item1 in db.ContractMembers
    join item2 in db.Contracts on item1.ContractID equals item2.ContractID into finalGroup0
    from finalItem0 in finalGroup0.DefaultIfEmpty()
    where finalItem0.StartDate <= DatProcedure && finalItem0.EndDate >= DatProcedure && item1.CardNumber == card_num
    select new { finalItem0.ContractNumber, item1.ContractMemberID, finalItem0.ContractID, item1.PlanTypeID, finalItem0.StartDate, finalItem0.EndDate }).OrderByDescending(aa => aa.ContractID).FirstOrDefault();

                //string contract_num = cont_num[0].contractnumber;
                int contract_member_id = cont_num.ContractMemberID;
                int contract_id = cont_num.ContractID;
                int plan_type_id = cont_num.PlanTypeID;


                decimal deduct_per = 0;
                if (claimservice_id == 14 || claimservice_id == 19)
                {
                    deduct_per = 0;
                }
                else
                {
                    var re5 = db.ContractServiceCeilings.FirstOrDefault(d => d.ContractID == contract_id && d.PlanTypeID == plan_type_id && d.ServiceID == claimservice_id);
                    deduct_per = re5.DeductPer;
                }

                var tdeductamt = deduct_per * claimitems_Done;
                var arrlist = new ArrayList();
                arrlist.Add(deduct_per * 100);
                arrlist.Add(tdeductamt);
                arrlist.Add(claimitems_Done);

                return Json(arrlist, JsonRequestBehavior.AllowGet);
            }
            catch
            {
                return Json("falsee", JsonRequestBehavior.AllowGet);

            }
        }

        [HttpGet]
        public ActionResult ClaimServices(string card_number, string claim_formnumber)
        {
            ViewBag.card_numberqq = card_number;
            ViewBag.claim_formnumberqq = claim_formnumber;

            ViewBag.CardNumber = "";
            if ((Session["UserID"] == null) || (int.Parse(Session["Medical_Services"].ToString()) == 0))
            {
                return RedirectToAction("login", "ClaimForm");
            }
            return View();
        }

        public JsonResult delete_ClaimFormImage(string id)
        {
            var claimformnumber = long.Parse(id);
            var claimonlinedata = db.CLMClaimsONLINEs.FirstOrDefault(d=>d.ClaimFormNum== claimformnumber);
            if (claimonlinedata != null)
            {
                return Json("falsee", JsonRequestBehavior.AllowGet);

            }
            var MemberClaimFormsselect = db.MemberClaimForms.FirstOrDefault(d => d.ClaimFormNumber == claimformnumber);
            int providerid = int.Parse(Session["providerid"].ToString());
            var entryid = MemberClaimFormsselect.EntryID;
            if(entryid!= providerid)
            {
                return Json("falsee1", JsonRequestBehavior.AllowGet);
            }
            db.MemberClaimForms.Remove(MemberClaimFormsselect);
            db.SaveChanges();
            return Json("truee", JsonRequestBehavior.AllowGet);
        }

        public static List<CustomerClaimsInquiry> CustomerClaimsInquiries_Data_Inquiry;

        public JsonResult Get_Member_info(string id)
        {

            var datenow = DateTime.Now;

            var result1 = (
  from item1 in db.ContractMembers
  join item2 in db.Contracts on item1.ContractID equals item2.ContractID into finalGroup0
  from finalItem0 in finalGroup0.DefaultIfEmpty()
  join item22 in db.CustomerDatas on finalItem0.CustomerID equals item22.CustomerID into finalGroup00
  from finalItem00 in finalGroup00.DefaultIfEmpty()
  join item3 in db.MemberActivityStatusDatas on item1.MemberActivityStatusID equals item3.MemberActivityStatusID into finalGroup1
  from finalItem1 in finalGroup1.DefaultIfEmpty()
  join item4 in db.PlanTypeDatas on item1.PlanTypeID equals item4.PlanTypeID into finalGroup11
  from finalItem11 in finalGroup11.DefaultIfEmpty()

  where /*finalItem0.IsActive == true &&*/ item1.CardNumber == id && finalItem0.StartDate <= datenow && finalItem0.EndDate >= datenow && item1.ActiveDate <= datenow
  select new
  {
      item1.MemberName,
      item1.CardNumber,
      finalItem00.CustomerName,
      finalItem1.MemberActivityStatusName,
      item1.SexType,
      item1.ActiveDate,
      item1.DeActiveDate,
      item1.MemberAge,
      item1.MemberBirthDate,
      finalItem0.ContractNumber,
      finalItem11.PlanTypeName,

      finalItem0.ContractID,
      finalItem11.PlanTypeID,

      finalItem0.StartDate,
      finalItem0.EndDate,

      finalItem0.CancelDate,
      finalItem0.IsActive,

      finalItem0.ContractTypeID,
      item1.IsOverCeiling

  }).OrderByDescending(a => a.ContractID).FirstOrDefault();

            var arlist = new ArrayList();
            if (result1 != null)
            {

                arlist.Add(result1.MemberName);
                arlist.Add(result1.CustomerName);
                arlist.Add(result1.CardNumber);
                arlist.Add(result1.ContractNumber);
                arlist.Add(result1.PlanTypeName);
                arlist.Add(result1.ActiveDate);
                arlist.Add(result1.MemberActivityStatusName);
                arlist.Add(result1.DeActiveDate);
                arlist.Add(result1.MemberBirthDate);
                arlist.Add(result1.MemberAge);
                arlist.Add(result1.SexType);

                var contract_idd = result1.ContractID;
                var contract_Numberr = result1.ContractNumber;
                var PlanType_IDD = result1.PlanTypeID;
                var Card_Number = result1.CardNumber;
                var contractNumber = result1.ContractNumber;

                var CustomerPremiumAmt_result = (from c in db.LQMemberAddDeletes
                                                 where c.ContractID == contract_idd
                                                 select c).AsNoTracking().ToList();

                var MemberPermuim_Amt = CustomerPremiumAmt_result.Sum(d => d.MemberPermuimAmt);
                var Credit_Amt = CustomerPremiumAmt_result.Sum(d => d.CreditAmt);
                var CustomerPremium_Amt = MemberPermuim_Amt - Credit_Amt;


                var CustomerClaimsData = (from c in db.CustomerClaimsInquiryNews
                                          where c.ContractID == contract_idd && c.ServiceName != "Exceed Limit" && c.ServiceName != "Excluded Cost" && c.ServiceName != "Humanitarian Fund"

                                          select c).AsNoTracking().ToList();

                var Customer_TotalDueAmt = CustomerClaimsData.Sum(d => d.TotalDueAmt);
                //var Customer_TotalDueAmt = CustomerClaimsData.Where(dd => dd.ServiceName != "Exceed Limit" && dd.ServiceName != "Excluded Cost").Sum(d => d.TotalDueAmt);

                //CustomerClaimsInquiries_Data_Inquiry = CustomerClaimsData;

    //            DateTime Exact3Months = DateTime.Now.AddMonths(-3);

    //            var ServicesUti = (
    //   from item1 in db.CLMClaimItems
    //   join item2 in db.CLMClaims on item1.ClaimID equals item2.ClaimID into finalGroup0
    //   from finalItem0 in finalGroup0.DefaultIfEmpty()
    //   join item3 in db.ContractMembers on finalItem0.ContractMemberID equals item3.ContractMemberID into finalGroup1
    //   from finalItem1 in finalGroup1.DefaultIfEmpty()
    //   join item33 in db.Contracts on finalItem1.ContractID equals item33.ContractID into finalGroup11
    //   from finalItem11 in finalGroup11.DefaultIfEmpty()
    //   where finalItem11.ContractNumber == contractNumber && finalItem0.ClaimServiceID != 14 && finalItem0.ClaimServiceID != 19 && finalItem0.ClaimServiceID != 15
    //   select item1.NetPaymentAmt).ToList().Sum();


    //            var ApprovalsUti = (
    //from item1 in db.CLMApprovals
    //join item2 in db.CLMApprovalItems on item1.ApprovalID equals item2.ApprovalID
    //join item3 in db.ContractMembers on item1.ContractMemberID equals item3.ContractMemberID into finalGroup1
    //from finalItem1 in finalGroup1.DefaultIfEmpty()
    //join item33 in db.Contracts on finalItem1.ContractID equals item33.ContractID into finalGroup11
    //from finalItem11 in finalGroup11.DefaultIfEmpty()
    //where finalItem11.ContractNumber == contractNumber && item2.CLaimServiceID != 14 && item2.CLaimServiceID != 19 && item2.CLaimServiceID != 15
    //&& item1.ApprovalDate >= Exact3Months && item1.ApprovalStatus == 1 && !db.CLMClaims.Any(d => d.ClaimFormNum == item1.ApprovalCode)
    //select item2.ServiceAmt).ToList().Sum();

    //            var Customer_TotalDueAmt = ServicesUti + ApprovalsUti;
                //  CustomerClaimsInquiry.ContractID = ContractMembers.ContractID and ContractServiceCeiling.CeilingTypeID is not null


                //var Customer_TotalDueAmt = (from c in db.CustomerClaimsInquiries
                //                             join item in db.ContractMembers on c.CardNumber equals item.CardNumber
                //                             join item1 in db.LUServices on c.ServiceName equals item1.ServiceName
                //                             join item2 in db.ContractServiceCeilings on c.ContractID equals item2.ContractID

                //                             where c.ContractID == contract_idd &&item2.ServiceID==item1.ServiceID &&item2.PlanTypeID== item.PlanTypeID
                //                             && item2.CeilingTypeID == 1 && c.ContractID == item.ContractID /*&& item2.CeilingTypeID !=null */

                //                             select c.TotalDueAmt).Sum();




                var AnnualCeilinggg = (from c in db.ContractPlanCeilings
                                       where c.ContractID == contract_idd && c.PlanTypeID == PlanType_IDD
                                       select c.AnnualCeiling).FirstOrDefault();

                var Member_TotalDueAmt = CustomerClaimsData.Where(a => a.CardNumber == Card_Number).Sum(d => d.TotalDueAmt);

    //            var ServicesUtiu = (
    //   from item1 in db.CLMClaimItems
    //   join item2 in db.CLMClaims on item1.ClaimID equals item2.ClaimID into finalGroup0
    //   from finalItem0 in finalGroup0.DefaultIfEmpty()
    //   join item3 in db.ContractMembers on finalItem0.ContractMemberID equals item3.ContractMemberID into finalGroup1
    //   from finalItem1 in finalGroup1.DefaultIfEmpty()
    //   join item33 in db.Contracts on finalItem1.ContractID equals item33.ContractID into finalGroup11
    //   from finalItem11 in finalGroup11.DefaultIfEmpty()
    //   where finalItem11.ContractNumber == contractNumber && finalItem0.CardNumber == Card_Number && finalItem0.ClaimServiceID != 14 && finalItem0.ClaimServiceID != 19 && finalItem0.ClaimServiceID != 15
    //   select item1.NetPaymentAmt).ToList().Sum();


    //            var ApprovalsUtiu = (
    //from item1 in db.CLMApprovals
    //join item2 in db.CLMApprovalItems on item1.ApprovalID equals item2.ApprovalID

    //join item3 in db.ContractMembers on item1.ContractMemberID equals item3.ContractMemberID into finalGroup1
    //from finalItem1 in finalGroup1.DefaultIfEmpty()
    //join item33 in db.Contracts on finalItem1.ContractID equals item33.ContractID into finalGroup11
    //from finalItem11 in finalGroup11.DefaultIfEmpty()
    //where finalItem11.ContractNumber == contractNumber && item1.CardNumber == Card_Number && item2.CLaimServiceID != 14 && item2.CLaimServiceID != 19 && item2.CLaimServiceID != 15
    //&& item1.ApprovalDate >= Exact3Months && item1.ApprovalStatus == 1 && !db.CLMClaims.Any(d => d.ClaimFormNum == item1.ApprovalCode)
    //select item2.ServiceAmt).ToList().Sum();

    //            var Member_TotalDueAmt = ServicesUtiu + ApprovalsUtiu;
                double perc = 0;
                if (CustomerPremium_Amt != 0)
                {
                    var diva = (Customer_TotalDueAmt / CustomerPremium_Amt);
                    var divb = double.Parse(diva.ToString());
                    perc = Math.Round(divb, 2);

                }
                //var expList = new ArrayList();
                if (CustomerPremium_Amt <= Customer_TotalDueAmt)
                {
                    arlist.Add("1");

                }
                else if (AnnualCeilinggg <= Member_TotalDueAmt)
                {
                    arlist.Add("2");
                }
                else if ((perc) >= (0.90))
                {
                    arlist.Add("11");
                }
                else
                {
                    arlist.Add("0");
                }
                arlist.Add(CustomerPremium_Amt);
                arlist.Add(Customer_TotalDueAmt);
                arlist.Add(AnnualCeilinggg);
                arlist.Add(Member_TotalDueAmt);

                arlist.Add(result1.StartDate);
                arlist.Add(result1.EndDate);
                if ((result1.EndDate < DateTime.Now)|| (result1.CancelDate < DateTime.Now)|| (result1.IsActive == false)|| (result1.MemberActivityStatusName == "Stopped"))
                {
                    arlist.Add("1");

                }
                else
                {
                    arlist.Add("0");

                }


                arlist.Add(result1.ContractTypeID);

                arlist.Add(result1.IsOverCeiling);

                arlist.Add(result1.CancelDate);
                if (result1.IsActive == true)
                {
                    arlist.Add("1");
                }
                else
                {
                    arlist.Add("0");
                }
                var DeductabelPer = (from c in db.ContractServiceCeilings
                                       where c.ContractID == contract_idd && c.PlanTypeID == PlanType_IDD&& c.ServiceID == 25
                                       select c.DeductPer).FirstOrDefault();

                arlist.Add(DeductabelPer*100);

                //arlist.Add(expList);
            }
            return Json(arlist, JsonRequestBehavior.AllowGet);
        }


        public JsonResult select_members(string id)
        {
            //var dat = db.ContractMembers.Where(s => s.MemberName.ToLower().Contains(id)).ToList();
            var dat = (from values in db.ContractMembers
                       join item in db.Contracts on values.ContractID equals item.ContractID
                       where values.MemberName.ToLower().Contains(id) && item.IsActive == true
                       select values.MemberName).AsNoTracking().Distinct().ToList();

            var arrl = new ArrayList();
            arrl.Add(dat);
            var card_number = db.ContractMembers.Where(d => d.MemberName == id).OrderByDescending(m => m.ContractMemberID).FirstOrDefault();
            if (card_number != null)
            {
                var car_num = card_number.CardNumber;
                arrl.Add(car_num);
            }
            else
            {
                arrl.Add("a");

            }


            return Json(arrl, JsonRequestBehavior.AllowGet);
        }

        public JsonResult Get_MemberClaimForms(string id)
        {
            //int providerid = int.Parse(Session["providerid"].ToString());
            //var providerdataa = db.PVProviderDatas.FirstOrDefault(d => d.ProviderID == providerid);
            //var providercatt = providerdataa.ProviderCatID;
            //int? provcatid = 0;
            //if ((providercatt == 1) || (providercatt == 5) || (providercatt == 6) || (providercatt == 7) || (providercatt == 8) || (providercatt == 9))
            //{
            //    provcatid = 1;
            //}
            //else
            //{
            //    provcatid = providercatt;
            //}
            //var member_claimform = db.MemberClaimForms.Where(d => d.CardNumber == id && d.MemberClaimformStatus==1).ToList();
            //var member_claimform = db.MemberClaimForms.Where(d => d.CardNumber == id && d.MemberClaimformStatus == 1/*&& d.ProviderCat== provcatid*/).OrderBy(d=>d.MemberClaimformID).ToList();

            int providerid = int.Parse(Session["providerid"].ToString());
            var pvprovData = db.PVProviderDatas.Where(d => d.ProviderID == providerid).OrderByDescending(d=>d.ProviderID).FirstOrDefault();
            int? provCatId = pvprovData.ProviderCatID;

            var member_claimform =
            (
                from values in db.MemberClaimForms
                join item in db.ProviderClaimRoles on values.MemberClaimformID equals item.MemberClaimformID
                where values.CardNumber == id && values.MemberClaimformStatus == 1 && item.ProviderCatId == provCatId && item.ISActive == true
                select values
            ).ToList();

            return Json(member_claimform, JsonRequestBehavior.AllowGet);
        }

        public JsonResult Check_ServiceName(string id)
        {
            List<string> servicess_data = new List<string>();
            int providerid = int.Parse(Session["providerid"].ToString());
            var rec = db.PVProviderDatas.FirstOrDefault(d => d.ProviderID == providerid);
            var providercategoryid = rec.ProviderCatID;
            string Outp = "";

            if (providercategoryid == 2)
            {
                //var drug = (from values in db.PVMedDatas
                //            select values.MedDrugName).ToList();
                //servicess_data = drug;
                var dat = db.PVMedDatas.FirstOrDefault(d => d.MedDrugName.Replace(" ", string.Empty) == (id.Replace(" ", string.Empty)));
                //if (dat==null)
                //{
                //    var re2 = db.PVMedDatas.FirstOrDefault(d => d.MedDrugName.Replace(" ", string.Empty).Contains(id.Replace(" ", string.Empty)));
                //    dat = re2;
                //}
                //var dat = db.PVMedDatas.Where(s => s.MedDrugName == id).FirstOrDefault();
                if (dat != null)
                {
                    Outp = "1";
                }
            }
            else
            {
                DateTime noww = DateTime.Now;
                var re3 = db.PVContracts
                                 .Where(m => m.ProviderID == providerid && m.StartDate <= noww && m.EndDate >= noww)
                                 .OrderByDescending(m => m.ProviderContractID)
                                 .FirstOrDefault();
                if (re3 == null)
                {
                    var re32 = db.PVContracts
                      .Where(m => m.ProviderID == providerid && m.IsActive == true)
                      .OrderByDescending(m => m.StartDate)
                      .FirstOrDefault();
                    re3 = re32;

                }
                //var re3 = db.PVContracts
                //      .Where(m => m.ProviderID == providerid)
                //      .OrderByDescending(m => m.StartDate)
                //      .FirstOrDefault();
                int Provider_Contract_ID = re3.ProviderContractID;

                var provider_category = (from values in db.PVServicePricings
                                         join item1 in db.PVServices on values.ServiceID equals item1.ServiceID
                                         where values.ProviderContractID == Provider_Contract_ID
                                         select item1.ServiceName).ToList();
                servicess_data = provider_category;
                var dat = servicess_data.Where(s => s.Replace(" ", string.Empty) == id.Replace(" ", string.Empty)).FirstOrDefault();
                if (dat != null)
                {
                    Outp = "1";
                }
            }

            //var dat = servicess_data.Where(s => s==id).FirstOrDefault();
            //if(dat!=null)
            //{
            //    Outp = "1";
            //}
            return Json(Outp, JsonRequestBehavior.AllowGet);
        }

        public JsonResult select_Servicess(string id)
        {
            List<string> servicess_data = new List<string>();
            int providerid = int.Parse(Session["providerid"].ToString());
            var rec = db.PVProviderDatas.FirstOrDefault(d => d.ProviderID == providerid);
            var providercategoryid = rec.ProviderCatID;

            if (providercategoryid == 2)
            { 
                var drug = (from values in db.PVMedDatas
                            where values.Collection_ID==2
                            select values.MedDrugName).ToList();
                servicess_data = drug;
            }
            else
            {

                //var re3 = db.PVContracts
                //      .Where(m => m.ProviderID == providerid)
                //      .OrderByDescending(m => m.ProviderContractID)
                //      .FirstOrDefault();
                DateTime noww = DateTime.Now;
                var re3 = db.PVContracts
                                 .Where(m => m.ProviderID == providerid && m.StartDate <= noww && m.EndDate >= noww)
                                 .OrderByDescending(m => m.ProviderContractID)
                                 .FirstOrDefault();
                if (re3 == null)
                {
                    var re32 = db.PVContracts
                      .Where(m => m.ProviderID == providerid && m.IsActive == true)
                      .OrderByDescending(m => m.StartDate)
                      .FirstOrDefault();
                    re3 = re32;

                }
                int Provider_Contract_ID = re3.ProviderContractID;

                var provider_category = (from values in db.PVServicePricings
                                         join item1 in db.PVServices on values.ServiceID equals item1.ServiceID
                                         where values.ProviderContractID == Provider_Contract_ID
                                         select item1.ServiceName).ToList();
                servicess_data = provider_category;

            }

            var dat = servicess_data.Where(s => s.ToLower().Contains(id)).ToList();
            return Json(dat, JsonRequestBehavior.AllowGet);
        }

        public JsonResult Get_service_data(string selectServicelist)
        {
            var arlistall = new ArrayList();
            var ser = 0;
            int Providerid = int.Parse(Session["providerid"].ToString());
            var ProviderCattid = db.PVProviderDatas.FirstOrDefault(d => d.ProviderID == Providerid).ProviderCatID;
            //var arlist = new ArrayList();
            if (ProviderCattid != 2)
            {
                DateTime DatProcedure = DateTime.Now;

                var re3 = db.PVContracts
                            .Where(m => m.ProviderID == Providerid && m.StartDate <= DatProcedure && m.EndDate >= DatProcedure)
                            .OrderByDescending(m => m.ProviderContractID)
                            .FirstOrDefault();
                if (re3 == null)
                {
                    var re32 = db.PVContracts
                            .Where(m => m.ProviderID == Providerid && m.IsActive == true)
                            .OrderByDescending(m => m.ProviderContractID)
                            .FirstOrDefault();
                    if (re32 == null)
                    {
                        ser = 0;
                        arlistall.Add(ser);
                        var ErorMessage = "Select Provider Name is Requierd OR Claim Form Number is not Correct";
                        arlistall.Add(ErorMessage);

                        return Json(arlistall, JsonRequestBehavior.AllowGet);
                    }
                    else
                    {
                        re3 = re32;
                    }

                }
                int Provider_Contract_ID = re3.ProviderContractID;

                var re2 = (from valuess in db.PVServices
                           join itemm in db.PVServicePricings on valuess.ServiceID equals itemm.ServiceID
                           where (valuess.ServiceName.Replace(" ", string.Empty) == selectServicelist.Replace(" ", string.Empty)) && (itemm.ProviderContractID == Provider_Contract_ID)
                           select valuess).FirstOrDefault();
                if (re2 == null)
                {
                    ser = 0;
                    arlistall.Add(ser);
                    var ErorMessage = "Select Service Name is Requierd OR Service Name is Mistake";
                    arlistall.Add(ErorMessage);

                    return Json(arlistall, JsonRequestBehavior.AllowGet);
                }
                int service_id = re2.ServiceID;
                string service_code = re2.ServiceCode;

                var re4 = db.PVServicePricings
                            .Where(d => d.ProviderContractID == Provider_Contract_ID && d.ServiceID == service_id)
                            .OrderByDescending(m => m.ServicePricingID)
                            .FirstOrDefault();
                if (re4 == null)
                {
                    ser = 0;
                    arlistall.Add(ser);
                    var ErorMessage = "Service Name Not Exist in Provider";
                    arlistall.Add(ErorMessage);

                    return Json(arlistall, JsonRequestBehavior.AllowGet);
                }
                decimal service_price = re4.Price;

                ser = 1;
                arlistall.Add(ser);
                arlistall.Add(service_price);

            }
            else
            {

                var re2 = db.PVMedDatas.FirstOrDefault(d => d.MedDrugName.Replace(" ", string.Empty) == (selectServicelist.Replace(" ", string.Empty)));
                if (re2 == null)
                {
                    var re22 = db.PVMedDatas.FirstOrDefault(d => d.MedDrugName.Replace(" ", string.Empty).Contains(selectServicelist.Replace(" ", string.Empty)));
                    re2 = re22;
                }
                if (re2 == null)
                {

                    ser = 0;
                    arlistall.Add(ser);
                    var ErorMessage = "Select Service Name is Requierd OR Service Name is Mistake ";
                    arlistall.Add(ErorMessage);

                    return Json(arlistall, JsonRequestBehavior.AllowGet);
                }
                decimal service_price = re2.Price;
                ser = 1;
                arlistall.Add(ser);
                arlistall.Add(service_price);


            }

            return Json(arlistall, JsonRequestBehavior.AllowGet);
        }


        public JsonResult Check_service_Modification(string selectServicelist,string LastService_Nam)
        {
            var arlistall = new ArrayList();
            var ser = 0;
            int Providerid = int.Parse(Session["providerid"].ToString());
            var ProviderCattid = db.PVProviderDatas.FirstOrDefault(d => d.ProviderID == Providerid).ProviderCatID;
            //var arlist = new ArrayList();
            if (ProviderCattid != 2)
            {
                DateTime DatProcedure = DateTime.Now;

                var re3 = db.PVContracts
                            .Where(m => m.ProviderID == Providerid && m.StartDate <= DatProcedure && m.EndDate >= DatProcedure)
                            .OrderByDescending(m => m.ProviderContractID)
                            .FirstOrDefault();
                if (re3 == null)
                {
                    var re32 = db.PVContracts
                            .Where(m => m.ProviderID == Providerid && m.IsActive == true)
                            .OrderByDescending(m => m.ProviderContractID)
                            .FirstOrDefault();
                    if (re32 == null)
                    {
                        ser = 0;
                        arlistall.Add(ser);
                        var ErorMessage = "Select Provider Name is Requierd OR Claim Form Number is not Correct.";
                        arlistall.Add(ErorMessage);

                        return Json(arlistall, JsonRequestBehavior.AllowGet);
                    }
                    else
                    {
                        re3 = re32;
                    }

                }
                int Provider_Contract_ID = re3.ProviderContractID;

                var re2 = (from valuess in db.PVServices
                           join itemm in db.PVServicePricings on valuess.ServiceID equals itemm.ServiceID
                           where (valuess.ServiceName.Replace(" ", string.Empty) == selectServicelist.Replace(" ", string.Empty)) && (itemm.ProviderContractID == Provider_Contract_ID)
                           select valuess).FirstOrDefault();
                if (re2 == null)
                {
                    ser = 0;
                    arlistall.Add(ser);
                    var ErorMessage = "Select Service Name is Requierd OR Service Name is Mistake.";
                    arlistall.Add(ErorMessage);

                    return Json(arlistall, JsonRequestBehavior.AllowGet);
                }
                int service_id = re2.ServiceID;
                string service_code = re2.ServiceCode;

                var re4 = db.PVServicePricings
                            .Where(d => d.ProviderContractID == Provider_Contract_ID && d.ServiceID == service_id)
                            .OrderByDescending(m => m.ServicePricingID)
                            .FirstOrDefault();
                if (re4 == null)
                {
                    ser = 0;
                    arlistall.Add(ser);
                    var ErorMessage = "Service Name Not Exist in Provider.";
                    arlistall.Add(ErorMessage);

                    return Json(arlistall, JsonRequestBehavior.AllowGet);
                }

                var re2Last = (from valuess in db.PVServices
                               join itemm in db.PVServicePricings on valuess.ServiceID equals itemm.ServiceID
                               where (valuess.ServiceName.Replace(" ", string.Empty) == LastService_Nam.Replace(" ", string.Empty)) && (itemm.ProviderContractID == Provider_Contract_ID)
                               select valuess).FirstOrDefault();
                if (re2Last == null)
                {
                    ser = 0;
                    arlistall.Add(ser);
                    var ErorMessage = "Select Service Modification Name is Requierd OR Service Modification Name is Mistake.";
                    arlistall.Add(ErorMessage);

                    return Json(arlistall, JsonRequestBehavior.AllowGet);
                }
                int service_idLast = re2Last.ServiceID;

                var re4last = db.PVServicePricings
                            .Where(d => d.ProviderContractID == Provider_Contract_ID && d.ServiceID == service_idLast)
                            .OrderByDescending(m => m.ServicePricingID)
                            .FirstOrDefault();
                if (re4last == null)
                {
                    ser = 0;
                    arlistall.Add(ser);
                    var ErorMessage = "Service Modification Name Not Exist in Provider.";
                    arlistall.Add(ErorMessage);

                    return Json(arlistall, JsonRequestBehavior.AllowGet);
                }
                decimal service_price = re4.Price;
                decimal service_priceLast = re4last.Price;


                if (service_price > service_priceLast)
                {
                    ser = 0;
                    arlistall.Add(ser);
                    var ErorMessage = "Service Modification Name : ( " + selectServicelist + " ) , Price : ( " + service_price + " ), more than Price of Service Name : ( " + LastService_Nam + " ), Price : ( " + service_priceLast + " ) .";
                    arlistall.Add(ErorMessage);

                    return Json(arlistall, JsonRequestBehavior.AllowGet);
                }

                ser = 1;
                arlistall.Add(ser);
                arlistall.Add(service_price);

            }
            else
            {

                var re2 = db.PVMedDatas.FirstOrDefault(d => d.MedDrugName.Replace(" ", string.Empty) == (selectServicelist.Replace(" ", string.Empty)));
                if (re2 == null)
                {
                    var re22 = db.PVMedDatas.FirstOrDefault(d => d.MedDrugName.Replace(" ", string.Empty).Contains(selectServicelist.Replace(" ", string.Empty)));
                    re2 = re22;
                }
                if (re2 == null)
                {

                    ser = 0;
                    arlistall.Add(ser);
                    var ErorMessage = "Select Service Name is Requierd OR Service Name is Mistake .";
                    arlistall.Add(ErorMessage);

                    return Json(arlistall, JsonRequestBehavior.AllowGet);
                }

                var re2last = db.PVMedDatas.FirstOrDefault(d => d.MedDrugName.Replace(" ", string.Empty) == (LastService_Nam.Replace(" ", string.Empty)));
                if (re2last == null)
                {
                    var re22last = db.PVMedDatas.FirstOrDefault(d => d.MedDrugName.Replace(" ", string.Empty).Contains(LastService_Nam.Replace(" ", string.Empty)));
                    re2last = re22last;
                }
                if (re2last == null)
                {

                    ser = 0;
                    arlistall.Add(ser);
                    var ErorMessage = "Select Service Modification is Requierd OR Service Modification is Mistake .";
                    arlistall.Add(ErorMessage);

                    return Json(arlistall, JsonRequestBehavior.AllowGet);
                }

                string service_Effective = re2.Effective_Material;
                string service_EffectiveLast = re2last.Effective_Material;
                if (service_Effective == null)
                {
                    service_Effective = "";

                }
                if (service_EffectiveLast == null)
                {
                    service_EffectiveLast = "";

                }
                string[] service_Effectives = service_Effective.Split('+');
                string[] service_Effectives_Last = service_EffectiveLast.Split('+');

                bool SameEffective = false;
                if (service_Effective == ""|| service_EffectiveLast == "")
                {
                    SameEffective = false;

                }
                else if (service_Effectives.Count()==1 && service_Effectives_Last.Count() > 1)
                {
                    if (service_EffectiveLast.Replace(" ", string.Empty).Contains(service_Effectives[0].Replace(" ", string.Empty)))
                    {
                        SameEffective = true;
                    }
                    else
                    {
                        SameEffective = false;
                    }
                }
                else if (service_Effectives.Count() > 1 && service_Effectives_Last.Count() == 1)
                {
                    if (service_Effective.Replace(" ", string.Empty).Contains(service_Effectives_Last[0].Replace(" ", string.Empty)))
                    {
                        SameEffective = true;
                    }
                    else
                    {
                        SameEffective = false;
                    }

                }
                else if (service_Effectives.Count() > 1 && service_Effectives_Last.Count() > 1)
                {
                    bool sameeffect = false;
                    foreach (var service_Effectiv in service_Effectives)
                    {
                        if (service_EffectiveLast.Replace(" ", string.Empty).Contains(service_Effectiv.Replace(" ", string.Empty)))
                        {
                            sameeffect = true;
                        }
                       
                    }
                    if (sameeffect == true)
                    {
                        SameEffective = true;
                    }
                    else
                    {
                        SameEffective = false;

                    }
                }
                else
                {
                    if(service_Effectives[0].Replace(" ", string.Empty).Contains(service_Effectives_Last[0].Replace(" ", string.Empty)))
                    {
                        SameEffective = true;
                    }
                    else if (service_Effectives_Last[0].Replace(" ", string.Empty).Contains(service_Effectives[0].Replace(" ", string.Empty)))
                    {
                        SameEffective = true;

                    }
                    else
                    {
                        SameEffective = false;

                    }

                }

                if(SameEffective == false)
                {
                    ser = 0;
                    arlistall.Add(ser);
                    var ErorMessage = "Service Modification Name : ( " + selectServicelist + " ) does not have the same Effective Material For Service Name : ( "+ LastService_Nam+" ) .";

                    arlistall.Add(ErorMessage);

                    return Json(arlistall, JsonRequestBehavior.AllowGet);
                }
             


                decimal service_price = re2.Price;
                decimal service_priceLast = re2last.Price;

                if(service_price> service_priceLast)
                {
                    ser = 0;
                    arlistall.Add(ser);
                    var ErorMessage = "Service Modification Name : ( " + selectServicelist + " ) , Price : ( " + service_price + " ), more than Price of Service Name : ( " + LastService_Nam + " ), Price : ( " + service_priceLast + " ) .";

                    arlistall.Add(ErorMessage);

                    return Json(arlistall, JsonRequestBehavior.AllowGet);
                }

                ser = 1;
                arlistall.Add(ser);
                arlistall.Add(service_price);


            }

            return Json(arlistall, JsonRequestBehavior.AllowGet);
        }
        public JsonResult UpdateServiceModificationName(string Claimitem_id, string service_name)
        {
            try
            {
                int Claimitem_ID = int.Parse(Claimitem_id);

                using (var database = new MedInsuranceProEntities())
                {
                    var foo = database.CLMClaimsItemsONLINEs.Where(f => f.ClaimItemID == Claimitem_ID).FirstOrDefault();
                    foo.ServiceName = service_name;
                    database.SaveChanges();
                }

                return Json("", JsonRequestBehavior.AllowGet);

            }
            catch
            {
                return Json("1", JsonRequestBehavior.AllowGet);

            }


        }
        public JsonResult select_Servicesspharmacy(string id)
        {

            List<string> Provider_Services = new List<string>();

            var drug = (from values in db.PVMedDatas
                        where values.Collection_ID==2
                        select values.MedDrugName).ToList();
            Provider_Services = drug;

            var dat = Provider_Services.Where(s => s.ToLower().Contains(id)).ToList();
            return Json(dat, JsonRequestBehavior.AllowGet);
        }
        public JsonResult select_Servicesslab(string id)
        {

            List<string> Provider_Services = new List<string>();

            var lab = (from values in db.PVServicePricings
                       join item1 in db.PVServices on values.ServiceID equals item1.ServiceID
                       join item2 in db.PVProviderDatas on values.ProviderID equals item2.ProviderID
                       where item2.ProviderCatID == 3

                       select item1.ServiceName).Distinct().ToList();
            Provider_Services = lab;

            var dat = Provider_Services.Where(s => s.ToLower().Contains(id)).ToList();
            return Json(dat, JsonRequestBehavior.AllowGet);
        }
        public JsonResult select_Servicessscan(string id)
        {

            List<string> Provider_Services = new List<string>();

            var scan = (from values in db.PVServicePricings
                        join item1 in db.PVServices on values.ServiceID equals item1.ServiceID
                        join item2 in db.PVProviderDatas on values.ProviderID equals item2.ProviderID
                        where item2.ProviderCatID == 4

                        select item1.ServiceName).Distinct().ToList();
            Provider_Services = scan;

            var dat = Provider_Services.Where(s => s.ToLower().Contains(id)).ToList();
            return Json(dat, JsonRequestBehavior.AllowGet);
        }
        public JsonResult select_ServicessOptical(string id)
        {

            List<string> Provider_Services = new List<string>();

            var Optical = (from values in db.PVServicePricings
                           join item1 in db.PVServices on values.ServiceID equals item1.ServiceID
                           join item2 in db.PVProviderDatas on values.ProviderID equals item2.ProviderID
                           where item2.ProviderCatID == 8

                           select item1.ServiceName).Distinct().ToList();
            Provider_Services = Optical;

            var dat = Provider_Services.Where(s => s.ToLower().Contains(id)).ToList();
            return Json(dat, JsonRequestBehavior.AllowGet);
        }
        public JsonResult select_Servicessphysiothrapy(string id)
        {

            List<string> Provider_Services = new List<string>();

            var physiothrapy = (from values in db.PVServicePricings
                                join item1 in db.PVServices on values.ServiceID equals item1.ServiceID
                                join item2 in db.PVProviderDatas on values.ProviderID equals item2.ProviderID
                                where item2.ProviderCatID == 9

                                select item1.ServiceName).Distinct().ToList();
            Provider_Services = physiothrapy;

            var dat = Provider_Services.Where(s => s.ToLower().Contains(id)).ToList();
            return Json(dat, JsonRequestBehavior.AllowGet);
        }


        public JsonResult CheckServiceNam(string id)
        {
            var dat = db.PVMedDatas.FirstOrDefault(d => d.MedDrugName.Replace(" ", string.Empty) == (id.Replace(" ", string.Empty)));

            if (dat == null)
            {
                return Json("aa", JsonRequestBehavior.AllowGet);
            }
            var price = dat.Price;

            return Json(price, JsonRequestBehavior.AllowGet);
        }
        public JsonResult CheckServiceNamlab(string id)
        {
            var lab = (from values in db.PVServicePricings
                       join item1 in db.PVServices on values.ServiceID equals item1.ServiceID
                       join item2 in db.PVProviderDatas on values.ProviderID equals item2.ProviderID
                       where item2.ProviderCatID == 3 && item1.ServiceName.Replace(" ", string.Empty) == id.Replace(" ", string.Empty)
                       orderby values.ProviderContractID descending
                       select values).FirstOrDefault();

            if (lab == null)
            {
                return Json("aa", JsonRequestBehavior.AllowGet);
            }
            var price = lab.Price;

            return Json(price, JsonRequestBehavior.AllowGet);
        }
        public JsonResult CheckServiceNamscan(string id)
        {
            var lab = (from values in db.PVServicePricings
                       join item1 in db.PVServices on values.ServiceID equals item1.ServiceID
                       join item2 in db.PVProviderDatas on values.ProviderID equals item2.ProviderID
                       where item2.ProviderCatID == 4 && item1.ServiceName.Replace(" ", string.Empty) == id.Replace(" ", string.Empty)
                       orderby values.ProviderContractID descending
                       select values).FirstOrDefault();

            if (lab == null)
            {
                return Json("aa", JsonRequestBehavior.AllowGet);
            }
            var price = lab.Price;

            return Json(price, JsonRequestBehavior.AllowGet);
        }
        public JsonResult CheckServiceNamoptical(string id)
        {
            var optical = (from values in db.PVServicePricings
                           join item1 in db.PVServices on values.ServiceID equals item1.ServiceID
                           join item2 in db.PVProviderDatas on values.ProviderID equals item2.ProviderID
                           where item2.ProviderCatID == 8 && item1.ServiceName.Replace(" ", string.Empty) == id.Replace(" ", string.Empty)
                           orderby values.ProviderContractID descending
                           select values).FirstOrDefault();

            if (optical == null)
            {
                return Json("aa", JsonRequestBehavior.AllowGet);
            }
            var price = optical.Price;

            return Json(price, JsonRequestBehavior.AllowGet);
        }
        public JsonResult CheckServiceNamphysiothrapy(string id)
        {
            var physiothrapy = (from values in db.PVServicePricings
                                join item1 in db.PVServices on values.ServiceID equals item1.ServiceID
                                join item2 in db.PVProviderDatas on values.ProviderID equals item2.ProviderID
                                where item2.ProviderCatID == 9 && item1.ServiceName.Replace(" ", string.Empty) == id.Replace(" ", string.Empty)
                                orderby values.ProviderContractID descending
                                select values).FirstOrDefault();

            if (physiothrapy == null)
            {
                return Json("aa", JsonRequestBehavior.AllowGet);
            }
            var price = physiothrapy.Price;

            return Json(price, JsonRequestBehavior.AllowGet);
        }



        public class Claims_Servicess
        {
            public string Service_Name { get; set; }
            public decimal Service_Qnt { get; set; }
            public decimal Requested_Price { get; set; }

            public string Claim_Form_Number { get; set; }
            public string Card_numberr { get; set; }

            public string FirstDiagnosis { get; set; }
            public string SecondDiagnosis { get; set; }
            public string ThirdDiagnosis { get; set; }

            public string ProcedureDat { get; set; }


        }

        public JsonResult InsertClaims_Servicess(List<Claims_Servicess> Claim_Servicese)
        {

            int insertedRecords = 0;
            var arrlis = new ArrayList();


            if (/*Claim_Servicese.Count == 0||*/ Claim_Servicese == null)
            {
                insertedRecords = 1;
                arrlis.Add(insertedRecords);
                var ErorMessage = "Claim Services not Exist , You Must ADD at least One Service";
                arrlis.Add(ErorMessage);
                return Json(arrlis, JsonRequestBehavior.AllowGet);

            }

            int providerid = int.Parse(Session["providerid"].ToString());
            int branchid = int.Parse(Session["BranchID"].ToString());
            string branchname = Session["branchName"].ToString();
            var Provider_Data = db.PVProviderDatas.FirstOrDefault(d => d.ProviderID == providerid);
            string Provider_name = Provider_Data.ProviderName;
            var Provider_Cat_ID = Provider_Data.ProviderCatID;
            if ((Claim_Servicese[0].Claim_Form_Number == null))
            {
                insertedRecords = 1;
                arrlis.Add(insertedRecords);
                var ErorMessage = "Claim Form Number is not Correct";
                arrlis.Add(ErorMessage);
                return Json(arrlis, JsonRequestBehavior.AllowGet);
            }

          
            var FirstDiagnose = Claim_Servicese[0].FirstDiagnosis;
            var SecondDiagnose = Claim_Servicese[0].SecondDiagnosis;
            var ThirdDiagnose = Claim_Servicese[0].ThirdDiagnosis;
            var FirstICD = "";
            var SecondICD = "";
            var ThirdICD = "";

            var claim_form_num = long.Parse(Claim_Servicese[0].Claim_Form_Number);
            var claim_formdata = db.MemberClaimForms.FirstOrDefault(d => d.ClaimFormNumber == claim_form_num);
            if (claim_formdata == null)
            {

                var providernam = (from values in db.PVProviderClaimBooks
                                   join o in db.PVProviderDatas on values.ProviderID equals o.ProviderID
                                   where values.FromSerial <= claim_form_num && values.ToSerial >= claim_form_num
                                   orderby values.FromSerial descending
                                   select new { o.ProviderName, o.ProviderID }).ToList();
                if (providernam.Count == 0)
                {
                    var approvalselcted = db.CLMApprovals.FirstOrDefault(n => n.ApprovalCode == claim_form_num && n.ApprovalStatus == 1);
                    if (approvalselcted == null)
                    {
                        insertedRecords = 1;
                        arrlis.Add(insertedRecords);
                        var ErorMessage = "Claim Form Number is not Correct";
                        arrlis.Add(ErorMessage);
                        return Json(arrlis, JsonRequestBehavior.AllowGet);
                    }
                }

                if(FirstDiagnose==""|| FirstDiagnose == null)
                {
                    insertedRecords = 1;
                    arrlis.Add(insertedRecords);
                    var ErorMessage = "Upload Claim Form Image Or Enter First Diagnose is Reqiard";
                    arrlis.Add(ErorMessage);
                    return Json(arrlis, JsonRequestBehavior.AllowGet);
                }
                var FirstDiagnoseselcted = db.ICDCodes.FirstOrDefault(n => n.ShortName == FirstDiagnose);
                if (FirstDiagnoseselcted == null)
                {
                    insertedRecords = 1;
                    arrlis.Add(insertedRecords);
                    var ErorMessage = "First Diagnose is not Correct";
                    arrlis.Add(ErorMessage);
                    return Json(arrlis, JsonRequestBehavior.AllowGet);
                }
                FirstICD = FirstDiagnoseselcted.ICDCode1;
                if (SecondDiagnose == "" || SecondDiagnose == null)
                {
                    SecondDiagnose = "";
                }
                else
                {
                    var SecondDiagnoseselcted = db.ICDCodes.FirstOrDefault(n => n.ShortName == SecondDiagnose);
                    if (SecondDiagnoseselcted == null)
                    {
                        insertedRecords = 1;
                        arrlis.Add(insertedRecords);
                        var ErorMessage = "Second Diagnose is not Correct";
                        arrlis.Add(ErorMessage);
                        return Json(arrlis, JsonRequestBehavior.AllowGet);
                    }
                    SecondICD = SecondDiagnoseselcted.ICDCode1;


                }
                if (ThirdDiagnose == "" || ThirdDiagnose == null)
                {
                    ThirdDiagnose = "";
                }
                else
                {
                    var ThirdDiagnoseselcted = db.ICDCodes.FirstOrDefault(n => n.ShortName == ThirdDiagnose);
                    if (ThirdDiagnoseselcted == null)
                    {
                        insertedRecords = 1;
                        arrlis.Add(insertedRecords);
                        var ErorMessage = "Third Diagnose is not Correct";
                        arrlis.Add(ErorMessage);
                        return Json(arrlis, JsonRequestBehavior.AllowGet);
                    }

                   ThirdICD = ThirdDiagnoseselcted.ICDCode1;

                }

            }
            else
            {
                if (FirstDiagnose == "" || FirstDiagnose == null)
                {
                    FirstDiagnose = "";
                }
                else
                {
                    var firstDiagnoseselcted = db.ICDCodes.FirstOrDefault(n => n.ShortName == FirstDiagnose);
                    if (firstDiagnoseselcted == null)
                    {
                        insertedRecords = 1;
                        arrlis.Add(insertedRecords);
                        var ErorMessage = "First Diagnose is not Correct";
                        arrlis.Add(ErorMessage);
                        return Json(arrlis, JsonRequestBehavior.AllowGet);
                    }
                    FirstICD = firstDiagnoseselcted.ICDCode1;


                }
                if (SecondDiagnose == "" || SecondDiagnose == null)
                {
                    SecondDiagnose = "";
                }
                else
                {
                    var SecondDiagnoseselcted = db.ICDCodes.FirstOrDefault(n => n.ShortName == SecondDiagnose);
                    if (SecondDiagnoseselcted == null)
                    {
                        insertedRecords = 1;
                        arrlis.Add(insertedRecords);
                        var ErorMessage = "Second Diagnose is not Correct";
                        arrlis.Add(ErorMessage);
                        return Json(arrlis, JsonRequestBehavior.AllowGet);
                    }
                    SecondICD = SecondDiagnoseselcted.ICDCode1;


                }
                if (ThirdDiagnose == "" || ThirdDiagnose == null)
                {
                    ThirdDiagnose = "";
                }
                else
                {
                    var ThirdDiagnoseselcted = db.ICDCodes.FirstOrDefault(n => n.ShortName == ThirdDiagnose);
                    if (ThirdDiagnoseselcted == null)
                    {
                        insertedRecords = 1;
                        arrlis.Add(insertedRecords);
                        var ErorMessage = "Third Diagnose is not Correct";
                        arrlis.Add(ErorMessage);
                        return Json(arrlis, JsonRequestBehavior.AllowGet);
                    }

                    ThirdICD = ThirdDiagnoseselcted.ICDCode1;

                }
            }
            //var a = db.CLMClaimsONLINEs.FirstOrDefault(n => n.ClaimFormNum == claim_form_num && n.ProviderID== providerid);
            //if (a != null)
            //{
            //    insertedRecords = 1;
            //    arrlis.Add(insertedRecords);
            //    var ErorMessage = "Claim Form Number is Added Since while";
            //    arrlis.Add(ErorMessage);

            //    return Json(arrlis, JsonRequestBehavior.AllowGet);

            //}

            //else
            //{

            string card_num = Claim_Servicese[0].Card_numberr;
            var datenow = DateTime.Now;
            var cont_num = (
from item1 in db.ContractMembers
join item2 in db.Contracts on item1.ContractID equals item2.ContractID into finalGroup0
from finalItem0 in finalGroup0.DefaultIfEmpty()
where finalItem0.IsActive == true && finalItem0.StartDate <= datenow && finalItem0.EndDate >= datenow && item1.CardNumber == card_num
select new { finalItem0.ContractNumber, item1.ContractMemberID, finalItem0.ContractID, item1.PlanTypeID }).OrderByDescending(d => d.ContractMemberID).ToList();
            if (cont_num.Count == 0)
            {
                insertedRecords = 1;
                arrlis.Add(insertedRecords);
                var ErorMessage = "Card Number is not Correct";
                arrlis.Add(ErorMessage);
                return Json(arrlis);
            }
            string contract_num = cont_num[0].ContractNumber;
            int Contract_Member_ID = cont_num[0].ContractMemberID;
            int Contract_ID = cont_num[0].ContractID;
            int plan_type_id = cont_num[0].PlanTypeID;

            if (FirstDiagnose == null)
            {
                FirstDiagnose = "";
            }
            if ( SecondDiagnose == null)
            {
                SecondDiagnose = "";
            }
            if (ThirdDiagnose == null)
            {
                ThirdDiagnose = "";
            }

            var datetimee = DateTime.Now;
            if ((Claim_Servicese[0].ProcedureDat != null))
            {
                try
                {
                    var qq = Claim_Servicese[0].ProcedureDat;
                    datetimee = DateTime.Parse(qq);
                }
                catch
                {

                }
            }

            CLMClaimsONLINE newdataa = new CLMClaimsONLINE()
            {

                //ClaimID = calimid,
                ClaimFormNum = claim_form_num,
                ContractMemberID = Contract_Member_ID,
                symptomsDate = datetimee,
                ProcedureDate = datetimee /*new DateTime()*/,
                ClaimServiceID = 0,
               
                ClaimStatusID = 1,
                // will edit 
                UserAddID = providerid /*int.Parse(Session["UserID"].ToString())*/,
                UserApproveID = 0,
                ContractNum = contract_num,
                CardNumber = card_num,
                ProviderID = providerid,
                ProviderCatID = Provider_Cat_ID,
                BranchID = branchid,
                BranchName = branchname,

                Diagnosis = FirstDiagnose,
                ICDCode = FirstICD,
                SecondDiagnosis = SecondDiagnose,
                SecondICDCode = SecondICD,
                ThirdDiagnosis = ThirdDiagnose,
                ThirdICDCode = ThirdICD,
                //LastUpdateDate = new DateTime(),
            };
           


            //var re3 = db.PVContracts.Where(m => m.ProviderID == providerid)
            //                             .OrderByDescending(m => m.StartDate)
            //                             .FirstOrDefault();
            var date = DateTime.Now;
            var re3 = db.PVContracts
                                .Where(m => m.ProviderID == providerid && m.StartDate <= date && m.EndDate >= date)
                                .OrderByDescending(m => m.ProviderContractID)
                                .FirstOrDefault();
            if (re3 == null)
            {
                var re32 = db.PVContracts
                  .Where(m => m.ProviderID == providerid && m.IsActive == true)
                  .OrderByDescending(m => m.StartDate)
                  .FirstOrDefault();
                re3 = re32;

            }
            int Provider_Contract_ID = re3.ProviderContractID;
            decimal Local_Discount_Per = re3.LocalDiscountPer;
            decimal Foreign_Discount_Per = re3.ForeignDiscountPer;
            decimal? Chronic_LocalDis_Per = re3.ChronicLocalDisPer;
            decimal? Chronic_Foreign_DisPer = re3.ChronicForeignDisPer;
            IList<CLMClaimsItemsONLINE> CLMClaimsItemsONLINEs = new List<CLMClaimsItemsONLINE>();
            for (var i = 0; i < Claim_Servicese.Count; i++)
            {

                // get service code
                string Service_Namee = Claim_Servicese[i].Service_Name;

                int service_id = 0;
                string service_code = "";
                bool is_import = false;
                bool is_coverd = false;

                decimal Number_of_dayss = 1;
                decimal service_price = 0;
                if (Provider_Cat_ID != 2)
                {

                    //var reco2 = db.PVServices.FirstOrDefault(d => d.ServiceName.Replace(" ", string.Empty) == Service_Namee.Replace(" ", string.Empty));
                    var reco2 = (from valuess in db.PVServices
                                 join itemm in db.PVServicePricings on valuess.ServiceID equals itemm.ServiceID
                                 where (valuess.ServiceName.Replace(" ", string.Empty) == Service_Namee.Replace(" ", string.Empty)) && (itemm.ProviderContractID == Provider_Contract_ID)
                                 select new { valuess, itemm }).FirstOrDefault();

                    service_id = reco2.valuess.ServiceID;
                    service_code = reco2.valuess.ServiceCode;
                    service_price = reco2.itemm.Price;
                    Number_of_dayss = 1;

                }
                else
                {

                    var re2 = db.PVMedDatas.FirstOrDefault(d => d.MedDrugName.Replace(" ", string.Empty)==(Service_Namee.Replace(" ", string.Empty)));
                    if (re2 == null)
                    {
                        var re22 = db.PVMedDatas.FirstOrDefault(d => d.MedDrugName.Replace(" ", string.Empty).Contains(Service_Namee.Replace(" ", string.Empty)));
                        re2 = re22;
                    }
                    service_id = re2.MedDrugID;
                    service_code = re2.MedDrugCode;

                    service_price = re2.Price;
                    int med_cat = re2.MedCatID;
                    is_import = re2.IsImported;
                    is_coverd = re2.IsCoverd;
                    Number_of_dayss = 7;

                }

                decimal Service_Qnt = Claim_Servicese[i].Service_Qnt;
                decimal Requested_Pricee = Claim_Servicese[i].Requested_Price;

                CLMClaimsItemsONLINE newdataaa = new CLMClaimsItemsONLINE()
                {

                    ClaimID = 0,
                    ServiceID = service_id,
                    ServiceCode = service_code,
                    ServiceName = Service_Namee,
                    ServiceQnt = Service_Qnt,
                    ServicePrice = service_price,
                    RequestedPrice = Requested_Pricee,
                    ServiceAmt = Service_Qnt * service_price,
                    // will edit
                    UserEntryID = providerid,
                    UserEntryDate = DateTime.Now,
                    UserApprove = false,
                    UserApproveID = 0,
                    ClaimNum = claim_form_num,
                    ClaimServiceID = 0,
                    IsCovered = is_coverd,
                    IsImported = is_import,
                    Refuse_Note = "",
                    ClaimItemStatusID = 1,
                    Number_of_times_a_day = 1,
                    Dosage_size = 1,
                    Number_of_days = /*7*/Number_of_dayss,

                };
                CLMClaimsItemsONLINEs.Add(newdataaa);

                //db.CLMClaimItems.Add(newdataaa);
                //db.SaveChanges();


            }
            db.CLMClaimsONLINEs.Add(newdataa);
            db.SaveChanges();
            var claim_dataa = db.CLMClaimsONLINEs.Where(d => d.ProviderID == providerid &&d.BranchID == branchid && d.ClaimFormNum == claim_form_num).OrderByDescending(d => d.ClaimID).FirstOrDefault();
            var claim_id_inserted = claim_dataa.ClaimID;
            foreach (var row in CLMClaimsItemsONLINEs)
            {
                row.ClaimID = claim_id_inserted;
            }
            using (var context = new MedInsuranceProEntities())
            {
                context.CLMClaimsItemsONLINEs.AddRange(CLMClaimsItemsONLINEs);
                context.SaveChanges();
            }

          
            //}
            insertedRecords = 0;
            arrlis.Add(insertedRecords);
            //arrlis.Add(TDeductAmt);
            using (var database = new MedInsuranceProEntities())
            {
                var foo = database.AlertCMCDatas.OrderByDescending(f => f.AlertCMCID).FirstOrDefault();
                foo.ClaimFormNum = claim_dataa.ClaimFormNum;
                foo.CardNumber = claim_dataa.CardNumber;
                foo.BranchName = claim_dataa.BranchName;
                foo.ProviderID = claim_dataa.ProviderID;
                foo.BranchID = claim_dataa.BranchID;
                database.SaveChanges();
            }

            return Json(arrlis, JsonRequestBehavior.AllowGet);
            //            var claimform_member = db.MemberClaimForms.FirstOrDefault(d => d.ClaimFormNumber == claim_form_num && d.MemberClaimformStatus == 2);
            //            var claimformData_Online = db.CLMClaimsONLINEs.FirstOrDefault(d => d.ClaimFormNum == claim_form_num);

            //            if ((claimform_member == null)|| (claimformData_Online == null))
            //            {
            //                insertedRecords = 1;
            //                arrlis.Add(insertedRecords);
            //                var ErorMessage = "Claim Form Number is not Approved";
            //                arrlis.Add(ErorMessage);
            //                return Json(arrlis, JsonRequestBehavior.AllowGet);
            //            }

            //            var maxValu = db.CLMClaims.Max(o => o.ClaimID);
            //                var resul = db.CLMClaims.First(o => o.ClaimID == maxValu);
            //                int calimid = (resul.ClaimID) + 1;

            //            string card_num = claimformData_Online.CardNumber;

            //            var cont_num = (
            //from item1 in db.ContractMembers
            //join item2 in db.Contracts on item1.ContractID equals item2.ContractID into finalGroup0
            //from finalItem0 in finalGroup0.DefaultIfEmpty()
            //where finalItem0.IsActive == true && item1.CardNumber == card_num
            //select new { finalItem0.ContractNumber, item1.ContractMemberID, finalItem0.ContractID, item1.PlanTypeID }).ToList();

            //            string contract_num = cont_num[0].ContractNumber;
            //            int Contract_Member_ID = cont_num[0].ContractMemberID;
            //            int Contract_ID = cont_num[0].ContractID;
            //            int plan_type_id = cont_num[0].PlanTypeID;


            //            int? ContractMember_ID = claimformData_Online.ContractMemberID;
            //            DateTime? symptoms_Date = claimformData_Online.symptomsDate;
            //            DateTime? Procedure_Date = claimformData_Online.ProcedureDate;
            //            int? ClaimService_ID = claimformData_Online.ClaimServiceID;
            //            string Diagnosiss = claimformData_Online.Diagnosis;
            //            string ICD_Code = claimformData_Online.ICDCode;
            //            string Contract_Num = claimformData_Online.ContractNum;
            //            string Card_Number = claimformData_Online.CardNumber;

            //            CLMClaim newdataa = new CLMClaim()
            //                {

            //                    ClaimID = calimid,
            //                    RecBatchID = 0,
            //                    ClaimFormNum = claim_form_num,
            //                    ContractMemberID = ContractMember_ID,
            //                    symptomsDate = symptoms_Date,
            //                    ProcedureDate = Procedure_Date,
            //                    ClaimServiceID = ClaimService_ID,
            //                    Diagnosis = Diagnosiss,
            //                    ICDCode = ICD_Code,
            //                    TClaimAmt = 0,
            //                    TDeductAmt = 0,
            //                    TDiscountAmt = 0,
            //                    TAClaimAmt = 0,
            //                    TMedicalDeductAmt = 0,
            //                    TDifferenceAmt = 0,
            //                    ClaimStatusID = 1,
            //                    // will edit 
            //                    UserAddID = 0,
            //                    UserApproveID = 0,
            //                    NetClaimedAmt = 0,
            //                    TRequestedAmt = 0,
            //                    BatchNumber = "",
            //                    ContractNum = Contract_Num,
            //                    CardNumber = Card_Number,
            //                    ClaimDeductAmt = 0,
            //                    ClaimDeductNote = "",
            //                    TAuditDeductAmt = 0,
            //                    IsOverCeiling = false,
            //                    RepType = false,
            //                    ProviderID = providerid,
            //                    ProviderName = Provider_name

            //                };
            //                db.CLMClaims.Add(newdataa);
            //                db.SaveChanges();


            //                var maxValu1 = db.CLMClaims.Max(o => o.ClaimID);
            //                var resul1 = db.CLMClaims.First(o => o.ClaimID == maxValu1);
            //                int calimid1 = resul1.ClaimID;

            //                //CLMClaimItem

            //                var maxVal = db.CLMClaimItems.Max(o => o.ClaimItemID);
            //                var resu = db.CLMClaimItems.First(o => o.ClaimItemID == maxVal);
            //                int claimitemiD = resu.ClaimItemID;

            //                int provi_id = providerid;
            //                var re3 = db.PVContracts
            //                                 .Where(m => m.ProviderID == provi_id)
            //                                 .OrderByDescending(m => m.ProviderContractID)
            //                                 .FirstOrDefault();
            //                int Provider_Contract_ID = re3.ProviderContractID;
            //            decimal Local_Discount_Per = re3.LocalDiscountPer;
            //            decimal Foreign_Discount_Per = re3.ForeignDiscountPer;
            //            decimal? Chronic_LocalDis_Per = re3.ChronicLocalDisPer;
            //            decimal? Chronic_Foreign_DisPer = re3.ChronicForeignDisPer;


            //            decimal TClaim_Amt = 0;
            //                decimal TDeductAmt = 0;
            //                decimal TDiscountAmt = 0;
            //                //decimal TAClaimAmt = 0;
            //                decimal TMedicalDeductAmt = 0;
            //                decimal TDifferenceAmt = 0;
            //                decimal NetClaimedAmt = 0;
            //                decimal TRequestedAmt = 0;
            //                decimal ClaimDeductAmt = 0;
            //                //decimal TAuditDeductAmt = 0;

            //                for (var i = 0; i < Claim_Servicese.Count; i++)
            //                {

            //                    claimitemiD++;
            //                    // get service code
            //                    string Service_Namee = Claim_Servicese[i].Service_Name;

            //                int service_id = 0;
            //                string service_code = "";
            //                bool is_import = false;
            //                bool is_coverd = false;

            //                decimal service_price = 0;
            //                decimal service_DiscountPerc = 0;
            //                decimal service_DiscountVal = 0;
            //                if (Provider_Cat_ID != 2)
            //                {

            //                    var reco2 = db.PVServices.FirstOrDefault(d => d.ServiceName==Service_Namee);
            //                    service_id = reco2.ServiceID;
            //                    service_code = reco2.ServiceCode;

            //                    var re4 = db.PVServicePricings
            //                            .Where(d => d.ProviderContractID == Provider_Contract_ID && d.ServiceID == service_id)
            //                            .OrderByDescending(m => m.ServicePricingID)
            //                            .FirstOrDefault();
            //                    //var re4 = db.PVServicePricings.FirstOrDefault(d => d.ProviderContractID == Provider_Contract_ID && d.ServiceID== service_id);
            //                    service_price = re4.Price;
            //                    service_DiscountPerc = re4.DiscountPerc;
            //                    service_DiscountVal = re4.DiscountVal;

            //                }
            //                else
            //                {
            //                    var re2 = db.PVMedDatas.FirstOrDefault(d => d.MedDrugName==Service_Namee);
            //                    service_id = re2.MedDrugID;
            //                    service_code = re2.MedDrugCode;
            //                    service_price = re2.Price;
            //                    int med_cat = re2.MedCatID;
            //                    is_import = re2.IsImported;
            //                    is_coverd = re2.IsCoverd;

            //                    service_DiscountPerc = 0;
            //                    if ((med_cat == 3) || (med_cat == 7) || (med_cat == 8) || (med_cat == 11))
            //                    {
            //                        service_DiscountPerc = Local_Discount_Per;
            //                    }
            //                    else if ((med_cat == 1) || (med_cat == 2) || (med_cat == 4) || (med_cat == 5) || (med_cat == 6) || (med_cat == 10))
            //                    {
            //                        service_DiscountPerc = Foreign_Discount_Per;
            //                    }

            //                    service_DiscountVal = service_DiscountPerc * service_price;



            //                }





            //                var re5 = db.ContractServiceCeilings.FirstOrDefault(d => d.ContractID == Contract_ID && d.PlanTypeID == plan_type_id && d.ServiceID == ClaimService_ID);
            //                    decimal Deduct_Per = re5.DeductPer;

            //                    decimal Service_Deduct_Amt = Deduct_Per * service_price;
            //                    decimal Net_Service_Amt = service_price - Service_Deduct_Amt - service_DiscountVal;



            //                    decimal Service_Qnt = Claim_Servicese[i].Service_Qnt;
            //                    decimal Service_UnitAmt = service_price;
            //                    decimal Claimed_Amt = Service_UnitAmt * Service_Qnt;
            //                    decimal Deduct_Amt = Claimed_Amt* Deduct_Per;
            //                    decimal Discount_Amt = Claimed_Amt*service_DiscountPerc;
            //                    decimal Medical_DeductAmt =0;
            //                    decimal Due_Amt = Claimed_Amt- Deduct_Amt- Discount_Amt;
            //                    decimal Requested_Amt = Due_Amt;
            //                    decimal Difference_Amt = 0;
            //                    decimal Net_Payment_Amt = Due_Amt;

            //                    string reject_reson ="";

            //                //int a = ClaimService_ID;

            //                    CLMClaimItem newdataaa = new CLMClaimItem()
            //                    {

            //                        ClaimItemID = claimitemiD,
            //                        ClaimID = calimid1,
            //                        ServiceID = service_id,
            //                        ServiceCode = service_code,
            //                        ServiceName = Service_Namee,
            //                        ServiceQnt = Service_Qnt,
            //                        ServiceUnitAmt = Service_UnitAmt,
            //                        ClaimedAmt = Claimed_Amt,
            //                        DeductAmt = Deduct_Amt,
            //                        DiscountAmt = Discount_Amt,
            //                        FDiscountAmt = 0,
            //                        MedicalDeductAmt = Medical_DeductAmt,
            //                        DueAmt = Due_Amt,
            //                        RequestedAmt = Requested_Amt,
            //                        DifferenceAmt = Difference_Amt,
            //                        RejectNotes = reject_reson,
            //                        IsSysEntry = false,
            //                        // will edit
            //                        UserEntryID = 0,
            //                        UserEntryDate = DateTime.Now,
            //                        UserApprove = false,
            //                        UserApproveID = 0,
            //                        BatchNumber = "",
            //                        ClaimNum = claim_form_num,
            //                        ServiceAmt = service_price,
            //                        ServiceDeductAmt = Service_Deduct_Amt,
            //                        ServiceDiscountAmt = service_DiscountVal,
            //                        NetServiceAmt = Net_Service_Amt,
            //                        ClaimServiceID =int.Parse(ClaimService_ID.ToString()),
            //                        ServiceDiscountPer = service_DiscountPerc,
            //                        IsCovered = is_coverd,
            //                        IsImported = is_import,
            //                        NetPaymentAmt = Net_Payment_Amt,
            //                        ReimbPer = 0,
            //                        AuditDeductAmt = 0,
            //                        AuditComments = "",
            //                        IsOverCeiling = false

            //                    };
            //                    db.CLMClaimItems.Add(newdataaa);
            //                    db.SaveChanges();

            //                    TClaim_Amt += Claimed_Amt;
            //                    TDeductAmt += Deduct_Amt;
            //                    TDiscountAmt += Discount_Amt;
            //                    //TAClaimAmt = 0;
            //                    TMedicalDeductAmt += Medical_DeductAmt;
            //                    TDifferenceAmt += Difference_Amt;
            //                    NetClaimedAmt += Net_Payment_Amt;
            //                    TRequestedAmt += Requested_Amt;
            //                    ClaimDeductAmt += Service_Deduct_Amt;
            //                    //TAuditDeductAmt = 0;


            //                }



            //                using (var database = new MedInsuranceProEntities())
            //                {

            //                    var foo = database.CLMClaims.Where(f => f.ClaimID == calimid1).FirstOrDefault();
            //                    foo.TClaimAmt = TClaim_Amt;
            //                    foo.TDeductAmt = TDeductAmt;
            //                    foo.TDiscountAmt = TDiscountAmt;
            //                    //foo.TAClaimAmt = TClaim_Amt;
            //                    foo.TMedicalDeductAmt = TMedicalDeductAmt;
            //                    foo.TDifferenceAmt = TDifferenceAmt;
            //                    foo.NetClaimedAmt = NetClaimedAmt;
            //                    foo.TRequestedAmt = TRequestedAmt;
            //                    foo.ClaimDeductAmt = ClaimDeductAmt;
            //                    //foo.TAuditDeductAmt = TClaim_Amt;
            //                    database.SaveChanges();
            //                }


        }

        public async Task<JsonResult> DeactiveAlert(string id)
        {
            try { 
            await Task.Delay(1);

            using (var database = new MedInsuranceProEntities())
            {
                var foo = database.AlertProviderDatas.OrderByDescending(f => f.AlertProviderID).FirstOrDefault();
                foo.ClaimFormNum = 0;
                foo.CardNumber = "0";
                database.SaveChanges();
            }
            int providerid1 = int.Parse(Session["providerid"].ToString());
            int branchid = int.Parse(Session["BranchID"].ToString());
            var ClaimFormNumbers = (from values in db.CLMClaimsONLINEs
                                    where values.ClaimStatusID == 2 && values.ProviderID == providerid1 && values.BranchID == branchid
                                   select values.ClaimID).Count();

            return Json(ClaimFormNumbers, JsonRequestBehavior.AllowGet);
            }
            catch
            {
                return Json(0, JsonRequestBehavior.AllowGet);

            }
        }


        public async Task<JsonResult> DeactiveAlertSmartCard(string id)
        {
            try { 
            await Task.Delay(1);

            using (var database = new MedInsuranceProEntities())
            {
                var foo = database.AlertSmartCardDatas.OrderByDescending(f => f.AlertProviderCardID).FirstOrDefault();
                foo.CardNumber = "0";
                database.SaveChanges();
            }
            int providerid1 = int.Parse(Session["providerid"].ToString());
            int branchid = int.Parse(Session["BranchID"].ToString());
            var datetimeActive = DateTime.Now.AddDays(-7);
            var ClaimFormNumbers = (from values in db.SmartCardRequestDatas
                                    where values.SmartCardRequestStatus == 1 && values.ProviderID == providerid1 && values.BranchID == branchid && values.EntryDate >= datetimeActive
                                    select values.SmartCardRequestID).Count();
            return Json(ClaimFormNumbers, JsonRequestBehavior.AllowGet);
            }
            catch
            {
                return Json(0, JsonRequestBehavior.AllowGet);

            }
        }


        public async Task<JsonResult> getcountallert()
        {
            try { 
            await Task.Delay(1);

            int providerid1 = int.Parse(Session["providerid"].ToString());
            int branchid = int.Parse(Session["BranchID"].ToString());
            var ClaimFormNumbers = (from values in db.CLMClaimsONLINEs
                                    where values.ClaimStatusID == 2 && values.ProviderID == providerid1 && values.BranchID == branchid
                                    select values.ClaimID).Count();

            return Json(ClaimFormNumbers, JsonRequestBehavior.AllowGet);
            }
            catch
            {
                return Json(0, JsonRequestBehavior.AllowGet);

            }
        }


        public async Task<JsonResult> getcountallertSmartCard()
        {
            try { 
            await Task.Delay(1);

                                    //select values.ClaimID).Count();
            int providerid1 = int.Parse(Session["providerid"].ToString());
            int branchid = int.Parse(Session["BranchID"].ToString());
            var datetimeActive = DateTime.Now.AddDays(-7);
            var ClaimFormNumbers = (from values in db.SmartCardRequestDatas
                                    where values.SmartCardRequestStatus == 1 && values.ProviderID == providerid1 && values.BranchID == branchid && values.EntryDate >= datetimeActive
                                    select values.SmartCardRequestID).Count();
            return Json(ClaimFormNumbers, JsonRequestBehavior.AllowGet);
            }
            catch
            {
                return Json(0, JsonRequestBehavior.AllowGet);

            }
        }
        public async Task<JsonResult> getcountmessage(/*int Provider_id,int Branch_id*/)
        {
            try
            {
                await Task.Delay(1);

                //int providerid1 = Provider_id;
                //int branchid = Branch_id;
                int providerid1 = int.Parse(Session["providerid"].ToString());
                int branchid = int.Parse(Session["BranchID"].ToString());
                var ProvidernotShowMessages = (from values in db.Messangers
                                               where values.ProviderShow == false && values.ProviderID == providerid1 && values.BranchID == branchid
                                               select values.MessageID).Count();

                return Json(ProvidernotShowMessages, JsonRequestBehavior.AllowGet);
            }
            catch
            {
                return Json(0, JsonRequestBehavior.AllowGet);

            }
        }
        public async Task<JsonResult> showallmessage(/*int Provider_id, int Branch_id*/)
        {
            try
            {
                await Task.Delay(1);

            int providerid1 = int.Parse(Session["providerid"].ToString());
            int branchid = int.Parse(Session["BranchID"].ToString());
            //int providerid1 = Provider_id;
            //int branchid = Branch_id;
            var allmsg = db.Messangers.Where(d => d.ProviderID == providerid1 && d.BranchID == branchid && d.ProviderShow == false).ToList();
            foreach (var item in allmsg)
            {
                var messid = item.MessageID;
                using (var database = new MedInsuranceProEntities())
                {
                    var foo = database.Messangers.Where(f => f.MessageID==messid).FirstOrDefault();
                    foo.ProviderShow = true;
                    database.SaveChanges();
                }

            }
            return Json("", JsonRequestBehavior.AllowGet);
            }
            catch
            {
                return Json("", JsonRequestBehavior.AllowGet);

            }
        }
        [HttpGet]
        public ActionResult ProviderClaimFormApproved()
        {
            if ((Session["UserID"] == null) || (int.Parse(Session["ClaimForm_Approved"].ToString()) == 0))
            {
                return RedirectToAction("login", "ClaimForm");
            }

            int providerid1 = int.Parse(Session["providerid"].ToString());
            int branchid = int.Parse(Session["BranchID"].ToString());
            var ClaimFormNumbers = (from values in db.CLMClaimsONLINEs
                                    where values.ClaimStatusID == 2 && values.ProviderID == providerid1 && values.BranchID == branchid
                                    select values.ClaimFormNum + " ## " + values.CardNumber + " ## " + values.ClaimID).ToArray();
            ViewBag.lis = ClaimFormNumbers;


            return View();
        }

        public JsonResult AddRealtimeApproval(string Card_Number, string ClaimForm_Num, string Provider_ID, string Branch_ID)
        {
            var ClaimForm_Num1 = long.Parse(ClaimForm_Num);
            var Provider_ID1 = int.Parse(Provider_ID);
            var Branch_ID1 = int.Parse(Branch_ID);
            var ClaimFormNumbers = (from values in db.CLMClaimsONLINEs
                                    where values.ClaimStatusID == 2 && values.CardNumber == Card_Number && values.ClaimFormNum == ClaimForm_Num1 && values.ProviderID == Provider_ID1 && values.BranchID == Branch_ID1
                                    orderby values.ClaimID descending
                                    select values.ClaimFormNum + " ## " + values.CardNumber + " ## " + values.ClaimID).FirstOrDefault();


            return Json(ClaimFormNumbers, JsonRequestBehavior.AllowGet);
        }
       public JsonResult Provider_Approve_ClaimForm(int claim_id)
        {

            var clmclaimsitemsdata_online = db.CLMClaimsItemsONLINEs.Where(d => d.ClaimID == claim_id && d.ClaimItemStatusID == 3).ToList();
            if (clmclaimsitemsdata_online.Count == 0)
            {
                using (var database = new MedInsuranceProEntities())
                {

                    var foo = database.CLMClaimsONLINEs.Where(f => f.ClaimID == claim_id).FirstOrDefault();
                    foo.ClaimStatusID = 3;
                    database.SaveChanges();
                }
                var arrlistt = new ArrayList();

                int provideri = int.Parse(Session["providerid"].ToString());
                int branchi = int.Parse(Session["BranchID"].ToString());

                var ClaimFormNumberss = (from values in db.CLMClaimsONLINEs
                                        where values.ClaimStatusID == 2 && values.ProviderID == provideri && values.BranchID == branchi
                                        select values.ClaimFormNum + " ## " + values.CardNumber + " ## " + values.ClaimID).ToArray();
                arrlistt.Add("aaaa");
                arrlistt.Add(ClaimFormNumberss);

                return Json(arrlistt, JsonRequestBehavior.AllowGet);
            }
            var claimformdata_online = db.CLMClaimsONLINEs.FirstOrDefault(d => d.ClaimID == claim_id);
            int calimid = 0;
            try
            {
                var maxvalu = db.CLMClaims.Max(o => o.ClaimID);
                var resul = db.CLMClaims.First(o => o.ClaimID == maxvalu);
                calimid = (resul.ClaimID) + 1;
            }
            catch
            {
                 calimid = 1;
            }
            string card_num = claimformdata_online.CardNumber;
            DateTime? DatProcedure = claimformdata_online.ProcedureDate;

            var cont_num = (
from item1 in db.ContractMembers
join item2 in db.Contracts on item1.ContractID equals item2.ContractID into finalGroup0
from finalItem0 in finalGroup0.DefaultIfEmpty()
where finalItem0.StartDate <= DatProcedure && finalItem0.EndDate >= DatProcedure && item1.CardNumber == card_num
select new { finalItem0.ContractNumber, item1.ContractMemberID, finalItem0.ContractID, item1.PlanTypeID, finalItem0.StartDate, finalItem0.EndDate }).OrderByDescending(aa => aa.ContractID).FirstOrDefault();

            //string contract_num = cont_num[0].contractnumber;
            int contract_member_id = cont_num.ContractMemberID;
            int contract_id = cont_num.ContractID;
            int plan_type_id = cont_num.PlanTypeID;


            var claim_form_num = long.Parse(claimformdata_online.ClaimFormNum.ToString());
            int? contractmember_id = claimformdata_online.ContractMemberID;
            DateTime? symptoms_date = claimformdata_online.symptomsDate;
            DateTime? procedure_date = claimformdata_online.ProcedureDate;
            int? claimservice_id = claimformdata_online.ClaimServiceID;
            string diagnosiss = claimformdata_online.Diagnosis;
            string icd_code = claimformdata_online.ICDCode;
            string contract_num = claimformdata_online.ContractNum;
            string card_number = claimformdata_online.CardNumber;
            int providerid = int.Parse(claimformdata_online.ProviderID.ToString());
            int? ProviderCat_ID = claimformdata_online.ProviderCatID;
            var provider_name = db.PVProviderDatas.FirstOrDefault(d => d.ProviderID == providerid).ProviderName;
            string branchname = claimformdata_online.BranchName;

            var T_ClaimAmt = clmclaimsitemsdata_online.Sum(d => d.ServicePrice * d.ServiceQnt);
            var claimsdata_insertedfrombefore = db.CLMClaims.Where(d => d.RecBatchID == 0 && d.ProviderID == providerid && d.ProviderName == provider_name && d.BranchName == branchname).OrderByDescending(d => d.ClaimID).FirstOrDefault();
            //&& d.ClaimFormNum == claim_form_num && d.ContractMemberID == contractmember_id && d.symptomsDate == symptoms_date && d.ProcedureDate == procedure_date && d.ClaimServiceID == claimservice_id && d.Diagnosis == diagnosiss && d.ICDCode == icd_code &&
            if (claimsdata_insertedfrombefore != null)
            {
                if (claimsdata_insertedfrombefore.ClaimFormNum == claim_form_num && claimsdata_insertedfrombefore.ContractMemberID == contractmember_id
                    && claimsdata_insertedfrombefore.symptomsDate == symptoms_date && claimsdata_insertedfrombefore.ProcedureDate == procedure_date
                    && claimsdata_insertedfrombefore.ClaimServiceID == claimservice_id && claimsdata_insertedfrombefore.Diagnosis == diagnosiss
                    && claimsdata_insertedfrombefore.ICDCode == icd_code && claimsdata_insertedfrombefore.TClaimAmt == T_ClaimAmt)
                {
                    var arrlistt = new ArrayList();

                    int provideri = int.Parse(Session["providerid"].ToString());
                    int branchi = int.Parse(Session["BranchID"].ToString());

                    var ClaimFormNumberss = (from values in db.CLMClaimsONLINEs
                                             where values.ClaimStatusID == 2 && values.ProviderID == provideri && values.BranchID == branchi
                                             select values.ClaimFormNum + " ## " + values.CardNumber + " ## " + values.ClaimID).ToArray();
                    arrlistt.Add("bbbb");
                    arrlistt.Add(ClaimFormNumberss);

                    return Json(arrlistt, JsonRequestBehavior.AllowGet);
                }

            }
            CLMClaim newdataa = new CLMClaim()
            {

                ClaimID = calimid,
                RecBatchID = 0,
                ClaimFormNum = claim_form_num,
                ContractMemberID = contractmember_id,
                symptomsDate = symptoms_date,
                ProcedureDate = procedure_date,
                ClaimServiceID = claimservice_id,
                Diagnosis = diagnosiss,
                ICDCode = icd_code,
                TClaimAmt = 0,
                TDeductAmt = 0,
                TDiscountAmt = 0,
                TAClaimAmt = 0,
                TMedicalDeductAmt = 0,
                TDifferenceAmt = 0,
                ClaimStatusID = 1,
                // will edit 
                UserAddID = 0,
                UserApproveID = 0,
                NetClaimedAmt = 0,
                TRequestedAmt = 0,
                BatchNumber = "",
                ContractNum = contract_num,
                CardNumber = card_number,
                ClaimDeductAmt = 0,
                ClaimDeductNote = "",
                TAuditDeductAmt = 0,
                IsOverCeiling = false,
                RepType = false,
                ProviderID = providerid,
                ProviderName = provider_name,
                BranchName = branchname

            };
            db.CLMClaims.Add(newdataa);
            db.SaveChanges();


            //var maxvalu1 = db.clmclaims.max(o => o.claimid);
            //var resul1 = db.clmclaims.first(o => o.claimid == maxvalu1);
            //int calimid1 = resul1.claimid;
            var claim_dataa = db.CLMClaims.Where(d => d.RecBatchID == 0 && d.ClaimFormNum == claim_form_num && d.ProviderID == providerid).OrderByDescending(d => d.ClaimID).FirstOrDefault();
            var claim_id_inserted = claim_dataa.ClaimID;
            //clmclaimitem
            int claimitemid = 0;
            try
            {
                var maxval = db.CLMClaimItems.Max(o => o.ClaimItemID);
                var resu = db.CLMClaimItems.First(o => o.ClaimItemID == maxval);
                claimitemid = resu.ClaimItemID;
            }
            catch
            {
               claimitemid = 1;
            }
            int provi_id = providerid;
            var re3 = db.PVContracts
                                .Where(m => m.ProviderID == provi_id && m.StartDate <= procedure_date && m.EndDate >= procedure_date)
                                .OrderByDescending(m => m.ProviderContractID)
                                .FirstOrDefault();
            if (re3 == null)
            {
                var re32 = db.PVContracts
                        .Where(m => m.ProviderID == provi_id && m.IsActive == true)
                        .OrderByDescending(m => m.ProviderContractID)
                        .FirstOrDefault();
                re3 = re32;
            }
            int provider_contract_id = re3.ProviderContractID;
            decimal local_discount_per = re3.LocalDiscountPer;
            decimal foreign_discount_per = re3.ForeignDiscountPer;
            decimal? chronic_localdis_per = re3.ChronicLocalDisPer;
            decimal? chronic_foreign_disper = re3.ChronicForeignDisPer;


            decimal deductable = 0;

            decimal tclaim_amt = 0;
            decimal tdeductamt = 0;
            decimal tdiscountamt = 0;
            //decimal taclaimamt = 0;
            decimal tmedicaldeductamt = 0;
            decimal tdifferenceamt = 0;
            decimal netclaimedamt = 0;
            decimal trequestedamt = 0;
            decimal claimdeductamt = 0;
            //decimal tauditdeductamt = 0;
            IList<CLMClaimItem> CLMClaimItems = new List<CLMClaimItem>();

            for (var i = 0; i < clmclaimsitemsdata_online.Count; i++)
            {

                claimitemid++;
                // get service code
                string service_namee = clmclaimsitemsdata_online[i].ServiceName;

                int service_id = clmclaimsitemsdata_online[i].ServiceID;
                string service_code = clmclaimsitemsdata_online[i].ServiceCode;

                bool is_import = false;
                bool is_coverd = false;

                decimal service_price = decimal.Parse(clmclaimsitemsdata_online[i].ServicePrice.ToString());
                decimal Number_of_timesday = decimal.Parse(clmclaimsitemsdata_online[i].Number_of_times_a_day.ToString());
                decimal Dosage_sizes = decimal.Parse(clmclaimsitemsdata_online[i].Dosage_size.ToString());
                decimal Number_of_dayss = decimal.Parse(clmclaimsitemsdata_online[i].Number_of_days.ToString());
                //int Number_of_daysss = int.TryParse(Number_of_dayss.ToString());
                int Number_of_daysss = Decimal.ToInt32(Number_of_dayss);
                DateTime EndDate_Effective = DateTime.Now.AddDays(Number_of_daysss);
                decimal service_discountperc = 0;
                decimal service_discountval = 0;
                if (ProviderCat_ID != 2)
                {

                    var re4 = db.PVServicePricings
                            .Where(d => d.ProviderContractID == provider_contract_id && d.ServiceID == service_id)
                            .OrderByDescending(m => m.ServicePricingID)
                            .FirstOrDefault();
                    //var re4 = db.pvservicepricings.firstordefault(d => d.providercontractid == provider_contract_id && d.serviceid== service_id);
                    service_discountperc = re4.DiscountPerc;
                    service_discountval = re4.DiscountVal;

                }
                else
                {
                    //var re2 = db.PVMedDatas.FirstOrDefault(d => d.MedDrugName.Replace(" ", string.Empty).Contains(service_namee.Replace(" ", string.Empty)));
                    var re2 = db.PVMedDatas.FirstOrDefault(d => d.MedDrugID == service_id);
                    int med_cat = re2.MedCatID;
                    is_import = re2.IsImported;
                    is_coverd = re2.IsCoverd;

                    var PVMedExceptionData = db.PVMedExceptions.FirstOrDefault(d => d.MedDrugName.Replace(" ", string.Empty) == (service_namee.Replace(" ", string.Empty)) && d.ProviderContractID == provider_contract_id);
                    if (PVMedExceptionData != null)
                    {
                        local_discount_per = PVMedExceptionData.DiscountPer;
                        foreign_discount_per = PVMedExceptionData.DiscountPer;
                        chronic_localdis_per = PVMedExceptionData.DiscountPer;
                        chronic_foreign_disper = PVMedExceptionData.DiscountPer;


                    }
                    else
                    {
                        local_discount_per = re3.LocalDiscountPer;
                        foreign_discount_per = re3.ForeignDiscountPer;
                        chronic_localdis_per = re3.ChronicLocalDisPer;
                        chronic_foreign_disper = re3.ChronicForeignDisPer;

                    }
                    service_discountperc = 0;

                    if (is_import == true)
                    {
                        service_discountperc = foreign_discount_per;
                    }
                    else
                    {
                        service_discountperc = local_discount_per;
                    }

                    service_discountval = service_discountperc * service_price;

                }
                int claim_service_id1 = clmclaimsitemsdata_online[i].ClaimServiceID;

                decimal deduct_per = 0;
                if (claimservice_id == 14 || claimservice_id == 19 || claim_service_id1 == 14 || claim_service_id1 == 19)
                {
                    deduct_per = 0;
                }
                else
                {
                    var re5 = db.ContractServiceCeilings.FirstOrDefault(d => d.ContractID == contract_id && d.PlanTypeID == plan_type_id && d.ServiceID == claim_service_id1);
                    deduct_per = re5.DeductPer;
                }
                deductable = deduct_per;

                //var re5 = db.contractserviceceilings.firstordefault(d => d.contractid == contract_id && d.plantypeid == plan_type_id && d.serviceid == claimservice_id);
                //decimal deduct_per = re5.deductper;

                decimal service_deduct_amt = deduct_per * service_price;
                decimal net_service_amt = service_price - service_deduct_amt - service_discountval;

                decimal service_request = decimal.Parse(clmclaimsitemsdata_online[i].RequestedPrice.ToString());


                decimal service_qnt = decimal.Parse(clmclaimsitemsdata_online[i].ServiceQnt.ToString());
                decimal service_unitamt = service_price;
                decimal claimed_amt = service_unitamt * service_qnt;

                decimal claimedRequest_amt = service_request * service_qnt;

                decimal deduct_amt = claimedRequest_amt * deduct_per;
                //decimal deduct_amt = claimed_amt * deduct_per;
                decimal discount_amt = claimed_amt * service_discountperc;
                decimal medical_deductamt = 0;
                decimal due_amt = claimed_amt - deduct_amt - discount_amt;
                //decimal requested_amt = due_amt;
                //decimal difference_amt = 0;

                decimal requested_amt = (service_request* service_qnt) - deduct_amt - discount_amt;
                decimal difference_amt = requested_amt- due_amt;


                decimal net_payment_amt = due_amt;

                //string reject_reson = "";
                string reject_reson = clmclaimsitemsdata_online[i].Refuse_Note;

                //int a = claimservice_id;

                CLMClaimItem newdataaa = new CLMClaimItem()
                {

                    ClaimItemID = claimitemid,
                    ClaimID = claim_id_inserted,
                    ServiceID = service_id,
                    ServiceCode = service_code,
                    ServiceName = service_namee,
                    ServiceQnt = service_qnt,
                    ServiceUnitAmt = service_unitamt,
                    ClaimedAmt = claimed_amt,
                    DeductAmt = deduct_amt,
                    DiscountAmt = discount_amt,
                    FDiscountAmt = 0,
                    MedicalDeductAmt = medical_deductamt,
                    DueAmt = due_amt,
                    RequestedAmt = requested_amt,
                    DifferenceAmt = difference_amt,
                    RejectNotes = reject_reson,
                    IsSysEntry = false,
                    // will edit
                    UserEntryID = 0,
                    UserEntryDate = DateTime.Now,
                    UserApprove = false,
                    UserApproveID = 0,
                    BatchNumber = "",
                    ClaimNum = claim_form_num,
                    ServiceAmt = service_price,
                    ServiceDeductAmt = service_deduct_amt,
                    ServiceDiscountAmt = service_discountval,
                    NetServiceAmt = net_service_amt,
                    ClaimServiceID = claim_service_id1,
                    ServiceDiscountPer = service_discountperc,
                    IsCovered = is_coverd,
                    IsImported = is_import,
                    NetPaymentAmt = net_payment_amt,
                    ReimbPer = 0,
                    AuditDeductAmt = 0,
                    AuditComments = "",
                    Number_of_times_a_day = Number_of_timesday,
                    Dosage_size = Dosage_sizes,
                    Number_of_days = Number_of_dayss,
                    EndDate_Effective_Material = EndDate_Effective,
                    IsOverCeiling = false

                };
                //db.CLMClaimItems.Add(newdataaa);
                //db.SaveChanges();
                CLMClaimItems.Add(newdataaa);

                tclaim_amt += claimed_amt;
                tdeductamt += deduct_amt;
                tdiscountamt += discount_amt;
                //taclaimamt = 0;
                tmedicaldeductamt += medical_deductamt;
                tdifferenceamt += difference_amt;
                netclaimedamt += net_payment_amt;
                trequestedamt += requested_amt;
                claimdeductamt += service_deduct_amt;
                //tauditdeductamt = 0;


            }
            using (var context = new MedInsuranceProEntities())
            {
                context.CLMClaimItems.AddRange(CLMClaimItems);
                context.SaveChanges();
            }


            using (var database = new MedInsuranceProEntities())
            {

                var foo = database.CLMClaims.Where(f => f.ClaimID == claim_id_inserted).FirstOrDefault();
                foo.TClaimAmt = tclaim_amt;
                foo.TDeductAmt = tdeductamt;
                foo.TDiscountAmt = tdiscountamt;
                //foo.taclaimamt = tclaim_amt;
                foo.TMedicalDeductAmt = tmedicaldeductamt;
                foo.TDifferenceAmt = tdifferenceamt;
                foo.NetClaimedAmt = netclaimedamt;
                foo.TRequestedAmt = trequestedamt;
                foo.ClaimDeductAmt = claimdeductamt;
                //foo.tauditdeductamt = tclaim_amt;
                database.SaveChanges();
            }

            using (var database = new MedInsuranceProEntities())
            {

                var foo = database.CLMClaimsONLINEs.Where(f => f.ClaimID == claim_id).FirstOrDefault();
                foo.ClaimStatusID = 3;
                database.SaveChanges();
            }
            var arrlist = new ArrayList();
            arrlist.Add(deductable * 100);
            arrlist.Add(tdeductamt);
            arrlist.Add(tclaim_amt);

            int providerid1 = int.Parse(Session["providerid"].ToString());
            int branchid = int.Parse(Session["BranchID"].ToString());

            var ClaimFormNumbers = (from values in db.CLMClaimsONLINEs
                                    where values.ClaimStatusID == 2 && values.ProviderID == providerid1 && values.BranchID == branchid
                                    select values.ClaimFormNum + " ## " + values.CardNumber + " ## " + values.ClaimID).ToArray();
            arrlist.Add(ClaimFormNumbers);
            arrlist.Add(claim_id_inserted);

            return Json(arrlist, JsonRequestBehavior.AllowGet);
        }

        public JsonResult Provider_Cancel_ClaimForm(int claim_id)
        {


            var claimformdata_online = db.CLMClaimsONLINEs.FirstOrDefault(d => d.ClaimID == claim_id);
            db.CLMClaimsONLINEs.Remove(claimformdata_online);
            db.SaveChanges();

            IEnumerable<CLMClaimsItemsONLINE> cs = db.CLMClaimsItemsONLINEs.Where(c => c.ClaimID == claim_id).ToList();
            db.CLMClaimsItemsONLINEs.RemoveRange(cs);
            db.SaveChanges();

            //var arrlistt = new ArrayList();

            int provideri = int.Parse(Session["providerid"].ToString());
            int branchi = int.Parse(Session["BranchID"].ToString());


            var ClaimFormNumberss = (from values in db.CLMClaimsONLINEs
                                         where values.ClaimStatusID == 2 && values.ProviderID == provideri && values.BranchID == branchi
                                         select values.ClaimFormNum + " ## " + values.CardNumber + " ## " + values.ClaimID).ToArray();

                return Json(ClaimFormNumberss, JsonRequestBehavior.AllowGet);

        }

        public JsonResult Provider_Resend_ClaimForm(int claim_id)
        {

            //var claimformdata_online = db.CLMClaimsONLINEs.FirstOrDefault(d => d.ClaimID == claim_id);
            //db.CLMClaimsONLINEs.Remove(claimformdata_online);
            //db.SaveChanges();

            using (var database = new MedInsuranceProEntities())
            {
                var foo = database.CLMClaimsONLINEs.Where(f => f.ClaimID == claim_id).FirstOrDefault();
                foo.ClaimStatusID = 1;
                database.SaveChanges();
            }

            IEnumerable<CLMClaimsItemsONLINE> cs = db.CLMClaimsItemsONLINEs.Where(c => c.ClaimID == claim_id).ToList();
            foreach (var item in cs)
            {
                using (var database = new MedInsuranceProEntities())
                {
                    var ClaimItem_ID = item.ClaimItemID;
                    var foo = database.CLMClaimsItemsONLINEs.Where(f => f.ClaimItemID == ClaimItem_ID).FirstOrDefault();
                    foo.ClaimItemStatusID = 1;
                    database.SaveChanges();
                }

            }
            //db.CLMClaimsItemsONLINEs.RemoveRange(cs);
            //db.SaveChanges();
            var claim_dataa = db.CLMClaimsONLINEs.Where(f => f.ClaimID == claim_id).FirstOrDefault();

            using (var database = new MedInsuranceProEntities())
            {
                var foo = database.AlertCMCDatas.OrderByDescending(f => f.AlertCMCID).FirstOrDefault();
                foo.ClaimFormNum = claim_dataa.ClaimFormNum;
                foo.CardNumber = claim_dataa.CardNumber;
                foo.BranchName = claim_dataa.BranchName;
                foo.ProviderID = claim_dataa.ProviderID;
                foo.BranchID = claim_dataa.BranchID;
                database.SaveChanges();
            }

            int provideri = int.Parse(Session["providerid"].ToString());
            int branchi = int.Parse(Session["BranchID"].ToString());

            var ClaimFormNumberss = (from values in db.CLMClaimsONLINEs
                                     where values.ClaimStatusID == 2 && values.ProviderID == provideri && values.BranchID == branchi
                                     select values.ClaimFormNum + " ## " + values.CardNumber + " ## " + values.ClaimID).ToArray();

            return Json(ClaimFormNumberss, JsonRequestBehavior.AllowGet);

        }

        [HttpGet]
        public ActionResult Provider_Expended_services()
        {

            if ((Session["UserID"] == null) || (int.Parse(Session["Expanded_Services"].ToString()) == 0))
            {
                return RedirectToAction("login", "ClaimForm");
            }
            int providerid = int.Parse(Session["providerid"].ToString());


            var res = (from values in db.CLMClaims
                           //join item1 in db.CLMClaimItems on values.ClaimID equals item1.ClaimID
                       where values.ProviderID == providerid && values.RecBatchID == 0 && values.RepType == true
                       select new { values.ClaimFormNum, values.CardNumber, values.ProcedureDate, values.TClaimAmt, values.TDeductAmt, values.TDiscountAmt, values.NetClaimedAmt, values.TRequestedAmt, values.TDifferenceAmt, values.ClaimID, values.BranchName }).OrderBy(d => d.ClaimID).ToList();

            ViewBag.services = res;

            return View();
        }

        public class Expanded_Services
        {
            public int Claim_Id { get; set; }
            public string ClaimForm_Number { get; set; }
            public string Card_Number { get; set; }
            public DateTime Procedure_Date { get; set; }
            public decimal Contract_Amt { get; set; }
            public decimal Deductable_Amt { get; set; }
            public decimal Discount_Amt { get; set; }
            public decimal Due_Amt { get; set; }
            public decimal Requested_Amt { get; set; }
           
        }
        public JsonResult Send_to_payy(List<Expanded_Services> Expanded_Services)
        {
            if (Expanded_Services == null)
            {
                return Json("0", JsonRequestBehavior.AllowGet);

            }
            var claim_id0 = Expanded_Services[0].Claim_Id;
            var provider_data = db.CLMClaims.FirstOrDefault(d => d.ClaimID == claim_id0);
            var provider_id = provider_data.ProviderID;
            var recbatchiddd = provider_data.RecBatchID;
            if (recbatchiddd != 0)
            {
                return Json("1", JsonRequestBehavior.AllowGet);

            }


            var rec = db.PVProviderDatas.FirstOrDefault(d => d.ProviderID == provider_id);
            int paymentday = int.Parse(rec.PaymentPeriod.ToString());
            var paymentdate = DateTime.Now.AddDays(paymentday);

            var claimsnum = Expanded_Services.Count;

            var claimtotalamt = Expanded_Services.Sum(d => d.Requested_Amt);
            long lastbatchnumber = 0;
            try
            {
                var maxValue = db.CLMRecBatches.Where(a => a.CustomerID == 0).Max(o => o.RecBatchID);
                var result = db.CLMRecBatches.First(o => o.RecBatchID == maxValue);
                 lastbatchnumber = long.Parse(result.BatchNumber);
            }
            catch
            {
                lastbatchnumber = 1200000000;
            }
            var batchnumber = (lastbatchnumber + 1).ToString();
            var batchdata = db.CLMRecBatches.FirstOrDefault(d => d.BatchNumber == batchnumber);
            while (batchdata != null)
            {
                batchnumber = (batchnumber + 1).ToString();
                batchdata = db.CLMRecBatches.FirstOrDefault(d => d.BatchNumber == batchnumber);
            }
            CLMRecBatch newdataa = new CLMRecBatch()
            {
                ProviderID = provider_id,
                RecDate = DateTime.Now,
                PaymentDate = paymentdate,
                ClaimsNum = claimsnum,
                DeliverBy = "ONLINE",
                ClaimTotalAmt = claimtotalamt,
                BatchNumber = batchnumber,
                IsImported = false,
                ProviderNumber = "",
                CustomerID = 0,
                BClaimedAmt = 0,
                BDeductAmt = 0,
                BDiscountAmt = 0,
                WithHoldingTaxPer = 0,
                WithHoldingTaxAmt = 0,
                BDuteAmt = 0,
                BatchStatus = 1,
                BClaimedNetAmt = 0,
                BMedicalDeductAmt = 0,
                ProviderBranchid = "",

            };
            db.CLMRecBatches.Add(newdataa);
            db.SaveChanges();

            var lastrecbatch = db.CLMRecBatches.Where(d => d.BatchNumber == batchnumber && d.ProviderID == provider_id && d.DeliverBy == "ONLINE").OrderByDescending(d => d.RecBatchID).FirstOrDefault().RecBatchID;
            foreach (var item in Expanded_Services)
            {

                using (var database = new MedInsuranceProEntities())
                {
                    var Clai_Id = item.Claim_Id;
                    var foo = database.CLMClaims.Where(f => f.ClaimID == Clai_Id).FirstOrDefault();
                    foo.RecBatchID = lastrecbatch;
                    foo.BatchNumber = batchnumber;
                    database.SaveChanges();
                }
            }

            return Json(batchnumber, JsonRequestBehavior.AllowGet);
        }

        [HttpGet]
        public ActionResult PaymentInquiry()
        {

            if ((Session["UserID"] == null) || (int.Parse(Session["Expanded_Services"].ToString()) == 0))
            {
                return RedirectToAction("login", "ClaimForm");
            }
            int providerid = int.Parse(Session["providerid"].ToString());

            var res = (from values in db.LQBatchPaymentStatus
                       join item1 in db.CLMRecBatches on values.BatchNumber equals item1.BatchNumber
                       where item1.DeliverBy == "ONLINE" && item1.ProviderID == providerid
                       select new { ProviderName = values.ProviderName??"", BatchNumber=values.BatchNumber??"", RecDate=values.RecDate??new DateTime(), DueDate= values.DueDate ?? new DateTime(), PaymentDate=values.PaymentDate ?? new DateTime(), RecBatchAmt = values.RecBatchAmt??0, PaymentMethodName=values.PaymentMethodName??"", BatchStatusName=values.BatchStatusName ?? "", Expr2=values.Expr2??0 , WHTaxAmt=values.WHTaxAmt ?? 0, AdminFeesAmt=values.AdminFeesAmt ?? 0, PaymentAmt=values.PaymentAmt ?? 0, DueNetPaymentAmt=values.DueNetPaymentAmt ?? 0, rest= (values.PaymentAmt-values.DueNetPaymentAmt) ?? 0, PayamentID=values.PayamentID ?? 0 }).OrderBy(d => d.RecDate).ToList();

            ViewBag.services = res;

            return View();
        }

        public JsonResult ProvExpended_services()
        {
            int providerid = int.Parse(Session["providerid"].ToString());


            var res = (from values in db.CLMClaims
                       join item1 in db.CLMClaimItems on values.ClaimID equals item1.ClaimID
                       where values.ProviderID == providerid && values.RecBatchID == 0
                       select new { item1.ServiceName, item1.ServiceQnt, item1.ClaimedAmt, item1.DeductAmt, item1.DiscountAmt, item1.DueAmt }).ToList();

            return Json(res, JsonRequestBehavior.AllowGet);
        }

        [HttpGet]
        public ActionResult Invoices_Audit()
        {

            if ((Session["UserID"] == null) || (int.Parse(Session["Invoices_Audit"].ToString()) == 0))
            {
                return RedirectToAction("login", "ClaimForm");
            }
            //int providerid = int.Parse(Session["providerid"].ToString());


            //var res = (from values in db.CLMClaims
            //               //join item1 in db.CLMClaimItems on values.ClaimID equals item1.ClaimID
            //           where values.ProviderID == providerid && values.RecBatchID == 0
            //           select new { values.ClaimFormNum, values.CardNumber, values.ProcedureDate, values.TClaimAmt, values.TDeductAmt, values.TDiscountAmt, values.NetClaimedAmt, values.TRequestedAmt, values.TDifferenceAmt, values.ClaimID, values.BranchName }).OrderBy(d => d.ClaimID).ToList();

            //ViewBag.services = res;

            return View();
        }

        public class OrderListViewModel
        {
           
            public int ClaimID { get; set; }
            public long? ClaimFormNum { get; set; }
            public string CardNumber { get; set; }
            public string MemberName { get; set; }
            public string ProcedureDate { get; set; }
            public decimal? TClaimAmt { get; set; }
            public decimal? TDeductAmt { get; set; }
            public decimal? TDiscountAmt { get; set; }
            public decimal? NetClaimedAmt { get; set; }
            public decimal? TRequestedAmt { get; set; }
            public decimal? TDifferenceAmt { get; set; }
            public string BranchName { get; set; }

            public List<OrderListDetailViewModel> OrderListDetails { get; set; }

        }
        public class OrderListDetailViewModel
        {
            public int ClaimID { get; set; }
            public int ClaimitemID { get; set; }
            public string ServiceName { get; set; }
            public decimal ServiceQnt { get; set; }
            public decimal ServiceUnitAmt { get; set; }
            public decimal ServiceAmt { get; set; }
            public decimal ServiceDeductAmt { get; set; }
            public decimal? ServiceDiscountAmt { get; set; }
            public decimal ServiceDueAmt { get; set; }
            public decimal? ServiceRequestedAmt { get; set; }
            public decimal? ServiceDifferenceAmt { get; set; }
            public string ServiceNote { get; set; }
        }
        public JsonResult GetOrderList()
        {
            int providerid = int.Parse(Session["providerid"].ToString());
            string branchName = Session["branchName"].ToString();
            bool ProviderAdminn = bool.Parse(Session["ProviderAdmin"].ToString());

            if (ProviderAdminn == true)
            {
                var Approvals = (
                from item1 in db.CLMClaims
                join item2 in db.CLMClaimItems on item1.ClaimID equals item2.ClaimID
            //join item3 in db.ContractMembers on item1.ContractMemberID equals item3.ContractMemberID

            where item1.ProviderID == providerid && item1.RecBatchID == 0 && item1.RepType == false
                orderby item1.ClaimID
                select new
                {
                    item1.ClaimFormNum,
                    item1.CardNumber,
                //item3.MemberName,
                item1.ProcedureDate,
                    item1.TClaimAmt,
                    item1.TDeductAmt,
                    item1.TDiscountAmt,
                    item1.NetClaimedAmt,
                    item1.TRequestedAmt,
                    item1.TDifferenceAmt,
                    item1.ClaimID,
                    item2.ClaimItemID,
                    Address = item1 != null ? item1.BranchName : "",
                    ServiceName = item2 != null ? item2.ServiceName : "",
                    ServiceQnt = item2 != null ? item2.ServiceQnt : 0,
                    ServiceUnitAmt = item2 != null ? item2.ServiceUnitAmt : 0,
                    ServiceAmt = item2 != null ? item2.ClaimedAmt : 0,
                    ServiceDeductAmt = item2 != null ? item2.DeductAmt : 0,
                    ServicediscAmt = item2 != null ? item2.DiscountAmt : 0,
                    ServiceDueAmt = item2 != null ? item2.DueAmt : 0,
                    ServicerequAmt = item2 != null ? item2.RequestedAmt : 0,
                    ServicediffAmt = item2 != null ? item2.DifferenceAmt : 0,
                    ServiceNote = item2 != null ? item2.RejectNotes : ""
                }).ToList();
                var a = Approvals.GroupBy(c => new { c.ClaimID, c.ProcedureDate })
                   .Select(c => new OrderListViewModel()
                   {
                       ClaimID = c.Key.ClaimID,
                       ClaimFormNum = c.FirstOrDefault().ClaimFormNum,
                       CardNumber = c.FirstOrDefault().CardNumber,
                   //MemberName = c.FirstOrDefault().MemberName,
                   ProcedureDate = DateTime.Parse(c.Key.ProcedureDate.ToString()).ToString("yyyy-MM-dd"),
                       TClaimAmt = c.FirstOrDefault().TClaimAmt,
                       TDeductAmt = c.FirstOrDefault().TDeductAmt /*Math.Round((decimal.Parse( c.FirstOrDefault().TDeductAmt .ToString())/ decimal.Parse(c.FirstOrDefault().TClaimAmt.ToString())) *(decimal.Parse(c.FirstOrDefault().TRequestedAmt.ToString()) + decimal.Parse(c.FirstOrDefault().TDiscountAmt.ToString()) + decimal.Parse(c.FirstOrDefault().TDeductAmt.ToString())), 2)*/,
                       TDiscountAmt = c.FirstOrDefault().TDiscountAmt /*Math.Round((decimal.Parse(c.FirstOrDefault().TDiscountAmt.ToString()) / decimal.Parse(c.FirstOrDefault().TClaimAmt.ToString())) * (decimal.Parse(c.FirstOrDefault().TRequestedAmt.ToString()) + decimal.Parse(c.FirstOrDefault().TDiscountAmt.ToString()) + decimal.Parse(c.FirstOrDefault().TDeductAmt.ToString())), 2)*/,
                       NetClaimedAmt = c.FirstOrDefault().NetClaimedAmt,
                       TRequestedAmt = c.FirstOrDefault().TClaimAmt + c.FirstOrDefault().TDifferenceAmt /*c.FirstOrDefault().TRequestedAmt*//*+ Math.Round((decimal.Parse(c.FirstOrDefault().TDeductAmt.ToString()) / decimal.Parse(c.FirstOrDefault().TClaimAmt.ToString())) * (decimal.Parse(c.FirstOrDefault().TRequestedAmt.ToString()) + decimal.Parse(c.FirstOrDefault().TDiscountAmt.ToString()) + decimal.Parse(c.FirstOrDefault().TDeductAmt.ToString())), 2)+ Math.Round((decimal.Parse(c.FirstOrDefault().TDiscountAmt.ToString()) / decimal.Parse(c.FirstOrDefault().TClaimAmt.ToString())) * (decimal.Parse(c.FirstOrDefault().TRequestedAmt.ToString()) + decimal.Parse(c.FirstOrDefault().TDiscountAmt.ToString()) + decimal.Parse(c.FirstOrDefault().TDeductAmt.ToString())), 2)*/,
                       TDifferenceAmt = c.FirstOrDefault().TDifferenceAmt,
                       BranchName = c.FirstOrDefault().Address,
                       OrderListDetails = c.Select(d => new OrderListDetailViewModel()
                       {
                           ClaimID = d.ClaimID,
                           ClaimitemID = d.ClaimItemID,
                           ServiceName = d.ServiceName,
                           ServiceQnt = d.ServiceQnt,
                           ServiceUnitAmt = d.ServiceUnitAmt,
                           ServiceAmt = d.ServiceAmt,
                           ServiceDeductAmt = d.ServiceDeductAmt/*Math.Round((d.ServiceDeductAmt/ d.ServiceAmt) *(d.ServicerequAmt + d.ServiceDeductAmt + d.ServicediscAmt),2)*/,
                           ServiceDiscountAmt = d.ServicediscAmt/* Math.Round((d.ServicediscAmt/ d.ServiceAmt) *(d.ServicerequAmt + d.ServiceDeductAmt + d.ServicediscAmt ),2)*/,
                           ServiceDueAmt = d.ServiceDueAmt,
                           ServiceRequestedAmt = d.ServiceAmt + d.ServicediffAmt /*d.ServicerequAmt+ Math.Round((d.ServiceDeductAmt / d.ServiceAmt) * (d.ServicerequAmt + d.ServiceDeductAmt + d.ServicediscAmt), 2)+ Math.Round((d.ServicediscAmt / d.ServiceAmt) * (d.ServicerequAmt + d.ServiceDeductAmt + d.ServicediscAmt ), 2)*/,
                           ServiceDifferenceAmt = d.ServicediffAmt,
                           ServiceNote = d.ServiceNote
                       }).ToList()
                   }).ToList();

                return Json(a, JsonRequestBehavior.AllowGet);
            }
            else
            {
                var Approvals = (
              from item1 in db.CLMClaims
              join item2 in db.CLMClaimItems on item1.ClaimID equals item2.ClaimID
                //join item3 in db.ContractMembers on item1.ContractMemberID equals item3.ContractMemberID

                where item1.ProviderID == providerid && item1.RecBatchID == 0 && item1.RepType == false && item1.BranchName == branchName
              orderby item1.ClaimID
              select new
              {
                  item1.ClaimFormNum,
                  item1.CardNumber,
                    //item3.MemberName,
                    item1.ProcedureDate,
                  item1.TClaimAmt,
                  item1.TDeductAmt,
                  item1.TDiscountAmt,
                  item1.NetClaimedAmt,
                  item1.TRequestedAmt,
                  item1.TDifferenceAmt,
                  item1.ClaimID,
                  item2.ClaimItemID,
                  Address = item1 != null ? item1.BranchName : "",
                  ServiceName = item2 != null ? item2.ServiceName : "",
                  ServiceQnt = item2 != null ? item2.ServiceQnt : 0,
                  ServiceUnitAmt = item2 != null ? item2.ServiceUnitAmt : 0,
                  ServiceAmt = item2 != null ? item2.ClaimedAmt : 0,
                  ServiceDeductAmt = item2 != null ? item2.DeductAmt : 0,
                  ServicediscAmt = item2 != null ? item2.DiscountAmt : 0,
                  ServiceDueAmt = item2 != null ? item2.DueAmt : 0,
                  ServicerequAmt = item2 != null ? item2.RequestedAmt : 0,
                  ServicediffAmt = item2 != null ? item2.DifferenceAmt : 0,
                  ServiceNote = item2 != null ? item2.RejectNotes : ""
              }).ToList();
                var a = Approvals.GroupBy(c => new { c.ClaimID, c.ProcedureDate })
                   .Select(c => new OrderListViewModel()
                   {
                       ClaimID = c.Key.ClaimID,
                       ClaimFormNum = c.FirstOrDefault().ClaimFormNum,
                       CardNumber = c.FirstOrDefault().CardNumber,
                       //MemberName = c.FirstOrDefault().MemberName,
                       ProcedureDate = DateTime.Parse(c.Key.ProcedureDate.ToString()).ToString("yyyy-MM-dd"),
                       TClaimAmt = c.FirstOrDefault().TClaimAmt,
                       TDeductAmt = c.FirstOrDefault().TDeductAmt /*Math.Round((decimal.Parse( c.FirstOrDefault().TDeductAmt .ToString())/ decimal.Parse(c.FirstOrDefault().TClaimAmt.ToString())) *(decimal.Parse(c.FirstOrDefault().TRequestedAmt.ToString()) + decimal.Parse(c.FirstOrDefault().TDiscountAmt.ToString()) + decimal.Parse(c.FirstOrDefault().TDeductAmt.ToString())), 2)*/,
                       TDiscountAmt = c.FirstOrDefault().TDiscountAmt /*Math.Round((decimal.Parse(c.FirstOrDefault().TDiscountAmt.ToString()) / decimal.Parse(c.FirstOrDefault().TClaimAmt.ToString())) * (decimal.Parse(c.FirstOrDefault().TRequestedAmt.ToString()) + decimal.Parse(c.FirstOrDefault().TDiscountAmt.ToString()) + decimal.Parse(c.FirstOrDefault().TDeductAmt.ToString())), 2)*/,
                       NetClaimedAmt = c.FirstOrDefault().NetClaimedAmt,
                       TRequestedAmt = c.FirstOrDefault().TClaimAmt + c.FirstOrDefault().TDifferenceAmt /*c.FirstOrDefault().TRequestedAmt*//*+ Math.Round((decimal.Parse(c.FirstOrDefault().TDeductAmt.ToString()) / decimal.Parse(c.FirstOrDefault().TClaimAmt.ToString())) * (decimal.Parse(c.FirstOrDefault().TRequestedAmt.ToString()) + decimal.Parse(c.FirstOrDefault().TDiscountAmt.ToString()) + decimal.Parse(c.FirstOrDefault().TDeductAmt.ToString())), 2)+ Math.Round((decimal.Parse(c.FirstOrDefault().TDiscountAmt.ToString()) / decimal.Parse(c.FirstOrDefault().TClaimAmt.ToString())) * (decimal.Parse(c.FirstOrDefault().TRequestedAmt.ToString()) + decimal.Parse(c.FirstOrDefault().TDiscountAmt.ToString()) + decimal.Parse(c.FirstOrDefault().TDeductAmt.ToString())), 2)*/,
                       TDifferenceAmt = c.FirstOrDefault().TDifferenceAmt,
                       BranchName = c.FirstOrDefault().Address,
                       OrderListDetails = c.Select(d => new OrderListDetailViewModel()
                       {
                           ClaimID = d.ClaimID,
                           ClaimitemID = d.ClaimItemID,
                           ServiceName = d.ServiceName,
                           ServiceQnt = d.ServiceQnt,
                           ServiceUnitAmt = d.ServiceUnitAmt,
                           ServiceAmt = d.ServiceAmt,
                           ServiceDeductAmt = d.ServiceDeductAmt/*Math.Round((d.ServiceDeductAmt/ d.ServiceAmt) *(d.ServicerequAmt + d.ServiceDeductAmt + d.ServicediscAmt),2)*/,
                           ServiceDiscountAmt = d.ServicediscAmt/* Math.Round((d.ServicediscAmt/ d.ServiceAmt) *(d.ServicerequAmt + d.ServiceDeductAmt + d.ServicediscAmt ),2)*/,
                           ServiceDueAmt = d.ServiceDueAmt,
                           ServiceRequestedAmt = d.ServiceAmt + d.ServicediffAmt /*d.ServicerequAmt+ Math.Round((d.ServiceDeductAmt / d.ServiceAmt) * (d.ServicerequAmt + d.ServiceDeductAmt + d.ServicediscAmt), 2)+ Math.Round((d.ServicediscAmt / d.ServiceAmt) * (d.ServicerequAmt + d.ServiceDeductAmt + d.ServicediscAmt ), 2)*/,
                           ServiceDifferenceAmt = d.ServicediffAmt,
                           ServiceNote = d.ServiceNote
                       }).ToList()
                   }).ToList();

                return Json(a, JsonRequestBehavior.AllowGet);

            }
            //return a;
        }
        public JsonResult updaterequestAmt(string Claim_ID, string ClaimItem_ID, string reques_Amt, string Differance_Amt)
        {
            try
            {
                int Claim_IDd = int.Parse(Claim_ID);
                int ClaimItem_IDd = int.Parse(ClaimItem_ID);
                decimal Differance_Amtt = decimal.Parse(Differance_Amt);
                decimal reques_Amtt = decimal.Parse(reques_Amt);

                using (var database = new MedInsuranceProEntities())
                {
                    var foo = database.CLMClaimItems.Where(f => f.ClaimItemID == ClaimItem_IDd).FirstOrDefault();
                    foo.RequestedAmt = reques_Amtt;
                    foo.DifferenceAmt = Differance_Amtt;
                    database.SaveChanges();
                }

                var totalrequestamt = db.CLMClaimItems.Where(f => f.ClaimID == Claim_IDd).Sum(d=>d.RequestedAmt);
                var totaldifferanceamt = db.CLMClaimItems.Where(f => f.ClaimID == Claim_IDd).Sum(d=>d.DifferenceAmt);
                using (var database = new MedInsuranceProEntities())
                {
                    var foo = database.CLMClaims.Where(f => f.ClaimID == Claim_IDd).FirstOrDefault();
                    foo.TRequestedAmt = totalrequestamt;
                    foo.TDifferenceAmt = totaldifferanceamt;
                    database.SaveChanges();
                }
                var arrlist = new ArrayList();
                var totalClaimedamt = db.CLMClaimItems.Where(f => f.ClaimID == Claim_IDd).Sum(d => d.ClaimedAmt);
                var totalNewRequest = totaldifferanceamt + totalClaimedamt;
                arrlist.Add(totalNewRequest);
                //arrlist.Add(totalrequestamt);
                arrlist.Add(totaldifferanceamt);
                return Json(arrlist, JsonRequestBehavior.AllowGet);

            }
            catch
            {
                return Json("1", JsonRequestBehavior.AllowGet);

            }

          
        }
        public JsonResult Send_to_Accounting_Department(List<Expanded_Services> Expanded_Services)
        {
            if (Expanded_Services == null)
            {
                return Json("0", JsonRequestBehavior.AllowGet);

            }
            foreach (var item in Expanded_Services)
            {
                using (var database = new MedInsuranceProEntities())
                {
                    var Clai_Id = item.Claim_Id;
                    var foo = database.CLMClaims.Where(f => f.ClaimID == Clai_Id).FirstOrDefault();
                    foo.RepType = true;
                    database.SaveChanges();
                }
            }

            return Json("", JsonRequestBehavior.AllowGet);
        }
        public JsonResult recover_to_Audit_Department(List<Expanded_Services> Expanded_Services)
        {
            if (Expanded_Services == null)
            {
                return Json("0", JsonRequestBehavior.AllowGet);

            }
            foreach (var item in Expanded_Services)
            {
                using (var database = new MedInsuranceProEntities())
                {
                    var Clai_Id = item.Claim_Id;
                    var foo = database.CLMClaims.Where(f => f.ClaimID == Clai_Id).FirstOrDefault();
                    foo.RepType = false;
                    database.SaveChanges();
                }
            }

            return Json("", JsonRequestBehavior.AllowGet);
        }




        [HttpGet]
        public ActionResult Chronic_Approvals()
        {

            if ((Session["UserID"] == null) || (int.Parse(Session["Approval"].ToString()) == 0))
            {
                return RedirectToAction("login", "ClaimForm");
            }
            return View();
        }

        public class ChronicOrderListViewModel
        {

            public int ClaimID { get; set; }
            public long? ClaimFormNum { get; set; }
            public string CustomerName { get; set; }
            public string MemberName { get; set; }
            public string CardNumber { get; set; }
            public string ProcedureDate { get; set; }
            public decimal? TContractAmt { get; set; }
            public decimal? TRequestedAmt { get; set; }
            public decimal? DeductablePerc { get; set; }
            public decimal? DeductableAmt { get; set; }

            public List<ChronicOrderListDetailViewModel> OrderListDetails { get; set; }

        }
        public class ChronicOrderListDetailViewModel
        {
            public int ClaimID { get; set; }
            public int ClaimitemID { get; set; }
            public string ServiceName { get; set; }
            public decimal? ServiceQnt { get; set; }
            public decimal? ServiceUnitAmt { get; set; }
            public decimal? ServiceRequestedPrice { get; set; }
            public decimal? ServiceAmt { get; set; }
            public decimal? ServiceRequestedAmt { get; set; }
            public decimal? ServiceDeductPerc { get; set; }
            public decimal ServiceDeductAmt { get; set; }
            public string ServiceNote { get; set; }
        }
        public JsonResult GetChronicOrderList()
        {
            int providerid = int.Parse(Session["providerid"].ToString());
            string branchName = Session["branchName"].ToString();
            bool ProviderAdminn = bool.Parse(Session["ProviderAdmin"].ToString());

            var providercatID = db.PVProviderDatas.FirstOrDefault(d => d.ProviderID == providerid).ProviderCatID;
            var datetimeActive = DateTime.Now.AddDays(-20);

            var Approvals = (
            from item1 in db.CLMClaimsONLINEs
            join item2 in db.CLMClaimsItemsONLINEs on item1.ClaimID equals item2.ClaimID
            join item3 in db.ContractMembers on item1.ContractMemberID equals item3.ContractMemberID
            join item4 in db.CustomerDatas on item3.CustomerID equals item4.CustomerID
            join item5 in db.ContractServiceCeilings on new { ContractIDd=item3.ContractID, PlanTypeIDd=item3.PlanTypeID  } equals new { ContractIDd=item5.ContractID, PlanTypeIDd=item5.PlanTypeID}

            where item1.ProviderCatID == providercatID && item1.ProviderID == providerid && item1.BranchID == 0 && item1.ProcedureDate >= datetimeActive && item1.ClaimStatusID == 2  && /*item1.ClaimServiceID== */item5.ServiceID ==4
            orderby item1.ClaimID
            select new
            {
                item1.ClaimFormNum,
                item4.CustomerName,
                item3.MemberName,
                item1.CardNumber,
                item1.ProcedureDate,
                item5.DeductPer,
                //item1.TClaimAmt,
                //item1.TDeductAmt,
                //item1.TDiscountAmt,
                //item1.NetClaimedAmt,
                //item1.TRequestedAmt,
                //item1.TDifferenceAmt,
                item1.ClaimID,
                item2.ClaimItemID,
                //Address = item1 != null ? item1.BranchName : "",
                ServiceName = item2 != null ? item2.ServiceName : "",
                ServiceQnt = item2 != null ? item2.ServiceQnt : 0,
                ServiceUnitAmt = item2 != null ? item2.ServicePrice : 0,
                Servicerequprice = item2 != null ? item2.RequestedPrice : 0,
                ServiceAmt = item2 != null ? item2.ServiceAmt : 0,
                //ServicerequAmt = item2 != null ? item2.RequestedAmt : 0,

                //ServiceDeductAmt = item2 != null ? item2.DeductAmt : 0,
                //ServicediscAmt = item2 != null ? item2.DiscountAmt : 0,
                //ServiceDueAmt = item2 != null ? item2.DueAmt : 0,
                //ServicediffAmt = item2 != null ? item2.DifferenceAmt : 0,
                ServiceNote = item2 != null ? item2.Refuse_Note : ""

            }).ToList();
            var a = Approvals.GroupBy(c => new { c.ClaimID, c.ProcedureDate })
                .Select(c => new ChronicOrderListViewModel()
                {
                    ClaimID = c.Key.ClaimID,
                    ClaimFormNum = c.FirstOrDefault().ClaimFormNum,
                    CustomerName = c.FirstOrDefault().CustomerName,
                    MemberName = c.FirstOrDefault().MemberName,
                    CardNumber = c.FirstOrDefault().CardNumber,
                    ProcedureDate = DateTime.Parse(c.Key.ProcedureDate.ToString()).ToString("yyyy-MM-dd"),
                    TContractAmt = c.Sum(d=>d.ServiceAmt),
                    TRequestedAmt = Math.Round(decimal.Parse(c.Sum(d => d.Servicerequprice*d.ServiceQnt).ToString()), 2),
                    DeductablePerc = c.FirstOrDefault().DeductPer,
                    DeductableAmt = Math.Round(decimal.Parse(c.Sum(d => d.Servicerequprice * d.ServiceQnt).ToString()) * decimal.Parse(c.FirstOrDefault().DeductPer.ToString()),2) ,
                    //TDiscountAmt = c.FirstOrDefault().TDiscountAmt /*Math.Round((decimal.Parse(c.FirstOrDefault().TDiscountAmt.ToString()) / decimal.Parse(c.FirstOrDefault().TClaimAmt.ToString())) * (decimal.Parse(c.FirstOrDefault().TRequestedAmt.ToString()) + decimal.Parse(c.FirstOrDefault().TDiscountAmt.ToString()) + decimal.Parse(c.FirstOrDefault().TDeductAmt.ToString())), 2)*/,
                    //NetClaimedAmt = c.FirstOrDefault().NetClaimedAmt,
                    //TDifferenceAmt = c.FirstOrDefault().TDifferenceAmt,
                    //BranchName = c.FirstOrDefault().Address,
                    OrderListDetails = c.Select(d => new ChronicOrderListDetailViewModel()
                    {
                        ClaimID = d.ClaimID,
                        ClaimitemID = d.ClaimItemID,
                        ServiceName = d.ServiceName,
                        ServiceQnt = d.ServiceQnt,
                        ServiceUnitAmt = d.ServiceUnitAmt,
                        ServiceRequestedPrice = d.Servicerequprice,
                        ServiceAmt = d.ServiceAmt,
                        ServiceRequestedAmt = Math.Round(decimal.Parse((d.Servicerequprice * d.ServiceQnt).ToString()), 2),
                        ServiceDeductPerc = c.FirstOrDefault().DeductPer,
                        ServiceDeductAmt = Math.Round(decimal.Parse((d.Servicerequprice * d.ServiceQnt).ToString()) * decimal.Parse(c.FirstOrDefault().DeductPer.ToString()), 2),
                        ServiceNote = d.ServiceNote
                    }).ToList()
                }).ToList();

            return Json(a, JsonRequestBehavior.AllowGet);
           
            //return a;
        }
        public JsonResult updateChronicrequestAmt(string Claim_ID, string ClaimItem_ID, string reques_price)
        {
            try
            {
                int Claim_IDd = int.Parse(Claim_ID);
                int ClaimItem_IDd = int.Parse(ClaimItem_ID);
                decimal reques_Price = decimal.Parse(reques_price);

                using (var database = new MedInsuranceProEntities())
                {
                    var foo = database.CLMClaimsItemsONLINEs.Where(f => f.ClaimItemID == ClaimItem_IDd).FirstOrDefault();
                    foo.RequestedPrice = reques_Price;
                    database.SaveChanges();
                }

                var arrlist = new ArrayList();
               
                var totalClaimedamt = db.CLMClaimsItemsONLINEs.Where(f => f.ClaimID == Claim_IDd).Sum(d => d.RequestedPrice * d.ServiceQnt);
                var totalNewRequest = Math.Round(decimal.Parse(totalClaimedamt.ToString()), 2);
                arrlist.Add(totalNewRequest);
                return Json(arrlist, JsonRequestBehavior.AllowGet);

            }
            catch
            {
                return Json("A", JsonRequestBehavior.AllowGet);

            }


        }
        public JsonResult AcceptChronicApprovals (List<Expanded_Services> Expanded_Services)
        {
            if (Expanded_Services == null)
            {
                return Json("0", JsonRequestBehavior.AllowGet);

            }

            int calimid = 0;
            try
            {
                var maxvalu = db.CLMClaims.Max(o => o.ClaimID);
                var resul = db.CLMClaims.First(o => o.ClaimID == maxvalu);
                calimid = (resul.ClaimID) + 1;
            }
            catch
            {
                calimid = 1;
            }

            int claimitemid = 0;
            try
            {
                var maxval = db.CLMClaimItems.Max(o => o.ClaimItemID);
                var resu = db.CLMClaimItems.First(o => o.ClaimItemID == maxval);
                claimitemid = resu.ClaimItemID;
            }
            catch
            {
                claimitemid = 1;
            }

            var PvMedData = db.PVMedDatas.ToList();
            var PVContract = db.PVContracts.ToList();
            var PVMedException = db.PVMedExceptions.ToList();


            var contract_Memberss = (
                            from item1 in db.ContractMembers
                            join finalItem0 in db.Contracts on item1.ContractID equals finalItem0.ContractID 
                            select new { item1, finalItem0 }).ToList();

            IList<CLMClaim> cLMClaims = new List<CLMClaim>();

            foreach (var item in Expanded_Services)
            {
                calimid++;
                var claim_id = item.Claim_Id;
                var Deductable_Perc = item.Deductable_Amt;

                var clmclaimsitemsdata_online = db.CLMClaimsItemsONLINEs.Where(d => d.ClaimID == claim_id && d.ClaimItemStatusID == 3).ToList();
                if (clmclaimsitemsdata_online.Count == 0)
                {
                    using (var database = new MedInsuranceProEntities())
                    {

                        var foo = database.CLMClaimsONLINEs.Where(f => f.ClaimID == claim_id).FirstOrDefault();
                        foo.ClaimStatusID = 3;
                        database.SaveChanges();
                    }
                    continue;
                }
                var claimformdata_online = db.CLMClaimsONLINEs.FirstOrDefault(d => d.ClaimID == claim_id);
                ////////////////////////////////////////
               
                string card_num = claimformdata_online.CardNumber;
                DateTime? DatProcedure = claimformdata_online.ProcedureDate;

    //            var cont_num = (
    //from item1 in db.ContractMembers
    //join item2 in db.Contracts on item1.ContractID equals item2.ContractID into finalGroup0
    //from finalItem0 in finalGroup0.DefaultIfEmpty()
    //where finalItem0.StartDate <= DatProcedure && finalItem0.EndDate >= DatProcedure && item1.CardNumber == card_num
    //select new { finalItem0.ContractNumber, item1.ContractMemberID, finalItem0.ContractID, item1.PlanTypeID, finalItem0.StartDate, finalItem0.EndDate }).OrderByDescending(aa => aa.ContractID).FirstOrDefault();

                var cont_num = (
                from item2 in contract_Memberss
                where item2.finalItem0.StartDate <= DatProcedure && item2.finalItem0.EndDate >= DatProcedure && item2.item1.CardNumber == card_num
                select new { item2.finalItem0.ContractNumber, item2.item1.ContractMemberID, item2.finalItem0.ContractID, item2.item1.PlanTypeID, item2.finalItem0.StartDate, item2.finalItem0.EndDate }).OrderByDescending(aa => aa.ContractID).FirstOrDefault();

                //string contract_num = cont_num[0].contractnumber;
                int contract_member_id = cont_num.ContractMemberID;
                int contract_id = cont_num.ContractID;
                int plan_type_id = cont_num.PlanTypeID;


                var claim_form_num = long.Parse(claimformdata_online.ClaimFormNum.ToString());
                int? contractmember_id = claimformdata_online.ContractMemberID;
                DateTime? symptoms_date = claimformdata_online.symptomsDate;
                DateTime? procedure_date = claimformdata_online.ProcedureDate;
                int? claimservice_id = claimformdata_online.ClaimServiceID;
                string diagnosiss = claimformdata_online.Diagnosis;
                string icd_code = claimformdata_online.ICDCode;
                string contract_num = claimformdata_online.ContractNum;
                string card_number = claimformdata_online.CardNumber;
                int providerid = int.Parse(claimformdata_online.ProviderID.ToString());
                int? ProviderCat_ID = claimformdata_online.ProviderCatID;
                var provider_name = db.PVProviderDatas.FirstOrDefault(d => d.ProviderID == providerid).ProviderName;
                //string branchname = claimformdata_online.BranchName;
                string branchname = Session["branchName"].ToString();
                int BranchID = int.Parse(Session["BranchID"].ToString());

                //var T_ClaimAmt = clmclaimsitemsdata_online.Sum(d => d.ServicePrice * d.ServiceQnt);
                var claimsdata_insertedfrombefore = db.CLMClaims.Where(d => /*d.RecBatchID == 0 &&*/ d.ClaimFormNum == claim_form_num /*&& d.ProviderName == provider_name && d.BranchName == branchname*/).OrderByDescending(d => d.ClaimID).FirstOrDefault();
                //&& d.ClaimFormNum == claim_form_num && d.ContractMemberID == contractmember_id && d.symptomsDate == symptoms_date && d.ProcedureDate == procedure_date && d.ClaimServiceID == claimservice_id && d.Diagnosis == diagnosiss && d.ICDCode == icd_code &&
                if (claimsdata_insertedfrombefore != null)
                {
                    using (var database = new MedInsuranceProEntities())
                    {

                        var foo = database.CLMClaimsONLINEs.Where(f => f.ClaimID == claim_id).FirstOrDefault();
                        foo.ClaimStatusID = 3;
                        database.SaveChanges();
                    }
                    continue;
                }
                CLMClaim newdataa = new CLMClaim()
                {

                    ClaimID = calimid,
                    RecBatchID = 0,
                    ClaimFormNum = claim_form_num,
                    ContractMemberID = contractmember_id,
                    symptomsDate = symptoms_date,
                    ProcedureDate = procedure_date,
                    ClaimServiceID = claimservice_id,
                    Diagnosis = diagnosiss,
                    ICDCode = icd_code,
                    TClaimAmt = 0,
                    TDeductAmt = 0,
                    TDiscountAmt = 0,
                    TAClaimAmt = 0,
                    TMedicalDeductAmt = 0,
                    TDifferenceAmt = 0,
                    ClaimStatusID = 1,
                    // will edit 
                    UserAddID = 0,
                    UserApproveID = 0,
                    NetClaimedAmt = 0,
                    TRequestedAmt = 0,
                    BatchNumber = "",
                    ContractNum = contract_num,
                    CardNumber = card_number,
                    ClaimDeductAmt = 0,
                    ClaimDeductNote = "",
                    TAuditDeductAmt = 0,
                    IsOverCeiling = false,
                    RepType = false,
                    ProviderID = providerid,
                    ProviderName = provider_name,
                    BranchName = branchname

                };
                cLMClaims.Add(newdataa);


                //db.CLMClaims.Add(newdataa);
                //db.SaveChanges();

            }

            using (var context = new MedInsuranceProEntities())
            {
                context.CLMClaims.AddRange(cLMClaims);
                context.SaveChanges();
            }


            var ClaimsDataEnter = ( from item1 in Expanded_Services
                                    join item2 in db.CLMClaimsONLINEs on item1.Claim_Id equals item2.ClaimID 
                                    join item3 in db.CLMClaims on item2.ClaimFormNum equals item3.ClaimFormNum
                                    where item3.RecBatchID==0
                                    select new { item3.ClaimFormNum, item3.ClaimID, item3.ProviderID, item1.Claim_Id, item1.Deductable_Amt, item2.ProcedureDate }).OrderBy(aa => aa.Claim_Id).ToList();


            IList<CLMClaimItem> CLMClaimItems = new List<CLMClaimItem>();

            //foreach (var item in Expanded_Services)
            foreach (var item in ClaimsDataEnter)
            {
                var claim_id = item.Claim_Id;
                var Deductable_Perc = item.Deductable_Amt;




                //var claim_dataa = db.CLMClaims.Where(d => d.RecBatchID == 0 && d.ClaimFormNum == claim_form_num && d.ProviderID == providerid).OrderByDescending(d => d.ClaimID).FirstOrDefault();
                var claim_dataa = ClaimsDataEnter.Where(d => d.Claim_Id == claim_id).OrderByDescending(d => d.Claim_Id).FirstOrDefault();
                var claim_id_inserted = claim_dataa.ClaimID;
                //clmclaimitem


                var claim_form_num = long.Parse(item.ClaimFormNum.ToString());
                DateTime? procedure_date = claim_dataa.ProcedureDate;
                string branchname = Session["branchName"].ToString();
                int BranchID = int.Parse(Session["BranchID"].ToString());

                var clmclaimsitemsdata_online = db.CLMClaimsItemsONLINEs.Where(d => d.ClaimID == claim_id && d.ClaimItemStatusID == 3).ToList();


                int provi_id = claim_dataa.ProviderID;
                //int provi_id = providerid;
                var re3 = PVContract
                                    .Where(m => m.ProviderID == provi_id && m.StartDate <= procedure_date && m.EndDate >= procedure_date)
                                    .OrderByDescending(m => m.ProviderContractID)
                                    .FirstOrDefault();
                if (re3 == null)
                {
                    var re32 = PVContract
                            .Where(m => m.ProviderID == provi_id && m.IsActive == true)
                            .OrderByDescending(m => m.ProviderContractID)
                            .FirstOrDefault();
                    re3 = re32;
                }
                int provider_contract_id = re3.ProviderContractID;
                decimal local_discount_per = re3.LocalDiscountPer;
                decimal foreign_discount_per = re3.ForeignDiscountPer;
                decimal? chronic_localdis_per = re3.ChronicLocalDisPer;
                decimal? chronic_foreign_disper = re3.ChronicForeignDisPer;


                decimal deductable = 0;

                decimal tclaim_amt = 0;
                decimal tdeductamt = 0;
                decimal tdiscountamt = 0;
                //decimal taclaimamt = 0;
                decimal tmedicaldeductamt = 0;
                decimal tdifferenceamt = 0;
                decimal netclaimedamt = 0;
                decimal trequestedamt = 0;
                decimal claimdeductamt = 0;
                //decimal tauditdeductamt = 0;




                for (var i = 0; i < clmclaimsitemsdata_online.Count; i++)
                {

                    claimitemid++;
                    // get service code
                    string service_namee = clmclaimsitemsdata_online[i].ServiceName;

                    int service_id = clmclaimsitemsdata_online[i].ServiceID;
                    string service_code = clmclaimsitemsdata_online[i].ServiceCode;

                    bool is_import = false;
                    bool is_coverd = false;

                    decimal service_price = decimal.Parse(clmclaimsitemsdata_online[i].ServicePrice.ToString());
                    decimal Number_of_timesday = decimal.Parse(clmclaimsitemsdata_online[i].Number_of_times_a_day.ToString());
                    decimal Dosage_sizes = decimal.Parse(clmclaimsitemsdata_online[i].Dosage_size.ToString());
                    decimal Number_of_dayss = decimal.Parse(clmclaimsitemsdata_online[i].Number_of_days.ToString());
                    //int Number_of_daysss = int.TryParse(Number_of_dayss.ToString());
                    int Number_of_daysss = Decimal.ToInt32(Number_of_dayss);
                    DateTime EndDate_Effective = DateTime.Now.AddDays(Number_of_daysss);
                    decimal service_discountperc = 0;
                    decimal service_discountval = 0;


                    //var re2 = db.PVMedDatas.FirstOrDefault(d => d.MedDrugName.Replace(" ", string.Empty).Contains(service_namee.Replace(" ", string.Empty)));
                    //var re2 = db.PVMedDatas.FirstOrDefault(d => d.MedDrugID == service_id);

                    //var re2 = PvMedData.FirstOrDefault(d => d.MedDrugID == service_id);

                    //int med_cat = re2.MedCatID;
                    //is_import = re2.IsImported;
                    //is_coverd = re2.IsCoverd;

                    int med_cat = 0;

                    var re2 = PvMedData.FirstOrDefault(d => d.MedDrugID == service_id);
                    if (re2 == null)
                    {
                        try
                        {
                            var re2wqw = db.PVMedDatas.FirstOrDefault(d => d.MedDrugName.Replace(" ", string.Empty) == (service_namee.Replace(" ", string.Empty)));
                            med_cat = re2wqw.MedCatID;
                            is_import = re2wqw.IsImported;
                            is_coverd = re2wqw.IsCoverd;
                        }
                        catch
                        {

                        }
                    }
                    else
                    {
                        med_cat = re2.MedCatID;
                        is_import = re2.IsImported;
                        is_coverd = re2.IsCoverd;
                    }

                    var PVMedExceptionData = PVMedException.FirstOrDefault(d => d.MedDrugName.Replace(" ", string.Empty) == (service_namee.Replace(" ", string.Empty)) && d.ProviderContractID == provider_contract_id);
                    if (PVMedExceptionData != null)
                    {
                        local_discount_per = PVMedExceptionData.DiscountPer;
                        foreign_discount_per = PVMedExceptionData.DiscountPer;
                        chronic_localdis_per = PVMedExceptionData.DiscountPer;
                        chronic_foreign_disper = PVMedExceptionData.DiscountPer;


                    }
                    else
                    {
                        local_discount_per = re3.LocalDiscountPer;
                        foreign_discount_per = re3.ForeignDiscountPer;
                        chronic_localdis_per = re3.ChronicLocalDisPer;
                        chronic_foreign_disper = re3.ChronicForeignDisPer;

                    }
                    service_discountperc = 0;

                    if (is_import == true)
                    {
                        service_discountperc = foreign_discount_per;
                    }
                    else
                    {
                        service_discountperc = local_discount_per;
                    }

                    service_discountval = service_discountperc * service_price;

                    
                    int claim_service_id1 = clmclaimsitemsdata_online[i].ClaimServiceID;

                    decimal deduct_per = Deductable_Perc;
                    //if (claimservice_id == 14 || claimservice_id == 19 || claim_service_id1 == 14 || claim_service_id1 == 19)
                    //{
                    //    deduct_per = 0;
                    //}
                    //else
                    //{
                    //    var re5 = db.ContractServiceCeilings.FirstOrDefault(d => d.ContractID == contract_id && d.PlanTypeID == plan_type_id && d.ServiceID == claim_service_id1);
                    //    deduct_per = re5.DeductPer;
                    //}
                    deductable = deduct_per;

                    //var re5 = db.contractserviceceilings.firstordefault(d => d.contractid == contract_id && d.plantypeid == plan_type_id && d.serviceid == claimservice_id);
                    //decimal deduct_per = re5.deductper;

                    decimal service_deduct_amt = deduct_per * service_price;
                    decimal net_service_amt = service_price - service_deduct_amt - service_discountval;

                    decimal service_request = decimal.Parse(clmclaimsitemsdata_online[i].RequestedPrice.ToString());


                    decimal service_qnt = decimal.Parse(clmclaimsitemsdata_online[i].ServiceQnt.ToString());
                    decimal service_unitamt = service_price;
                    decimal claimed_amt = service_unitamt * service_qnt;

                    decimal claimedRequest_amt = service_request * service_qnt;

                    decimal deduct_amt = claimedRequest_amt * deduct_per;
                    //decimal deduct_amt = claimed_amt * deduct_per;
                    decimal discount_amt = claimed_amt * service_discountperc;
                    decimal medical_deductamt = 0;
                    decimal due_amt = claimed_amt - deduct_amt - discount_amt;
                    //decimal requested_amt = due_amt;
                    //decimal difference_amt = 0;

                    decimal requested_amt = (service_request * service_qnt) - deduct_amt - discount_amt;
                    decimal difference_amt = requested_amt - due_amt;


                    decimal net_payment_amt = due_amt;

                    //string reject_reson = "";
                    string reject_reson = clmclaimsitemsdata_online[i].Refuse_Note;

                    //int a = claimservice_id;

                    CLMClaimItem newdataaa = new CLMClaimItem()
                    {

                        ClaimItemID = claimitemid,
                        ClaimID = claim_id_inserted,
                        ServiceID = service_id,
                        ServiceCode = service_code,
                        ServiceName = service_namee,
                        ServiceQnt = service_qnt,
                        ServiceUnitAmt = service_unitamt,
                        ClaimedAmt = claimed_amt,
                        DeductAmt = deduct_amt,
                        DiscountAmt = discount_amt,
                        FDiscountAmt = 0,
                        MedicalDeductAmt = medical_deductamt,
                        DueAmt = due_amt,
                        RequestedAmt = requested_amt,
                        DifferenceAmt = difference_amt,
                        RejectNotes = reject_reson,
                        IsSysEntry = false,
                        // will edit
                        UserEntryID = 0,
                        UserEntryDate = DateTime.Now,
                        UserApprove = false,
                        UserApproveID = 0,
                        BatchNumber = "",
                        ClaimNum = claim_form_num,
                        ServiceAmt = service_price,
                        ServiceDeductAmt = service_deduct_amt,
                        ServiceDiscountAmt = service_discountval,
                        NetServiceAmt = net_service_amt,
                        ClaimServiceID = claim_service_id1,
                        ServiceDiscountPer = service_discountperc,
                        IsCovered = is_coverd,
                        IsImported = is_import,
                        NetPaymentAmt = net_payment_amt,
                        ReimbPer = 0,
                        AuditDeductAmt = 0,
                        AuditComments = "",
                        Number_of_times_a_day = Number_of_timesday,
                        Dosage_size = Dosage_sizes,
                        Number_of_days = Number_of_dayss,
                        EndDate_Effective_Material = EndDate_Effective,
                        IsOverCeiling = false

                    };
                    //db.CLMClaimItems.Add(newdataaa);
                    //db.SaveChanges();
                    CLMClaimItems.Add(newdataaa);

                    tclaim_amt += claimed_amt;
                    tdeductamt += deduct_amt;
                    tdiscountamt += discount_amt;
                    //taclaimamt = 0;
                    tmedicaldeductamt += medical_deductamt;
                    tdifferenceamt += difference_amt;
                    netclaimedamt += net_payment_amt;
                    trequestedamt += requested_amt;
                    claimdeductamt += service_deduct_amt;
                    //tauditdeductamt = 0;


                }
               

                using (var database = new MedInsuranceProEntities())
                {

                    var foo = database.CLMClaims.Where(f => f.ClaimID == claim_id_inserted).FirstOrDefault();
                    foo.TClaimAmt = tclaim_amt;
                    foo.TDeductAmt = tdeductamt;
                    foo.TDiscountAmt = tdiscountamt;
                    //foo.taclaimamt = tclaim_amt;
                    foo.TMedicalDeductAmt = tmedicaldeductamt;
                    foo.TDifferenceAmt = tdifferenceamt;
                    foo.NetClaimedAmt = netclaimedamt;
                    foo.TRequestedAmt = trequestedamt;
                    foo.ClaimDeductAmt = claimdeductamt;
                    //foo.tauditdeductamt = tclaim_amt;
                    database.SaveChanges();
                }

                using (var database = new MedInsuranceProEntities())
                {

                    var foo = database.CLMClaimsONLINEs.Where(f => f.ClaimID == claim_id).FirstOrDefault();
                    foo.ClaimStatusID = 3;
                    foo.BranchID = BranchID;
                    foo.BranchName = branchname;
                    database.SaveChanges();
                }
              
            }
            using (var context = new MedInsuranceProEntities())
            {
                context.CLMClaimItems.AddRange(CLMClaimItems);
                context.SaveChanges();
            }

            //var arrlist = new ArrayList();
           
            //arrlist.Add(ClaimsDataEnter);

            return Json(ClaimsDataEnter, JsonRequestBehavior.AllowGet);

        }

        [HttpGet]
        public ActionResult Messanger()
        {
            //var ClaimFormNumbers = (from values in db.MemberClaimForms
            //                        where values.MemberClaimformStatus == 2
            //                        select values.ClaimFormNumber).ToArray();

            //ViewBag.lis = ClaimFormNumbers;
            if ((Session["UserID"] == null) || ((int.Parse(Session["ClaimForm_Approved"].ToString()) == 0) && (int.Parse(Session["Doctor_ClaimForm"].ToString()) == 0)))
            {
                return RedirectToAction("login", "ClaimForm");
            }

            ViewBag.image = Session["UserImage"].ToString();
            ViewBag.providerID = Session["providerid"].ToString();
            ViewBag.BranchID = Session["BranchID"].ToString();
            return View();
        }

        public JsonResult ADDMessage(string message)
        {
            int providerid = int.Parse(Session["providerid"].ToString());
            string Providername = Session["Providername"].ToString();
            string branchName = Session["branchName"].ToString();
            int BranchID = int.Parse(Session["BranchID"].ToString());
            int UserID = int.Parse(Session["UserID"].ToString());

            Messanger newdataa = new Messanger()
            {

                ProviderID = providerid,
                BranchID = BranchID,
                ProviderUserID = UserID,
                ProviderName = Providername,
                BranchName = branchName,
                CMCUserID = 0,
                CMCName = "CMC user",
                Message = message,
                MessageDateTime = DateTime.Now,
                IsProvider = true,
                IsCMC = false,
                ProviderShow = true,
                CMCShow = false,
            };
            db.Messangers.Add(newdataa);
            db.SaveChanges();
            var arrlit = new ArrayList();
            arrlit.Add(Providername);
            arrlit.Add(branchName);
            arrlit.Add(DateTime.Now);
            arrlit.Add(message);


            return Json(arrlit, JsonRequestBehavior.AllowGet);
        }


        [HttpGet]
        public ActionResult Approval()
        {
            //var ClaimFormNumbers = (from values in db.MemberClaimForms
            //                        where values.MemberClaimformStatus == 2
            //                        select values.ClaimFormNumber).ToArray();

            //ViewBag.lis = ClaimFormNumbers;
            if ((Session["UserID"] == null) || (int.Parse(Session["Approval"].ToString()) == 0))
            {
                return RedirectToAction("login", "ClaimForm");
            }

            //ViewBag.image = Session["UserImage"].ToString();
            //ViewBag.providerID = Session["providerid"].ToString();
            //ViewBag.BranchID = Session["BranchID"].ToString();
            return View();
        }

        public ActionResult Get_Approvals(string id)
        {
            try
            {
                
                ArrayList arr_lis = new ArrayList();
                int providerid = int.Parse(Session["providerid"].ToString());
                var providercatID = db.PVProviderDatas.FirstOrDefault(d => d.ProviderID == providerid).ProviderCatID;
                var datetimeActive = DateTime.Now.AddDays(-7);

                var claimid =0;
                decimal? claimitems_Done = 0;
                if (providercatID == 2)
                {
                    //var re2 = db.PVMedDatas.FirstOrDefault(d => d.MedDrugName.Replace(" ", string.Empty) == (service_Nam.Replace(" ", string.Empty)));
                    //(from o in db.PVMedDatas
                    // where o.MedDrugName.Replace(" ", string.Empty) == item1.ServiceName.Replace(" ", string.Empty)
                    // orderby o.MedDrugID descending
                    // select o.MedDrugID).FirstOrDefault()   equals item2.MedDrugID

                    var claimitems_Active = (from values in db.CLMClaimsONLINEs
                                             join item1 in db.CLMClaimsItemsONLINEs on values.ClaimID equals item1.ClaimID
                                             //join item2 in db.PVMedDatas on  item1.ServiceName.Replace(" ", string.Empty) equals item2.MedDrugName.Replace(" ", string.Empty)
                                             where values.ProviderCatID == providercatID && values.CardNumber == id && values.ProviderID == 0 && values.BranchID == 0 && values.ProcedureDate >= datetimeActive && values.ClaimStatusID == 2 && item1.ClaimItemStatusID == 3
                                             select new { values.ClaimFormNum, item1.ServiceName, item1.ServiceQnt, Price=(from em2 in db.PVMedDatas
                                                                                                                           where (item1.ServiceName.Replace(" ", string.Empty) == em2.MedDrugName.Replace(" ", string.Empty))
                                                                                                                           orderby em2.MedDrugID descending
                                                                                                                           select em2.Price).FirstOrDefault(), ServiceAmt= item1.ServiceQnt, RequestedPrice = item1.ServiceQnt , item1.Refuse_Note, item1.ClaimItemID, values.ClaimID }).OrderBy(d => d.ClaimItemID).ToList();
                                             //select new { values.ClaimFormNum, item1.ServiceName, item1.ServiceQnt, item2.Price, ServiceAmt= item1.ServiceQnt* item2.Price, RequestedPrice = item1.ServiceQnt * item2.Price, item1.Refuse_Note, item1.ClaimItemID, values.ClaimID }).OrderBy(d => d.ClaimItemID).ToList();

                    var filterr = (from values in claimitems_Active
                                   select new { values.ClaimFormNum, values.ServiceName, values.ServiceQnt, values.Price, ServiceAmt= decimal.Parse(values.ServiceQnt.ToString() )* decimal.Parse(values.Price.ToString()), RequestedPrice = decimal.Parse(values.ServiceQnt.ToString()) * decimal.Parse(values.Price.ToString()), values.Refuse_Note, values.ClaimItemID, values.ClaimID }).OrderBy(d => d.ClaimItemID).ToList();

                                  
                    arr_lis.Add(filterr);
                    //arr_lis.Add(claimitems_Active);
                    claimid = claimitems_Active[0].ClaimID;
                    claimitems_Done = claimitems_Active.Sum(d=>d.ServiceAmt);
                }
                else
                {
                    var datenow = DateTime.Now.Date;
                    var claimitems_Active = (from values in db.CLMClaimsONLINEs
                                             join item1 in db.CLMClaimsItemsONLINEs on values.ClaimID equals item1.ClaimID
                                             join item2 in db.PVServices on item1.ServiceName.Replace(" ", string.Empty) equals item2.ServiceName.Replace(" ", string.Empty)
                                             join item3 in db.PVServicePricings on item2.ServiceID equals item3.ServiceID
                                             join item4 in db.PVContracts on item3.ProviderContractID equals item4.ProviderContractID
                                             where values.ProviderCatID == providercatID && values.CardNumber == id && values.ProviderID == 0 && values.BranchID == 0 && values.ProcedureDate >= datetimeActive && values.ClaimStatusID == 2 && item1.ClaimItemStatusID == 3 && item4.StartDate <= datenow&& item4.EndDate >= datenow && item4.ProviderID == providerid 
                                             select new { values.ClaimFormNum, item1.ServiceName, item1.ServiceQnt, item3.Price, ServiceAmt = item1.ServiceQnt * item3.Price, RequestedPrice = item1.ServiceQnt * item3.Price, item1.Refuse_Note, item1.ClaimItemID, values.ClaimID }).OrderBy(d => d.ClaimItemID).ToList();

                    arr_lis.Add(claimitems_Active);
                    claimid = claimitems_Active[0].ClaimID;
                    claimitems_Done = claimitems_Active.Sum(d => d.ServiceAmt);

                }

                var claimsdata = db.CLMClaimsONLINEs.FirstOrDefault(d => d.ClaimID == claimid);
                var claimservice_id = claimsdata.ClaimServiceID;
                var DatProcedure = claimsdata.ProcedureDate;
                var card_num = claimsdata.CardNumber;

                var cont_num = (
    from item1 in db.ContractMembers
    join item2 in db.Contracts on item1.ContractID equals item2.ContractID into finalGroup0
    from finalItem0 in finalGroup0.DefaultIfEmpty()
    where finalItem0.StartDate <= DatProcedure && finalItem0.EndDate >= DatProcedure && item1.CardNumber == card_num
    select new { finalItem0.ContractNumber, item1.ContractMemberID, finalItem0.ContractID, item1.PlanTypeID, finalItem0.StartDate, finalItem0.EndDate }).OrderByDescending(aa => aa.ContractID).FirstOrDefault();

                int contract_member_id = cont_num.ContractMemberID;
                int contract_id = cont_num.ContractID;
                int plan_type_id = cont_num.PlanTypeID;


                decimal deduct_per = 0;
                if (claimservice_id == 14 || claimservice_id == 19)
                {
                    deduct_per = 0;
                }
                else
                {
                    var re5 = db.ContractServiceCeilings.FirstOrDefault(d => d.ContractID == contract_id && d.PlanTypeID == plan_type_id && d.ServiceID == claimservice_id);
                    deduct_per = re5.DeductPer;
                }

                var tdeductamt = deduct_per * claimitems_Done;
                arr_lis.Add(deduct_per * 100);
                arr_lis.Add(tdeductamt);
                arr_lis.Add(claimitems_Done);
                return Json(arr_lis, JsonRequestBehavior.AllowGet);
            }
            catch
            {
                return Json("", JsonRequestBehavior.AllowGet);

            }

        }



        public class AcceptApproval
        {
            public int ClaimItemIDd { get; set; }
            public string ClaimFormNumber { get; set; }
            public string ServiceName { get; set; }
            public decimal ServicePrice { get; set; }
            public decimal ServiceQnt { get; set; } 
            public decimal ServiceAmt { get; set; }
            public decimal RequestedPrice { get; set; }

        }
        public JsonResult Provider_Accept_Approve(List<AcceptApproval> Expanded_Services)
        {

            var claimitemidSelect = Expanded_Services[0].ClaimItemIDd;
            var claimIdSelect = db.CLMClaimsItemsONLINEs.FirstOrDefault(d => d.ClaimItemID == claimitemidSelect).ClaimID;
            var claimItemsCount = db.CLMClaimsItemsONLINEs.Where(d => d.ClaimID == claimIdSelect).Count();

            var Expanded_ServicesCount = Expanded_Services.Count();
            var claimAdded_branchid = db.CLMClaimsONLINEs.FirstOrDefault(d => d.ClaimID == claimIdSelect).BranchID;
            //var branIDDD = claimAdded.BranchID;
            if (claimAdded_branchid != 0)
            {
                return Json("0", JsonRequestBehavior.AllowGet);
            }
            int provideridd = int.Parse(Session["providerid"].ToString());
            int branchid = int.Parse(Session["BranchID"].ToString());
            string branchnamee = Session["branchName"].ToString();
            var Provider_Data = db.PVProviderDatas.FirstOrDefault(d => d.ProviderID == provideridd);
            var Provider_Cat_ID = Provider_Data.ProviderCatID;

            var date = DateTime.Now;
            var re3contract = db.PVContracts
                                .Where(m => m.ProviderID == provideridd && m.StartDate <= date && m.EndDate >= date)
                                .OrderByDescending(m => m.ProviderContractID)
                                .FirstOrDefault();
            if (re3contract == null)
            {
                var re32 = db.PVContracts
                  .Where(m => m.ProviderID == provideridd && m.IsActive == true)
                  .OrderByDescending(m => m.StartDate)
                  .FirstOrDefault();
                re3contract = re32;

            }
            int Provider_Contract_ID = re3contract.ProviderContractID;

            if (claimItemsCount != Expanded_ServicesCount)
            {
                var claimsOnlineSelect = db.CLMClaimsONLINEs.Where(f => f.ClaimID == claimIdSelect).FirstOrDefault();
                var claimFormselected = claimsOnlineSelect.ClaimFormNum;
                CLMClaimsONLINE newdataaa11 = new CLMClaimsONLINE()
                {

                    ClaimFormNum = claimsOnlineSelect.ClaimFormNum,
                    ContractMemberID = claimsOnlineSelect.ContractMemberID,
                    symptomsDate = claimsOnlineSelect.symptomsDate,
                    ProcedureDate = claimsOnlineSelect.ProcedureDate,
                    ClaimServiceID = claimsOnlineSelect.ClaimServiceID,
                    Diagnosis = claimsOnlineSelect.Diagnosis,
                    ICDCode = claimsOnlineSelect.ICDCode,
                    SecondDiagnosis = claimsOnlineSelect.SecondDiagnosis,
                    SecondICDCode = claimsOnlineSelect.SecondICDCode,
                    ThirdDiagnosis = claimsOnlineSelect.ThirdDiagnosis,
                    ThirdICDCode = claimsOnlineSelect.ThirdICDCode,
                    ClaimStatusID = 3,
                    UserAddID = claimsOnlineSelect.UserAddID,
                    UserApproveID = claimsOnlineSelect.UserApproveID,
                    ContractNum = claimsOnlineSelect.ContractNum,
                    CardNumber = claimsOnlineSelect.CardNumber,
                    ProviderID = provideridd,
                    ProviderCatID = claimsOnlineSelect.ProviderCatID,
                    BranchID = branchid,
                    BranchName = branchnamee,
                };
                db.CLMClaimsONLINEs.Add(newdataaa11);
                db.SaveChanges();
                var claim_dataaAdded = db.CLMClaimsONLINEs.Where(d => d.ProviderID == provideridd && d.BranchID == branchid && d.ClaimFormNum == claimFormselected).OrderByDescending(d => d.ClaimID).FirstOrDefault();
                var claim_id_insertedNew = claim_dataaAdded.ClaimID;
                claimIdSelect = claim_id_insertedNew;
                foreach (var item in Expanded_Services)
                {
                    var service_id = 0;
                    var service_code = "";
                    var service_Namenew = "";
                    var Service_Namee = item.ServiceName;
                    if (Provider_Cat_ID != 2)
                    {
                        var reco2 = (from valuess in db.PVServices
                                     join itemm in db.PVServicePricings on valuess.ServiceID equals itemm.ServiceID
                                     where (valuess.ServiceName.Replace(" ", string.Empty) == Service_Namee.Replace(" ", string.Empty)) && (itemm.ProviderContractID == Provider_Contract_ID)
                                     select new { valuess, itemm }).FirstOrDefault();
                        service_id = reco2.valuess.ServiceID;
                        service_code = reco2.valuess.ServiceCode;
                        service_Namenew = reco2.valuess.ServiceName;

                    }
                    else
                    {
                        var re2 = db.PVMedDatas.FirstOrDefault(d => d.MedDrugName.Replace(" ", string.Empty) == (Service_Namee.Replace(" ", string.Empty)));
                        if (re2 == null)
                        {
                            var re22 = db.PVMedDatas.FirstOrDefault(d => d.MedDrugName.Replace(" ", string.Empty).Contains(Service_Namee.Replace(" ", string.Empty)));
                            re2 = re22;
                        }
                        service_id = re2.MedDrugID;
                        service_code = re2.MedDrugCode;
                        service_Namenew = re2.MedDrugName;

                    }
                    using (var database = new MedInsuranceProEntities())
                    {
                        var Service_Price = item.ServicePrice;
                        var service_Amt = item.ServiceAmt;
                        var Requested_Price = item.RequestedPrice;
                        var ClaimItem_ID = item.ClaimItemIDd;
                        var foo = database.CLMClaimsItemsONLINEs.Where(f => f.ClaimItemID == ClaimItem_ID).FirstOrDefault();
                        foo.ClaimID = claim_id_insertedNew;
                        foo.ServiceID = service_id;
                        foo.ServiceCode = service_code;
                        foo.ServiceName = service_Namenew;
                        foo.UserApproveID = int.Parse(Session["UserID"].ToString());
                        foo.ServicePrice = Service_Price;
                        foo.ServiceAmt = service_Amt;
                        foo.RequestedPrice = Requested_Price;
                        database.SaveChanges();
                    }
                }


            }
            else
            {
                using (var database = new MedInsuranceProEntities())
                {
                    var foo = database.CLMClaimsONLINEs.Where(f => f.ClaimID == claimIdSelect).FirstOrDefault();
                    foo.ClaimStatusID = 3;
                    foo.ProviderID = provideridd;
                    foo.BranchID = branchid;
                    foo.BranchName = branchnamee;
                    foo.UserApproveID = int.Parse(Session["UserID"].ToString());
                    database.SaveChanges();
                }
                foreach (var item in Expanded_Services)
                {
                    var service_id = 0;
                    var service_code = "";
                    var service_Namenew = "";
                    var Service_Namee = item.ServiceName;
                    if (Provider_Cat_ID != 2)
                    {
                        var reco2 = (from valuess in db.PVServices
                                     join itemm in db.PVServicePricings on valuess.ServiceID equals itemm.ServiceID
                                     where (valuess.ServiceName.Replace(" ", string.Empty) == Service_Namee.Replace(" ", string.Empty)) && (itemm.ProviderContractID == Provider_Contract_ID)
                                     select new { valuess, itemm }).FirstOrDefault();
                        service_id = reco2.valuess.ServiceID;
                        service_code = reco2.valuess.ServiceCode;
                        service_Namenew = reco2.valuess.ServiceName;

                    }
                    else
                    {
                        var re2 = db.PVMedDatas.FirstOrDefault(d => d.MedDrugName.Replace(" ", string.Empty) == (Service_Namee.Replace(" ", string.Empty)));
                        if (re2 == null)
                        {
                            var re22 = db.PVMedDatas.FirstOrDefault(d => d.MedDrugName.Replace(" ", string.Empty).Contains(Service_Namee.Replace(" ", string.Empty)));
                            re2 = re22;
                        }
                        service_id = re2.MedDrugID;
                        service_code = re2.MedDrugCode;
                        service_Namenew = re2.MedDrugName;

                    }
                    using (var database = new MedInsuranceProEntities())
                    {
                        var Service_Price = item.ServicePrice;
                        var service_Amt = item.ServiceAmt;
                        var Requested_Price = item.RequestedPrice;
                        var ClaimItem_ID = item.ClaimItemIDd;
                        var foo = database.CLMClaimsItemsONLINEs.Where(f => f.ClaimItemID == ClaimItem_ID).FirstOrDefault();
                        foo.ServiceID = service_id;
                        foo.ServiceCode = service_code;
                        foo.ServiceName = service_Namenew;
                        foo.UserApproveID = int.Parse(Session["UserID"].ToString());
                        foo.ServicePrice     = Service_Price;
                        foo.ServiceAmt = service_Amt;
                        foo.RequestedPrice = Requested_Price;
                        database.SaveChanges();
                    }
                }

            }

            var claimformdata_online = db.CLMClaimsONLINEs.FirstOrDefault(d => d.ClaimID == claimIdSelect);
            int calimid = 0;
            try
            {
                var maxvalu = db.CLMClaims.Max(o => o.ClaimID);
                var resul = db.CLMClaims.First(o => o.ClaimID == maxvalu);
                calimid = (resul.ClaimID) + 1;
            }
            catch
            {
                calimid = 1;
            }
            string card_num = claimformdata_online.CardNumber;
            DateTime? DatProcedure = claimformdata_online.ProcedureDate;

            var cont_num = (
from item1 in db.ContractMembers
join item2 in db.Contracts on item1.ContractID equals item2.ContractID into finalGroup0
from finalItem0 in finalGroup0.DefaultIfEmpty()
where finalItem0.StartDate <= DatProcedure && finalItem0.EndDate >= DatProcedure && item1.CardNumber == card_num
select new { finalItem0.ContractNumber, item1.ContractMemberID, finalItem0.ContractID, item1.PlanTypeID, finalItem0.StartDate, finalItem0.EndDate }).OrderByDescending(aa => aa.ContractID).FirstOrDefault();

            int contract_member_id = cont_num.ContractMemberID;
            int contract_id = cont_num.ContractID;
            int plan_type_id = cont_num.PlanTypeID;


            var claim_form_num = long.Parse(claimformdata_online.ClaimFormNum.ToString());
            int? contractmember_id = claimformdata_online.ContractMemberID;
            DateTime? symptoms_date = claimformdata_online.symptomsDate;
            DateTime? procedure_date = claimformdata_online.ProcedureDate;
            int? claimservice_id = claimformdata_online.ClaimServiceID;
            string diagnosiss = claimformdata_online.Diagnosis;
            string icd_code = claimformdata_online.ICDCode;
            string contract_num = claimformdata_online.ContractNum;
            string card_number = claimformdata_online.CardNumber;
            int providerid = provideridd;
            //int providerid = int.Parse(claimformdata_online.ProviderID.ToString());
            int? ProviderCat_ID = claimformdata_online.ProviderCatID;
            int userAddID = int.Parse(claimformdata_online.UserAddID.ToString());
            //int userApprovID = int.Parse(claimformdata_online.UserApproveID.ToString());       
            int userApprovID = providerid;

            var provider_name = db.PVProviderDatas.FirstOrDefault(d => d.ProviderID == providerid).ProviderName;
            //string branchname = claimformdata_online.BranchName;
            string branchname = branchnamee;

            CLMClaim newdataa = new CLMClaim()
            {

                ClaimID = calimid,
                RecBatchID = 0,
                ClaimFormNum = claim_form_num,
                ContractMemberID = contractmember_id,
                symptomsDate = symptoms_date,
                ProcedureDate = procedure_date,
                ClaimServiceID = claimservice_id,
                Diagnosis = diagnosiss,
                ICDCode = icd_code,
                TClaimAmt = 0,
                TDeductAmt = 0,
                TDiscountAmt = 0,
                TAClaimAmt = 0,
                TMedicalDeductAmt = 0,
                TDifferenceAmt = 0,
                ClaimStatusID = 1,
                // will edit 
                UserAddID = userAddID,
                UserApproveID = userApprovID,
                NetClaimedAmt = 0,
                TRequestedAmt = 0,
                BatchNumber = "",
                ContractNum = contract_num,
                CardNumber = card_number,
                ClaimDeductAmt = 0,
                ClaimDeductNote = "",
                TAuditDeductAmt = 0,
                IsOverCeiling = false,
                RepType = false,
                ProviderID = providerid,
                ProviderName = provider_name,
                BranchName = branchname

            };
            db.CLMClaims.Add(newdataa);
            db.SaveChanges();


            //var maxvalu1 = db.clmclaims.max(o => o.claimid);
            //var resul1 = db.clmclaims.first(o => o.claimid == maxvalu1);
            //int calimid1 = resul1.claimid;
            var claim_dataa = db.CLMClaims.Where(d => d.RecBatchID == 0 && d.ClaimFormNum == claim_form_num && d.ProviderID == providerid).OrderByDescending(d => d.ClaimID).FirstOrDefault();
            var claim_id_inserted = claim_dataa.ClaimID;
            //clmclaimitem
            int claimitemid = 0;
            try
            {
                var maxval = db.CLMClaimItems.Max(o => o.ClaimItemID);
                var resu = db.CLMClaimItems.First(o => o.ClaimItemID == maxval);
                claimitemid = resu.ClaimItemID;
            }
            catch
            {
                claimitemid = 1;
            }
            int provi_id = providerid;
            var re3 = db.PVContracts
                                .Where(m => m.ProviderID == provi_id && m.StartDate <= procedure_date && m.EndDate >= procedure_date)
                                .OrderByDescending(m => m.ProviderContractID)
                                .FirstOrDefault();
            if (re3 == null)
            {
                var re32 = db.PVContracts
                        .Where(m => m.ProviderID == provi_id && m.IsActive == true)
                        .OrderByDescending(m => m.ProviderContractID)
                        .FirstOrDefault();
                re3 = re32;
            }
            int provider_contract_id = re3.ProviderContractID;
            decimal local_discount_per = re3.LocalDiscountPer;
            decimal foreign_discount_per = re3.ForeignDiscountPer;
            decimal? chronic_localdis_per = re3.ChronicLocalDisPer;
            decimal? chronic_foreign_disper = re3.ChronicForeignDisPer;


            bool? isDeductablee = false;
            //var notess = "";
            var ApprovalOnline = db.ClmApprovalOnlines.FirstOrDefault(d => d.ApprovalCode == claim_form_num && d.ContractMemberID == contractmember_id);
            if (ApprovalOnline == null)
            {
                var ApprovalOnlineWithForm = db.ClmApprovalOnlines.FirstOrDefault(d => d.ClaimFormNum == claim_form_num && d.ContractMemberID == contractmember_id);
                isDeductablee = ApprovalOnlineWithForm.IsDeduct;
                //notess = ApprovalOnlineWithForm.Notes;
            }
            else
            {
                isDeductablee = ApprovalOnline.IsDeduct;
                //notess = ApprovalOnline.Notes;
            }


            decimal deductable = 0;

            decimal tclaim_amt = 0;
            decimal tdeductamt = 0;
            decimal tdiscountamt = 0;
            //decimal taclaimamt = 0;
            decimal tmedicaldeductamt = 0;
            decimal tdifferenceamt = 0;
            decimal netclaimedamt = 0;
            decimal trequestedamt = 0;
            decimal claimdeductamt = 0;
            //decimal tauditdeductamt = 0;
            IList<CLMClaimItem> CLMClaimItems = new List<CLMClaimItem>();

            for (var i = 0; i < Expanded_Services.Count; i++)
            {

                claimitemid++;
                var claimitemidSelect1 = Expanded_Services[i].ClaimItemIDd;
                var claimsSelectData = db.CLMClaimsItemsONLINEs.FirstOrDefault(d => d.ClaimItemID == claimitemidSelect1);

                // get service code
                string service_namee = claimsSelectData.ServiceName;

                //int service_id = claimsSelectData.ServiceID;
                //string service_code = claimsSelectData.ServiceCode;


                var service_id = 0;
                var service_code = "";
                if (Provider_Cat_ID != 2)
                {
                    var reco2 = (from valuess in db.PVServices
                                 join itemm in db.PVServicePricings on valuess.ServiceID equals itemm.ServiceID
                                 where (valuess.ServiceName.Replace(" ", string.Empty) == service_namee.Replace(" ", string.Empty)) && (itemm.ProviderContractID == Provider_Contract_ID)
                                 select new { valuess, itemm }).FirstOrDefault();
                    service_id = reco2.valuess.ServiceID;
                    service_code = reco2.valuess.ServiceCode;

                }
                else
                {
                    var re2 = db.PVMedDatas.FirstOrDefault(d => d.MedDrugName.Replace(" ", string.Empty) == (service_namee.Replace(" ", string.Empty)));
                    if (re2 == null)
                    {
                        var re22 = db.PVMedDatas.FirstOrDefault(d => d.MedDrugName.Replace(" ", string.Empty).Contains(service_namee.Replace(" ", string.Empty)));
                        re2 = re22;
                    }
                    service_id = re2.MedDrugID;
                    service_code = re2.MedDrugCode;

                }
                bool is_import = false;
                bool is_coverd = false;

                decimal service_price =decimal.Parse((Expanded_Services[i].ServicePrice).ToString());
                decimal service_discountperc = 0;
                decimal service_discountval = 0;
                if (ProviderCat_ID != 2)
                {
                    var re4 = db.PVServicePricings
                            .Where(d => d.ProviderContractID == provider_contract_id && d.ServiceID == service_id)
                            .OrderByDescending(m => m.ServicePricingID)
                            .FirstOrDefault();
                    service_discountperc = re4.DiscountPerc;
                    service_discountval = re4.DiscountVal;

                }
                else
                {
                    var PVMedExceptionData = db.PVMedExceptions.FirstOrDefault(d => d.MedDrugName.Replace(" ", string.Empty) == (service_namee.Replace(" ", string.Empty)) && d.ProviderContractID == Provider_Contract_ID);
                    if (PVMedExceptionData != null)
                    {
                        local_discount_per = PVMedExceptionData.DiscountPer;
                        foreign_discount_per = PVMedExceptionData.DiscountPer;
                        chronic_localdis_per = PVMedExceptionData.DiscountPer;
                        chronic_foreign_disper = PVMedExceptionData.DiscountPer;


                    }
                    else
                    {
                        local_discount_per = re3.LocalDiscountPer;
                        foreign_discount_per = re3.ForeignDiscountPer;
                        chronic_localdis_per = re3.ChronicLocalDisPer;
                        chronic_foreign_disper = re3.ChronicForeignDisPer;

                    }
                    var re2 = db.PVMedDatas.FirstOrDefault(d => d.MedDrugID == service_id);
                    int med_cat = re2.MedCatID;
                    is_import = re2.IsImported;
                    is_coverd = re2.IsCoverd;

                    service_discountperc = 0;

                    if (is_import == true)
                    {
                        service_discountperc = foreign_discount_per;
                    }
                    else
                    {
                        service_discountperc = local_discount_per;
                    }

                    service_discountval = service_discountperc * service_price;

                }
                int claim_service_id1 = claimsSelectData.ClaimServiceID;

                decimal deduct_per = 0;
                if (claimservice_id == 14 || claimservice_id == 19 || claim_service_id1 == 14 || claim_service_id1 == 19 || isDeductablee == true)
                {
                    deduct_per = 0;
                }
                else
                {
                    var re5 = db.ContractServiceCeilings.FirstOrDefault(d => d.ContractID == contract_id && d.PlanTypeID == plan_type_id && d.ServiceID == claim_service_id1);
                    deduct_per = re5.DeductPer;
                }
                deductable = deduct_per;

                //var re5 = db.contractserviceceilings.firstordefault(d => d.contractid == contract_id && d.plantypeid == plan_type_id && d.serviceid == claimservice_id);
                //decimal deduct_per = re5.deductper;

                decimal service_deduct_amt = deduct_per * service_price;
                decimal net_service_amt = service_price - service_deduct_amt - service_discountval;

                decimal service_request = decimal.Parse((Expanded_Services[i].RequestedPrice).ToString());


                decimal service_qnt = decimal.Parse((claimsSelectData.ServiceQnt).ToString());
                decimal service_unitamt = service_price;
                decimal claimed_amt = service_unitamt * service_qnt;
                decimal claimedRequest_amt = service_request /** service_qnt*/;

                decimal deduct_amt = claimedRequest_amt * deduct_per;
                //decimal deduct_amt = claimed_amt * deduct_per;
                decimal discount_amt = claimed_amt * service_discountperc;
                decimal medical_deductamt = 0;
                decimal due_amt = claimed_amt - deduct_amt - discount_amt;
                //decimal requested_amt = due_amt;
                //decimal difference_amt = 0;

                decimal requested_amt = (service_request /** service_qnt*/) - deduct_amt - discount_amt;
                decimal difference_amt = requested_amt - due_amt;


                decimal net_payment_amt = due_amt;

                string reject_reson = "";

                decimal Number_of_timesday = decimal.Parse(claimsSelectData.Number_of_times_a_day.ToString());
                decimal Dosage_sizes = decimal.Parse(claimsSelectData.Dosage_size.ToString());
                decimal Number_of_dayss = decimal.Parse(claimsSelectData.Number_of_days.ToString());
                int Number_of_daysss = Decimal.ToInt32(Number_of_dayss);
                DateTime EndDate_Effective = DateTime.Now.AddDays(Number_of_daysss);
               
                //int a = claimservice_id;

                CLMClaimItem newdataaa = new CLMClaimItem()
                {

                    ClaimItemID = claimitemid,
                    ClaimID = claim_id_inserted,
                    ServiceID = service_id,
                    ServiceCode = service_code,
                    ServiceName = service_namee,
                    ServiceQnt = service_qnt,
                    ServiceUnitAmt = service_unitamt,
                    ClaimedAmt = claimed_amt,
                    DeductAmt = deduct_amt,
                    DiscountAmt = discount_amt,
                    FDiscountAmt = 0,
                    MedicalDeductAmt = medical_deductamt,
                    DueAmt = due_amt,
                    RequestedAmt = requested_amt,
                    DifferenceAmt = difference_amt,
                    RejectNotes = reject_reson,
                    IsSysEntry = false,
                    // will edit
                    UserEntryID = userAddID,
                    UserEntryDate = DateTime.Now,
                    UserApprove = false,
                    UserApproveID = userApprovID,
                    BatchNumber = "",
                    ClaimNum = claim_form_num,
                    ServiceAmt = service_price,
                    ServiceDeductAmt = service_deduct_amt,
                    ServiceDiscountAmt = service_discountval,
                    NetServiceAmt = net_service_amt,
                    ClaimServiceID = claim_service_id1,
                    ServiceDiscountPer = service_discountperc,
                    IsCovered = is_coverd,
                    IsImported = is_import,
                    NetPaymentAmt = net_payment_amt,
                    ReimbPer = 0,
                    AuditDeductAmt = 0,
                    AuditComments = "",
                    Number_of_times_a_day = Number_of_timesday,
                    Dosage_size = Dosage_sizes,
                    Number_of_days = Number_of_dayss,
                    EndDate_Effective_Material = EndDate_Effective,

                    IsOverCeiling = false

                };
                //db.CLMClaimItems.Add(newdataaa);
                //db.SaveChanges();
                CLMClaimItems.Add(newdataaa);

                tclaim_amt += claimed_amt;
                tdeductamt += deduct_amt;
                tdiscountamt += discount_amt;
                //taclaimamt = 0;
                tmedicaldeductamt += medical_deductamt;
                tdifferenceamt += difference_amt;
                netclaimedamt += net_payment_amt;
                trequestedamt += requested_amt;
                claimdeductamt += service_deduct_amt;
                //tauditdeductamt = 0;


            }
            using (var context = new MedInsuranceProEntities())
            {
                context.CLMClaimItems.AddRange(CLMClaimItems);
                context.SaveChanges();
            }


            using (var database = new MedInsuranceProEntities())
            {

                var foo = database.CLMClaims.Where(f => f.ClaimID == claim_id_inserted).FirstOrDefault();
                foo.TClaimAmt = tclaim_amt;
                foo.TDeductAmt = tdeductamt;
                foo.TDiscountAmt = tdiscountamt;
                //foo.taclaimamt = tclaim_amt;
                foo.TMedicalDeductAmt = tmedicaldeductamt;
                foo.TDifferenceAmt = tdifferenceamt;
                foo.NetClaimedAmt = netclaimedamt;
                foo.TRequestedAmt = trequestedamt;
                foo.ClaimDeductAmt = claimdeductamt;
                //foo.tauditdeductamt = tclaim_amt;
                database.SaveChanges();
            }

            //using (var database = new MedInsuranceProEntities())
            //{

            //    var foo = database.CLMClaimsONLINEs.Where(f => f.ClaimID == claim_id).FirstOrDefault();
            //    foo.ClaimStatusID = 3;
            //    database.SaveChanges();
            //}
            var arrlist = new ArrayList();
            arrlist.Add(deductable * 100);
            arrlist.Add(tdeductamt);
            arrlist.Add(trequestedamt);
            //arrlist.Add(tclaim_amt);

            //int providerid1 = int.Parse(Session["providerid"].ToString());
            //int branchid = int.Parse(Session["BranchID"].ToString());

            //var ClaimFormNumbers = (from values in db.CLMClaimsONLINEs
            //                        where values.ClaimStatusID == 2 && values.ProviderID == providerid1 && values.BranchID == branchid
            //                        select values.ClaimFormNum + " ## " + values.CardNumber + " ## " + values.ClaimID).ToArray();
            //arrlist.Add(ClaimFormNumbers);
            arrlist.Add(claim_id_inserted);

            return Json(arrlist, JsonRequestBehavior.AllowGet);
        }


        public JsonResult Get_Deduct_Claimform(string id)
        {
            try
            {
                var claimItemId = int.Parse(id);
                var ClaimIDD = db.CLMClaimsItemsONLINEs.FirstOrDefault(d => d.ClaimItemID == claimItemId).ClaimID;
                var claimdataOnline = db.CLMClaimsONLINEs.FirstOrDefault(d => d.ClaimID == ClaimIDD);

                var claimForm = claimdataOnline.ClaimFormNum;
                var ContractMemberID = claimdataOnline.ContractMemberID;
                var claimservice_id = claimdataOnline.ClaimServiceID;
                var card_num = claimdataOnline.CardNumber;
                var DatProcedure = claimdataOnline.ProcedureDate;

                var cont_num = (
    from item1 in db.ContractMembers
    join item2 in db.Contracts on item1.ContractID equals item2.ContractID into finalGroup0
    from finalItem0 in finalGroup0.DefaultIfEmpty()
    where finalItem0.StartDate <= DatProcedure && finalItem0.EndDate >= DatProcedure && item1.CardNumber == card_num
    select new { finalItem0.ContractNumber, item1.ContractMemberID, finalItem0.ContractID, item1.PlanTypeID, finalItem0.StartDate, finalItem0.EndDate }).OrderByDescending(aa => aa.ContractID).FirstOrDefault();

                int contract_member_id = cont_num.ContractMemberID;
                int contract_id = cont_num.ContractID;
                int plan_type_id = cont_num.PlanTypeID;

                bool? isDeductablee = false;
                var notess = "";
                var ApprovalOnline = db.ClmApprovalOnlines.FirstOrDefault(d => d.ApprovalCode == claimForm && d.ContractMemberID == ContractMemberID);
                if (ApprovalOnline == null)
                {
                    var ApprovalOnlineWithForm = db.ClmApprovalOnlines.FirstOrDefault(d => d.ClaimFormNum == claimForm && d.ContractMemberID == ContractMemberID);
                    isDeductablee = ApprovalOnlineWithForm.IsDeduct;
                    notess = ApprovalOnlineWithForm.Notes;
                }
                else
                {
                    isDeductablee = ApprovalOnline.IsDeduct;
                    notess = ApprovalOnline.Notes;
                }


                decimal deduct_per = 0;
                if (claimservice_id == 14 || claimservice_id == 19 || isDeductablee == true)
                {
                    deduct_per = 0;
                }
                else
                {
                    var re5 = db.ContractServiceCeilings.FirstOrDefault(d => d.ContractID == contract_id && d.PlanTypeID == plan_type_id && d.ServiceID == claimservice_id);
                    deduct_per = re5.DeductPer;
                }
                var arr_lis = new ArrayList();
                arr_lis.Add(deduct_per * 100);
                arr_lis.Add(notess);

                return Json(arr_lis, JsonRequestBehavior.AllowGet);
            }
            catch
            {
                return Json("qq", JsonRequestBehavior.AllowGet);

            }
        }



        [HttpGet]
        public ActionResult Doctor_ClaimForm()
        {

            if ((Session["UserID"] == null) || (int.Parse(Session["Doctor_ClaimForm"].ToString()) == 0))
            {
                return RedirectToAction("login", "ClaimForm");
            }

            ViewBag.Service_Name = "";

            var provider_Name = (from values in db.PVProviderDatas
                                 where values.IsActive == true
                                 select values.ProviderName).ToArray();
            ViewBag.ProviderName = provider_Name;

            var ProviderID = int.Parse(Session["providerid"].ToString());
            var providercatID = db.PVProviderDatas.FirstOrDefault(d => d.ProviderID == ProviderID).ProviderCatID;

            if (providercatID==5)
            {
                var DatProcedure = DateTime.Now;
                var re3 = db.PVContracts
                                      .Where(m => m.ProviderID == ProviderID && m.StartDate <= DatProcedure && m.EndDate >= DatProcedure)
                                      .OrderByDescending(m => m.ProviderContractID)
                                      .FirstOrDefault();
                if (re3 == null)
                {
                    var re32 = db.PVContracts
                            .Where(m => m.ProviderID == ProviderID && m.IsActive == true)
                            .OrderByDescending(m => m.ProviderContractID)
                            .FirstOrDefault();
                    re3 = re32;
                }

                int Provider_Contract_ID = re3.ProviderContractID;
                var provider_Services = (from values in db.PVServicePricings
                                         join item1 in db.PVServices on values.ServiceID equals item1.ServiceID
                                         where values.ProviderContractID == Provider_Contract_ID
                                         select new { item1.ServiceName, values.Price }).FirstOrDefault();
            ViewBag.providerServices = provider_Services;

            }else
            {
                ViewBag.providerServices = null;

            }
            return View();
        }



        public ActionResult MedicalHistory(string card_Number)
        {
           var Card_NUM = card_Number;
            var res = db.MedicalHistoryMembers.Where(d => d.CardNumber == Card_NUM).AsNoTracking().ToList();
            ViewBag.result = res;
            return View();
        }

        public class Approval_Servicess
        {
            public string Service_Name { get; set; }
            public decimal Service_Qnt { get; set; }
            public string Service_Note { get; set; }

            public decimal ServicePrice { get; set; }
            public decimal RequestedPrice { get; set; }
        }

        public static HttpPostedFileBase imgfilejson;
        public JsonResult addpdffiles(HttpPostedFileBase imgfile)
        {
            imgfilejson = imgfile;
            return Json("", JsonRequestBehavior.AllowGet);
        }
        public JsonResult InsertApproval_Servicess(List<Approval_Servicess> Claims_ServicesDoc, List<Approval_Servicess> Claim_Servicese, List<Approval_Servicess> Claims_Serviceslabs, List<Approval_Servicess> Claims_Servicesscan, List<Approval_Servicess> Claims_Servicesopticalcenter, List<Approval_Servicess> Claims_Servicesphysiothrapy, string Card_numberr, string Diagnosiss, string ICD_Numberr)
        {

            var imgfile = imgfilejson;

            int insertedRecords = 0;
            var arrlis = new ArrayList();
            if (Claims_ServicesDoc == null)
            {
                if (Claim_Servicese == null)
                {
                    if (Claims_Serviceslabs == null)
                    {
                        if (Claims_Servicesscan == null)
                        {
                            if (Claims_Servicesopticalcenter == null)
                            {
                                if (Claims_Servicesphysiothrapy == null)
                                {
                                    insertedRecords = 1;
                                    arrlis.Add(insertedRecords);
                                    var ErorMessage = "Claim Services not Exist , You Must ADD at least One Service";
                                    arrlis.Add(ErorMessage);
                                    return Json(arrlis, JsonRequestBehavior.AllowGet);
                                }
                            }
                        }
                    }
                }
            }
            //CLMApprovals
            int Aprovallid = 0;
            try
            {
                var maxValu = db.ClmApprovalOnlines.Max(o => o.ApprovalID);
                var resul = db.ClmApprovalOnlines.First(o => o.ApprovalID == maxValu);
                Aprovallid = (resul.ApprovalID) + 1;
            }
            catch
            {
                Aprovallid = 1;
            }
            string card_num = Card_numberr;
            var datenow = DateTime.Now;
            var cont_num = (
from item1 in db.ContractMembers
join item2 in db.Contracts on item1.ContractID equals item2.ContractID into finalGroup0
from finalItem0 in finalGroup0.DefaultIfEmpty()
where finalItem0.IsActive == true && finalItem0.StartDate <= datenow && finalItem0.EndDate >= datenow && item1.CardNumber == card_num
select new { finalItem0.ContractNumber, item1.ContractMemberID, finalItem0.ContractID, item1.PlanTypeID, item1.CustomerID, finalItem0.EndDate, item1.DeActiveDate }).OrderByDescending(a => a.ContractID).FirstOrDefault();
            if (cont_num == null)
            {
                insertedRecords = 1;
                arrlis.Add(insertedRecords);
                var ErorMessage = "Card Number is not Correct";
                arrlis.Add(ErorMessage);
                return Json(arrlis, JsonRequestBehavior.AllowGet);
            }

            string contract_num = cont_num.ContractNumber;
            int Contract_Member_ID = cont_num.ContractMemberID;
            int Contract_ID = cont_num.ContractID;
            int plan_type_id = cont_num.PlanTypeID;
            int Customer_ID = cont_num.CustomerID;
            DateTime End_Date = cont_num.EndDate;
            DateTime? DeActive_Date = cont_num.DeActiveDate;


            if (DeActive_Date < DateTime.Now && DeActive_Date?.ToShortDateString() != "01/01/0001")
            {
                insertedRecords = 1;
                arrlis.Add(insertedRecords);
                var ErorMessage = "Member Activity Status is Stopped \n Deactive Date is : " + DeActive_Date?.ToShortDateString();
                arrlis.Add(ErorMessage);
                return Json(arrlis, JsonRequestBehavior.AllowGet);
            }
            if (End_Date < DateTime.Now)
            {
                insertedRecords = 1;
                arrlis.Add(insertedRecords);
                var ErorMessage = "The Contract Period Time has Ended \n End Date is : " + End_Date.ToShortDateString();
                arrlis.Add(ErorMessage);
                return Json(arrlis, JsonRequestBehavior.AllowGet);
            }


            DateTime Approval_Datee = DateTime.Now;
            string Diagnosis = Diagnosiss;
            if (Diagnosis == null)
            {
                insertedRecords = 1;
                arrlis.Add(insertedRecords);
                var ErorMessage = "Select Diagnosis Name is Required";
                arrlis.Add(ErorMessage);
                return Json(arrlis, JsonRequestBehavior.AllowGet);
            }
            string ICD_Number = ICD_Numberr;
            long? claimformNum = 0;
            var re3 = db.ClmApprovalOnlines
                              .Where(m => m.ClaimFormNum.ToString().StartsWith("9")).Max(o => o.ClaimFormNum);
            if (re3 == null)
            {
                claimformNum = 9000000000;
            }
            else
            {
                claimformNum = re3 + 1;
            }
            var approval_code_check = db.ClmApprovalOnlines.Where(d => d.ClaimFormNum == claimformNum).OrderByDescending(d => d.ApprovalID).FirstOrDefault();
            if (approval_code_check != null)
            {
                var re31 = db.ClmApprovalOnlines
                                  .Where(m => m.ClaimFormNum.ToString().StartsWith("9")).Max(o => o.ClaimFormNum);
                if (re3 == null)
                {
                    claimformNum = 9000000000;
                }
                else
                {
                    claimformNum = re31 + 1;
                }
            }
            ClmApprovalOnline newdataa = new ClmApprovalOnline()
            {

                ApprovalID = Aprovallid,
                CustomerID = Customer_ID,
                ContractMemberID = Contract_Member_ID,
                ApprovalCode = 0 /*approval_code*/,
                ApprovalDate = Approval_Datee,
                Diagnosis = Diagnosis,
                ICDCode = ICD_Number,
                Notes = "",
                Approvalmemo = "",
                ContactPhone = "",
                CardNumber = card_num,
                ContractNum = contract_num,
                ClaimFormNum = claimformNum,
                ApprovalStatus = 4,
                //////////////
                IssueUserID = int.Parse(Session["providerid"].ToString()),
                ChronicTemplateID = 0,
                MemberShowStatus = 0,
                IsDeduct = false

            };
            int ApprovalItem_ID = 0;
            //CLMApprovalsItem
            try
            {
                var maxVall1 = db.ClmApprovalItemsOnlines.Max(o => o.ApprovalItemID);
                var resull1 = db.ClmApprovalItemsOnlines.First(o => o.ApprovalItemID == maxVall1);
                ApprovalItem_ID = (resull1.ApprovalItemID);
            }
            catch
            {
                ApprovalItem_ID = 1;
            }

            int providerid = int.Parse(Session["providerid"].ToString());
            var providercatID = db.PVProviderDatas.FirstOrDefault(d => d.ProviderID == providerid).ProviderCatID;
            string branchName = Session["branchName"].ToString();
            int BranchID = int.Parse(Session["BranchID"].ToString());


            IList<ClmApprovalItemsOnline> CLMApprovalItemsOnlineDoc = new List<ClmApprovalItemsOnline>();
            CLMClaimsONLINE CLMClaimsONLINE = new CLMClaimsONLINE();
            IList<CLMClaimsItemsONLINE> CLMClaimsItemsONLINEs = new List<CLMClaimsItemsONLINE>();

            if (Claims_ServicesDoc != null)
            {
                
                var date = DateTime.Now;
                var re3contract = db.PVContracts
                                    .Where(m => m.ProviderID == providerid && m.StartDate <= date && m.EndDate >= date)
                                    .OrderByDescending(m => m.ProviderContractID)
                                    .FirstOrDefault();
                if (re3contract == null)
                {
                    var re32 = db.PVContracts
                      .Where(m => m.ProviderID == providerid && m.IsActive == true)
                      .OrderByDescending(m => m.StartDate)
                      .FirstOrDefault();
                    re3contract = re32;

                }
                int Provider_Contract_ID = re3contract.ProviderContractID;

              

                CLMClaimsONLINE = new CLMClaimsONLINE()
                {

                    //ClaimID = calimid,
                    ClaimFormNum = 0,
                    ContractMemberID = Contract_Member_ID,
                    symptomsDate = Approval_Datee,
                    ProcedureDate = Approval_Datee,
                    ClaimServiceID = 0,
                    Diagnosis = Diagnosis,
                    ICDCode = ICD_Number,
                    SecondDiagnosis = "",
                    SecondICDCode = "",
                    ThirdDiagnosis = "",
                    ThirdICDCode = "",
                    ClaimStatusID = 3,
                    // will edit 
                    UserAddID = int.Parse(Session["UserID"].ToString()),
                    UserApproveID = 0,
                    ContractNum = contract_num,
                    CardNumber = card_num,
                    ProviderID = providerid,
                    ProviderCatID = providercatID,
                    BranchID = BranchID,
                    BranchName = branchName,
                };
                //CLMClaimsONLINE.Add(CLMClaimsONLINEm);
                for (var i = 0; i < Claims_ServicesDoc.Count; i++)
                {

                    ApprovalItem_ID++;
                    // get service code
                    string Service_Namee = Claims_ServicesDoc[i].Service_Name;

                    var reco2 = (from valuess in db.PVServices
                                 join itemm in db.PVServicePricings on valuess.ServiceID equals itemm.ServiceID
                                 where (valuess.ServiceName.Replace(" ", string.Empty) == Service_Namee.Replace(" ", string.Empty)) && (itemm.ProviderContractID == Provider_Contract_ID)
                                 select new { valuess, itemm }).FirstOrDefault();
                   int service_id = reco2.valuess.ServiceID;
                   string service_code = reco2.valuess.ServiceCode;
                    var service_Namenew = reco2.valuess.ServiceName;

                    decimal Service_Qnt = Claims_ServicesDoc[i].Service_Qnt;
                    string Service_Notes = Claims_ServicesDoc[i].Service_Note;
                    if (Service_Notes == null)
                    { Service_Notes = ""; }
                    decimal ServicePricedoc = Claims_ServicesDoc[i].ServicePrice;
                    decimal RequestedPricedoc = Claims_ServicesDoc[i].RequestedPrice;

                    ClmApprovalItemsOnline newdataaa1 = new ClmApprovalItemsOnline()
                    {

                        ApprovalItemID = ApprovalItem_ID,
                        ApprovalID = 0 /*approval_id_inserted*/,
                        ApprovalCode = 0 /*long.Parse(approval_code.ToString())*/,
                        ServiceName = Service_Namee,
                        ServiceQnt = Service_Qnt,
                        CLaimServiceID = 0,
                        ApprovalCLaimServiceID = 0,
                        ServiceNote = Service_Notes,
                        Number_of_times_a_day = 1,
                        Dosage_size = 1,
                        Number_of_days = 1,
                        ProviderCatId = providercatID


                    };
                    CLMApprovalItemsOnlineDoc.Add(newdataaa1);
                    //db.CLMApprovalItems.Add(newdataaa1);
                    //db.SaveChanges();

                    CLMClaimsItemsONLINE newdataaa = new CLMClaimsItemsONLINE()
                    {
                        ClaimID = 0,
                        ServiceID = service_id,
                        ServiceCode = service_code,
                        ServiceName = service_Namenew,
                        ServiceQnt = Service_Qnt,
                        ServicePrice = ServicePricedoc,
                        RequestedPrice = Service_Qnt* RequestedPricedoc,
                        ServiceAmt = Service_Qnt* ServicePricedoc,
                        // will edit
                        UserEntryID = int.Parse(Session["UserID"].ToString()),
                        UserEntryDate = DateTime.Now,
                        UserApprove = false,
                        UserApproveID = 0,
                        ClaimNum = 0,
                        ClaimServiceID = 0,
                        IsCovered = false,
                        IsImported = false,
                        Refuse_Note = Service_Notes,
                        ClaimItemStatusID = 3,
                        Number_of_times_a_day = 1,
                        Dosage_size = 1,
                        Number_of_days = 1,

                    };
                    CLMClaimsItemsONLINEs.Add(newdataaa);


                }
            }

            IList<ClmApprovalItemsOnline> CLMApprovalItemsOnline = new List<ClmApprovalItemsOnline>();
            if (Claim_Servicese != null)
            {
                for (var i = 0; i < Claim_Servicese.Count; i++)
                {

                    ApprovalItem_ID++;
                    // get service code
                    string Service_Namee = Claim_Servicese[i].Service_Name;

                    decimal Service_Qnt = Claim_Servicese[i].Service_Qnt;
                    string Service_Notes = Claim_Servicese[i].Service_Note;
                    if (Service_Notes == null)
                    { Service_Notes = ""; }
                    ClmApprovalItemsOnline newdataaa1 = new ClmApprovalItemsOnline()
                    {

                        ApprovalItemID = ApprovalItem_ID,
                        ApprovalID = 0 /*approval_id_inserted*/,
                        ApprovalCode = 0 /*long.Parse(approval_code.ToString())*/,
                        ServiceName = Service_Namee,
                        ServiceQnt = Service_Qnt,
                        CLaimServiceID = 0,
                        ApprovalCLaimServiceID = 0,
                        ServiceNote = Service_Notes,
                        Number_of_times_a_day = 1,
                        Dosage_size = 1,
                        Number_of_days = 7,
                        ProviderCatId = 2


                    };
                    CLMApprovalItemsOnline.Add(newdataaa1);
                    //db.CLMApprovalItems.Add(newdataaa1);
                    //db.SaveChanges();
                    ApprovalItem_ID++;

                }
            }

            IList<ClmApprovalItemsOnline> CLMApprovalItemsOnlineLab = new List<ClmApprovalItemsOnline>();
            if (Claims_Serviceslabs != null)
            {

                for (var i = 0; i < Claims_Serviceslabs.Count; i++)
                {

                    // get service code
                    string Service_Namee = Claims_Serviceslabs[i].Service_Name;

                    decimal Service_Qnt = Claims_Serviceslabs[i].Service_Qnt;
                    string Service_Notes = Claims_Serviceslabs[i].Service_Note;
                    if (Service_Notes == null)
                    { Service_Notes = ""; }
                    ClmApprovalItemsOnline newdataaa1 = new ClmApprovalItemsOnline()
                    {

                        ApprovalItemID = ApprovalItem_ID,
                        ApprovalID = 0 /*approval_id_inserted*/,
                        ApprovalCode = 0 /*long.Parse(approval_code.ToString())*/,
                        ServiceName = Service_Namee,
                        ServiceQnt = Service_Qnt,
                        CLaimServiceID = 0,
                        ApprovalCLaimServiceID = 0,
                        ServiceNote = Service_Notes,
                        Number_of_times_a_day = 1,
                        Dosage_size = 1,
                        Number_of_days = 1,
                        ProviderCatId = 3



                    };
                    CLMApprovalItemsOnlineLab.Add(newdataaa1);
                    //db.CLMApprovalItems.Add(newdataaa1);
                    //db.SaveChanges();
                    ApprovalItem_ID++;

                }
            }


            IList<ClmApprovalItemsOnline> CLMApprovalItemsOnlineScan = new List<ClmApprovalItemsOnline>();
            if (Claims_Servicesscan != null)
            {

                for (var i = 0; i < Claims_Servicesscan.Count; i++)
                {

                    // get service code
                    string Service_Namee = Claims_Servicesscan[i].Service_Name;

                    decimal Service_Qnt = Claims_Servicesscan[i].Service_Qnt;
                    string Service_Notes = Claims_Servicesscan[i].Service_Note;
                    if (Service_Notes == null)
                    { Service_Notes = ""; }
                    ClmApprovalItemsOnline newdataaa1 = new ClmApprovalItemsOnline()
                    {

                        ApprovalItemID = ApprovalItem_ID,
                        ApprovalID = 0 /*approval_id_inserted*/,
                        ApprovalCode = 0 /*long.Parse(approval_code.ToString())*/,
                        ServiceName = Service_Namee,
                        ServiceQnt = Service_Qnt,
                        CLaimServiceID = 0,
                        ApprovalCLaimServiceID = 0,
                        ServiceNote = Service_Notes,
                        Number_of_times_a_day = 1,
                        Dosage_size = 1,
                        Number_of_days = 1,
                        ProviderCatId = 4


                    };
                    CLMApprovalItemsOnlineScan.Add(newdataaa1);

                    ApprovalItem_ID++;

                }
            }

            IList<ClmApprovalItemsOnline> CLMApprovalItemsOnlineOptical = new List<ClmApprovalItemsOnline>();
            if (Claims_Servicesopticalcenter != null)
            {

                for (var i = 0; i < Claims_Servicesopticalcenter.Count; i++)
                {

                    // get service code
                    string Service_Namee = Claims_Servicesopticalcenter[i].Service_Name;

                    decimal Service_Qnt = Claims_Servicesopticalcenter[i].Service_Qnt;
                    string Service_Notes = Claims_Servicesopticalcenter[i].Service_Note;
                    if (Service_Notes == null)
                    { Service_Notes = ""; }
                    ClmApprovalItemsOnline newdataaa1 = new ClmApprovalItemsOnline()
                    {

                        ApprovalItemID = ApprovalItem_ID,
                        ApprovalID = 0 /*approval_id_inserted*/,
                        ApprovalCode = 0 /*long.Parse(approval_code.ToString())*/,
                        ServiceName = Service_Namee,
                        ServiceQnt = Service_Qnt,
                        CLaimServiceID = 0,
                        ApprovalCLaimServiceID = 0,
                        ServiceNote = Service_Notes,
                        Number_of_times_a_day = 1,
                        Dosage_size = 1,
                        Number_of_days = 1,
                        ProviderCatId = 8


                    };
                    CLMApprovalItemsOnlineOptical.Add(newdataaa1);
                    //db.CLMApprovalItems.Add(newdataaa1);
                    //db.SaveChanges();

                    ApprovalItem_ID++;

                }
            }

            IList<ClmApprovalItemsOnline> CLMApprovalItemsOnlinePhysiothrapy = new List<ClmApprovalItemsOnline>();
            if (Claims_Servicesphysiothrapy != null)
            {

                for (var i = 0; i < Claims_Servicesphysiothrapy.Count; i++)
                {

                    // get service code
                    string Service_Namee = Claims_Servicesphysiothrapy[i].Service_Name;

                    decimal Service_Qnt = Claims_Servicesphysiothrapy[i].Service_Qnt;
                    string Service_Notes = Claims_Servicesphysiothrapy[i].Service_Note;
                    if (Service_Notes == null)
                    { Service_Notes = ""; }
                    ClmApprovalItemsOnline newdataaa1 = new ClmApprovalItemsOnline()
                    {

                        ApprovalItemID = ApprovalItem_ID,
                        ApprovalID = 0 /*approval_id_inserted*/,
                        ApprovalCode = 0 /*long.Parse(approval_code.ToString())*/,
                        ServiceName = Service_Namee,
                        ServiceQnt = Service_Qnt,
                        CLaimServiceID = 0,
                        ApprovalCLaimServiceID = 0,
                        ServiceNote = Service_Notes,
                        Number_of_times_a_day = 1,
                        Dosage_size = 1,
                        Number_of_days = 1,
                        ProviderCatId = 9


                    };
                    CLMApprovalItemsOnlinePhysiothrapy.Add(newdataaa1);
                    //db.CLMApprovalItems.Add(newdataaa1);
                    //db.SaveChanges();

                    ApprovalItem_ID++;

                }
            }
            long? approval_code = 0;
            var re333 = db.ClmApprovalOnlines
                              .Where(m => m.ApprovalCode.ToString().StartsWith("6")).Max(o => o.ApprovalCode);
            if (re333 == null)
            {
                approval_code = 6000000000;
            }
            else
            {
                approval_code = re333 + 1;
            }
            var approval_code_check33 = db.ClmApprovalOnlines.Where(d => d.ApprovalCode == approval_code).OrderByDescending(d => d.ApprovalID).FirstOrDefault();
            if (approval_code_check33 != null)
            {
                var re31 = db.ClmApprovalOnlines
                                  .Where(m => m.ApprovalCode.ToString().StartsWith("6")).Max(o => o.ApprovalCode);
                if (re31 == null)
                {
                    approval_code = 6000000000;
                }
                else
                {
                    approval_code = re31 + 1;
                }
            }
           
            newdataa.ApprovalCode = approval_code;
            newdataa.ChronicTemplateID = 0;
            db.ClmApprovalOnlines.Add(newdataa);
            db.SaveChanges();

            var approval_dataa = db.ClmApprovalOnlines.Where(d => d.ApprovalCode == approval_code).OrderByDescending(d => d.ApprovalID).FirstOrDefault();
            var approval_id_inserted = approval_dataa.ApprovalID;
            if (Claims_ServicesDoc != null)
            {
                long? formformonline = 0;
                if (claimformNum == 0)
                {
                    CLMClaimsONLINE.ClaimFormNum = approval_code;
                    formformonline = approval_code;
                }
                else
                {
                    CLMClaimsONLINE.ClaimFormNum = claimformNum;
                    formformonline = claimformNum;

                }
                db.CLMClaimsONLINEs.Add(CLMClaimsONLINE);
                db.SaveChanges();

                var claim_dataa = db.CLMClaimsONLINEs.Where(d => d.ProviderCatID == providercatID && d.ClaimFormNum == formformonline).OrderByDescending(d => d.ClaimID).FirstOrDefault();
                var claim_id_inserted = claim_dataa.ClaimID;
                foreach (var row in CLMClaimsItemsONLINEs)
                {
                    row.ClaimID = claim_id_inserted;
                    row.ClaimNum = long.Parse(formformonline.ToString());
                }
                using (var context = new MedInsuranceProEntities())
                {
                    context.CLMClaimsItemsONLINEs.AddRange(CLMClaimsItemsONLINEs);
                    context.SaveChanges();
                }

                foreach (var row in CLMApprovalItemsOnlineDoc)
                {
                    row.ApprovalID = approval_id_inserted;
                    row.ApprovalCode = long.Parse(approval_code.ToString());
                }
                using (var context = new MedInsuranceProEntities())
                {
                    context.ClmApprovalItemsOnlines.AddRange(CLMApprovalItemsOnlineDoc);
                    context.SaveChanges();
                }

                ////////// upload image of claimForm 
                
            if(imgfile!=null)
                {
                    MemberClaimForm memberClaimForm = new MemberClaimForm();
                    memberClaimForm.ClaimFormNumber =long.Parse(formformonline.ToString());
                    memberClaimForm.EntryID = providerid;
                    memberClaimForm.EntryDate = DateTime.Now;
                    memberClaimForm.MemberClaimformStatus = 1;
                    memberClaimForm.CardNumber = card_num;
                    memberClaimForm.ProviderCat = providercatID;

                    string path = "";
                    if (imgfile.FileName.Length > 0)
                    {
                        path = "~/Images/" + Path.GetFileName(formformonline + imgfile.FileName);
                        imgfile.SaveAs(Server.MapPath(path));
                        string saveDirectory = @"C:\Publish\Guardian\Images\";
                        string secondpath = saveDirectory + Path.GetFileName(formformonline + imgfile.FileName);
                        imgfile.SaveAs(secondpath);
                    }
                    memberClaimForm.ClaimFormImage = path;

                    db.MemberClaimForms.Add(memberClaimForm);
                    db.SaveChanges();

                    var lastmemberclaimform = db.MemberClaimForms.Where(d => d.ClaimFormNumber == memberClaimForm.ClaimFormNumber && d.EntryID == memberClaimForm.EntryID && d.CardNumber == memberClaimForm.CardNumber).OrderByDescending(d => d.MemberClaimformID).FirstOrDefault();
                    var lastmemberclaimformid = lastmemberclaimform.MemberClaimformID;
                    List<ProviderClaimRole> providerClaimRole = new List<ProviderClaimRole>();

                    for (int i = 1; i <= 9; i++)
                    {
                        ProviderClaimRole PCR = new ProviderClaimRole();
                        PCR.MemberClaimformID = lastmemberclaimformid;
                        PCR.ProviderCatId = i;
                        PCR.ISActive = true;
                        providerClaimRole.Add(PCR);
                    }
                    using (var context = new MedInsuranceProEntities())
                    {
                        context.ProviderClaimRoles.AddRange(providerClaimRole);
                        context.SaveChanges();
                    }



                }

            }
            if (Claim_Servicese != null)
            {
                foreach (var row in CLMApprovalItemsOnline)
                {
                    row.ApprovalID = approval_id_inserted;
                    row.ApprovalCode = long.Parse(approval_code.ToString());
                }
                using (var context = new MedInsuranceProEntities())
                {
                    context.ClmApprovalItemsOnlines.AddRange(CLMApprovalItemsOnline);
                    context.SaveChanges();
                }
            }
            if (Claims_Serviceslabs != null)
            {
               
                foreach (var row in CLMApprovalItemsOnlineLab)
                {
                    row.ApprovalID = approval_id_inserted;
                    row.ApprovalCode = long.Parse(approval_code.ToString());
                }
                using (var context = new MedInsuranceProEntities())
                {
                    context.ClmApprovalItemsOnlines.AddRange(CLMApprovalItemsOnlineLab);
                    context.SaveChanges();
                }
            }
            if (Claims_Servicesscan != null)
            {
                foreach (var row in CLMApprovalItemsOnlineScan)
                {
                    row.ApprovalID = approval_id_inserted;
                    row.ApprovalCode = long.Parse(approval_code.ToString());
                }
                using (var context = new MedInsuranceProEntities())
                {
                    context.ClmApprovalItemsOnlines.AddRange(CLMApprovalItemsOnlineScan);
                    context.SaveChanges();
                }
            }
            if (Claims_Servicesopticalcenter != null)
            {
                foreach (var row in CLMApprovalItemsOnlineOptical)
                {
                    row.ApprovalID = approval_id_inserted;
                    row.ApprovalCode = long.Parse(approval_code.ToString());
                }
                using (var context = new MedInsuranceProEntities())
                {
                    context.ClmApprovalItemsOnlines.AddRange(CLMApprovalItemsOnlineOptical);
                    context.SaveChanges();
                }
            }
            if (Claims_Servicesphysiothrapy != null)
            {

                foreach (var row in CLMApprovalItemsOnlinePhysiothrapy)
                {
                    row.ApprovalID = approval_id_inserted;
                    row.ApprovalCode = long.Parse(approval_code.ToString());
                }
                using (var context = new MedInsuranceProEntities())
                {
                    context.ClmApprovalItemsOnlines.AddRange(CLMApprovalItemsOnlinePhysiothrapy);
                    context.SaveChanges();
                }
            }
            insertedRecords = 0;
            arrlis.Add(insertedRecords);
            arrlis.Add(claimformNum);
            //arrlis.Add(approval_code);
            arrlis.Add(approval_id_inserted);

            using (var database = new MedInsuranceProEntities())
            {
                var foo = database.AlertCMCDoctors.OrderByDescending(f => f.AlertCMCID).FirstOrDefault();
                foo.ClaimFormNum = claimformNum;
                foo.CardNumber = Card_numberr;
                foo.BranchName = branchName;
                foo.ProviderID = providerid;
                foo.BranchID = BranchID;
                database.SaveChanges();
            }

            return Json(arrlis, JsonRequestBehavior.AllowGet);
        }
































        [HttpGet]
        public ActionResult CMC_ClaimFormToReview()
        {
            //if ((Session["UserID"] == null) || (int.Parse(Session["Medical_Services"].ToString()) == 0))
            //{
            //    return RedirectToAction("login", "ClaimForm");
            //}
            //var ClaimFormNumbers = (from values in db.MemberClaimForms
            //                    where values.MemberClaimformStatus == 1
            //                    select values.ClaimFormNumber).ToArray();

            //ViewBag.lis = ClaimFormNumbers;

            var ClaimFormNumbers = (from values in db.CLMClaimsONLINEs
                                    join item in db.PVProviderDatas on values.ProviderID equals item.ProviderID
                                    where values.ClaimStatusID == 1

                                    select values.ClaimFormNum + " ## " + values.CardNumber + " ## " + values.ClaimID + " ## " + item.ProviderName).ToArray();
            ViewBag.lis = ClaimFormNumbers;


            var Service_Name = (from values in db.LUServices
                                select values.ServiceName).ToArray();
            ViewBag.Service_Name = Service_Name;

            ViewBag.diagnosi = diagnosisss;

            //ViewBag.drugs = drugss;

            return View();
        }

       
        public class Claims_Services
        {
            public string ApproveCheck { get; set; }
            public string Service_Name { get; set; }
            public decimal Service_Price { get; set; }
            public decimal Service_Qnt { get; set; }
            public decimal Service_Amt { get; set; }
            public string Refuse_note { get; set; }
            public string claim_id { get; set; }
            public string claimitem_id { get; set; }
            public string form_number { get; set; }
            public string Card_number { get; set; }
            public DateTime Date_Procedure { get; set; }
            public DateTime Date_symptoms { get; set; }
            public string Diagnosis { get; set; }
            public string ICD_Number { get; set; }
            public string Claim_Service { get; set; }

        }
        public JsonResult price_invoice(List<Claims_Services> Claims_Servicess)
        {
            int insertedRecords = 0;
            var arrlis = new ArrayList();

            if (/*Claims_Servicess.Count == 0 ||*/ Claims_Servicess == null)
            {
                insertedRecords = 1;
                arrlis.Add(insertedRecords);
                var ErorMessage = "Claim Services not Exist , You Must Exist at least One Service";
                arrlis.Add(ErorMessage);
                return Json(arrlis, JsonRequestBehavior.AllowGet);

            }
            int claimid_select = int.Parse(Claims_Servicess[0].claim_id);
            var claimonlinedata = db.CLMClaimsONLINEs.FirstOrDefault(d => d.ClaimID == claimid_select);
            int? providerid = claimonlinedata.ProviderID;
            int? providercatid = claimonlinedata.ProviderCatID;
            decimal totalprice = 0;
            if (providercatid == 2)
            {
                foreach (var item in Claims_Servicess)
                {
                    if (item.ApproveCheck == "True")
                    {
                        var service_Nam = item.Service_Name;

                        var re2 = db.PVMedDatas.FirstOrDefault(d => d.MedDrugName.Replace(" ", string.Empty) == (service_Nam.Replace(" ", string.Empty)));
                        if (re2 == null)
                        {
                            var re22 = db.PVMedDatas.FirstOrDefault(d => d.MedDrugName.Replace(" ", string.Empty).Contains(service_Nam.Replace(" ", string.Empty)));
                            re2 = re22;
                        }
                        //var re2 = db.PVMedDatas.FirstOrDefault(d => d.MedDrugName.Replace(" ", string.Empty).Contains(service_Nam.Replace(" ", string.Empty)));
                        var service_price = re2.Price;
                        var service_amt = service_price * item.Service_Qnt;
                        totalprice += service_amt;
                    }
                }
            }
            else
            {

                var reco3 = db.PVContracts
                                 .Where(m => m.ProviderID == providerid)
                                 .OrderByDescending(m => m.StartDate)
                                 .FirstOrDefault();
                int Provider_Contract_ID = reco3.ProviderContractID;

                foreach (var item in Claims_Servicess)
                {
                    if (item.ApproveCheck == "True")
                    {
                        var service_Nam = item.Service_Name;
                        var reco2 = (from valuess in db.PVServices
                                     join itemm in db.PVServicePricings on valuess.ServiceID equals itemm.ServiceID
                                     where (valuess.ServiceName.Replace(" ", string.Empty) == service_Nam.Replace(" ", string.Empty)) && (itemm.ProviderContractID == Provider_Contract_ID)
                                     select itemm).FirstOrDefault();

                        var service_price = reco2.Price;
                        var service_amt = service_price * item.Service_Qnt;
                        totalprice += service_amt;
                    }
                }
            }
            arrlis.Add(insertedRecords);
            arrlis.Add(totalprice);

            return Json(arrlis, JsonRequestBehavior.AllowGet);
        }
        public JsonResult cmc_approve_services(List<Claims_Services> Claims_Servicess)
        {
            int insertedRecords = 0;
            var arrlis = new ArrayList();

            if (/*Claims_Servicess.Count == 0 ||*/ Claims_Servicess == null)
            {
                insertedRecords = 1;
                arrlis.Add(insertedRecords);
                var ErorMessage = "Claim Services not Exist , You Must Exist at least One Service";
                arrlis.Add(ErorMessage);
                return Json(arrlis, JsonRequestBehavior.AllowGet);

            }
            if ((Claims_Servicess[0].form_number == null))
            {
                insertedRecords = 1;
                arrlis.Add(insertedRecords);
                var ErorMessage = "Claim Form Number is not Correct";
                arrlis.Add(ErorMessage);
                return Json(arrlis, JsonRequestBehavior.AllowGet);

            }
            if (Claims_Servicess[0].Card_number == "")
            {
                insertedRecords = 1;
                arrlis.Add(insertedRecords);
                var ErorMessage = "Card Number is not Correct";
                arrlis.Add(ErorMessage);
                return Json(arrlis);
            }
            string Service_Name1 = Claims_Servicess[0].Claim_Service;
            if (Service_Name1 == null || Service_Name1 == "whatever")
            {
                insertedRecords = 1;
                arrlis.Add(insertedRecords);
                var ErorMessage = "Select Claim Service Name is Required";
                arrlis.Add(ErorMessage);
                return Json(arrlis);
            }
            var re1 = db.LUServices.FirstOrDefault(d => d.ServiceName == Service_Name1);
            int claim_service_id = re1.ServiceID;

            DateTime Date_symptoms = Claims_Servicess[0].Date_symptoms;
            DateTime Date_Procedure = Claims_Servicess[0].Date_Procedure;
            string Diagnosis = Claims_Servicess[0].Diagnosis;
            if (Diagnosis == null)
            {
                insertedRecords = 1;
                arrlis.Add(insertedRecords);
                var ErorMessage = "Select Diagnosis Name is Required";
                arrlis.Add(ErorMessage);
                return Json(arrlis);
            }
            string ICD_Number = Claims_Servicess[0].ICD_Number;
            if (ICD_Number == null)
            {
                insertedRecords = 1;
                arrlis.Add(insertedRecords);
                var ErorMessage = "Select ICD Number is Required";
                arrlis.Add(ErorMessage);
                return Json(arrlis);
            }
            int claimid_select = int.Parse(Claims_Servicess[0].claim_id);
            using (var database = new MedInsuranceProEntities())
            {
                var foo = database.CLMClaimsONLINEs.Where(f => f.ClaimID == claimid_select).FirstOrDefault();
                foo.symptomsDate = Date_symptoms;
                foo.ProcedureDate = Date_Procedure;
                foo.ClaimServiceID = claim_service_id;
                foo.Diagnosis = Diagnosis;
                foo.ICDCode = ICD_Number;
                foo.ClaimStatusID = 2;
                foo.UserApproveID = int.Parse(Session["UserID"].ToString());
                database.SaveChanges();
            }

            foreach (var item in Claims_Servicess)
            {
                if (item.Refuse_note == null)
                {
                    item.Refuse_note = "";
                }
                var ClaimItemStatus_ID = 2;
                if (item.ApproveCheck == "True")
                {
                    ClaimItemStatus_ID = 3;
                }
                using (var database = new MedInsuranceProEntities())
                {
                    var service_nam = item.Service_Name;
                    var foo = database.CLMClaimsItemsONLINEs.Where(f => f.ClaimID == claimid_select && f.ServiceName == service_nam).FirstOrDefault();
                    foo.ServicePrice = item.Service_Price;
                    foo.ServiceQnt = item.Service_Qnt;
                    foo.ServiceAmt = item.Service_Amt;
                    foo.UserApproveID = int.Parse(Session["UserID"].ToString());
                    foo.ClaimServiceID = claim_service_id;
                    foo.ClaimItemStatusID = ClaimItemStatus_ID;
                    foo.Refuse_Note = item.Refuse_note;
                    database.SaveChanges();
                }
            }
            arrlis.Add(insertedRecords);
            var ClaimFormNumbers = (from values in db.CLMClaimsONLINEs
                                    join item in db.PVProviderDatas on values.ProviderID equals item.ProviderID
                                    where values.ClaimStatusID == 1

                                    select values.ClaimFormNum + " ## " + values.CardNumber + " ## " + values.ClaimID + " ## " + item.ProviderName).ToArray();
            arrlis.Add(ClaimFormNumbers);

            return Json(arrlis, JsonRequestBehavior.AllowGet);
        }

        public JsonResult ADD_ClaimData(string form_number, string Card_number, string Date_Proceduree,
            string Date_symptom, string Diagnosiss, string ICD_Numberr, string Claim_Service, string Provider_id)
        {

            int insertedRecords = 0;
            var arrlis = new ArrayList();

            int prov_id = int.Parse(Provider_id);
            if ((prov_id == -1) || (form_number == null))
            {
                insertedRecords = 1;
                arrlis.Add(insertedRecords);
                var ErorMessage = "Claim Form Number is not Correct";
                arrlis.Add(ErorMessage);
                return Json(arrlis, JsonRequestBehavior.AllowGet);

            }

            var claim_form_num = long.Parse(form_number);
            var a = db.CLMClaimsONLINEs.FirstOrDefault(n => n.ClaimFormNum == claim_form_num);
            if (a != null)
            {
                insertedRecords = 1;
                arrlis.Add(insertedRecords);
                var ErorMessage = "Claim Form Number is Added Since while";
                arrlis.Add(ErorMessage);

                return Json(arrlis, JsonRequestBehavior.AllowGet);

            }

            else
            {
                //CLMClaim

                //var maxValu = db.CLMClaimsONLINEs.Max(o => o.ClaimID);
                //var resul = db.CLMClaimsONLINEs.First(o => o.ClaimID == maxValu);
                //int calimid = (resul.ClaimID) + 1;

                string card_num = Card_number;
                var datenow = DateTime.Now;
                var cont_num = (
    from item1 in db.ContractMembers
    join item2 in db.Contracts on item1.ContractID equals item2.ContractID into finalGroup0
    from finalItem0 in finalGroup0.DefaultIfEmpty()
    where finalItem0.IsActive == true && finalItem0.StartDate <= datenow && finalItem0.EndDate >= datenow && item1.CardNumber == card_num
    select new { finalItem0.ContractNumber, item1.ContractMemberID, finalItem0.ContractID, item1.PlanTypeID }).ToList();
                if (cont_num.Count == 0)
                {
                    insertedRecords = 1;
                    arrlis.Add(insertedRecords);
                    var ErorMessage = "Card Number is not Correct";
                    arrlis.Add(ErorMessage);
                    return Json(arrlis);
                }
                string contract_num = cont_num[0].ContractNumber;
                int Contract_Member_ID = cont_num[0].ContractMemberID;
                int Contract_ID = cont_num[0].ContractID;
                int plan_type_id = cont_num[0].PlanTypeID;

                string Service_Name1 = Claim_Service;
                if (Service_Name1 == null)
                {
                    insertedRecords = 1;
                    arrlis.Add(insertedRecords);
                    var ErorMessage = "Select Claim Service Name is Required";
                    arrlis.Add(ErorMessage);
                    return Json(arrlis);
                }
                var re1 = db.LUServices.FirstOrDefault(d => d.ServiceName == Service_Name1);
                int claim_service_id = re1.ServiceID;



                string Claim_form_number = form_number;
                DateTime Date_symptoms = DateTime.Parse(Date_symptom);
                DateTime Date_Procedure = DateTime.Parse(Date_Proceduree);
                string Diagnosis = Diagnosiss;
                if (Diagnosis == null)
                {
                    insertedRecords = 1;
                    arrlis.Add(insertedRecords);
                    var ErorMessage = "Select Diagnosis Name is Required";
                    arrlis.Add(ErorMessage);
                    return Json(arrlis);
                }
                string ICD_Number = ICD_Numberr;
                CLMClaimsONLINE newdataa = new CLMClaimsONLINE()
                {

                    //ClaimID = calimid,
                    ClaimFormNum = long.Parse(Claim_form_number),
                    ContractMemberID = Contract_Member_ID,
                    symptomsDate = Date_symptoms,
                    ProcedureDate = Date_Procedure,
                    ClaimServiceID = claim_service_id,
                    Diagnosis = Diagnosis,
                    ICDCode = ICD_Number,
                    SecondDiagnosis = "",
                    SecondICDCode = "",
                    ThirdDiagnosis = "",
                    ThirdICDCode = "",
                    ClaimStatusID = 1,
                    // will edit 
                    UserAddID = int.Parse(Session["UserID"].ToString()),
                    UserApproveID = 0,
                    ContractNum = contract_num,
                    CardNumber = card_num,
                    ProviderID = 0,
                    ProviderCatID = 0,
                };
                db.CLMClaimsONLINEs.Add(newdataa);
                db.SaveChanges();
            }
            //else
            //{
            //    insertedRecords = 2;
            //    arrlis.Add(insertedRecords);

            //}
            insertedRecords = 0;
            arrlis.Add(insertedRecords);

            using (var database = new MedInsuranceProEntities())
            {
                long ClaimFormNum = long.Parse(form_number);
                var foo = database.MemberClaimForms.Where(f => f.ClaimFormNumber == ClaimFormNum).FirstOrDefault();
                foo.MemberClaimformStatus = 2;
                database.SaveChanges();
            }

            var ClaimFormNumbers = (from values in db.MemberClaimForms
                                    where values.MemberClaimformStatus == 1
                                    select values.ClaimFormNumber).ToArray();

            arrlis.Add(ClaimFormNumbers);

            return Json(arrlis, JsonRequestBehavior.AllowGet);
        }

        public JsonResult SendToDataEntry(string id)
        {
            try
            {
                using (var database = new MedInsuranceProEntities())
                {
                    long ClaimFormNum = long.Parse(id);
                    var foo = database.MemberClaimForms.Where(f => f.ClaimFormNumber == ClaimFormNum).FirstOrDefault();
                    foo.MemberClaimformStatus = 2;
                    database.SaveChanges();
                }
            }
            catch
            {
                return Json("1", JsonRequestBehavior.AllowGet);

            }

            var ClaimFormNumbers = (from values in db.MemberClaimForms
                                    where values.MemberClaimformStatus == 1
                                    select values.ClaimFormNumber).ToArray();

            return Json(ClaimFormNumbers, JsonRequestBehavior.AllowGet);
        }

        public static List<string> diagnosisss = new List<string>();
        public static List<string> drugss = new List<string>();

        public JsonResult List_Drugs_List_diagnosis()
        {
            var diagnosis = (from values in db.ICDCodes
                             select values.ShortName).ToList();
            diagnosisss = diagnosis;

            var drug = (from values in db.PVMedDatas
                        select values.MedDrugName).ToList();
            drugss = drug;
            return Json("", JsonRequestBehavior.AllowGet);
        }

        public JsonResult select_diagnosis(string id)
        {
            if (diagnosisss.Count == 0)
            {
                var diagnosis = (from values in db.ICDCodes
                                 select values.ShortName).ToList();
                diagnosisss = diagnosis;
            }

            var dat = diagnosisss.Where(s => s.ToLower().Contains(id)).ToList();
            return Json(dat, JsonRequestBehavior.AllowGet);
        }
        [HttpGet]
        public ActionResult DataEntry_ClaimFormToAdd()
        {
            var ClaimFormNumbers = (from values in db.MemberClaimForms
                                    where values.MemberClaimformStatus == 2
                                    select values.ClaimFormNumber).ToArray();

            ViewBag.lis = ClaimFormNumbers;


            //if ((Session["UserID"] == null) || (int.Parse(Session["Claim_Form"].ToString()) == 0))
            //{
            //    return RedirectToAction("login", "Home");
            //}


            var Service_Name = (from values in db.LUServices
                                select values.ServiceName).ToArray();
            ViewBag.Service_Name = Service_Name;


            //var re3 = db.PVContracts
            //           .Where(m => m.ProviderID == providerid)
            //           .OrderByDescending(m => m.ProviderContractID)
            //           .FirstOrDefault();
            //int Provider_Contract_ID = re3.ProviderContractID;

            //var provider_category = (from values in db.PVServicePricings
            //                         join item1 in db.PVServices on values.ServiceID equals item1.ServiceID
            //                         where values.ProviderContractID == Provider_Contract_ID
            //                         select item1.ServiceName).ToArray();
            //var re4 = db.PVServicePricings
            //            .Where(d => d.ProviderContractID == Provider_Contract_ID && d.ProviderID == providerid)
            //            .OrderByDescending(m => m.ServicePricingID)
            //            .FirstOrDefault();
            //List<string> provider_category = new List<string>();

            //provider_category =new Claims.ClaimFormController().get_provider_category(providerid);
            //provider_category = _ClaimFormController.get_provider_category(providerid);
            //ViewBag.provider_categor = provider_category;

            //var diagnosis = (from values in db.ICDCodes
            //                 select values.ShortName).ToArray();
            ViewBag.diagnosi = diagnosisss;

            //var drug = (from values in db.PVMedDatas
            //            select values.MedDrugName).ToArray();
            ViewBag.drugs = drugss;

            return View();
        }


        public JsonResult Get_Provider_Name(string id)
        {
            var providername = "";
            int? providerid = 0;
            var arrlist = new ArrayList();
            try
            {
                long claimformnumber = long.Parse(id);
                var providernam = (from values in db.PVProviderClaimBooks
                                   join o in db.PVProviderDatas on values.ProviderID equals o.ProviderID
                                   where values.FromSerial <= claimformnumber && values.ToSerial >= claimformnumber
                                   select new { o.ProviderName, o.ProviderID }).ToList();
                if (providernam.Count == 0)
                {
                    var approvalselcted = db.CLMApprovals.FirstOrDefault(n => n.ApprovalCode == claimformnumber && n.ApprovalStatus == 1);
                    providerid = approvalselcted.ProviderID;

                    var providerselected = db.PVProviderDatas.FirstOrDefault(n => n.ProviderID == providerid);
                    providername = providerselected.ProviderName;
                    arrlist.Add(providername);
                    //var a = db.CLMClaims.FirstOrDefault(n => /*n.RecBatchID == recbat &&*/ n.ClaimFormNum == claimformnumber);
                    //if (a != null)
                    //{
                    //    arrlist.Add(-1);
                    //    int aa = 2;
                    //    arrlist.Add(aa);
                    //}
                    //else
                    //{
                    arrlist.Add(providerid);
                    int aa = 0;
                    arrlist.Add(aa);
                    //}
                    arrlist.Add(providerid);
                    arrlist.Add(0);

                }
                else
                {
                    providername = providernam[0].ProviderName;
                    //providerid = providernam[0].ProviderID;

                    arrlist.Add(providername);

                    //var ea = db.CLMClaims.Where(n => n.ClaimFormNum == claimformnumber).Count();
                    //var ea1 = db.CLMClaimsONLINEs.Where(n => n.ClaimFormNum == claimformnumber).Count();
                    //    //if (ea > 5)
                    //    if (ea > 0|| ea1 > 0)
                    //    {
                    //        arrlist.Add(-1);
                    //        int aa = 3;
                    //        arrlist.Add(aa);
                    //    }
                    //    else
                    //    {
                    arrlist.Add(providerid);
                    int aa = 0;
                    arrlist.Add(aa);
                    //}
                }
            }

            catch
            {
                providername = "";
                providerid = -1;
                arrlist.Add(providername);
                arrlist.Add(providerid);
                int aa = 0;
                arrlist.Add(aa);

            }

            return Json(arrlist, JsonRequestBehavior.AllowGet);
        }

        //public static List<string> servicess_data = new List<string>();



        [HttpGet]
        public ActionResult cmc_Claimform_stop()
        {

            var res = (from values in db.MemberClaimForms
                       where values.MemberClaimformStatus == 1
                       select values.ClaimFormNumber).Distinct().ToList();

            ViewBag.claimform = res;

            return View();
        }
        public JsonResult stop_ClaimForm(string claim_form)
        {
            long claimform_num = long.Parse(claim_form);
            var memberclaimdata = db.MemberClaimForms.Where(d => d.ClaimFormNumber == claimform_num).ToList();
            foreach (var item in memberclaimdata)
            {

                using (var database = new MedInsuranceProEntities())
                {
                    var MemberClaimform_ID = item.MemberClaimformID;
                    var foo = database.MemberClaimForms.Where(f => f.MemberClaimformID == MemberClaimform_ID).FirstOrDefault();
                    foo.MemberClaimformStatus = 2;
                    database.SaveChanges();
                }
            }

            return Json("", JsonRequestBehavior.AllowGet);

        }


        [HttpGet]
        public ActionResult cmc_Claimform_search()
        {
            return View();
        }

        [HttpGet]
        public ActionResult Print_CancelAPProval()
        {
            if ((Session["UserID"] == null) || ((int.Parse(Session["Medical_Services"].ToString()) == 0)&&(int.Parse(Session["Doctor_ClaimForm"].ToString()) == 0)))
            {
                return RedirectToAction("login", "ClaimForm");
            }
            return View();
        }


        public JsonResult GetApprovalsDateRange(string id)
        {
            try
            {
                String[] spearator = { "-" };
                Int32 count = 2;
                String[] strlist = id.Split(spearator, count,
                       StringSplitOptions.RemoveEmptyEntries);
                string StartDateSTR = strlist[0].Replace(" ", string.Empty);
                string EndDateSTR = strlist[1].Replace(" ", string.Empty);

                DateTime StartDate = DateTime.ParseExact(StartDateSTR, "yyyy/MM/dd", CultureInfo.InvariantCulture);
                DateTime EndDate = DateTime.ParseExact(EndDateSTR, "yyyy/MM/dd", CultureInfo.InvariantCulture);

                string Providername = Session["Providername"].ToString();
                string branchName = Session["branchName"].ToString();

                var res = (from values in db.CLMClaims
                           join item in db.ContractMembers on values.ContractMemberID equals item.ContractMemberID
                           where values.ProviderName == Providername && values.BranchName == branchName && values.ProcedureDate >= StartDate && values.ProcedureDate <= EndDate
                           select new { values.ClaimID, values.ClaimFormNum, item.CardNumber, item.MemberName, values.ProcedureDate, values.TClaimAmt, values.TDeductAmt }).ToList();

                return Json(res, JsonRequestBehavior.AllowGet);
            }
            catch
            {
                return Json("Falsee", JsonRequestBehavior.AllowGet);

            }
        }



        public JsonResult Get_ClaimServices(string CLaimID)
        {

            var ClaimIDD = int.Parse(CLaimID);
                var res = (from values in db.CLMClaimItems
                           where values.ClaimID == ClaimIDD
                           select new { values.ServiceName, values.ServiceUnitAmt, values.ServiceQnt, values.ClaimedAmt, values.DeductAmt, values.RejectNotes }).ToList();

                return Json(res, JsonRequestBehavior.AllowGet);
        }

        public JsonResult Delete_Invoice(string CLaimID,string ReasonText)
        {

            var ClaimIDD = int.Parse(CLaimID);

            var clmclaimData = db.CLMClaims.FirstOrDefault(d => d.ClaimID == ClaimIDD);
            var recbatch_ID = clmclaimData.RecBatchID;
            var ClaimForm_Num = clmclaimData.ClaimFormNum;
            var ContractMember_ID = clmclaimData.ContractMemberID;
            var Procedure_Date = clmclaimData.ProcedureDate;
            var Provider_ID = clmclaimData.ProviderID;
            var Branch_Name = clmclaimData.BranchName;
            var TClaim_Amt = clmclaimData.TClaimAmt;

            if(recbatch_ID!=0)
            {
                var CLMRecBatch = db.CLMRecBatches.FirstOrDefault(d => d.RecBatchID == recbatch_ID);
                var batchnumber = CLMRecBatch.BatchNumber;
                var RecDate = CLMRecBatch.RecDate;
                var arrliss = new ArrayList();
                arrliss.Add(batchnumber);
                arrliss.Add(RecDate);
                return Json(arrliss, JsonRequestBehavior.AllowGet);
            }

            var result = (from item1 in db.CLMClaimsONLINEs
            join item2 in db.CLMClaimsItemsONLINEs on item1.ClaimID equals item2.ClaimID
            where item1.ClaimFormNum == ClaimForm_Num && item1.ContractMemberID == ContractMember_ID
            && item1.ProcedureDate == Procedure_Date && item1.ClaimStatusID == 3 && item2.ClaimItemStatusID == 3
            && item1.ProviderID == Provider_ID && item1.BranchName == Branch_Name
            group new {item2} by new {item2.ClaimID} into cvrg
            select new
            {
                cvrg.Key.ClaimID,
                Nb_V1 = cvrg.Select(cvr => cvr.item2.ServiceAmt).Sum()
            }).ToList();

            var claimOnline = result.Where(a => a.Nb_V1 == TClaim_Amt).OrderByDescending(a => a.ClaimID).FirstOrDefault();
            if (claimOnline ==null)
            {
                return Json("Error", JsonRequestBehavior.AllowGet);
            }
            var claimIDOnline = claimOnline.ClaimID;

            CLMClaim clmclaimselect = db.CLMClaims.FirstOrDefault(a=>a.ClaimID== ClaimIDD);

            int UserID = int.Parse(Session["UserID"].ToString());
            int BranchIDD = int.Parse(Session["BranchID"].ToString());
            var claimIDD = clmclaimselect.ClaimID;
            var ClaimFormNumm = long.Parse(clmclaimselect.ClaimFormNum.ToString());
            var ContractMemberIDD = int.Parse(clmclaimselect.ContractMemberID.ToString());
            var CardNumberr = clmclaimselect.CardNumber;
            var ProviderIDD = int.Parse(clmclaimselect.ProviderID.ToString());
            string Providername = clmclaimselect.ProviderName;
            var BranchNamee = clmclaimselect.BranchName;
            var CancelReasonDatee = DateTime.Now;
            var CancelReasonMessage = ReasonText;
            CancelReasonData newdataa = new CancelReasonData()
            {

                ClaimID = claimIDD,
                ClaimFormNum = ClaimFormNumm,
                ContractMemberID = ContractMemberIDD,
                UserID = UserID,
                CardNumber = CardNumberr,
                ProviderID = ProviderIDD,
                ProviderName = Providername,
                BranchName = BranchNamee,
                BranchID = BranchIDD,
                CancelReasonDate = CancelReasonDatee,
                CancelReasonMessage = CancelReasonMessage,

            };
            db.CancelReasonDatas.Add(newdataa);
            db.SaveChanges();

            db.CLMClaims.Remove(clmclaimselect);
            db.SaveChanges();

            IEnumerable<CLMClaimItem> clmclaimitemsselect = db.CLMClaimItems.Where(c =>c.ClaimID == ClaimIDD).ToList();
            db.CLMClaimItems.RemoveRange(clmclaimitemsselect);
            db.SaveChanges();


            var ApprovalCodeExistInClmApprovalOnlines = db.ClmApprovalOnlines.FirstOrDefault(d => d.ApprovalCode == ClaimForm_Num);
            var ClaimformExistInClmApprovalOnlines = db.ClmApprovalOnlines.FirstOrDefault(d => d.ClaimFormNum == ClaimForm_Num);
            if (ApprovalCodeExistInClmApprovalOnlines != null|| ClaimformExistInClmApprovalOnlines!=null)
            {
                using (var database = new MedInsuranceProEntities())
                {
                    var foo = database.CLMClaimsONLINEs.Where(f => f.ClaimID == claimIDOnline).FirstOrDefault();
                    foo.ClaimStatusID = 2;
                    foo.UserApproveID = 0;
                    foo.ProviderID = 0;
                    foo.BranchID = 0;
                    foo.BranchName = "";
                    database.SaveChanges();
                }

                return Json("Done", JsonRequestBehavior.AllowGet);
            }

            CLMClaimsONLINE clmclaimsonlineelect = db.CLMClaimsONLINEs.FirstOrDefault(a => a.ClaimID == claimIDOnline);
                db.CLMClaimsONLINEs.Remove(clmclaimsonlineelect);
                db.SaveChanges();

                IEnumerable<CLMClaimsItemsONLINE> clmclaimitemsonlineselect = db.CLMClaimsItemsONLINEs.Where(c => c.ClaimID == claimIDOnline).ToList();
                db.CLMClaimsItemsONLINEs.RemoveRange(clmclaimitemsonlineselect);
                db.SaveChanges();
           
            return Json("Done", JsonRequestBehavior.AllowGet);
        }

        //public JsonResult PrintAll_DateRange(string[] Claimids_print)
        //{


        //    foreach (var claimid in Claimids_print)
        //    {
        //        GenerateReport_PrintInvoice("PDF", claimid);

        //    }

        //    return Json("", JsonRequestBehavior.AllowGet);
        //}


        [HttpGet]
        public ActionResult ShowBatchData(string Batch_Numberes)
        {


            var recbatch = db.CLMRecBatches.FirstOrDefault(d => d.BatchNumber == Batch_Numberes);
            if (recbatch != null)
            {
                var Provider_ID = int.Parse(Session["providerid"].ToString());
                var providerid = recbatch.ProviderID;
                if(Provider_ID== providerid)
                {
                    ViewBag.BatchNumberr = Batch_Numberes;

                }
                else
                {
                    ViewBag.BatchNumberr = "";

                }
            }
            return View();
        }

        public class OrderList
        {

            public int ClaimID { get; set; }
            public long? ClaimFormNum { get; set; }
            public string CardNumber { get; set; }
            public string MemberName { get; set; }
            public string ProcedureDate { get; set; }
            public decimal? TClaimAmt { get; set; }
            public decimal? TDeductAmt { get; set; }
            public decimal? TDiscountAmt { get; set; }
            public decimal? NetClaimedAmt { get; set; }
            public decimal? TRequestedAmt { get; set; }
            public decimal? TDifferenceAmt { get; set; }
            public string BranchName { get; set; }

            public List<OrderListDetail> OrderListDetails { get; set; }

        }
        public class OrderListDetail
        {
            public int ClaimID { get; set; }
            public int ClaimitemID { get; set; }
            public string ServiceName { get; set; }
            public decimal ServiceQnt { get; set; }
            public decimal ServiceUnitAmt { get; set; }
            public decimal ServiceAmt { get; set; }
            public decimal ServiceDeductAmt { get; set; }
            public decimal? ServiceDiscountAmt { get; set; }
            public decimal ServiceDueAmt { get; set; }
            public decimal? ServiceRequestedAmt { get; set; }
            public decimal? ServiceDifferenceAmt { get; set; }
            public string ServiceNote { get; set; }
        }
        public JsonResult GetBatchList(string Batch_Number)
        {
            var Approvals = (
            from item3 in db.CLMRecBatches
            join item1 in db.CLMClaims on item3.RecBatchID equals item1.RecBatchID
            join item2 in db.CLMClaimItems on item1.ClaimID equals item2.ClaimID
            join item4 in db.ContractMembers on item1.ContractMemberID equals item4.ContractMemberID

            where item3.BatchNumber == Batch_Number
            orderby item1.ClaimID
            select new
            {
                item1.ClaimFormNum,
                item1.CardNumber,
                item4.MemberName,
                item1.ProcedureDate,
                item1.TClaimAmt,
                item1.TDeductAmt,
                item1.TDiscountAmt,
                item1.NetClaimedAmt,
                item1.TRequestedAmt,
                item1.TDifferenceAmt,
                item1.ClaimID,
                item2.ClaimItemID,
                Address = item1 != null ? item1.BranchName : "",
                ServiceName = item2 != null ? item2.ServiceName : "",
                ServiceQnt = item2 != null ? item2.ServiceQnt : 0,
                ServiceUnitAmt = item2 != null ? item2.ServiceUnitAmt : 0,
                ServiceAmt = item2 != null ? item2.ClaimedAmt : 0,
                ServiceDeductAmt = item2 != null ? item2.DeductAmt : 0,
                ServicediscAmt = item2 != null ? item2.DiscountAmt : 0,
                ServiceDueAmt = item2 != null ? item2.DueAmt : 0,
                ServicerequAmt = item2 != null ? item2.RequestedAmt : 0,
                ServicediffAmt = item2 != null ? item2.DifferenceAmt : 0,
                ServiceNote = item2 != null ? item2.RejectNotes : ""
            }).ToList();
            var a = Approvals.GroupBy(c => new { c.ClaimID, c.ProcedureDate })
               .Select(c => new OrderList()
               {
                   ClaimID = c.Key.ClaimID,
                   ClaimFormNum = c.FirstOrDefault().ClaimFormNum,
                   CardNumber = c.FirstOrDefault().CardNumber,
                   MemberName = c.FirstOrDefault().MemberName,
                   ProcedureDate = DateTime.Parse(c.Key.ProcedureDate.ToString()).ToString("yyyy-MM-dd"),
                   TClaimAmt = c.FirstOrDefault().TClaimAmt,
                   TDeductAmt = c.FirstOrDefault().TDeductAmt /*Math.Round((decimal.Parse( c.FirstOrDefault().TDeductAmt .ToString())/ decimal.Parse(c.FirstOrDefault().TClaimAmt.ToString())) *(decimal.Parse(c.FirstOrDefault().TRequestedAmt.ToString()) + decimal.Parse(c.FirstOrDefault().TDiscountAmt.ToString()) + decimal.Parse(c.FirstOrDefault().TDeductAmt.ToString())), 2)*/,
                   TDiscountAmt = c.FirstOrDefault().TDiscountAmt /*Math.Round((decimal.Parse(c.FirstOrDefault().TDiscountAmt.ToString()) / decimal.Parse(c.FirstOrDefault().TClaimAmt.ToString())) * (decimal.Parse(c.FirstOrDefault().TRequestedAmt.ToString()) + decimal.Parse(c.FirstOrDefault().TDiscountAmt.ToString()) + decimal.Parse(c.FirstOrDefault().TDeductAmt.ToString())), 2)*/,
                   NetClaimedAmt = c.FirstOrDefault().NetClaimedAmt,
                   TRequestedAmt = c.FirstOrDefault().TClaimAmt + c.FirstOrDefault().TDifferenceAmt /*c.FirstOrDefault().TRequestedAmt*//*+ Math.Round((decimal.Parse(c.FirstOrDefault().TDeductAmt.ToString()) / decimal.Parse(c.FirstOrDefault().TClaimAmt.ToString())) * (decimal.Parse(c.FirstOrDefault().TRequestedAmt.ToString()) + decimal.Parse(c.FirstOrDefault().TDiscountAmt.ToString()) + decimal.Parse(c.FirstOrDefault().TDeductAmt.ToString())), 2)+ Math.Round((decimal.Parse(c.FirstOrDefault().TDiscountAmt.ToString()) / decimal.Parse(c.FirstOrDefault().TClaimAmt.ToString())) * (decimal.Parse(c.FirstOrDefault().TRequestedAmt.ToString()) + decimal.Parse(c.FirstOrDefault().TDiscountAmt.ToString()) + decimal.Parse(c.FirstOrDefault().TDeductAmt.ToString())), 2)*/,
                   TDifferenceAmt = c.FirstOrDefault().TDifferenceAmt,
                   BranchName = c.FirstOrDefault().Address,
                   OrderListDetails = c.Select(d => new OrderListDetail()
                   {
                       ClaimID = d.ClaimID,
                       ClaimitemID = d.ClaimItemID,
                       ServiceName = d.ServiceName,
                       ServiceQnt = d.ServiceQnt,
                       ServiceUnitAmt = d.ServiceUnitAmt,
                       ServiceAmt = d.ServiceAmt,
                       ServiceDeductAmt = d.ServiceDeductAmt/*Math.Round((d.ServiceDeductAmt/ d.ServiceAmt) *(d.ServicerequAmt + d.ServiceDeductAmt + d.ServicediscAmt),2)*/,
                       ServiceDiscountAmt = d.ServicediscAmt/* Math.Round((d.ServicediscAmt/ d.ServiceAmt) *(d.ServicerequAmt + d.ServiceDeductAmt + d.ServicediscAmt ),2)*/,
                       ServiceDueAmt = d.ServiceDueAmt,
                       ServiceRequestedAmt = d.ServiceAmt + d.ServicediffAmt /*d.ServicerequAmt+ Math.Round((d.ServiceDeductAmt / d.ServiceAmt) * (d.ServicerequAmt + d.ServiceDeductAmt + d.ServicediscAmt), 2)+ Math.Round((d.ServicediscAmt / d.ServiceAmt) * (d.ServicerequAmt + d.ServiceDeductAmt + d.ServicediscAmt ), 2)*/,
                       ServiceDifferenceAmt = d.ServicediffAmt,
                       ServiceNote = d.ServiceNote
                   }).ToList()
               }).ToList();

            return Json(a, JsonRequestBehavior.AllowGet);

            //return a;
        }


        [HttpGet]
        public ActionResult MemberSmartCardRequests(string Member_Request, string claim_formnumber)
        {

            ViewBag.Member_Requestt = Member_Request;
            ViewBag.claim_formnumberqq = claim_formnumber;

            if ((Session["UserID"] == null) || ((int.Parse(Session["ClaimForm_Approved"].ToString()) == 0)&&(int.Parse(Session["Approval"].ToString()) == 0)&&(int.Parse(Session["Doctor_ClaimForm"].ToString()) == 0)))
            {
                return RedirectToAction("login", "Home");
            }

           
            var datetimeActive = DateTime.Now.AddDays(-7);

            int providerid1 = int.Parse(Session["providerid"].ToString());
            int branchid = int.Parse(Session["BranchID"].ToString());
            var ClaimFormNumbers = (from values in db.SmartCardRequestDatas
                                    where values.SmartCardRequestStatus == 1 && values.ProviderID == providerid1 && values.BranchID == branchid &&values.EntryDate>= datetimeActive
                                    select values.CardNumber + " ## " + values.EntryDate.ToString() + " ## " + values.SmartCardRequestID).ToArray();
            ViewBag.lis = ClaimFormNumbers;

            var provCatId = db.PVProviderDatas.FirstOrDefault(d => d.ProviderID == providerid1).ProviderCatID;
            ViewBag.providerCatId = provCatId;


            //var ProviderID = int.Parse(Session["providerid"].ToString());
            //var providercatID = db.PVProviderDatas.FirstOrDefault(d => d.ProviderID == ProviderID).ProviderCatID;

            if (provCatId == 5)
            {
                var DatProcedure = DateTime.Now;
                var re3 = db.PVContracts
                                      .Where(m => m.ProviderID == providerid1 && m.StartDate <= DatProcedure && m.EndDate >= DatProcedure)
                                      .OrderByDescending(m => m.ProviderContractID)
                                      .FirstOrDefault();
                if (re3 == null)
                {
                    var re32 = db.PVContracts
                            .Where(m => m.ProviderID == providerid1 && m.IsActive == true)
                            .OrderByDescending(m => m.ProviderContractID)
                            .FirstOrDefault();
                    re3 = re32;
                }

                int Provider_Contract_ID = re3.ProviderContractID;
                var provider_Services = (from values in db.PVServicePricings
                                         join item1 in db.PVServices on values.ServiceID equals item1.ServiceID
                                         where values.ProviderContractID == Provider_Contract_ID
                                         select new { item1.ServiceName, values.Price }).FirstOrDefault();
                ViewBag.providerServices = provider_Services;

            }
            else
            {
                ViewBag.providerServices = null;

            }

            return View();
        }

        public JsonResult AddRealtimeApprovalSmartCard(string Card_Number, string Provider_ID, string Branch_ID)
        {
            var Provider_ID1 = int.Parse(Provider_ID);
            var Branch_ID1 = int.Parse(Branch_ID);
            var datetimeActive = DateTime.Now.AddDays(-7);
            var ClaimFormNumbers = (from values in db.SmartCardRequestDatas
                                    where values.SmartCardRequestStatus == 1 && values.CardNumber == Card_Number && values.ProviderID == Provider_ID1 && values.BranchID == Branch_ID1 && values.EntryDate >= datetimeActive
                                    orderby values.SmartCardRequestID descending

                                    select values.CardNumber + " ## " + values.EntryDate.ToString() + " ## " + values.SmartCardRequestID).FirstOrDefault();


            return Json(ClaimFormNumbers, JsonRequestBehavior.AllowGet);
        }

        public JsonResult CheckApprovalExist(string Card_Number)
        {
            int providerid = int.Parse(Session["providerid"].ToString());
            var providercatID = db.PVProviderDatas.FirstOrDefault(d => d.ProviderID == providerid).ProviderCatID;
            var datetimeActive = DateTime.Now.AddDays(-7);

            var claimid = 0;
            if (providercatID == 2)
            {
                var claimitems_Active = (from values in db.CLMClaimsONLINEs
                                         join item1 in db.CLMClaimsItemsONLINEs on values.ClaimID equals item1.ClaimID
                                         where values.ProviderCatID == providercatID && values.CardNumber == Card_Number && values.ProviderID == 0 && values.BranchID == 0 && values.ProcedureDate >= datetimeActive && values.ClaimStatusID == 2 && item1.ClaimItemStatusID == 3
                                         select new
                                         {
                                             values.ClaimID
                                         }).ToList();

                if (claimitems_Active.Count == 0)
                {
                     claimid = 0;
                }
                else
                {
                    claimid = 1;
                }

            }
            else
            {
                var datenow = DateTime.Now;
                var claimitems_Active = (from values in db.CLMClaimsONLINEs
                                         join item1 in db.CLMClaimsItemsONLINEs on values.ClaimID equals item1.ClaimID
                                         join item2 in db.PVServices on item1.ServiceName.Replace(" ", string.Empty) equals item2.ServiceName.Replace(" ", string.Empty)
                                         join item3 in db.PVServicePricings on item2.ServiceID equals item3.ServiceID
                                         join item4 in db.PVContracts on item3.ProviderContractID equals item4.ProviderContractID
                                         where values.ProviderCatID == providercatID && values.CardNumber == Card_Number && values.ProviderID == 0 && values.BranchID == 0 && values.ProcedureDate >= datetimeActive && values.ClaimStatusID == 2 && item1.ClaimItemStatusID == 3 && item4.StartDate <= datenow && item4.EndDate >= datenow && item4.ProviderID == providerid
                                         select new { values.ClaimFormNum, item1.ServiceName, item1.ServiceQnt, item3.Price, ServiceAmt = item1.ServiceQnt * item3.Price, RequestedPrice = item1.ServiceQnt * item3.Price, item1.Refuse_Note, item1.ClaimItemID, values.ClaimID }).OrderBy(d => d.ClaimItemID).ToList();

                if (claimitems_Active.Count == 0)
                {
                    claimid = 0;
                }
                else
                {
                    claimid = 1;
                }
            }
            return Json(claimid, JsonRequestBehavior.AllowGet);
        }
        public JsonResult updatemembersmartRequest(string Card_Number, string Smart_CardRequestID)
        {
            try
            {

                if (Smart_CardRequestID == "")
                {
                    int providerid1 = int.Parse(Session["providerid"].ToString());
                    int branchid = int.Parse(Session["BranchID"].ToString());
                    using (var database = new MedInsuranceProEntities())
                    {
                        var foo = database.SmartCardRequestDatas.Where(f => f.CardNumber == Card_Number && f.ProviderID== providerid1&& f.BranchID== branchid).OrderByDescending(f=>f.SmartCardRequestID).FirstOrDefault();
                        if (foo != null)
                        {
                            foo.SmartCardRequestStatus = 2;
                            database.SaveChanges();
                        }
                    }
                }
                else
                {
                    int Smart_CardRequestIDd = int.Parse(Smart_CardRequestID);
                    using (var database = new MedInsuranceProEntities())
                    {
                        var foo = database.SmartCardRequestDatas.Where(f => f.SmartCardRequestID == Smart_CardRequestIDd).FirstOrDefault();
                        if (foo != null)
                        {
                            foo.SmartCardRequestStatus = 2;
                            database.SaveChanges();
                        }
                    }

                }


                return Json("", JsonRequestBehavior.AllowGet);

            }
            catch
            {
                return Json("1", JsonRequestBehavior.AllowGet);

            }


        }


        public ActionResult Claimsform()
        {


            int providerid = int.Parse(Session["providerid"].ToString());
            //int Login_enterid = int.Parse(Session["Loginenterid"].ToString());
            //ViewBag.Login_enter_id = Login_enterid;

            var Provider_address = (from item1 in db.PVProviderDatas
                                    join item2 in db.PVBranchDatas on item1.ProviderID equals item2.ProviderID into finalGroup0
                                    from finalItem0 in finalGroup0.DefaultIfEmpty()
                                    where item1.ProviderID == providerid
                                    select new { finalItem0.Address, item1.ProviderAddress }).ToList();

            var Service_Name = (from values in db.LUServices
                                select values.ServiceName).ToArray();
            ViewBag.Service_Name = Service_Name;

            List<string> addr = new List<string>();
            foreach (var item in Provider_address)
            {
                if (item.Address == null)
                {
                    addr.Add(item.ProviderAddress);
                }
                else
                {
                    addr.Add(item.Address);

                }
            }

            ViewBag.lis = addr;

            var rec = db.PVProviderDatas.FirstOrDefault(d => d.ProviderID == providerid);
            if (rec != null)
            {
                var provider_name = rec.ProviderName;
                ViewBag.scn = provider_name;
            }
            else
            {
                ViewBag.scn = "";

            }

            List<string> provider_category = new List<string>();
            provider_category = get_provider_category(providerid);
            ViewBag.provider_categor = provider_category;

            var diagnosis = (from values in db.ICDCodes
                             select values.ShortName).ToArray();
            ViewBag.diagnosi = diagnosis;

            var drug = (from values in db.PVMedDatas
                        select values.MedDrugName).ToArray();
            ViewBag.drugs = drug;

            return View();
        }

//        public JsonResult Get_Member_info(string id)
//        {

//            var result1 = (
//  from item1 in db.ContractMembers
//  join item2 in db.Contracts on item1.ContractID equals item2.ContractID into finalGroup0
//  from finalItem0 in finalGroup0.DefaultIfEmpty()
//  join item22 in db.CustomerDatas on finalItem0.CustomerID equals item22.CustomerID into finalGroup00
//  from finalItem00 in finalGroup00.DefaultIfEmpty()
//  join item3 in db.MemberActivityStatusDatas on item1.MemberActivityStatusID equals item3.MemberActivityStatusID into finalGroup1
//  from finalItem1 in finalGroup1.DefaultIfEmpty()
//  join item4 in db.PlanTypeDatas on item1.PlanTypeID equals item4.PlanTypeID into finalGroup11
//  from finalItem11 in finalGroup11.DefaultIfEmpty()

//  where finalItem0.IsActive == true && item1.CardNumber == id
//  select new
//  {
//      item1.MemberName,
//      item1.CardNumber,
//      finalItem00.CustomerName,
//      finalItem1.MemberActivityStatusName,
//      item1.SexType,
//      item1.ActiveDate,
//      item1.DeActiveDate,
//      item1.MemberAge,
//      item1.MemberBirthDate,
//      finalItem0.ContractNumber,
//      finalItem11.PlanTypeName

//  }).ToList();

//            var arlist = new ArrayList();
//            if (result1.Count != 0)
//            {
//                arlist.Add(result1[0].MemberName);
//                arlist.Add(result1[0].CustomerName);
//                arlist.Add(result1[0].CardNumber);
//                arlist.Add(result1[0].ContractNumber);
//                arlist.Add(result1[0].PlanTypeName);
//                arlist.Add(result1[0].ActiveDate);
//                arlist.Add(result1[0].MemberActivityStatusName);
//                arlist.Add(result1[0].DeActiveDate);
//                arlist.Add(result1[0].MemberBirthDate);
//                arlist.Add(result1[0].MemberAge);
//                arlist.Add(result1[0].SexType);
//            }
//            return Json(arlist, JsonRequestBehavior.AllowGet);
//        }

//        public JsonResult Get_Member_util(string id, string name)
//        {


//            int insertedRecords = 0;
//            var arrlis = new ArrayList();

//            var cont_num = (
//from item1 in db.ContractMembers
//join item2 in db.Contracts on item1.ContractID equals item2.ContractID into finalGroup0
//from finalItem0 in finalGroup0.DefaultIfEmpty()
//where finalItem0.IsActive == true && item1.CardNumber == name
//select finalItem0.ContractNumber).ToList();

//            if (cont_num.Count == 0)
//            {
//                insertedRecords = 1;
//                arrlis.Add(insertedRecords);
//                var ErorMessage = "Card Number is Required";
//                arrlis.Add(ErorMessage);
//                return Json(arrlis, JsonRequestBehavior.AllowGet);
//            }
//            string contract_num = cont_num[0];
//            //string contract_num = "2527.18.19";

//            if (id == "")
//            {
//                insertedRecords = 1;
//                arrlis.Add(insertedRecords);
//                var ErorMessage = "Select Claim Service Name is Required";
//                arrlis.Add(ErorMessage);
//                return Json(arrlis, JsonRequestBehavior.AllowGet);
//            }

//            var rec = db.LUServices.FirstOrDefault(d => d.ServiceName == id);
//            int service_id = rec.ServiceID;


//            var result = (
//  from item1 in db.CLMClaimItems
//  join item2 in db.CLMClaims on item1.ClaimID equals item2.ClaimID into finalGroup0
//  from finalItem0 in finalGroup0.DefaultIfEmpty()
//  join item3 in db.ContractMembers on finalItem0.ContractMemberID equals item3.ContractMemberID into finalGroup1
//  from finalItem1 in finalGroup1.DefaultIfEmpty()
//  join item33 in db.Contracts on finalItem1.ContractID equals item33.ContractID into finalGroup11
//  from finalItem11 in finalGroup11.DefaultIfEmpty()
//  where finalItem11.ContractNumber == contract_num && finalItem0.CardNumber == name && item1.ClaimServiceID == service_id
//  select item1.NetPaymentAmt).ToList().Sum();

//            decimal Utilization_Amt = result;

//            var rec1 = db.Contracts.FirstOrDefault(d => d.ContractNumber == contract_num);
//            var rec2 = db.ContractMembers.FirstOrDefault(d => d.ContractID == rec1.ContractID && d.CardNumber == name);
//            var rec3 = db.ContractServiceCeilings.FirstOrDefault(d => d.ContractID == rec1.ContractID && d.ServiceID == service_id && d.PlanTypeID == rec2.PlanTypeID);
//            //rec1.ContractID;
//            var arlist = new ArrayList();

//            if (rec3 != null)
//            {
//                decimal Ceiling_Amt = rec3.CeilingAmt;
//                decimal Available_Amt = Ceiling_Amt - Utilization_Amt;


//                arlist.Add(Ceiling_Amt);
//                arlist.Add(Utilization_Amt);
//                arlist.Add(Available_Amt);
//            }
//            arrlis.Add(insertedRecords);
//            arrlis.Add(arlist);

//            return Json(arrlis, JsonRequestBehavior.AllowGet);
//        }

        public List<string> get_provider_category(int? provider_id)
        {

            /*

            select PVServiceCat.ServiceCatName from PVProviderData
            LEFT JOIN PVBranchData ON PVProviderData.ProviderID=PVBranchData.ProviderID
            LEFT JOIN PVServicePricing ON PVProviderData.ProviderID=PVServicePricing.ProviderID
            LEFT JOIN PVServiceCat ON PVServiceCat.ServiceCatID=PVServicePricing.ServiceCatID
            where PVProviderData.IsActive=	1 and PVServicePricing.ProviderID=100 and PVServicePricing.ServiceCatID!=0
            group by PVServiceCat.ServiceCatName ,PVServiceCat.ServiceCatID*/

            var result1 = (
 from item1 in db.PVProviderDatas
 join item2 in db.PVBranchDatas on item1.ProviderID equals item2.ProviderID into finalGroup0
 from finalItem0 in finalGroup0.DefaultIfEmpty()
 join item22 in db.PVServicePricings on item1.ProviderID equals item22.ProviderID into finalGroup00
 from finalItem00 in finalGroup00.DefaultIfEmpty()
 join item222 in db.PVServiceCats on finalItem00.ServiceCatID equals item222.ServiceCatID into finalGroup000
 from finalItem000 in finalGroup000.DefaultIfEmpty()
 where finalItem00.ProviderID == provider_id && item1.IsActive == true && finalItem00.ServiceCatID != 0
 group new { finalItem00, finalItem000 } by new { finalItem00.ServiceCatID, finalItem000.ServiceCatName } into cvrg
 orderby cvrg.Key.ServiceCatID
 select new
 {
     cvrg.Key.ServiceCatName


 }).ToList();
            List<string> aa = new List<string>();
            foreach (var item in result1)
            {
                aa.Add(item.ServiceCatName);
            }
            return aa;

        }

        public JsonResult Get_Icd_code(string id)
        {
            var rec = db.ICDCodes.FirstOrDefault(d => d.ShortName == id);

            return Json(rec, JsonRequestBehavior.AllowGet);
        }

        public JsonResult Get_Diagnosis_Name(string id)
        {
            var rec = db.ICDCodes.FirstOrDefault(d => d.ICDCode1 == id);

            return Json(rec, JsonRequestBehavior.AllowGet);
        }

        public JsonResult Doctor_Add_claims(string id, string name, string provider_Name, string Claim_Service, string Claim_form_number, string clist, string selectServicelist, string Date_Procedure, string Date_symptoms, string Diagnosis, string ICD_Number)
        {
            int recbatchid = 0;
            try
            {
                //CLMRecBatch
                var maxValue = db.CLMRecBatches.Max(o => o.RecBatchID);
                var result = db.CLMRecBatches.First(o => o.RecBatchID == maxValue);
                recbatchid = (result.RecBatchID) + 1;
            }
            catch
            {
                recbatchid = 1;
            }
            //int providerid = int.Parse(Session["providerid"].ToString());
            var rec7 = db.PVProviderDatas.FirstOrDefault(d => d.ProviderName.Contains(provider_Name));
            int providerid = rec7.ProviderID;

            DateTime recdate = DateTime.Now;

            var rec = db.PVProviderDatas.FirstOrDefault(d => d.ProviderID == providerid);
            int paymentday = int.Parse(rec.PaymentPeriod.ToString());
            DateTime paymentdate = DateTime.Now.AddDays(paymentday);

            var rec1 = db.ContractMembers.FirstOrDefault(d => d.CardNumber == name);
            int customerid = rec1.CustomerID;

            string providerbranchname = id;

            int calimid = 0;
            try
            {
                //CLMClaim
                var maxValu = db.CLMClaims.Max(o => o.ClaimID);
                var resul = db.CLMClaims.First(o => o.ClaimID == maxValu);
                calimid = (resul.ClaimID) + 1;
            }
            catch
            {
                calimid = 1;
            }

            var datenow = DateTime.Now;
            var cont_num = (
from item1 in db.ContractMembers
join item2 in db.Contracts on item1.ContractID equals item2.ContractID into finalGroup0
from finalItem0 in finalGroup0.DefaultIfEmpty()
where finalItem0.IsActive == true && finalItem0.StartDate <= datenow && finalItem0.EndDate >= datenow && item1.CardNumber == name
select new { finalItem0.ContractNumber, item1.ContractMemberID, finalItem0.ContractID, item1.PlanTypeID }).ToList();

            string contract_num = cont_num[0].ContractNumber;
            int Contract_Member_ID = cont_num[0].ContractMemberID;
            int Contract_ID = cont_num[0].ContractID;
            int plan_type_id = cont_num[0].PlanTypeID;

            var re1 = db.LUServices.FirstOrDefault(d => d.ServiceName == Claim_Service);
            int claim_service_id = re1.ServiceID;


            int claimitemiD = 0;
            //CLMClaimItem
            try
            {
                var maxVal = db.CLMClaimItems.Max(o => o.ClaimItemID);
                var resu = db.CLMClaimItems.First(o => o.ClaimItemID == maxVal);
                 claimitemiD = (resu.ClaimItemID) + 1;
            }
            catch
            {
                claimitemiD = 1;
            }
            // get service code
            var re2 = db.PVServices.FirstOrDefault(d => d.ServiceName == selectServicelist);
            int service_id = re2.ServiceID;
            string service_code = re2.ServiceCode;

            //var re3 = db.PVContracts.LastOrDefault(d => d.ProviderID == providerid);
            // provider contract
            var re3 = db.PVContracts
                          .Where(m => m.ProviderID == providerid)
                          .OrderByDescending(m => m.ProviderContractID)
                          .FirstOrDefault();
            int Provider_Contract_ID = re3.ProviderContractID;

            var re4 = db.PVServicePricings
                        .Where(d => d.ProviderContractID == Provider_Contract_ID && d.ServiceID == service_id)
                        .OrderByDescending(m => m.ServicePricingID)
                        .FirstOrDefault();
            //var re4 = db.PVServicePricings.FirstOrDefault(d => d.ProviderContractID == Provider_Contract_ID && d.ServiceID== service_id);
            decimal service_price = re4.Price;
            decimal service_DiscountPerc = re4.DiscountPerc;
            decimal service_DiscountVal = re4.DiscountVal;

            decimal Claimed_Amt = service_price * 1;

            var re5 = db.ContractServiceCeilings.FirstOrDefault(d => d.ContractID == Contract_ID && d.PlanTypeID == plan_type_id && d.ServiceID == claim_service_id);
            decimal Deduct_Per = re5.DeductPer;

            decimal Deduct_Amt = Deduct_Per * Claimed_Amt;
            decimal Medical_DeductAmt = 0;

            decimal Due_Amt = Claimed_Amt - Deduct_Amt - service_DiscountVal;
            decimal Requested_Amt = Due_Amt - Medical_DeductAmt;
            decimal Difference_Amt = Requested_Amt - Due_Amt;



            CLMRecBatch newdata = new CLMRecBatch()
            {

                RecBatchID = recbatchid,
                ProviderID = providerid,
                RecDate = recdate,
                PaymentDate = paymentdate,
                ClaimsNum = 1,
                DeliverBy = "Online",
                ClaimTotalAmt = Requested_Amt,
                BatchNumber = "0",
                IsImported = false,
                ProviderNumber = "0",
                CustomerID = customerid,
                BClaimedAmt = 0,
                BDeductAmt = 0,
                BDiscountAmt = 0,
                WithHoldingTaxPer = 0,
                WithHoldingTaxAmt = 0,
                BDuteAmt = 0,
                BatchStatus = 1,
                BClaimedNetAmt = 0,
                BMedicalDeductAmt = 0,
                ProviderBranchid = providerbranchname

            };
            db.CLMRecBatches.Add(newdata);
            db.SaveChanges();
            int recbatch_id = 0;
            try
            {
                var maxValue10 = db.CLMRecBatches.Max(o => o.RecBatchID);
                var result10 = db.CLMRecBatches.First(o => o.RecBatchID == maxValue10);
                recbatch_id = (result10.RecBatchID);
            }
            catch
            {
                recbatch_id = 1;
            }
            CLMClaim newdataa = new CLMClaim()
            {

                ClaimID = calimid,
                RecBatchID = recbatch_id,
                ClaimFormNum = long.Parse(Claim_form_number),
                ContractMemberID = Contract_Member_ID,
                symptomsDate = DateTime.Parse(Date_symptoms),
                ProcedureDate = DateTime.Parse(Date_Procedure),
                ClaimServiceID = claim_service_id,
                Diagnosis = Diagnosis,
                ICDCode = ICD_Number,
                TClaimAmt = Claimed_Amt,
                TDeductAmt = Deduct_Amt,
                TDiscountAmt = service_DiscountVal,
                TAClaimAmt = 0,
                TMedicalDeductAmt = Medical_DeductAmt,
                TDifferenceAmt = Difference_Amt,
                ClaimStatusID = 1,
                UserAddID = 0,
                UserApproveID = 0,
                NetClaimedAmt = Due_Amt,
                TRequestedAmt = Requested_Amt,
                BatchNumber = "",
                ContractNum = contract_num,
                CardNumber = name,
                ClaimDeductAmt = 0,
                ClaimDeductNote = "",
                TAuditDeductAmt = 0,
                IsOverCeiling = false,
                RepType = false,
                ProviderID = 0,
                ProviderName = ""

            };
            db.CLMClaims.Add(newdataa);
            db.SaveChanges();
            int calim_id = 0;
            try
            {
                var maxValui = db.CLMClaims.Max(o => o.ClaimID);
                var resuli = db.CLMClaims.First(o => o.ClaimID == maxValui);
                calim_id = (resuli.ClaimID);
            }
            catch
            {
                calim_id = 1;
            }
            CLMClaimItem newdataaa = new CLMClaimItem()
            {

                ClaimItemID = claimitemiD,
                ClaimID = calim_id,
                ServiceID = service_id,
                ServiceCode = service_code,
                ServiceName = selectServicelist,
                ServiceQnt = 1,
                ServiceUnitAmt = service_price,
                ClaimedAmt = Claimed_Amt,
                DeductAmt = Deduct_Amt,
                DiscountAmt = service_DiscountVal,
                FDiscountAmt = 0,
                MedicalDeductAmt = Medical_DeductAmt,
                DueAmt = Due_Amt,
                RequestedAmt = Requested_Amt,
                DifferenceAmt = Difference_Amt,
                RejectNotes = "",
                IsSysEntry = false,
                UserEntryID = 0,
                UserEntryDate = DateTime.Now,
                UserApprove = false,
                UserApproveID = 0,
                BatchNumber = "",
                ClaimNum = long.Parse(Claim_form_number),
                ServiceAmt = service_price,
                ServiceDeductAmt = Deduct_Amt,
                ServiceDiscountAmt = service_DiscountVal,
                NetServiceAmt = Due_Amt,
                ClaimServiceID = claim_service_id,
                ServiceDiscountPer = service_DiscountPerc,
                IsCovered = false,
                IsImported = false,
                NetPaymentAmt = Due_Amt,
                ReimbPer = 0,
                AuditDeductAmt = 0,
                AuditComments = "",
                IsOverCeiling = false

            };
            db.CLMClaimItems.Add(newdataaa);
            db.SaveChanges();


            return Json("true", JsonRequestBehavior.AllowGet);
        }


        [HttpGet]
        public ActionResult Print_CancelClaimForm()
        {
            if ((Session["UserID"] == null) || (int.Parse(Session["Doctor_ClaimForm"].ToString()) == 0))
            {
                return RedirectToAction("login", "ClaimForm");
            }
            return View();
        }


        public JsonResult GetClaimFormsDateRange(string id)
        {
            try
            {
                String[] spearator = { "-" };
                Int32 count = 2;
                String[] strlist = id.Split(spearator, count,
                       StringSplitOptions.RemoveEmptyEntries);
                string StartDateSTR = strlist[0].Replace(" ", string.Empty);
                string EndDateSTR = strlist[1].Replace(" ", string.Empty);

                DateTime StartDate = DateTime.ParseExact(StartDateSTR, "yyyy/MM/dd", CultureInfo.InvariantCulture);
                DateTime EndDate = DateTime.ParseExact(EndDateSTR, "yyyy/MM/dd", CultureInfo.InvariantCulture);

                int providerid = int.Parse(Session["providerid"].ToString());
                int BranchID = int.Parse(Session["BranchID"].ToString());

                var res = (from values in db.CLMClaimsONLINEs
                           join item1 in db.ClmApprovalOnlines on new { values.ClaimFormNum, values.ContractMemberID } equals new { item1.ClaimFormNum, item1.ContractMemberID }
                           join item in db.ContractMembers on values.ContractMemberID equals item.ContractMemberID
                           where values.ProcedureDate == item1.ApprovalDate && values.ProviderID == item1.IssueUserID && values.ProviderID == providerid && values.BranchID == BranchID && values.ProcedureDate >= StartDate && values.ProcedureDate <= EndDate
                           select new { item1.ApprovalID, values.ClaimFormNum, item.CardNumber, item.MemberName, values.ProcedureDate }).ToList();

                return Json(res, JsonRequestBehavior.AllowGet);
            }
            catch
            {
                return Json("Falsee", JsonRequestBehavior.AllowGet);

            }
        }



        public JsonResult Get_Doctor_ClaimFormServices(string ApprovalID)
        {

            var ApprovalIDD = int.Parse(ApprovalID);
            var res = (from values in db.ClmApprovalItemsOnlines
                       where values.ApprovalID == ApprovalIDD
                       select new { values.ServiceName, values.ServiceQnt, values.ServiceNote }).ToList();

            return Json(res, JsonRequestBehavior.AllowGet);
        }

        //public JsonResult Get_diagnosis(string id)
        //{
        //    var rec = db.ICDCodes.FirstOrDefault(d => d.ICDCode1 == id);
        //    return Json(rec, JsonRequestBehavior.AllowGet);
        //}
        public ActionResult GenerateReport_PrintInvoice(string typeOfReport, string ClaimId)
        {
            LocalReport lr = new LocalReport();
            string path = Path.Combine(Server.MapPath("~/Report"), "InvoiceReport.rdlc");
            if (System.IO.File.Exists(path))
            {
                lr.ReportPath = path;
            }
            else
            {
                return null;
            }

            var datatable1 = new List<object>();
            var datatable2 = new List<object>();

            //int i = 1;
            try
            {
                var claim_ID = int.Parse(ClaimId);
                //if ((Approval_numberr != "") /*&& (Approval_numberr != -1)*/)
                //{

                var CLM_Claims = (from values in db.CLMClaims
                                  join o in db.CLMClaimItems on values.ClaimID equals o.ClaimID 
                                  join gf in db.ContractMembers on values.ContractMemberID equals gf.ContractMemberID
                                  where values.ClaimID == claim_ID
                                  select new
                                  {
                                      values.ClaimFormNum,
                                      values.ContractMemberID,
                                      values.ClaimServiceID,
                                      values.TClaimAmt,
                                      values.TDeductAmt,
                                      values.ProviderName,
                                      values.BranchName,
                                      values.ProcedureDate,
                                      gf.MemberName,
                                      gf.CardNumber,
                                      gf.CustomerID,
                                      gf.ContractID,
                                      gf.PlanTypeID,
                                      o.ServiceName,
                                      o.ServiceUnitAmt,
                                      o.ServiceQnt,
                                      o.ClaimedAmt,
                                      o.DeductAmt,
                                      o.RequestedAmt,
                                      o.DiscountAmt,
                                      o.RejectNotes,
                                  }).ToList();

                //value.ToString("0,0.0",CultureInfo.InvariantCulture)
                foreach (var item in CLM_Claims)
                {
                    datatable1.Add(new
                    {
                        ServiceNamee = item.ServiceName,
                        ServicePrice = item.ServiceUnitAmt,
                        ServiceQntt = item.ServiceQnt,
                        ServiceAmt = item.ClaimedAmt,
                        ServiceDed = item.DeductAmt,
                        RequestPrice = item.RequestedAmt+ item.DiscountAmt+item.DeductAmt,
                        ServiceNote = item.RejectNotes,


                    });
                }

                var claim_service_id = CLM_Claims[0].ClaimServiceID;
                var Contract_ID = CLM_Claims[0].ContractID;
                var plan_type_id = CLM_Claims[0].PlanTypeID;

                var ClaimForm_Num = CLM_Claims[0].ClaimFormNum;
                var ContractMember_ID = CLM_Claims[0].ContractMemberID;

                bool? isDeductablee = false;
                bool? isChronic = false;
                var notess = "";
                var ApprovalOnline = db.ClmApprovalOnlines.Where(d => d.ApprovalCode == ClaimForm_Num && d.ContractMemberID == ContractMember_ID).OrderByDescending(d=>d.ApprovalID).FirstOrDefault();
                if (ApprovalOnline == null)
                {
                    var ApprovalOnlineWithForm = db.ClmApprovalOnlines.Where(d => d.ClaimFormNum == ClaimForm_Num && d.ContractMemberID == ContractMember_ID).OrderByDescending(d => d.ApprovalID).FirstOrDefault();
                    if (ApprovalOnlineWithForm != null)
                    {
                        isDeductablee = ApprovalOnlineWithForm.IsDeduct;
                        notess = ApprovalOnlineWithForm.Notes;
                    }
                }
                else
                {
                    isDeductablee = ApprovalOnline.IsDeduct;
                    notess = ApprovalOnline.Notes;
                    
                }

                var ApprovalOnline1 = db.ClmApprovalOnlines.Where(d => d.ClaimFormNum == ClaimForm_Num && d.ContractMemberID == ContractMember_ID).OrderByDescending(d => d.ApprovalID).FirstOrDefault();
                if (ApprovalOnline1 != null)
                {
                    var chrotemp = ApprovalOnline1.ChronicTemplateID;
                    if (chrotemp != 0)
                    {
                        isChronic = true;
                    }
                }

                    decimal Deduct_Per = 0;
                if (isChronic==true)
                {
                    var re5 = db.ContractServiceCeilings.FirstOrDefault(d => d.ContractID == Contract_ID && d.PlanTypeID == plan_type_id && d.ServiceID == 4);
                    Deduct_Per = re5.DeductPer;
                }
                else if (claim_service_id == 14 || claim_service_id == 19 || isDeductablee == true)
                {
                    Deduct_Per = 0;
                }
                else
                {
                    var re5 = db.ContractServiceCeilings.FirstOrDefault(d => d.ContractID == Contract_ID && d.PlanTypeID == plan_type_id && d.ServiceID == claim_service_id);
                    Deduct_Per = re5.DeductPer;
                }

                var customerid = CLM_Claims[0].CustomerID;
                var customername = db.CustomerDatas.FirstOrDefault(d => d.CustomerID == customerid).CustomerName;
                string FirstName = Session["FirstName"].ToString();
                string LasttName = Session["LastName"].ToString();
                string FullName = FirstName + " " + LasttName;
                datatable2.Add(new
                {
                    ClaimID = claim_ID,
                    ClaimFormNumber = CLM_Claims[0].ClaimFormNum,
                    InvoiceDate = CLM_Claims[0].ProcedureDate?.ToString("yyyy/MM/dd"),
                    //InvoiceDate = (DateTime.Now).ToString("yyyy/MM/dd"),
                    ProviderNamee = CLM_Claims[0].ProviderName,
                    ProviderAddresss = CLM_Claims[0].BranchName,
                    MemberNamee = CLM_Claims[0].MemberName,
                    CardNumbere = CLM_Claims[0].CardNumber,
                    DeductPerr = Deduct_Per.ToString("P", CultureInfo.InvariantCulture),
                    CustomerNamee = customername,
                    UserName = FullName,
                    InvoiceAmt = CLM_Claims[0].TClaimAmt,
                    //InvoiceAmt = (CLM_Claims[0].DeductPer)?.ToString("P", CultureInfo.InvariantCulture),
                    InvoiceDeduct = CLM_Claims[0].TDeductAmt,
                    Notes = notess,
                });

                // }
                // i++;

            }
            catch
            {
                return null;
            }
            //}



            ReportDataSource rd1 = new ReportDataSource("DataSet1", datatable1);
            ReportDataSource rd2 = new ReportDataSource("DataSet2", datatable2);
            lr.DataSources.Clear();
            lr.DataSources.Add(rd1);
            lr.DataSources.Add(rd2);
            string reportType = typeOfReport;
            string mimeType;
            string encoding;
            string fileNameExtension;
            string deviceInfo =

                "<DeviceInfo>" +

                "<OutputFormat>" + typeOfReport + "</OutputFormat>" +

                "<PageWidth>8.5in</PageWidth>" +

                "</DeviceInfo>";


            Warning[] warning;
            string[] streams;
            byte[] renderedBytes;

            renderedBytes = lr.Render(
                reportType,
                deviceInfo,
                out mimeType,
                out encoding,
                out fileNameExtension,
                out streams,
                out warning);

            //byte[] Bytes = lr.Render(format: "PDF", deviceInfo: "");
            ////Server.MapPath("~/Downloads/output.pdf")
            ////@"C:\Publish\Guardian\Images\"
            ////@"C:\Users\MohamedAlaa\Downloads\output.pdf"
            //var user = Environment.UserName;
            //var nameee = "Invoice"+ ClaimId+ ".pdf";
            //var path11 = Path.Combine(Path.Combine(@"C:\Users", user), Path.Combine(@"Downloads\", nameee) );
            //using (FileStream fs = new FileStream(path11, FileMode.Create))
            //{
            //    fs.Write(Bytes, 0, Bytes.Length);
            //}
            List<byte[]> srcPDFs = new List<byte[]>();
            srcPDFs.Add(renderedBytes);
            byte[] byteInfo;
            using (var ms = new MemoryStream())
            {
                using (var resultPDF = new PdfDocument(ms))
                {
                    foreach (var pdf in srcPDFs)
                    {
                        using (var src = new MemoryStream(pdf))
                        {
                            using (var srcPDF = PdfReader.Open(src, PdfDocumentOpenMode.Import))
                            {
                                for (var i = 0; i < srcPDF.PageCount; i++)
                                {
                                    resultPDF.AddPage(srcPDF.Pages[i]);
                                }
                            }
                        }
                    }
                    resultPDF.Save(ms);
                    byteInfo = ms.ToArray();
                    //final.adde(ms);
                    // return File(byteInfo, "application/pdf");
                    //var a = File(byteInfo, "application/pdf");

                    // return File(byteInfo, mimeType);
                    // return View("", "application/pdf");
                    //return new FileStreamResult(byteInfo, "application/pdf");
                }
            }
            var nameee = "Invoice" + ClaimId /*+ ".pdf"*/;
            return File(byteInfo, "application/pdf", nameee + ".pdf");

            //return File(renderedBytes, mimeType); ;


        }

        public ActionResult GenerateReport_PrintClaimForm(string typeOfReport, string ApprovalID)
        {

            var apprvaliddd = 0;
            LocalReport lr = new LocalReport();
            string path = Path.Combine(Server.MapPath("~/Report"), "ClaimFormReport.rdlc");
            if (System.IO.File.Exists(path))
            {
                lr.ReportPath = path;
            }
            else
            {
                return null;
            }

            var datatable1 = new List<object>();
            var datatable2 = new List<object>();

            //int i = 1;
            try
            {
                var Approval_IDDD = long.Parse(ApprovalID);
                //if ((Approval_numberr != "") /*&& (Approval_numberr != -1)*/)
                //{

                var CLM_Claims = (from values in db.ClmApprovalOnlines
                                  join o in db.ClmApprovalItemsOnlines on values.ApprovalID equals o.ApprovalID
                                  join gf in db.ContractMembers on values.ContractMemberID equals gf.ContractMemberID
                                  where values.ApprovalID == Approval_IDDD
                                  select new
                                  {
                                      values.ApprovalID,
                                      values.ApprovalCode,
                                      values.ClaimFormNum,
                                      values.ContractMemberID,
                                      values.ApprovalDate,
                                      values.IssueUserID,
                                      values.Notes,
                                      gf.MemberName,
                                      gf.CardNumber,
                                      gf.CustomerID,
                                      gf.ContractID,
                                      gf.PlanTypeID,
                                      o.ServiceName,
                                      o.ServiceQnt,
                                      o.ServiceNote,
                                  }).ToList();

                //value.ToString("0,0.0",CultureInfo.InvariantCulture)
                foreach (var item in CLM_Claims)
                {
                    datatable1.Add(new
                    {
                        ServiceNamee = item.ServiceName,
                        ServiceQntt = item.ServiceQnt,
                        ServiceNote = item.ServiceNote,


                    });
                }

                var claim_service_id = 25;
                var Contract_ID = CLM_Claims[0].ContractID;
                var plan_type_id = CLM_Claims[0].PlanTypeID;

                var ClaimForm_Num = CLM_Claims[0].ClaimFormNum;
                var ContractMember_ID = CLM_Claims[0].ContractMemberID;
                var Approval_Date = CLM_Claims[0].ApprovalDate;
                var ProviderIDD = CLM_Claims[0].IssueUserID;
                var Approval_ID = CLM_Claims[0].ApprovalID;
                 apprvaliddd = Approval_ID;

                var notess = CLM_Claims[0].Notes;
               

                decimal Deduct_Per = 0;
              
                var re5 = db.ContractServiceCeilings.FirstOrDefault(d => d.ContractID == Contract_ID && d.PlanTypeID == plan_type_id && d.ServiceID == claim_service_id);
                if (re5 == null)
                {
                    Deduct_Per = 0;

                }
                else
                {
                    Deduct_Per = re5.DeductPer;
                }
                var customerid = CLM_Claims[0].CustomerID;
                var customername = db.CustomerDatas.FirstOrDefault(d => d.CustomerID == customerid).CustomerName;
                string FirstName = Session["FirstName"].ToString();
                string LasttName = Session["LastName"].ToString();
                string FullName = FirstName + " " + LasttName;

                var Provider_Name = "";
                var branch_Name = "";
                decimal? Invoice_Amt = 0;
                decimal? Invoice_Deduct = 0;

                var CLMClaims_ONLINEs = db.CLMClaimsONLINEs.Where(d => d.ClaimFormNum == ClaimForm_Num &&d.ProcedureDate== Approval_Date /*&& d.ProviderCatID== 1*/ && d.ProviderID== ProviderIDD).OrderByDescending(d => d.ClaimID).FirstOrDefault();
                if (CLMClaims_ONLINEs != null)
                {
                     branch_Name = CLMClaims_ONLINEs.BranchName;
                    var ClaimOnlineID = CLMClaims_ONLINEs.ClaimID;
                    Invoice_Amt = db.CLMClaimsItemsONLINEs.Where(d => d.ClaimID == ClaimOnlineID).Sum(d=>d.RequestedPrice);
                    Invoice_Deduct = Invoice_Amt * Deduct_Per;

                }
                var PVProvider_Datas = db.PVProviderDatas.Where(d => d.ProviderID == ProviderIDD).OrderByDescending(d => d.ProviderID).FirstOrDefault();
                if (PVProvider_Datas != null)
                {
                    Provider_Name = PVProvider_Datas.ProviderName;
                }


                datatable2.Add(new
                {
                    ClaimID = Approval_ID,
                    ClaimFormNumber = CLM_Claims[0].ClaimFormNum,
                    InvoiceDate = CLM_Claims[0].ApprovalDate?.ToString("yyyy/MM/dd"),
                    ProviderNamee = Provider_Name,
                    ProviderAddresss = branch_Name,
                    MemberNamee = CLM_Claims[0].MemberName,
                    CardNumbere = CLM_Claims[0].CardNumber,
                    DeductPerr = Deduct_Per.ToString("P", CultureInfo.InvariantCulture),
                    CustomerNamee = customername,
                    UserName = FullName,
                    Notes = notess,
                    InvoiceAmt = Math.Round(double.Parse(Invoice_Amt.ToString()),2),
                    InvoiceDeduct = Math.Round(double.Parse(Invoice_Deduct.ToString()),2),
                });

                // }
                // i++;

            }
            catch
            {
                return null;
            }
            //}



            ReportDataSource rd1 = new ReportDataSource("DataSet1", datatable1);
            ReportDataSource rd2 = new ReportDataSource("DataSet2", datatable2);
            lr.DataSources.Clear();
            lr.DataSources.Add(rd1);
            lr.DataSources.Add(rd2);
            string reportType = typeOfReport;
            string mimeType;
            string encoding;
            string fileNameExtension;
            string deviceInfo =

                "<DeviceInfo>" +

                "<OutputFormat>" + typeOfReport + "</OutputFormat>" +

                "<PageWidth>8.5in</PageWidth>" +

                "</DeviceInfo>";


            Warning[] warning;
            string[] streams;
            byte[] renderedBytes;

            renderedBytes = lr.Render(
                reportType,
                deviceInfo,
                out mimeType,
                out encoding,
                out fileNameExtension,
                out streams,
                out warning);

           
            List<byte[]> srcPDFs = new List<byte[]>();
            srcPDFs.Add(renderedBytes);
            byte[] byteInfo;
            using (var ms = new MemoryStream())
            {
                using (var resultPDF = new PdfDocument(ms))
                {
                    foreach (var pdf in srcPDFs)
                    {
                        using (var src = new MemoryStream(pdf))
                        {
                            using (var srcPDF = PdfReader.Open(src, PdfDocumentOpenMode.Import))
                            {
                                for (var i = 0; i < srcPDF.PageCount; i++)
                                {
                                    resultPDF.AddPage(srcPDF.Pages[i]);
                                }
                            }
                        }
                    }
                    resultPDF.Save(ms);
                    byteInfo = ms.ToArray();
                  
                }
            }
            var nameee = "ClaimForm" + apprvaliddd /*+ ".pdf"*/;
            return File(byteInfo, "application/pdf", nameee + ".pdf");

            //return File(renderedBytes, mimeType); ;


        }

    }
}