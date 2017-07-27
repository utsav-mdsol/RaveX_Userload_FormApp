using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;
using System.Management.Automation;
using System.Collections.ObjectModel;
using System.Management.Automation.Runspaces;
using Google.Apis.Auth.OAuth2;
using Google.Apis.Sheets.v4;
using Google.Apis.Services;
using System.IO;
using System.Threading;
using Google.Apis.Util.Store;
using Data = Google.Apis.Sheets.v4.Data;

namespace RaveX_UserLoad_Performance_Utility
{
    public partial class Form1 : Form
    {
        public Form1()
        {
            InitializeComponent();

           
            
           


        }

        private void Submit_Btn_Click(object sender, EventArgs e)
        {

            string startTime = startTimeTextBox.Text;
            string endTime = endTimeTextBox.Text;

            string testTime = string.Concat(startTime, "&to=", endTime);

            string applicationID = "7051128";//6758980
            string timePeriod = "2017-07-05T17:29:00+00:00&to=2017-07-05T18:29:00+00:00";
            string envType = "Gambit";
            string metricsURL = ("https://api.newrelic.com/v2/applications/" + applicationID + "/metrics/data.json?names[]=EndUser/Apdex&from=" + timePeriod + "&summarize=true");


            //For loop for two environment One is Gambit and Checkmate
            for (int i = 0; i <= 1; i++)
            {
                RunspaceConfiguration runspaceConfiguration = RunspaceConfiguration.Create();

                Runspace runspace = RunspaceFactory.CreateRunspace(runspaceConfiguration);
                runspace.Open();

                RunspaceInvoke scriptInvoker = new RunspaceInvoke(runspace);

                Pipeline pipeline = runspace.CreatePipeline();



                //Here's how you add a new script with arguments
                Command myCommand = new Command("C:\\Users\\udhungel\\Desktop\\ps.ps1");
                myCommand.Parameters.Add(new CommandParameter("applicationID", applicationID));
                myCommand.Parameters.Add(new CommandParameter("timePeriod", timePeriod));
                myCommand.Parameters.Add(new CommandParameter("envType", envType));
                //   myCommand.Parameters.Add(testParam);

                pipeline.Commands.Add(myCommand);

                // Execute PowerShell script
                var results = pipeline.Invoke();


                List<dynamic> listsOfMetrics = new List<dynamic>();
                foreach (var listOfMetrics in results)
                {
                    listsOfMetrics.Add(listOfMetrics);
                }

                listsOfMetrics.Add("Test UTC Start Time");
                listsOfMetrics.Add(startTime);
                listsOfMetrics.Add("Test UTC End Time");
                listsOfMetrics.Add(endTime);

                runspace.Close();
                //     RunScript();
                appendEmptyColumn();
                getTagName(listsOfMetrics, i);


                applicationID = "6758980";//6758980
                envType = "Checkmate";

            }

        }

        public static string Base64Encode(string textIn)
        {
            string b = Base64Encode(textIn);
            return textIn;
        }



        //Gets the tag names of the 1st column in the RAVEX reports
        private static string getTagName(List<dynamic> listOfMetrics, int iterate)
        {
            //Spreadsheet ID for the google spreadsheet => form the URL link
            string spreadsheetId = "11QWSoBvPXm6_4NQ9t1xomfTHJSHYtYKd0GQP_qNF6EU";

            //Range to look for the tag names
            string range = "RaveX June 2017 Incremental Perf Test Metrics!A:A";


            SheetsService sheetsService = new SheetsService(new BaseClientService.Initializer
            {
                HttpClientInitializer = GetCredential(),
                ApplicationName = "RaveReportAutomationUtility",
            });



            SpreadsheetsResource.ValuesResource.GetRequest request =
                  sheetsService.Spreadsheets.Values.Get(spreadsheetId, range); //puts an api call to get the items in the row with the provided range

            Data.ValueRange response = request.Execute();

            IList<IList<Object>> values = response.Values; //generic list to store the row name tags

            string colRange = getColumnRange(iterate);

            int rowIndex = 1;

            foreach (var rowTag in values)
            {
                var newrowTag = rowTag.Select(p => p.ToString()).ToList();
                string strRowTag = string.Join("", newrowTag);
                foreach (var tagName in listOfMetrics)
                {
                    string newtagName = tagName.ToString();
                    if (strRowTag.Equals(newtagName))
                    {
                        rowIndex = values.IndexOf(rowTag);

                        string nullColIndexRange = "RaveX June 2017 Incremental Perf Test Metrics!";

                        string finalRange = string.Concat(nullColIndexRange, colRange, rowIndex + 1);

                        int indexOfMetrics = listOfMetrics.IndexOf(tagName) + 1;

                        dynamic metricsValue = listOfMetrics.ElementAt(indexOfMetrics).ToString();
                        if (!strRowTag.Substring(0,8).Equals("Test UTC"))
                        {
                            metricsValue = convertToDouble(metricsValue);
                        }

                        insertNewColumnValue(metricsValue, finalRange);
                    }
                }
            }


            return null;
        }



