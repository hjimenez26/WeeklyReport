using System;
using System.Collections.Generic;
using System.Data;
using System.IO;
using System.Linq;
using System.Net.Mail;
using System.Net.Mime;
using System.Text;
using System.Threading.Tasks;
using HenryErrorHandling;

namespace ewrWeeklyReport
{
    class Program
    {
        private static int intAppID = 1000;

      

        private static string strConnectMain = "Data Source=66.85.128.171;Initial Catalog=bridgemain;User ID=warren;Password=jl-a#1uKif?lrabrl?h@";

        private static string strConnectEWR = "Data Source=66.85.128.171;Initial Catalog=EWR;User ID=warren;Password=jl-a#1uKif?lrabrl?h@";


        private static string strErrorFilePath = @"c:\\log\ewrWeeklyReport\errorlog.txt";

    

        static DataTable dtModems;
        static DataTable dtFacilities;
        static DataTable dtRecipients;
        private static DataTable dtICU6;
        static private HenryErrorHandling.ExcHandler excHandler;

        static private DateTime dtStartDate;

        static private DateTime dtEndDate;
        //static int intProcessedCount = 0;



        static void Main(string[] args)
        {
            start();

            excHandler = new ExcHandler(strConnectMain,strErrorFilePath,1,intAppID);
        }


