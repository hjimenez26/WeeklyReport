using System;
using System.Data;
using System.Net.Mail;
using System.Text;
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
        static private ExcHandler excHandler;

        static private DateTime dtStartDate;

        static private DateTime dtEndDate;



        static void Main(string[] args)
        {
            start();

            excHandler = new ExcHandler(strConnectMain, strErrorFilePath, 1, intAppID);
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
                    strb.AppendLine("<center><table cellpadding=\"3\" style =\"border-collapse:collapse; width:75%; empty-cells:hide\" border='1'>" +
                        "<thead>" +
                            "<tr>" +
                                "<th style=\"padding:10px\" scope = \"col\">Name</th>" +
                                "<th  scope = \"col\">Door 1 Off</th>" +
                                "<th  scope = \"col\">Door 1 On</th>" +
                                "<th  scope = \"col\">Door 2 Off</th>" +
                                "<th  scope = \"col\">Door 2 On</th>" +
                                "<th  scope = \"col\">Door 3 Off</th>" +
                                "<th  scope = \"col\">Door 3 On</th>" +
                                "<th  scope = \"col\">Door 4 Off</th>" +
                                "<th  scope = \"col\">Door 4 On</th>" +
                                "<th  scope = \"col\">Door 5 Off</th>" +
                                "<th  scope = \"col\">Door 5 On</th>" +
                                "<th  scope = \"col\">Door 6 Off</th>" +
                                "<th  scope = \"col\">Door 6 On</th>" +
                                "<th  scope = \"col\">Total Off</th>" +
                                "<th  scope = \"col\">Total On</th>" +
                                "<th  scope = \"col\">Ratio ON/OFF</th>" +
                                "<th  scope = \"col\">Door 1 Max Off</th>" +
                                "<th  scope = \"col\">Door 1 Max On</th>" +
                                "<th  scope = \"col\">Door 2 Max Off</th>" +
                                "<th  scope = \"col\">Door 2 Max On</th>" +
                                "<th  scope = \"col\">Door 3 Max Off</th>" +
                                "<th  scope = \"col\">Door 3 Max On</th>" +
                                "<th  scope = \"col\">Door 4 Max Off</th>" +
                                "<th  scope = \"col\">Door 4 Max On</th>" +
                                "<th  scope = \"col\">Door 5 Max Off</th>" +
                                "<th  scope = \"col\">Door 5 Max On</th>" +
                                "<th  scope = \"col\">Door 6 Max Off</th>" +
                                "<th  scope = \"col\">Door 6 Max On</th>" +
                            "</tr>" +
                        "</thead>" +
                        "<tbody>"
                        );


                        for (int intModemCnt = 0; intModemCnt < dtModems.Rows.Count; intModemCnt++)     //each modem
                        {
                            Boolean bolRowWithNoData = false;

                            CountResults countResults = getModemCounts(dtFacilities.Rows[intFacCnt]["connectString"].ToString(), (int)dtModems.Rows[intModemCnt]["modemid"], dtStartDate, dtEndDate);
                            if (countResults.totalOffs != -999 && countResults.totalOns != -999 && !(countResults.totalOffs == 0 && countResults.totalOns == 0) && ((countResults.onOffRatio * 100) >= 95))//has data
                            {
                            // accurate data
                                int intDTErrors = getDateTimeErrors(dtStartDate, dtEndDate, (int)dtModems.Rows[intModemCnt]["modemid"], dtFacilities.Rows[intFacCnt]["connectString"].ToString());
                                var varICU6 = ParseICU6Modem((int)dtModems.Rows[intModemCnt]["modemid"]);

                                strb.AppendLine(addHTMLRowData(intTracker, dtModems, intFacId, intModemCnt, intDTErrors, countResults, varICU6.dtTimeStamp, varICU6.boolInDB));

                                intOffSum = intOffSum + countResults.totalOffs;     //compute total offs for all modems in facility
                                intOnSum = intOnSum + countResults.totalOns;        //compute total son for all modems in facility

                            } else if (countResults.totalOffs != -999 && countResults.totalOns != -999 && !(countResults.totalOffs == 0 && countResults.totalOns == 0) && ((countResults.onOffRatio * 100)) < 95) {
                            // inaccurate data
                                int intDTErrors = getDateTimeErrors(dtStartDate, dtEndDate, (int)dtModems.Rows[intModemCnt]["modemid"], dtFacilities.Rows[intFacCnt]["connectString"].ToString());
                                var varICU6 = ParseICU6Modem((int)dtModems.Rows[intModemCnt]["modemid"]);

                                strb.AppendLine(addHTMLRowInnacurateData(intTracker, dtModems, intFacId, intModemCnt, intDTErrors, countResults, varICU6.dtTimeStamp, varICU6.boolInDB));

                                intOffSum = intOffSum + countResults.totalOffs;
                                intOnSum = intOnSum + countResults.totalOns;

                            } else {
                            // no data
                                bolRowWithNoData = true;
                                var varICU6 = ParseICU6Modem((int)dtModems.Rows[intModemCnt]["modemid"]);
                                strb.AppendLine(addHTMLRowNoData(intTracker, intModemCnt, intFacId, dtModems, varICU6.dtTimeStamp, varICU6.boolInDB));
                            }

                            string strModemFix = getModemFixstring(dtStartDate, dtEndDate, (int)dtModems.Rows[intModemCnt]["modemid"],
                            dtFacilities.Rows[intFacCnt]["connectString"].ToString(), bolRowWithNoData);

                            intTracker++;

                        }//for modems
                        
                        strb.AppendLine("</tbody></table></center>");
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

                        strbFacs.AppendLine("<center><br><br><span style='font-weight:bold'><a href='http://btapi.net/berthDataSummary.aspx?facid=" + intFacId + "'>Summary</a><br><br>Total Offs: " + intOffSum + "<br>Total Ons: " + intOnSum);// + "<br>Accuracy: " + fltPercentage + "</span></center><br/><br><hr>");
                        if (fltPercentage < 96.5)
                        {
                            strbFacs.AppendLine("<br>Accuracy %: <font color = \"red\">" + fltPercentage + "</font></span></center><br/><br><hr>");
                        }
                        else
                        {
                            strbFacs.AppendLine("<br>Accuracy %: " + fltPercentage + "</span></center><br/><br><hr>");
                        }

                }//for facilities


                string strReport = strbFacs.ToString();

                sendmailAdminATbt3ck("", strReport);

                Console.WriteLine("Complete.");
            }
            catch (Exception e)
            {
                excHandler.logError(e, "start", 0);
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
                    dtTimestamp = (DateTime?)dr[0]["TimeSent"];
                    boolInDB = (bool?)dr[0]["boolDBVerified"];
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
                excHandler.logError(e, "ParseICU6Modem", 0);
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
            Int16 d4OffCount = 0;
            Int16 d5OffCount = 0;
            Int16 d6OffCount = 0;
            Int16 d1OnsCount = 0;
            Int16 d2OnsCount = 0;
            Int16 d3OnsCount = 0;
            Int16 d4OnsCount = 0;
            Int16 d5OnsCount = 0;
            Int16 d6OnsCount = 0;

            float onOffRatio = 0;

            Int16 d1MaxOffs = 0;
            Int16 d2MaxOffs = 0;
            Int16 d3MaxOffs = 0;
            Int16 d4MaxOffs = 0;
            Int16 d5MaxOffs = 0;
            Int16 d6MaxOffs = 0;
            Int16 d1MaxOns = 0;
            Int16 d2MaxOns = 0;
            Int16 d3MaxOns = 0;
            Int16 d4MaxOns = 0;
            Int16 d5MaxOns = 0;
            Int16 d6MaxOns = 0;

            Int16 offsTotal = 0;
            Int16 onsTotal = 0;

            if (!dtModemPCCountSummary.Rows[0].IsNull("d1offs"))
            {
                d1OffCount = Convert.ToInt16(dtModemPCCountSummary.Rows[0]["d1offs"]);
            }
            if (!dtModemPCCountSummary.Rows[0].IsNull("d2offs"))
            {
                d2OffCount = Convert.ToInt16(dtModemPCCountSummary.Rows[0]["d2offs"]);
            }
            if (!dtModemPCCountSummary.Rows[0].IsNull("d3offs"))
            {
                d3OffCount = Convert.ToInt16(dtModemPCCountSummary.Rows[0]["d3offs"]);
            }
            if (!dtModemPCCountSummary.Rows[0].IsNull("d4offs"))
            {
                d4OffCount = Convert.ToInt16(dtModemPCCountSummary.Rows[0]["d4offs"]);
            }
            if (!dtModemPCCountSummary.Rows[0].IsNull("d5offs"))
            {
                d5OffCount = Convert.ToInt16(dtModemPCCountSummary.Rows[0]["d5offs"]);
            }
            if (!dtModemPCCountSummary.Rows[0].IsNull("d6offs"))
            {
                d6OffCount = Convert.ToInt16(dtModemPCCountSummary.Rows[0]["d6offs"]);
            }
            if (!dtModemPCCountSummary.Rows[0].IsNull("d1ons"))
            {
                d1OnsCount = Convert.ToInt16(dtModemPCCountSummary.Rows[0]["d1ons"]);
            }
            if (!dtModemPCCountSummary.Rows[0].IsNull("d2ons"))
            {
                d2OnsCount = Convert.ToInt16(dtModemPCCountSummary.Rows[0]["d2ons"]);
            }
            if (!dtModemPCCountSummary.Rows[0].IsNull("d3ons"))
            {
                d3OnsCount = Convert.ToInt16(dtModemPCCountSummary.Rows[0]["d3ons"]);
            }
            if (!dtModemPCCountSummary.Rows[0].IsNull("d4ons"))
            {
                d4OnsCount = Convert.ToInt16(dtModemPCCountSummary.Rows[0]["d4ons"]);
            }
            if (!dtModemPCCountSummary.Rows[0].IsNull("d5ons"))
            {
                d5OnsCount = Convert.ToInt16(dtModemPCCountSummary.Rows[0]["d5ons"]);
            }
            if (!dtModemPCCountSummary.Rows[0].IsNull("d6ons"))
            {
                d6OnsCount = Convert.ToInt16(dtModemPCCountSummary.Rows[0]["d6ons"]);
            }


            if (!dtModemPCCountSummary.Rows[0].IsNull("m1off"))
            {
                d1MaxOffs = Convert.ToInt16(dtModemPCCountSummary.Rows[0]["m1off"]);
            }
            else
            {
                d1MaxOffs = -999;
            }
            if (!dtModemPCCountSummary.Rows[0].IsNull("m2off"))
            {
                d2MaxOffs = Convert.ToInt16(dtModemPCCountSummary.Rows[0]["m2off"]);
            }
            else
            {
                d2MaxOffs = -999;
            }
            if (!dtModemPCCountSummary.Rows[0].IsNull("m3off"))
            {
                d3MaxOffs = Convert.ToInt16(dtModemPCCountSummary.Rows[0]["m3off"]);
            }
            else
            {
                d3MaxOffs = -999;
            }
            if (!dtModemPCCountSummary.Rows[0].IsNull("m4off"))
            {
                d4MaxOffs = Convert.ToInt16(dtModemPCCountSummary.Rows[0]["m4off"]);
            }
            else
            {
                d4MaxOffs = -999;
            }
            if (!dtModemPCCountSummary.Rows[0].IsNull("m5off"))
            {
                d5MaxOffs = Convert.ToInt16(dtModemPCCountSummary.Rows[0]["m5off"]);
            }
            else
            {
                d5MaxOffs = -999;
            }
            if (!dtModemPCCountSummary.Rows[0].IsNull("m6off"))
            {
                d6MaxOffs = Convert.ToInt16(dtModemPCCountSummary.Rows[0]["m6off"]);
            }
            else
            {
                d6MaxOffs = -999;
            }



            if (!dtModemPCCountSummary.Rows[0].IsNull("m1on"))
            {
                d1MaxOns = Convert.ToInt16(dtModemPCCountSummary.Rows[0]["m1on"]);
            }
            else
            {
                d1MaxOns = -999;
            }
            if (!dtModemPCCountSummary.Rows[0].IsNull("m2on"))
            {
                d2MaxOns = Convert.ToInt16(dtModemPCCountSummary.Rows[0]["m2on"]);
            }
            else
            {
                d2MaxOns = -999;
            }
            if (!dtModemPCCountSummary.Rows[0].IsNull("m3on"))
            {
                d3MaxOns = Convert.ToInt16(dtModemPCCountSummary.Rows[0]["m3on"]);
            }
            else
            {
                d3MaxOns = -999;
            }
            if (!dtModemPCCountSummary.Rows[0].IsNull("m4on"))
            {
                d4MaxOns = Convert.ToInt16(dtModemPCCountSummary.Rows[0]["m4on"]);
            }
            else
            {
                d4MaxOns = -999;
            }
            if (!dtModemPCCountSummary.Rows[0].IsNull("m5on"))
            {
                d5MaxOns = Convert.ToInt16(dtModemPCCountSummary.Rows[0]["m5on"]);
            }
            else
            {
                d5MaxOns = -999;
            }
            if (!dtModemPCCountSummary.Rows[0].IsNull("m6on"))
            {
                d6MaxOns = Convert.ToInt16(dtModemPCCountSummary.Rows[0]["m6on"]);
            }
            else
            {
                d6MaxOns = -999;
            }



            if (d6OffCount != -999 && d5OffCount != -999 && d4OffCount != -999 && d3OffCount != -999 && d2OffCount != -999 && d1OffCount != -999)
            {
                offsTotal = Convert.ToInt16(d1OffCount + d2OffCount + d3OffCount + d4OffCount + d5OffCount + d6OffCount);
            }
            else
            {
                offsTotal = -999;
            }
            if (d6OnsCount != -999 && d5OnsCount != -999 && d4OnsCount != -999 && d3OnsCount != -999 && d2OnsCount != -999 && d1OnsCount != -999)
            {
                onsTotal = Convert.ToInt16(d1OnsCount + d2OnsCount + d3OnsCount + d4OnsCount + d5OnsCount + d6OnsCount);
            }
            else
            {
                onsTotal = -999;
            }

            if (offsTotal != -999 && onsTotal != -999 && offsTotal != 0 && onsTotal != 0)
            {
                if (onsTotal > offsTotal)
                {
                    onOffRatio = onsTotal / Convert.ToSingle(offsTotal);
                }
                else
                {
                    onOffRatio = (offsTotal / Convert.ToSingle(onsTotal)) * -1;
                }
            }

            countResults.d1Offs = d1OffCount;
            countResults.d2Offs = d2OffCount;
            countResults.d3Offs = d3OffCount;
            countResults.d4Offs = d4OffCount;
            countResults.d5Offs = d5OffCount;
            countResults.d6Offs = d6OffCount;
            countResults.d1Ons = d1OnsCount;
            countResults.d2Ons = d2OnsCount;
            countResults.d3Ons = d3OnsCount;
            countResults.d4Ons = d4OnsCount;
            countResults.d5Ons = d5OnsCount;
            countResults.d6Ons = d6OnsCount;

            countResults.d1maxOffs = d1MaxOffs;
            countResults.d2maxOffs = d2MaxOffs;
            countResults.d3maxOffs = d3MaxOffs;
            countResults.d4maxOffs = d4MaxOffs;
            countResults.d5maxOffs = d5MaxOffs;
            countResults.d6maxOffs = d6MaxOffs;
            countResults.d1maxOns = d1MaxOns;
            countResults.d2maxOns = d2MaxOns;
            countResults.d3maxOns = d3MaxOns;
            countResults.d4maxOns = d4MaxOns;
            countResults.d5maxOns = d5MaxOns;
            countResults.d6maxOns = d6MaxOns;

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
                dtICU6 = henrySqlStuff.execute.sqlExecuteSelectForever(strConnectEWR, strSelect, strErrorFilePath);


            }
            catch (Exception e)
            {
                excHandler.logError(e, "GetTblICU6Data", 0);
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
            string anotherString = "select sum(d1off) as d1offs,sum(d2off) as d2offs,sum(d3off) as d3offs, sum(d4off) as d4offs, sum(d5off) as d5offs, sum(d6off) as d6offs, sum(d1on) as d1ons,sum(d2on) as d2ons,sum(d3on) as d3ons, sum(d4on) as d4ons, sum(d5on) as d5ons, sum(d6on) as d6ons, max(d1off) as m1off,max(d2off) as m2off,max(d3off) as m3off, max(d4off) as m4off, max(d5off) as m5off, max(d6off) as m6off, max(d1on) as m1on,max(d2on) as m2on,max(d3on) as m3on, max(d4on) as m4on, max(d5on) as m5on, max(d6on) as m6on  FROM [dbo].[tblPeopleCount6] where modemid =" +
                    intModemId + " and gpsdatetime > '" + dtBegin + "' and gpsdatetime <'" + dtEnd + "'";

            DataTable dt = henrySqlStuff.execute.sqlExecuteSelectForever(strConnect, anotherString, strErrorFilePath);

            return dt;
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


                    if (strStatus == "32")  //only record type 32
                    {
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
            DataTable dt = henrySqlStuff.execute.sqlExecuteSelectForever(strFacConnect, strSelect, strErrorFilePath);
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


        static protected void sendmailAdminATbt3ck(string emailaddress, string strMessage)
        {

            MailAddress from = new MailAddress("admin@bt3ck.com");
            MailAddress to = new MailAddress("warren@bridgetech.net");
            MailAddress me = new MailAddress("warren@bridgetech.net");
            MailAddress meHotMail = new MailAddress("h_jimenez_26@hotmail.com");

            MailMessage msgContact = new MailMessage(from, to);
            msgContact.Bcc.Add(me);
            msgContact.CC.Add(meHotMail);


            msgContact.Subject = "admin@bt3ck.com test ";
            msgContact.IsBodyHtml = true;
            msgContact.Body = strMessage;


            SmtpClient client = new SmtpClient("smtpout.secureserver.net");


            client.Credentials = new System.Net.NetworkCredential("admin@bt3ck.com", "0+=R_Qu0-?ls2VUPH8d6");
            client.EnableSsl = false;


            client.Port = 587;

            client.Send(msgContact);
        }



        static private string addHTMLRowData(int intTracker, DataTable dtModems, int intFacId, int intModemCnt, int intDTErrors, CountResults countResults, DateTime? dtTimestamp, bool? boolRebooted)
        {
            string strRow;

            if (intTracker % 2 == 0)
            {
                strRow =
                    "<tr bgcolor=\"eeeeee\">" +
                    "<td align = \"center\"><a href='http://btapi.net/berthdata.aspx?facid=" + intFacId + "&mid=" + (int)dtModems.Rows[intModemCnt]["modemid"] + "&yr=" + System.Convert.ToInt16(dtStartDate.Year) + "&month=" + System.Convert.ToInt16(dtStartDate.Month) + "&day=" + System.Convert.ToInt16(dtStartDate.Day) + "'><span style=\"font-weight:bold\">" + dtModems.Rows[intModemCnt]["modemname"] + "</span></td>" + //bus
                    "<td align = \"center\">" + countResults.d1Offs + "</td>" + //d1 offs
                    "<td align = \"center\">" + countResults.d1Ons + "</td>" + //d1 ons
                    "<td align = \"center\">" + countResults.d2Offs + "</td>" + //d2 offs
                    "<td align = \"center\">" + countResults.d2Ons + "</td>" + //d2 ons
                    "<td align = \"center\">" + countResults.d3Offs + "</td>" + //d3 offs 
                    "<td align = \"center\">" + countResults.d3Ons + "</td>" + //d3 ons
                    "<td align = \"center\">" + countResults.d4Offs + "</td>" + //d4 offs 
                    "<td align = \"center\">" + countResults.d4Ons + "</td>" + //d4 ons
                    "<td align = \"center\">" + countResults.d5Offs + "</td>" + //d5 offs 
                    "<td align = \"center\">" + countResults.d5Ons + "</td>" + //d5 ons
                    "<td align = \"center\">" + countResults.d6Offs + "</td>" + //d6 offs 
                    "<td align = \"center\">" + countResults.d6Ons + "</td>" + //d6 ons
                    "<td align = \"center\">" + countResults.totalOffs + "</td>" + //total offs
                    "<td align = \"center\">" + countResults.totalOns + "</td>" + //total ons
                    "<td align = \"center\">" + countResults.onOffRatio + "</td>" + //On/off ratio
                    "<td align = \"center\">" + countResults.d1maxOffs + "</td>" + //d1 max offs
                    "<td align = \"center\">" + countResults.d1maxOns + "</td>" + //d1 max ons
                    "<td align = \"center\">" + countResults.d2maxOffs + "</td>" + //d2 max offs
                    "<td align = \"center\">" + countResults.d2maxOns + "</td>" + //d2 max ons
                    "<td align = \"center\">" + countResults.d3maxOffs + "</td>" + //d3 max offs
                    "<td align = \"center\">" + countResults.d3maxOns + "</td>" +
                    "<td align = \"center\">" + countResults.d4maxOffs + "</td>" +
                    "<td align = \"center\">" + countResults.d4maxOns + "</td>" +
                    "<td align = \"center\">" + countResults.d5maxOffs + "</td>" +
                    "<td align = \"center\">" + countResults.d5maxOns + "</td>" +
                    "<td align = \"center\">" + countResults.d6maxOffs + "</td>" +
                    "<td align = \"center\">" + countResults.d6maxOns + "</td>" +
                    "</tr>"; ;

            }
            else
            {
                strRow =
                    "<tr>" +
                    "<td align = \"center\"><a href='http://btapi.net/berthdata.aspx?facid=" + intFacId + "&mid=" + (int)dtModems.Rows[intModemCnt]["modemid"] + "&yr=" + System.Convert.ToInt16(dtStartDate.Year) + "&month=" + System.Convert.ToInt16(dtStartDate.Month) + "&day=" + System.Convert.ToInt16(dtStartDate.Day) + "'><span style=\"font-weight:bold\">" + dtModems.Rows[intModemCnt]["modemname"] + "</span></td>" + //bus
                    "<td align = \"center\">" + countResults.d1Offs + "</td>" + //d1 offs
                    "<td align = \"center\">" + countResults.d1Ons + "</td>" + //d1 ons
                    "<td align = \"center\">" + countResults.d2Offs + "</td>" + //d2 offs
                    "<td align = \"center\">" + countResults.d2Ons + "</td>" + //d2 ons
                    "<td align = \"center\">" + countResults.d3Offs + "</td>" + //d3 offs 
                    "<td align = \"center\">" + countResults.d3Ons + "</td>" + //d3 ons
                    "<td align = \"center\">" + countResults.d4Offs + "</td>" + //d4 offs 
                    "<td align = \"center\">" + countResults.d4Ons + "</td>" + //d4 ons
                    "<td align = \"center\">" + countResults.d5Offs + "</td>" + //d5 offs 
                    "<td align = \"center\">" + countResults.d5Ons + "</td>" + //d5 ons
                    "<td align = \"center\">" + countResults.d6Offs + "</td>" + //d6 offs 
                    "<td align = \"center\">" + countResults.d6Ons + "</td>" + //d6 ons
                    "<td align = \"center\">" + countResults.totalOffs + "</td>" + //total offs
                    "<td align = \"center\">" + countResults.totalOns + "</td>" + //total ons
                    "<td align = \"center\">" + countResults.onOffRatio + "</td>" + //On/off ratio
                    "<td align = \"center\">" + countResults.d1maxOffs + "</td>" + //d1 max offs
                    "<td align = \"center\">" + countResults.d1maxOns + "</td>" + //d1 max ons
                    "<td align = \"center\">" + countResults.d2maxOffs + "</td>" + //d2 max offs
                    "<td align = \"center\">" + countResults.d2maxOns + "</td>" + //d2 max ons
                    "<td align = \"center\">" + countResults.d3maxOffs + "</td>" + //d3 max offs
                    "<td align = \"center\">" + countResults.d3maxOns + "</td>" +
                    "<td align = \"center\">" + countResults.d4maxOffs + "</td>" +
                    "<td align = \"center\">" + countResults.d4maxOns + "</td>" +
                    "<td align = \"center\">" + countResults.d5maxOffs + "</td>" +
                    "<td align = \"center\">" + countResults.d5maxOns + "</td>" +
                    "<td align = \"center\">" + countResults.d6maxOffs + "</td>" +
                    "<td align = \"center\">" + countResults.d6maxOns + "</td>" +
                    "</tr>";

            }
            return strRow;
        }

        static private string addHTMLRowInnacurateData(int intTracker, DataTable dtModems, int intFacId, int intModemCnt, int intDTErrors, CountResults countResults, DateTime? dtTimestamp, bool? boolRebooted)
        {

            return "<tr bgcolor=\"ffcccc\">" +
                    "<td align = \"center\"><a href='http://btapi.net/berthdata.aspx?facid=" + intFacId + "&mid=" + (int)dtModems.Rows[intModemCnt]["modemid"] + "&yr=" + System.Convert.ToInt16(dtStartDate.Year) + "&month=" + System.Convert.ToInt16(dtStartDate.Month) + "&day=" + System.Convert.ToInt16(dtStartDate.Day) + "'><span style=\"font-weight:bold\">" + dtModems.Rows[intModemCnt]["modemname"] + "</span></td>" + //bus
                    "<td align = \"center\">" + countResults.d1Offs + "</td>" + //d1 offs
                    "<td align = \"center\">" + countResults.d1Ons + "</td>" + //d1 ons
                    "<td align = \"center\">" + countResults.d2Offs + "</td>" + //d2 offs
                    "<td align = \"center\">" + countResults.d2Ons + "</td>" + //d2 ons
                    "<td align = \"center\">" + countResults.d3Offs + "</td>" + //d3 offs 
                    "<td align = \"center\">" + countResults.d3Ons + "</td>" + //d3 ons
                    "<td align = \"center\">" + countResults.d4Offs + "</td>" + //d4 offs 
                    "<td align = \"center\">" + countResults.d4Ons + "</td>" + //d4 ons
                    "<td align = \"center\">" + countResults.d5Offs + "</td>" + //d5 offs 
                    "<td align = \"center\">" + countResults.d5Ons + "</td>" + //d5 ons
                    "<td align = \"center\">" + countResults.d6Offs + "</td>" + //d6 offs 
                    "<td align = \"center\">" + countResults.d6Ons + "</td>" + //d6 ons
                    "<td align = \"center\">" + countResults.totalOffs + "</td>" + //total offs
                    "<td align = \"center\">" + countResults.totalOns + "</td>" + //total ons
                    "<td align = \"center\">" + countResults.onOffRatio + "</td>" + //On/off ratio
                    "<td align = \"center\">" + countResults.d1maxOffs + "</td>" + //d1 max offs
                    "<td align = \"center\">" + countResults.d1maxOns + "</td>" + //d1 max ons
                    "<td align = \"center\">" + countResults.d2maxOffs + "</td>" + //d2 max offs
                    "<td align = \"center\">" + countResults.d2maxOns + "</td>" + //d2 max ons
                    "<td align = \"center\">" + countResults.d3maxOffs + "</td>" + //d3 max offs
                    "<td align = \"center\">" + countResults.d3maxOns + "</td>" +
                    "<td align = \"center\">" + countResults.d4maxOffs + "</td>" +
                    "<td align = \"center\">" + countResults.d4maxOns + "</td>" +
                    "<td align = \"center\">" + countResults.d5maxOffs + "</td>" +
                    "<td align = \"center\">" + countResults.d5maxOns + "</td>" +
                    "<td align = \"center\">" + countResults.d6maxOffs + "</td>" +
                    "<td align = \"center\">" + countResults.d6maxOns + "</td>" +
                    "</tr>"; ;
        }


        static private string addHTMLRowNoData(int intTracker, int intModemCnt, int intFacId, DataTable dtModems, DateTime? dtTimestamp, bool? boolRebooted)
        {

            string strRow;
            

            strRow =
                "<tr bgcolor=\"ffcccc\">" +
                "<td align = \"center\"><a href='http://btapi.net/berthdata.aspx?facid=" + intFacId + "&mid=" + (int)dtModems.Rows[intModemCnt]["modemid"] + "&yr=" + System.Convert.ToInt16(dtStartDate.Year) + "&month=" + System.Convert.ToInt16(dtStartDate.Month) + "&day=" + System.Convert.ToInt16(dtStartDate.Day) + "'><span style=\"font-weight:bold\">" + dtModems.Rows[intModemCnt]["modemname"] + "</span></td>" + //bus
                "<td align = \"center\">" + 0 + "</td>" + //d1 offs
                "<td align = \"center\">" + 0 + "</td>" + //d1 ons
                "<td align = \"center\">" + 0 + "</td>" + //d2 offs
                "<td align = \"center\">" + 0 + "</td>" + //d2 ons
                "<td align = \"center\">" + 0 + "</td>" + //d3 offs
                "<td align = \"center\">" + 0 + "</td>" + //d3 ons
                "<td align = \"center\">" + 0 + "</td>" +
                "<td align = \"center\">" + 0 + "</td>" +
                "<td align = \"center\">" + 0 + "</td>" +
                "<td align = \"center\">" + 0 + "</td>" +
                "<td align = \"center\">" + 0 + "</td>" +
                "<td align = \"center\">" + 0 + "</td>" +
                "<td align = \"center\"><font color=\"red\"></font></td>" + //offs
                "<td align = \"center\"><font color=\"red\"></font></td>" + //ons
                "<td align = \"center\">" + 0 + "</td>" + //On/off ratio
                "<td align = \"center\">" + 0 + "</td>" + //d1 max offs
                "<td align = \"center\">" + 0 + "</td>" + //d1 max ons
                "<td align = \"center\">" + 0 + "</td>" + //d2 max offs
                "<td align = \"center\">" + 0 + "</td>" + //d2 max ons
                "<td align = \"center\">" + 0 + "</td>" + //d3 max offs
                "<td align = \"center\">" + 0 + "</td>" + //d3 max ons
                "<td align = \"center\">" + 0 + "</td>" +
                "<td align = \"center\">" + 0 + "</td>" +
                "<td align = \"center\">" + 0 + "</td>" +
                "<td align = \"center\">" + 0 + "</td>" +
                "<td align = \"center\">" + 0 + "</td>" +
                "<td align = \"center\">" + 0 + "</td>" +
                "</tr>";

            return strRow;

        }


    }

    public class CountResults
    {
        public CountResults()
        {
            // no op (so we can use auto-gen getter/setter
        }

        public int d1Offs { get; set; }
        public int d2Offs { get; set; }
        public int d3Offs { get; set; }
        public int d4Offs { get; set; }
        public int d5Offs { get; set; }
        public int d6Offs { get; set; }
        public int d1Ons { get; set; }
        public int d2Ons { get; set; }
        public int d3Ons { get; set; }
        public int d4Ons { get; set; }
        public int d5Ons { get; set; }
        public int d6Ons { get; set; }

        public int totalOffs { get; set; }
        public int totalOns { get; set; }
        public float onOffRatio { get; set; }

        public int d1maxOffs { get; set; }
        public int d2maxOffs { get; set; }
        public int d3maxOffs { get; set; }
        public int d4maxOffs { get; set; }
        public int d5maxOffs { get; set; }
        public int d6maxOffs { get; set; }
        public int d1maxOns { get; set; }
        public int d2maxOns { get; set; }
        public int d3maxOns { get; set; }
        public int d4maxOns { get; set; }
        public int d5maxOns { get; set; }
        public int d6maxOns { get; set; }

    }
}
