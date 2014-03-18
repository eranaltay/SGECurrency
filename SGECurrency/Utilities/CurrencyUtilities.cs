/* Copyright 2013 
 * Eran Altay eranaltay@gmail.com
 
   Unless required by applicable law or agreed to in writing, software
   distributed under the License is distributed on an "AS IS" BASIS,
   WITHOUT WARRANTIES OR CONDITIONS OF ANY KIND, either express or implied.
   See the License for the specific language governing permissions and
   limitations under the License.
*/

using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Configuration;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Windows.Media;
using SGECurrency.DAL;
using SGECurrency.Objects;

namespace SGECurrency.Utilities
{
    /// <summary>
    /// this class have some util functions, sort of 'Logistic' stuff, 
    /// populate the flags pictures,
    /// validate for result of calculations numbers
    /// some extension method for CurrencyObject
    /// check the value of the currencies date of the XML to avoid fetching the same XML
    /// </summary>
    static public class CurrencyUtilities
    {
        #region Constants
        //backup file XML
        public static readonly string XMLFileBackup = Properties.Settings.Default.BackupFile;
        // Flags Images dir
        public static readonly string FlagsImagesDirectory = Properties.Settings.Default.FlagsDir;
        //images dir, arrrows for change rate
        public static readonly string AuxImagesDirectory = Properties.Settings.Default.AuxImagesDir;
        //supported extension for pictures
        public static readonly string SupportedExtensionFlagsImages = Properties.Settings.Default.FlagsFilesExtension;
        //observable collection
        public static ObservableCollection<CurrencyObject> RatesGridObservableCollection 
            = new ObservableCollection<CurrencyObject>();


        static CurrencyUtilities()
        {
            ConfigurationManager.RefreshSection("applicationSettings");
        }

        /// <summary>
        /// A lookup container to hold the flags images for the combo boxes
        /// </summary>
        public static ILookup<string, ImageSource> FlagsImages { get; set; }
        #endregion

        #region Flags Container Utility
        /// <summary>
        /// Method - will get the files name of the .ico dir (files names without extensions)
        /// </summary>
        public static void PopulateFlagImagesContainer()
        {
            FlagsImages = new DirectoryInfo(Path.Combine(Environment.CurrentDirectory, FlagsImagesDirectory))
                //get the files from the '~\CurrencyConverter\CurrencyConverter\bin\Debug\ico'
                                           .GetFiles(SupportedExtensionFlagsImages, SearchOption.TopDirectoryOnly)
                //convert the images files collection to Lookup collection 
                // Key - e.g: split - From 'Dollar-AUD.png' to 'Dollar-AUD'
                //value - e.g: '~/Dollar-AUD.png' - object of the file - a ref to image file.
                                           .ToLookup(key => key.Name.Split('.')[0],
                                            value => new ImageSourceConverter().ConvertFromString(value.FullName) as ImageSource);
        }
        #endregion

        #region Currency Object Extension Functions
        /// <summary>
        /// Extension Method
        /// set image of currency change status, for negative change, we will get sign with down arrow
        /// <returns>ImageSource</returns>
        /// </summary>
        public static ImageSource SetImageByCurrencyChangeRate(this CurrencyObject currencyObject)
        {
            if (currencyObject.Change > 0)
                return new ImageSourceConverter().ConvertFromString(Path.Combine(Environment.CurrentDirectory,
                                                                    AuxImagesDirectory,
                                                                    "bullet_green.png")) as ImageSource;

            if (currencyObject.Change < 0)
                return new ImageSourceConverter().ConvertFromString(Path.Combine(Environment.CurrentDirectory,
                                                                                   AuxImagesDirectory,
                                                                                   "bullet_red.png")) as ImageSource;

            return new ImageSourceConverter().ConvertFromString(Path.Combine(Environment.CurrentDirectory,
                                                                           AuxImagesDirectory,
                                                                           "bullet_yellow.png")) as ImageSource;
        }