        static void start()
        {

            try
            {

         

            loadTables();
            loadRecipients();
      

            DateTime dtNow = DateTime.Now;

            dtStartDate = DateTime.UtcNow.AddHours(-24);
            dtEndDate = DateTime.UtcNow;
                GetTblICU6Data();
                StringBuilder strbFacs = new StringBuilder();
       


            for (int intFacCnt = 0; intFacCnt < dtFacilities.Rows.Count; intFacCnt++)//each facility
            {
                //DELETE AFTER ASKING ABOUT PHOENIX GCM
                if (intFacCnt != 4)//need to remove, PhoenixGCM is throwing error because missing PeopleCnt2 table...
                {
                    strbFacs.AppendLine("<br/>");
                    strbFacs.AppendLine("<center><font size=\"6\"><strong>" + dtFacilities.Rows[intFacCnt]["FacilityName"].ToString() + "</strong></font></center><br/>");
                    int intFacId = (int)dtFacilities.Rows[intFacCnt]["FacilityID"];

             

                    loadModemsTable(intFacId);

                    int[][] intArryModems = new int[dtModems.Rows.Count][];

                    StringBuilder strb = new StringBuilder();
                    int intOffSum = 0;
                    int intOnSum = 0;
                    int intTracker = 0;

                        //Begin html table for each facility.
                        
                    strb.AppendLine("<center><table cellpadding=\"3\" style =\"border-collapse:collapse; width:75%; empty-cells:hide\" border='1' font-color=\"black\">" +
                        "<thead>" +
                            "<tr>" +
                                "<th style=\"padding:10px\" scope = \"col\">Name</th>" +
                                "<th  scope = \"col\">Door 1 Off</th>" + 
                                "<th  scope = \"col\">Door 1 On</th>" + 
                                "<th  scope = \"col\">Door 2 Off</th>" + 
                                "<th  scope = \"col\">Door 2 On</th>" + 
                                "<th  scope = \"col\">Door 3 Off</th>" + 
                                "<th  scope = \"col\">Door 3 On</th>" + 
                                "<th  scope = \"col\">Total Off</th>" +
                                "<th  scope = \"col\">Total On</th>" +
                                "<th  scope = \"col\">Ratio ON/OFF</th>" +
                                "<th  scope = \"col\">Door 1 Max Off</th>" +
                                "<th  scope = \"col\">Door 1 Max On</th>" +
                                "<th  scope = \"col\">Door 2 Max Off</th>" +
                                "<th  scope = \"col\">Door 2 Max On</th>" +
                                "<th  scope = \"col\">Door 3 Max Off</th>" +
                                "<th  scope = \"col\">Door 3 Max On</th>" +
                            "</tr>" +
                        "</thead>" +
                        "<tbody>"
                        );
                            


                    for (int intModemCnt = 0; intModemCnt < dtModems.Rows.Count; intModemCnt++)//each modem
                    {
                        Boolean bolRowWithNoData = false;

                            // ClassCounts scCounts = getModemCounts(dtFacilities.Rows[intFacCnt]["connectString"].ToString(),
                               //     (int)dtModems.Rows[intModemCnt]["modemid"], dtStartDate, dtEndDate, intFacId);
                         
                        CountResults countResults = getModemCounts(dtFacilities.Rows[intFacCnt]["connectString"].ToString(), (int)dtModems.Rows[intModemCnt]["modemid"], dtStartDate, dtEndDate);
                        if (countResults.totalOffs != -999 && countResults.totalOns != -999)//has data
                        {
                            int intDTErrors = getDateTimeErrors(dtStartDate, dtEndDate,(int)dtModems.Rows[intModemCnt]["modemid"],dtFacilities.Rows[intFacCnt]["connectString"].ToString());
                            var   varICU6 = ParseICU6Modem((int) dtModems.Rows[intModemCnt]["modemid"]);

                            if ((countResults.onOffRatio * 100) > 95)

                            //add table row with data
                            strb.AppendLine(addHTMLRowData(intTracker, dtModems, intModemCnt, intDTErrors, countResults,varICU6.dtTimeStamp,varICU6.boolInDB));

                                //intArryModems[intModemCnt] = intArryModemCount;
                                intOffSum = intOffSum + countResults.totalOffs;//compute total offs for all modems in facility
                                intOnSum = intOnSum + countResults.totalOns;//compute total son for all modems in facility

                            }
                        else
                        {
                            //add table row no data
                            bolRowWithNoData = true;
                            var varICU6 = ParseICU6Modem((int)dtModems.Rows[intModemCnt]["modemid"]);
                            strb.AppendLine(addHTMLRowNoData(intTracker, intModemCnt, dtModems, varICU6.dtTimeStamp, varICU6.boolInDB));


                        }

                        string strModemFix = getModemFixstring(dtStartDate, dtEndDate, (int)dtModems.Rows[intModemCnt]["modemid"],
                        dtFacilities.Rows[intFacCnt]["connectString"].ToString(), bolRowWithNoData);


                        intTracker++;

                    }//for modems


                    strbFacs.Append(strb);

                 

                    float fltPercentage;

                    if (intOffSum > intOnSum)
                    {
                        fltPercentage = ((float)intOnSum / (float)intOffSum) * 100;
                    }
                    else
                    {
                        fltPercentage = ((float)intOffSum / (float)intOnSum) * 100;
                    }
                   
                    strbFacs.AppendLine("</table><br><br><span style='font-weight:bold'><u>Summary</u><br><br>Total Offs: " + intOffSum + "<br>Total Ons: " + intOnSum);// + "<br>Accuracy: " + fltPercentage + "</span></center><br/><br><hr>");

                    if (fltPercentage < 96.5)
                    {
                        strbFacs.AppendLine("<br>Accuracy: <font color = \"red\">" + fltPercentage + "</font></span></center><br/><br><hr>");
                    }
                    else
                    {
                        strbFacs.AppendLine("<br>Accuracy: " + fltPercentage + "</span></center><br/><br><hr>");
                    }


                }
            }//for facilities


            string strReport = strbFacs.ToString();
        

            sendmail(strReport, dtNow);

            }
            catch (Exception e)
            {
                 excHandler.logError(e,"start",0);
            }


        }//*************************start*********************

