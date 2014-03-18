/* Copyright 2013 
 * Eran Altay eranaltay@gmail.com
 
   Unless required by applicable law or agreed to in writing, software
   distributed under the License is distributed on an "AS IS" BASIS,
   WITHOUT WARRANTIES OR CONDITIONS OF ANY KIND, either express or implied.
   See the License for the specific language governing permissions and
   limitations under the License.
*/

using System;
using System.Configuration;
using System.IO;
using System.Linq;
using System.Net;
using System.Reflection;
using System.Xml;
using System.Xml.Linq;
using SGECurrency.DAL;
using SGECurrency.Exceptions;
using SGECurrency.Objects;
using SGECurrency.Utilities;
using log4net;

namespace SGECurrency.Managers
{

    /// <summary>
    /// XMLManager- from here we can get the XML and parse.
    /// this class will populate our Dictionary container
    /// this class will also handle errors from the server, and will load the backup of the XML
    ///  </summary>
    public class XMLManager : WebClient
    {
        #region Variables
        // event- invoke event to main window - event of notifing user with MessageBox
        internal static event MessagesToNotifyUserDelegate MessagesToNotifyUserEvent;
        //instance of log4net framework
        private readonly ILog _currencyLogger = LogManager.GetLogger(MethodBase.GetCurrentMethod().DeclaringType);
        //set a timeout for getting the XML, to overcome problematic network
        public static readonly int WebRequestTimeout;
        //BOI URL
        public static readonly string BankOfIsraelURL;
        #endregion

        /// <summary>
        /// Static C-TOR
        /// </summary>
        static XMLManager()
        {
            //set 4 sec timeout
            WebRequestTimeout = Properties.Settings.Default.BOIRequestTimeout;
            //set URL of server
            BankOfIsraelURL = Properties.Settings.Default.BOIURL;
        }