        /// <summary>
        /// Extension Method
        /// get the regioninfo, for getting the country coin in a name, for $ we will get US-DOLLAR
        /// this object will get us also the twolettersname (e.g: for israel  - 'IL')
        /// <see cref="SetLCIDByRegionName"/>
        /// <returns>RegionInfo</returns>
        /// </summary>
        public static RegionInfo SetRegionByCurrencyCode(this CurrencyObject currencyObject)
        {
            return CultureInfo.GetCultures(CultureTypes.SpecificCultures)
                     .Select(culture => new RegionInfo(culture.LCID))
                     .FirstOrDefault(regionInfo => regionInfo.ISOCurrencySymbol.Equals(currencyObject.Code));
        }


        /// <summary>
        /// Extension Method
        /// get the CurrencyNumberFormat number of the specific region
        /// this is for set NumberFormatInfo specific for every region e.g: this will help
        /// to get number in nicer format, instead of 10000, we will get 10,000
        /// <returns>A Number Format info, to customize the object Number format to the Region/country</returns>
        /// </summary>
        public static NumberFormatInfo SetLCIDByRegionName(this CurrencyObject currencyObject)
        {
            var lcidNumber = CultureInfo.GetCultures(CultureTypes.AllCultures)
                       .Where(c => c.Name.EndsWith(currencyObject.Region.TwoLetterISORegionName))
                       .Select(x => x.LCID).FirstOrDefault();
            return CultureInfo.GetCultureInfo(lcidNumber).NumberFormat;
        }

        public static IEnumerable<CurrencyObject> GetAllCountriesWithExcept(
            this ICurrencyContainer<string,CurrencyObject> source, string except)
        {
            return (from currencyItem in source.Container.Values
                where currencyItem.Country != except
                orderby currencyItem.Country ascending
                select currencyItem);
        }

        public static IEnumerable<String> GetNamesOfCountriesAndCode(
            this ICurrencyContainer<string, CurrencyObject> source)
        {
            return (from currencyItem in source.Container.Values
             orderby currencyItem.Country ascending
             select currencyItem.Country + "-" + currencyItem.Code);
        }
        #endregion

        #region Result Number Utility
        /// <summary>
        /// Check for validation of result of the convertion
        /// <param name="resultNumberToCheck">the result of the convertion</param>
        /// <returns>bool to define if the validation check is ok
        /// true - for failing in specify the number type(not a number, infinties, negative)
        /// false - validation completed sucsessfully  - valid number</returns>
        /// <see cref="CurrencyContainer.Calculate"/>
        /// </summary>
        public static bool CheckForInvalidResultNumber(double resultNumberToCheck)
        {
            //if infinity
            if (Double.IsInfinity(resultNumberToCheck))
                return true;

            //number is OK
            if (resultNumberToCheck > 0)
                return false;

            //if negative return true 
            if (resultNumberToCheck < 0)
                return true;

            //if not a number
            if (Double.IsNaN(resultNumberToCheck))
                return true;

            if (Double.IsPositiveInfinity(resultNumberToCheck))
                return true;

            if (Double.IsNegativeInfinity(resultNumberToCheck))
                return true;

            //cheking failed in all fields
            return true;
        }

        #endregion

        #region XML Utilty
        /// <summary>
        /// Method- checking on every XML fetching.
        /// comparing the dates of the last XML fetching to the current
        /// to avoid useless fetch and parse of xml, and GUI component
        /// for matching dates, e.g: same XML, with no change.
        /// <param name="fetchedXMLDate">the date of the fetched XML, to compare to the existing XML date</param>
        /// <returns>false - for first initlize of Program (the date is null in the first place), or there is 
        /// a new XML from BOI, after user try to update the currency data</returns>
        /// <returns>true - for equal dates, the one with the XML that was fetched to the current one</returns>
        /// </summary>
        public static bool IsXMLDatesEquals(string fetchedXMLDate)
        {
            //convert string to datetime
            var date = Convert.ToDateTime(fetchedXMLDate);
            //compare dates
            bool isEqual = !(CurrencyContainer.Instance.CurrenciesDate.CompareTo(date) < 0);

            if (isEqual)
                return true;
            else
            {
                CurrencyContainer.Instance.CurrenciesDate = date;
                return false;
            }
        }
        #endregion
    }
}