        static (bool? boolInDB, DateTime? dtTimeStamp) ParseICU6Modem(int intModemid)
        {
            DateTime? dtTimestamp = null;
            bool? boolInDB = null;
            try
            {
                DataRow[] dr = dtICU6.Select("modemid='" + intModemid + "'");
                if (dr.Length == 1)
                {
                  dtTimestamp = (DateTime?)  dr[0]["TimeSent"];
                    boolInDB = (bool?) dr[0]["boolDBVerified"];
                }
                else if (dr.Length > 1)
                {
                    int intLength = dr.Length;
                    DateTime? dtTemp = (DateTime?)dr[0]["TimeSent"];
                    bool? boolTemp = (bool?)dr[0]["boolDBVerified"];
                    int intIndexToUse = 0;
                    for (int intCnt = 1; intCnt < intLength; intCnt++)
                    {
                        DateTime? dtNxt = (DateTime?)dr[intCnt]["TimeSent"];
                        if (dtNxt > dtTemp)
                        {
                            bool? boolNxt = (bool?)dr[intCnt]["boolDBVerified"];
                            if (boolNxt == true)
                            {
                                dtTemp = dtNxt;
                           
                                intIndexToUse = intCnt;
                            }
                         

                          
                        }

                    }
                    dtTimestamp = (DateTime?)dr[intIndexToUse]["TimeSent"];
                    boolInDB = (bool?)dr[intIndexToUse]["boolDBVerified"];
                }
                else if (dr.Length == 0)
                {
                    dtTimestamp = null;
                    boolInDB = false;
                }
            }
            catch (Exception e)
            {
               excHandler.logError(e,"ParseICU6Modem",0);
            }
            return (boolInDB, dtTimestamp);
        }//*****ParseICU6Modem******************************************

        