        private static void insertNewColumnValue(dynamic metrics, string Range)
        {

            // The A1 notation of a range to search for a logical table of data.
            // Values will be appended after the last row of the table.
            //string range = "1 - Performance Test Results";  // TODO: Update placeholder value.


            // string spreadsheetId = spreadsheetIDTextbox.Text; //Impliment this for the input entered in the textbox


            // The ID of the spreadsheet to update.
            string spreadsheetId = "11QWSoBvPXm6_4NQ9t1xomfTHJSHYtYKd0GQP_qNF6EU";  // TODO: Update placeholder value.

            SheetsService sheetsService = new SheetsService(new BaseClientService.Initializer
            {
                HttpClientInitializer = GetCredential(),
                ApplicationName = "RaveReportAutomationUtility",
            });

            Data.ValueRange valueRange = new Data.ValueRange();
            valueRange.MajorDimension = "ROWS";





            // var output = Math.Round(Convert.ToDouble(getOverallWorkflowMetricsData(nameTag, "Response time[s]", "Avg")), 3);
            var oblist = new List<Object>() { metrics };//Try to make

            valueRange.Values = new List<IList<object>> { oblist };


            SpreadsheetsResource.ValuesResource.UpdateRequest update = sheetsService.Spreadsheets.Values.Update(valueRange, spreadsheetId, Range);
            update.ValueInputOption = SpreadsheetsResource.ValuesResource.UpdateRequest.ValueInputOptionEnum.USERENTERED;
            Data.UpdateValuesResponse result = update.Execute();

            /*SpreadsheetsResource.ValuesResource.AppendRequest append = sheetsService.Spreadsheets.Values.Append(valueRange, spreadsheetId, range);
            append.ValueInputOption = SpreadsheetsResource.ValuesResource.AppendRequest.ValueInputOptionEnum.USERENTERED;
            Data.AppendValuesResponse result2 = append.Execute();*/

        }

        private static string getColumnRange(int iterate)
        {
            
            string spreadsheetId = "11QWSoBvPXm6_4NQ9t1xomfTHJSHYtYKd0GQP_qNF6EU";



            string range = "RaveX June 2017 Incremental Perf Test Metrics!A:A";

            SheetsService sheetsService = new SheetsService(new BaseClientService.Initializer
            {
                HttpClientInitializer = GetCredential(),
                ApplicationName = "RaveReportAutomationUtility",
            });


            bool loop = true;
            var startColNum = 1;
            char rangeConcat = 'R';

            while (loop == true)
            {
                string nullColumnIndexRange = "RaveX June 2017 Incremental Perf Test Metrics!"; //Range to be concatinated with
                string rangeStr = rangeConcat.ToString();

                string finalRange = string.Concat(nullColumnIndexRange, rangeStr, 1);//Concatinates the string to produce the range string for the right column to enter the data

                
                string colData = getColumnData(finalRange);//Loops through the google docs sheet to find the 

                if (colData.Equals("null found"))
                {
                    loop = false;
                    if (iterate == 1)
                    {
                        insertNewColumnValue("Test Run #", finalRange);
                    }
                    return rangeConcat.ToString();
                }
                rangeConcat++;
                startColNum++;

            }
            
            return rangeConcat.ToString();
        }

        private static string getColumnData(string range)
        {
            // The ID of the spreadsheet to update.
            string spreadsheetId = "11QWSoBvPXm6_4NQ9t1xomfTHJSHYtYKd0GQP_qNF6EU";  // TODO: Update placeholder value.

            SheetsService sheetsService = new SheetsService(new BaseClientService.Initializer
            {
                HttpClientInitializer = GetCredential(),
                ApplicationName = "RaveReportAutomationUtility",
            });

            SpreadsheetsResource.ValuesResource.GetRequest request =
                   sheetsService.Spreadsheets.Values.Get(spreadsheetId, range);

            Data.ValueRange response = request.Execute();
            IList<IList<Object>> values = response.Values;

            return nullTest(values);


        }