        #region Fetch And Parse XML
        /// <summary>
        /// Method, this manager will invoke the Container Populater
        /// only if there is a newer XML to get, or in the first launch of this program
        /// if there is no newer XML to get, it will finish the exceute of the Async Task
        /// <see cref="CurrencyMainWindow.GetBOIXMLAsynchronously"/>
        /// <returns>object, for unboxing it to another desired variable</returns>
        /// </summary>
        public bool XMLProcess()
        {
            XElement bankOfIsraelXML = default(XElement);
            //var to hold content of XML by string
            string xmlContent = default(string);
            //determin for same XML
            var isSameXML = default(bool);

            try
            {
                //fetch and parse XML
                FetchAndParseBOIXML(out bankOfIsraelXML, out xmlContent);
            }

            catch (CurrencyException currencyException)
            {
                //ERROR occured
                _currencyLogger.Error(currencyException.Message, currencyException);
            }


            finally
            {
                try
                {
                    //check the XML doc for errors, and save the file to main dir
                    //for future restore, on errors that will come up
                    if (bankOfIsraelXML != null && bankOfIsraelXML.Elements().Count()>10)
                    {
                        File.WriteAllText(new DirectoryInfo
                                              (Path.Combine(Environment.CurrentDirectory,
                                                            CurrencyUtilities.XMLFileBackup))
                                              .ToString(), xmlContent);
                    }

                    //if the doc is corrupted, or doc is not in the right syntax, ot there is a problem in connection
                    //get the last saved XML doc from main dir
                    else
                    {
                        //get the XML Information from the backup XML
                        bankOfIsraelXML =
                            XElement.Parse(
                                File.ReadAllText(
                                    Path.Combine(Environment.CurrentDirectory, CurrencyUtilities.XMLFileBackup)));
                        _currencyLogger.Info("Currency data will be load from backup file");
                        
                    }

                    //get the time of 'LAST_UPDATE' element from XML file
                    //and compare it with existing XML date
                    //to avoid getting the same XML with no change
                    // ReSharper disable PossibleNullReferenceException
                    if (CurrencyUtilities.IsXMLDatesEquals(bankOfIsraelXML.Element("LAST_UPDATE").Value))
                        isSameXML = true;

                    //else, there is newer XML, or this is the first launch of the program, invoke the PopulateContainer
                    //and get new elements
                    else
                    {
                        PopulateContainer(bankOfIsraelXML);
                        _currencyLogger.Info("Populating Container - new Data Fetched");
                        //'assign' state, if we get down to here, 'isSameXML' will be as it's default value - false
                        //so assigning 'isSameXML' is redundate and useless.
                    }
                }

                //if no backup file found, exit the program and notify the user about it
                catch (FileNotFoundException fileNotFoundException)
                {
                    MessagesToNotifyUserEvent.Invoke(
                        @"No Backup File Found, This Program Will Abort,
                        Try Again When You Will Have A Valid Connection To The Internet");
                    _currencyLogger.Fatal("Backup File Wasn't Found", fileNotFoundException);
                    //exit program
                    Environment.Exit(0);
                }

            }

            //true for same XML we have now on program
            //false for newer XML or, on start of the program
            return isSameXML;
        }

        /// <summary>
        /// FetchAndParseBOIXML - get the XML from BOI and parse it
        /// </summary>
        public void FetchAndParseBOIXML(out XElement bankOfIsraelXML, out string xmlContent)
        {
            try
            {
                //downloading XML as string
                xmlContent = DownloadString(new Uri(BankOfIsraelURL));
                //parse the document
                bankOfIsraelXML = XElement.Parse(xmlContent);
            }

             //if there is a problem at the connection, timeout...
            catch (WebException webException)
            {
                MessagesToNotifyUserEvent(@"Due To Network Problem, The Latest Currency Data Will Be Initilized");
                throw new CurrencyException(webException.Message, webException.InnerException);
            }

             // if user clicked the 'update' button, where is already a task to get XML in background
            // so we can avoid concurency.
            catch (NotSupportedException notSupportedException)
            {
                throw new CurrencyException(notSupportedException.Message, notSupportedException.InnerException);
            }
            
             //in some cases XML is fetched without the proper syntax, which may cause fatal errors on parsing
            catch (XmlException xmlException)
            {
                MessagesToNotifyUserEvent("Due To Bank-Of-Israel Service Problems, The Latest Currency Data Will Be Initilized");
                throw new CurrencyException(xmlException.Message, xmlException.InnerException);
            }

        } 
        #endregion

        #region Populate Container- LINQ
        /// <summary>
        /// Method - get the XML elements by LinQ,
        ///  and add to the HybridDictionary container
        /// </summary>
        public void PopulateContainer(XElement currenciesElements)
        {
            //LINQ TO XML
            (from currencyElement in currenciesElements.Descendants("CURRENCY")
             select new CurrencyObject
                 {
                     Name = (string)currencyElement.Element("NAME"),
                     Unit = (int)currencyElement.Element("UNIT"),
                     Code = (string)currencyElement.Element("CURRENCYCODE"),
                     Country = (string)currencyElement.Element("COUNTRY"),
                     Rate = (double)currencyElement.Element("RATE"),
                     Change = (double)currencyElement.Element("CHANGE")
                 })
                //ToList()- to list, enumrable, so we can iterates it
                //ForEach() -iterates the list, and add nodes to container
                //Key - Currency Code - e.g: 'USD'
                //value- Currency node - object        
                .ToList()               
                .ForEach(currencyNode => CurrencyContainer.Instance[currencyNode.Code] = currencyNode);
        }
        
        #endregion

        #region Override Method
        /// <summary>
        /// override method to overcome problematic network, and set a timeout for the XML fetch
        /// so the program will not get stuck on the fetch process
        /// </summary>
        protected override WebRequest GetWebRequest(Uri address)
        {
            //set web request for the server
            var webRequestResult = base.GetWebRequest(address);
            //set the timout for this operation
            webRequestResult.Timeout = WebRequestTimeout;
            //return result from server
            return webRequestResult;
        } 
        #endregion
    }
}