        static private CountResults getModemCounts(string strFacConnect, int intModemId, DateTime dtBegin, DateTime dtEndTime)
        {
            CountResults countResults = new CountResults();


            DataTable dtModemPCCountSummary = getModemDataSummary(dtBegin, dtEndTime, intModemId, strFacConnect);

            Int16 d1OffCount = 0;
            Int16 d2OffCount = 0;
            Int16 d3OffCount = 0;
            Int16 d1OnsCount = 0;
            Int16 d2OnsCount = 0;
            Int16 d3OnsCount = 0;

            float onOffRatio = 0;

            Int16 d1MaxOffs = 0;
            Int16 d2MaxOffs = 0;
            Int16 d3MaxOffs = 0;
            Int16 d1MaxOns = 0;
            Int16 d2MaxOns = 0;
            Int16 d3MaxOns = 0;

            Int16 offsTotal = 0;
            Int16 onsTotal = 0;

            if (!dtModemPCCountSummary.Rows[0].IsNull("d1offs"))
            {
                d1OffCount = System.Convert.ToInt16(dtModemPCCountSummary.Rows[0]["d1offs"]);
            }
            if (!dtModemPCCountSummary.Rows[0].IsNull("d2offs"))
            {
                d2OffCount = System.Convert.ToInt16(dtModemPCCountSummary.Rows[0]["d2offs"]);
            }
            if (!dtModemPCCountSummary.Rows[0].IsNull("d3offs"))
            {
                d3OffCount = System.Convert.ToInt16(dtModemPCCountSummary.Rows[0]["d3offs"]);
            }
            if (!dtModemPCCountSummary.Rows[0].IsNull("d1ons"))
            {
                d1OnsCount = System.Convert.ToInt16(dtModemPCCountSummary.Rows[0]["d1ons"]);
            }
            if (!dtModemPCCountSummary.Rows[0].IsNull("d2ons"))
            {
                d2OnsCount = System.Convert.ToInt16(dtModemPCCountSummary.Rows[0]["d2ons"]);
            }
            if (!dtModemPCCountSummary.Rows[0].IsNull("d3ons"))
            {
                d3OnsCount = System.Convert.ToInt16(dtModemPCCountSummary.Rows[0]["d3ons"]);
            }


            if (!dtModemPCCountSummary.Rows[0].IsNull("m1off"))
            {
                d1MaxOffs = System.Convert.ToInt16(dtModemPCCountSummary.Rows[0]["m1off"]);
            }
            else
            {
                d1MaxOffs = -999;
            }
            if (!dtModemPCCountSummary.Rows[0].IsNull("m2off"))
            {
                d2MaxOffs = System.Convert.ToInt16(dtModemPCCountSummary.Rows[0]["m2off"]);
            }
            else
            {
                d2MaxOffs = -999;
            }
            if (!dtModemPCCountSummary.Rows[0].IsNull("m3off"))
            {
                d3MaxOffs = System.Convert.ToInt16(dtModemPCCountSummary.Rows[0]["m3off"]);
            }
            else
            {
                d3MaxOffs = -999;
            }
            if (!dtModemPCCountSummary.Rows[0].IsNull("m1on"))
            {
                d1MaxOns = System.Convert.ToInt16(dtModemPCCountSummary.Rows[0]["m1on"]);
            }
            else
            {
                d1MaxOns = -999;
            }
            if (!dtModemPCCountSummary.Rows[0].IsNull("m2on"))
            {
                d2MaxOns = System.Convert.ToInt16(dtModemPCCountSummary.Rows[0]["m2on"]);
            }
            else
            {
                d2MaxOns = -999;
            }
            if (!dtModemPCCountSummary.Rows[0].IsNull("m3on"))
            {
                d3MaxOns = System.Convert.ToInt16(dtModemPCCountSummary.Rows[0]["m3on"]);
            }
            else
            {
                d3MaxOns = -999;
            }

            if (d3OffCount != -999 && d2OffCount != -999 && d1OffCount != -999)
            {
                offsTotal = System.Convert.ToInt16(d3OffCount + d2OffCount + d1OffCount);
            }
            else
            {
                offsTotal = -999;
            }
            if (d3OnsCount != -999 && d2OnsCount != -999 && d1OnsCount != -999)
            {
                onsTotal = System.Convert.ToInt16(d1OnsCount + d2OnsCount + d3OnsCount);
            }
            else
            {
                onsTotal = -999;
            }

            if (offsTotal != -999 && onsTotal != -999 && offsTotal != 0 && onsTotal != 0)
            {
                if (onsTotal > offsTotal)
                {
                    onOffRatio = onsTotal / System.Convert.ToSingle(offsTotal);
                }
                else
                {
                    onOffRatio = (offsTotal / System.Convert.ToSingle(onsTotal)) * -1;
                }
            }

            countResults.d1Offs = d1OffCount;
            countResults.d2Offs = d2OffCount;
            countResults.d3Offs = d3OffCount;
            countResults.d1Ons = d1OnsCount;
            countResults.d2Ons = d2OnsCount;
            countResults.d3Ons = d3OnsCount;

            countResults.d1maxOffs = d1MaxOffs;
            countResults.d2maxOffs = d2MaxOffs;
            countResults.d3maxOffs = d3MaxOffs;
            countResults.d1maxOns = d1MaxOns;
            countResults.d2maxOns = d2MaxOns;
            countResults.d3maxOns = d3MaxOns;

            countResults.totalOffs = offsTotal;
            countResults.totalOns = onsTotal;

            countResults.onOffRatio = onOffRatio;

            return countResults;
        }

        static private void GetTblICU6Data()
        {
            try
            {

       


            string strSelect =
                "SELECT [modemid], max([timeSent]) as [TimeSent]  ,[boolDBVerified] FROM[EWR].[dbo].[tblICU6] where timesent between '" +
                dtStartDate + "' and '" + dtEndDate + "' group by modemid,boolDBVerified order by modemid,timesent";
          dtICU6 =  henrySqlStuff.execute.sqlExecuteSelectForever(strConnectEWR, strSelect, strErrorFilePath);


            }
            catch (Exception e)
            {
               excHandler.logError(e,"GetTblICU6Data",0);
            }



        }