        private static string nullTest(dynamic testVar)
        {

            if (testVar == null)
            {
                return "null found";
            }
            else
            {
                string tagName = testVar.ToString();
                return tagName;
            }
        }

        private static void appendEmptyColumn()
        {

            // The A1 notation of a range to search for a logical table of data.
            // Values will be appended after the last row of the table.
            //   string range = "1 - Performance Test Results";  // TODO: Update placeholder value.
            //"'1 - Performance Test Results'!A:G"
            // The ID of the spreadsheet to update.

            // string spreadsheetId = spreadsheetIDTextbox.Text; //Impliment this for the input entered in the textbox

            string spreadsheetId = "11QWSoBvPXm6_4NQ9t1xomfTHJSHYtYKd0GQP_qNF6EU";  // TODO: Update placeholder value.

            SheetsService sheetsService = new SheetsService(new BaseClientService.Initializer
            {
                HttpClientInitializer = GetCredential(),
                ApplicationName = "RaveReportAutomationUtility",
            });

            Data.Request reqBody = new Data.Request
            {
                AppendDimension = new Data.AppendDimensionRequest
                {
                    SheetId = 920811147,
                    Dimension = "COLUMNS",
                    Length = 1

                }
            };

            List<Data.Request> requests = new List<Data.Request>();

            requests.Add(reqBody);


            // TODO: Assign values to desired properties of `requestBody`:
            Data.BatchUpdateSpreadsheetRequest requestBody = new Data.BatchUpdateSpreadsheetRequest();
            requestBody.Requests = requests;

            SpreadsheetsResource.BatchUpdateRequest request = sheetsService.Spreadsheets.BatchUpdate(requestBody, spreadsheetId);
            //SpreadsheetsResource.BatchUpdateRequest request = sheetsService.Spreadsheets.BatchUpdate(requestBody, spreadsheetId);


            // To execute asynchronously in an async method, replace `request.Execute()` as shown:
            Data.BatchUpdateSpreadsheetResponse response = request.Execute();




        }


        private static dynamic convertToDouble(string inputMetrics)
        {
            if ((inputMetrics != null))
            {
                double metrics_Value = Math.Round(Convert.ToDouble(inputMetrics), 5);
                return metrics_Value;
            }
            return "N/A";
        }

        private static UserCredential GetCredential()
        {

            UserCredential credential;
            string credPath = System.Environment.GetFolderPath(
                    System.Environment.SpecialFolder.Personal);
            credPath = Path.Combine(credPath, ".credentials/sheets.googleapis.com-dotnet-quickstart.json");
            string[] Scopes = { SheetsService.Scope.Drive };

            var stream = new FileStream("client_secret.json", FileMode.Open, FileAccess.Read);

            credential = GoogleWebAuthorizationBroker.AuthorizeAsync(
                        GoogleClientSecrets.Load(stream).Secrets,

                        Scopes,
                        "user",
                        CancellationToken.None,
                        new FileDataStore(credPath, true)).Result;
            Console.WriteLine("Credential file saved to: " + credPath);
            stream.Close();
            return credential;






        }



        private void textBox1_TextChanged(object sender, EventArgs e)
        {

            /*    using (PowerShell PowerShellInstance = PowerShell.Create())
              {
                  Dictionary<string , string> headers = new Dictionary<string,string>();

                  headers.Add("x-api-key", "ee3893a29097220baaf31fbf556e57a9f581bd89649f575");
                  headers.Add("AUTHORIZATION", "udhungel:Manchesterutd1");

                  string applicationID = "77321";
                  string timePeriod = "2017-07-05T17:10:37+00:00&to=2017-07-05T18:10:00+00:00";
                  string metricsURL = ("https://api.newrelic.com/v2/applications/"+applicationID+"/metrics/data.json?names[]=EndUser/Apdex&from="+timePeriod+"&summarize=true");
                  var data = PowerShellInstance.AddScript("Invoke-RestMethod -headers \""+headers+"\" -Method Get -uri \""+metricsURL+"\"");
               //   data = PowerShellInstance.AddScript();
                  var results = PowerShellInstance.Invoke();
                  string appdexScore = data+".metric_data.metrics.timeslices.values.score";


                  List<dynamic> lista = new List<dynamic>();
                  foreach (var psobj in results)
                  {
                      lista.Add(psobj);
                  }

                  PowerShellInstance.AddScript(appdexScore);



                //  $listOfMetrics += @(("Browser Apdex(Gambit)"),$appdexScore)

              }*/
        }


    }
}