        static private int getDateTimeErrors(DateTime dtBegin, DateTime dtEnd, int intModemId, string strConnect)
        {
            int intAnswer = 0;
            string strSelect = "SELECT count([gpsid]) FROM [dbo].[tblGpsData] where gpsdatetime between  '" + dtBegin +
                               "' and '" + dtEnd + "' and datediff(minute,gpsdatetime,receiveddatetime)< 100";
            DataTable dt = henrySqlStuff.execute.sqlExecuteSelectForever(strConnect, strSelect, strErrorFilePath);
            if (dt.Rows.Count > 1)
            {

                intAnswer = dt.Rows.Count;
            }
            return intAnswer;
        }


        static private DataTable getModemDataSummary(DateTime dtBegin, DateTime dtEnd, int intModemId, string strConnect)
        {
            string strSelect = "select sum(d1off+d2off+d2off+d4off+d5off+d6off) as offs,sum(d1on+d2on+d3on+d4on+d5on+d6on) as ons  FROM [dbo].[tblPeopleCount6] where modemid =" + intModemId + " and gpsdatetime > '" + dtBegin + "' and gpsdatetime <'" + dtEnd + "'";
            string anotherString = "select sum(d1off) as d1offs,sum(d2off) as d2offs,sum(d3off) as d3offs,sum(d1on) as d1ons,sum(d2on) as d2ons,sum(d3on) as d3ons,max(d1off) as m1off,max(d2off) as m2off,max(d3off) as m3off,max(d1on) as m1on,max(d2on) as m2on,max(d3on) as m3on  FROM [dbo].[tblPeopleCount6] where modemid =" +
                    intModemId + " and gpsdatetime > '" + dtBegin + "' and gpsdatetime <'" + dtEnd + "'";

            DataTable dt = henrySqlStuff.execute.sqlExecuteSelectForever(strConnect, strSelect, strErrorFilePath);
            //DataTable dt = henrySqlStuff.execute.sqlExecuteSelectForever(strConnect, strSelect, strErrorFilePath);
            DataTable tmp = henrySqlStuff.execute.sqlExecuteSelectForever(strConnect, anotherString, strErrorFilePath);

            return tmp;
        }


        static private string getModemFixstring(DateTime dtBegin, DateTime dtEnd, int intModemId, string strConnect, Boolean bolRowWithNoData)
        {
            StringBuilder strAnswer = new StringBuilder();
            Boolean bolFoundType32 = false;

            DataTable dt = getModemFix(strConnect, intModemId, dtBegin, dtEnd);
            int intRows = dt.Rows.Count;

            if (bolRowWithNoData == false)
            {
                for (int intCnt = 0; intCnt < intRows; intCnt++)
                {
                    string strCount = dt.Rows[intCnt]["count"].ToString();
                    string strStatus = dt.Rows[intCnt]["fixstatus"].ToString();


                    if (strStatus == "32")//only record type 32
                    {
                        //strAnswer.Append(" cnt: " + strCount + " fixStatus: " + strStatus + "<br/>");
                        bolFoundType32 = true;
                        strAnswer.Append(strCount + "<br/>");
                    }
                }

                if (bolFoundType32 == false)
                {
                    strAnswer.Append(0 + "<br/>");
                }
            }

            return strAnswer.ToString();
        }


        static private DataTable getModemFix(string strFacConnect, int intModemId, DateTime dtBegin, DateTime dtEndTime)
        {
            string strSelect = "SELECT count(gpsid) as count,fixstatus FROM [dbo].[tblGpsData] where modemid =" + intModemId + " and gpsdatetime > '" + dtBegin + "' and gpsdatetime <'" + dtEndTime + "' and fixstatus <> 0 group by fixstatus";
            DataTable dt = henrySqlStuff.execute.sqlExecuteSelectForever(strFacConnect, strSelect,strErrorFilePath);
            return dt;
        }


        static private void loadTables()
        {
            //select only type 1 facilities
            string strSelectMain =
                "select [FacilityID],[FacilityName],[FacilityCode],[Active],[TimeZoneID],[createdDate],[FacilityTypeID],[connectString],[honorDST],[nwLat],[nwLong],[seLat],[seLong] from tblFacilities where FacilityID=6";
            dtFacilities = henrySqlStuff.execute.sqlExecuteSelectForever(strConnectMain, strSelectMain, strErrorFilePath);



        }


        static private void loadModemsTable(int intFacId)
        {
            string strSelectModem = "SELECT [ModemID] ,[FacilityID] ,[ModemName] ,[IPAddress] ,[Active] ,[ESN] ,[MACAddress] ,[doorDirectionType],doorTypes,outSvc,outSvcWarn FROM [BridgeMain].[dbo].[Modem] where FacilityId ='" + intFacId + "' and modemtype=2 and active = '2' order by ModemName asc";
            dtModems = henrySqlStuff.execute.sqlExecuteSelectForever(strConnectMain, strSelectModem, strErrorFilePath);

        }//*****loadModemsTables

        //NEED TO MOVE TABLES TO BRIDGE MAIN
        static private void loadRecipients()
        {
            string strSelectRecipients =
                "SELECT enAssign.[appID], enAssign.[emailID],enAddress.emailAddress FROM [BridgeMain].[dbo].[tblEmailNotificationAssignments] enAssign inner join tblEmailNotificationAddresses enAddress on enAssign.emailID = enAddress.emailID and appid = '1000' and active ='true'";
            dtRecipients = henrySqlStuff.execute.sqlExecuteSelectForever(strConnectMain, strSelectRecipients, strErrorFilePath);
        }

        
   


        static protected void sendmail(string strMessage, DateTime dtNow)
        {
            try
            {

                MailAddress from = new MailAddress("admin@bridgetech.net");
            //MailAddress me = new MailAddress("henry@bridgetech.net");
            //MailAddress meHotMail = new MailAddress("h_jimenez_26@hotmail.com");


                // MailAddress to = new MailAddress(emailaddress);

                //MailAddress ian = new MailAddress("ian@bridgetech.net");

                //NEED TO ADD HENRY AND IAN TO TABLE
                //for (int intRecipientCount = 0;
                //    intRecipientCount < dtRecipients.Rows.Count;
                //    intRecipientCount++) //for each recipient in table
                //{
                    //MailAddress to = new MailAddress(dtRecipients.Rows[intRecipientCount]["emailAddress"].ToString());
                    MailAddress to = new MailAddress("warren@bridgetech.net");
                    MailMessage msgContact = new MailMessage(from, to);
                    //msgContact.Bcc.Add(me);
                    //msgContact.CC.Add(meHotMail);

                    msgContact.Subject = "admin@bridgetech.net test ";
                    msgContact.IsBodyHtml = true;


                    //  Attachment inlineLogo = new Attachment("bridge.png");
                    //   msgContact.Attachments.Add(inlineLogo);
                    //    string contentID = "Image";
                    //     inlineLogo.ContentId = contentID;
                    //    inlineLogo.ContentDisposition.Inline = true;
                    //    inlineLogo.ContentDisposition.DispositionType = DispositionTypeNames.Inline;
                    msgContact.Body = "<center></center><br/><br/>" + strMessage;

                    //  msgContact.CC.Add(ian);
                    SmtpClient client = new SmtpClient("smtpout.secureserver.net");


                    client.Credentials = new System.Net.NetworkCredential("admin@bridgetech.net", "3epHar83agachiWrodUtU@=3r7zitu");
                    client.EnableSsl = true;
                    client.Port = 587;


                    //   client2.Port = 587;

                    client.Send(msgContact);


                    msgContact.Dispose();

                    //string strUpdateRecLog = "INSERT INTO tblReportRecipientsLog VALUES('" + dtRecipients.Rows[intRecipientCount]["Recipient"].ToString() + "', '" + dtNow.Date.ToString("d") + "', '" + dtNow.ToShortTimeString() + "', '" + "Database Check Report')";
                    //henrySqlStuff.execute.sqlExecuteSelectForever(strConnectEWR, strUpdateRecLog, strErrorFilePath);
                //}
            
        }
        catch (Exception e)
        {
          excHandler.logError(e,"sendmail",0);
        }
} //


        static protected void sendmail2(string emailaddress, string strMessage)
        {

            MailAddress from = new MailAddress("admin@bridgetech.net");
            MailAddress to = new MailAddress(emailaddress);
            MailAddress me = new MailAddress("henry@bridgetech.net");
            MailAddress meHotMail = new MailAddress("h_jimenez_26@hotmail.com");

            MailMessage msgContact = new MailMessage(from, to);
            msgContact.Bcc.Add(me);
            msgContact.CC.Add(meHotMail);


            msgContact.Subject = "admin@bridgetech.net test ";
            msgContact.IsBodyHtml = true;
            msgContact.Body = strMessage;

        
            SmtpClient client = new SmtpClient("smtpout.secureserver.net");

          
            client.Credentials = new System.Net.NetworkCredential("admin@bridgetech.net", "3epHar83agachiWrodUtU@=3r7zitu");
            client.EnableSsl = true;
            client.Port = 587;


            //   client2.Port = 587;

            client.Send(msgContact);



        }



        static private string addHTMLRowData(int intTracker, DataTable dtModems, int intModemCnt, int intDTErrors, CountResults countResults,DateTime? dtTimestamp,bool? boolRebooted)
        {
            string strRow;
            string strLine = "";

            if (boolRebooted==true)//verified reboot in dbase
            {
                strLine = "<td align = \"center\">" + dtTimestamp + "</td>"; //rebooted time

            }
            else
            {
              strLine =  "<td style=\"color:red\" align = \"center\">" + dtTimestamp + " Unverified"+ "</td>"; //rebooted time
            }

            if (intTracker % 2 == 0)
            {
                strRow =
                    "<tr bgcolor=\"eeeeee\">" +
                    "<td align = \"center\"><strong>" + dtModems.Rows[intModemCnt]["modemname"] + "</td>" + //name
                    "<td align = \"center\">" + countResults.d1Offs + "</td>" + //d1 offs
                    "<td align = \"center\">" + countResults.d1Ons + "</td>" + //d1 ons
                    "<td align = \"center\">" + countResults.d2Offs + "</td>" + //d2 offs
                    "<td align = \"center\">" + countResults.d2Ons + "</td>" + //d2 ons
                    "<td align = \"center\">" + countResults.d3Offs + "</td>" + //d3 offs 
                    "<td align = \"center\">" + countResults.d3Ons + "</td>" + //d3 ons
                    "<td align = \"center\">" + countResults.totalOffs + "</td>" + //total offs
                    "<td align = \"center\">" + countResults.totalOns + "</td>"  + //total ons
                    "<td align = \"center\">" + countResults.onOffRatio + "</td>" + //On/off ratio
                     "<td align = \"center\">" + countResults.d1maxOffs + "</td>" + //d1 max offs
                     "<td align = \"center\">" + countResults.d1maxOns + "</td>" + //d1 max ons
                     "<td align = \"center\">" + countResults.d2maxOffs + "</td>" + //d2 max offs
                     "<td align = \"center\">" + countResults.d2maxOns + "</td>" + //d2 max ons
                     "<td align = \"center\">" + countResults.d3maxOffs + "</td>" + //d3 max offs
                     "<td align = \"center\">" + countResults.d3maxOns + "</td>";//d3 max ons

            }
            else
            {
                strRow =
                    "<tr>" +
                     "<td align = \"center\"><strong>" + dtModems.Rows[intModemCnt]["modemname"] + "</td>" + //name
                    "<td align = \"center\">" + countResults.d1Offs + "</td>" + //d1 offs
                    "<td align = \"center\">" + countResults.d1Ons + "</td>" + //d1 ons
                    "<td align = \"center\">" + countResults.d2Offs + "</td>" + //d2 offs
                    "<td align = \"center\">" + countResults.d2Ons + "</td>" + //d2 ons
                    "<td align = \"center\">" + countResults.d3Offs + "</td>" + //d3 offs
                    "<td align = \"center\">" + countResults.d3Ons + "</td>" + //d3 ons
                    "<td align = \"center\">" + countResults.totalOffs + "</td>" + //total offs
                    "<td align = \"center\">" + countResults.totalOns + "</td>" + //total ons
                    "<td align = \"center\">" + countResults.onOffRatio + "</td>" + //On/off ratio
                     "<td align = \"center\">" + countResults.d1maxOffs + "</td>" + //d1 max offs
                     "<td align = \"center\">" + countResults.d1maxOns + "</td>" + //d1 max ons
                     "<td align = \"center\">" + countResults.d2maxOffs + "</td>" + //d2 max offs
                     "<td align = \"center\">" + countResults.d2maxOns + "</td>" + //d2 max ons
                     "<td align = \"center\">" + countResults.d3maxOffs + "</td>" + //d3 max offs
                     "<td align = \"center\">" + countResults.d3maxOns + "</td>";//d3 max ons

            }
            return strRow;
        }


        static private string addHTMLRowNoData(int intTracker, int intModemCnt, DataTable dtModems, DateTime? dtTimestamp, bool? boolRebooted)
        {

            string strRow;
            string strLine = "";

            if (boolRebooted == true)//verified reboot in dbase
            {
                strLine = "<td align = \"center\">" + dtTimestamp + "</td>"; //rebooted time

            }
            else
            {
                strLine = "<td style=\"color:red\" align = \"center\">" + dtTimestamp + " Unverified" + "</td>"; //rebooted time
            }

            strRow =
                "<tr bgcolor=\"ffcccc\">" +
                "<td align = \"center\"><strong>" + dtModems.Rows[intModemCnt]["modemname"] + "</td>" + //name
                "<td align = \"center\">" + 0 + "</td>" + //d1 offs
                "<td align = \"center\">" + 0 + "</td>" + //d1 ons
                "<td align = \"center\">" + 0 + "</td>" + //d2 offs
                "<td align = \"center\">" + 0 + "</td>" + //d2 ons
                "<td align = \"center\">" + 0 + "</td>" + //d3 offs
                "<td align = \"center\">" + 0 + "</td>" + //d3 ons
                "<td align = \"center\"><font color=\"red\"></font></td>" + //offs
                "<td align = \"center\"><font color=\"red\"></font></td>" + //ons
                "<td align = \"center\">" + 0 + "</td>" + //On/off ratio
                 "<td align = \"center\">" + 0 + "</td>" + //d1 max offs
                 "<td align = \"center\">" + 0 + "</td>" + //d2 max ons
                 "<td align = \"center\">" + 0 + "</td>" + //d2 max offs
                 "<td align = \"center\">" + 0 + "</td>" + //d2 max ons
                 "<td align = \"center\">" + 0 + "</td>" + //d3 max offs
                 "<td align = \"center\">" + 0 + "</td>";//d3 max ons


            return strRow;

        }


    }

    public class CountResults
    {
        public CountResults()
        {
            // no op
        }

        public int d1Offs { get; set; }
        public int d2Offs { get; set; }
        public int d3Offs { get; set; }
        public int d1Ons { get; set; }
        public int d2Ons { get; set; }
        public int d3Ons { get; set; }

        public int totalOffs { get; set; }
        public int totalOns { get; set; }
        public float onOffRatio { get; set; }

        public int d1maxOffs { get; set; }
        public int d2maxOffs { get; set; }
        public int d3maxOffs { get; set; }
        public int d1maxOns { get; set; }
        public int d2maxOns { get; set; }
        public int d3maxOns { get; set; }

    }
}
