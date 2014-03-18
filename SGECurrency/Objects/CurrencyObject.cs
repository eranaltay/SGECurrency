/* Copyright 2013 
 * Eran Altay eranaltay@gmail.com
 
   Unless required by applicable law or agreed to in writing, software
   distributed under the License is distributed on an "AS IS" BASIS,
   WITHOUT WARRANTIES OR CONDITIONS OF ANY KIND, either express or implied.
   See the License for the specific language governing permissions and
   limitations under the License.
*/

using System.Globalization;
using System.Windows.Media;
using SGECurrency.DAL;
using SGECurrency.Utilities;

namespace SGECurrency.Objects
{
    /// <summary>
    /// Object for every Coin
    /// Name
    /// Unit
    /// Rate
    /// Country
    /// Code
    /// Change
    /// </summary>
    public class CurrencyObject
    {
        //change field - e.g: -0.232%
        private double _change;
        //code field - e.g: USD
        private string _code;

        #region C-TOR'S
        /// <summary>
        /// C-TOR
        /// </summary>
        public CurrencyObject(string name, int unit, double rate, string country, string code, double change)
        {
            Name = name;
            Unit = unit;
            Rate = rate;
            Country = country;
            Code = code;
            Change = change;
        }

        /// <summary>
        /// C-TOR - without args
        /// </summary>
        public CurrencyObject() { } 
        #endregion

        #region Implicit Operator And Override Methods
        /// <summary>
        /// implicit double operator to get the rate exchange by object
        /// </summary>
        public static implicit operator double(CurrencyObject objectToCalc)
        {
            return objectToCalc.Rate / objectToCalc.Unit;
        }

        /// <summary>
        ///override toString(), get the currrency english name. e.g: US Dollar
        /// </summary>
        public override string ToString()
        {
            return Region.CurrencyEnglishName;
        } 
        #endregion

        #region Main Properties
        /// <summary>
        ///Name of the coin. e.g: Dollar
        /// </summary>
        public string Name { get; set; }

        /// <summary>
        ///unit of the coin, e.g: 1
        /// </summary>
        public int Unit { get; set; }

        /// <summary>
        ///rate of the exchange - e.g: 3.121
        /// </summary>
        public double Rate { get; set; }

        /// <summary>
        ///name of the country of the coin, e.g: israel
        /// </summary>
        public string Country { get; set; }

        /// <summary>
        /// set the code e.g: USD
        /// on the property we set also the Region And CurrencyNumberFormat, as they on private
        /// </summary>
        public string Code
        {
            get { return _code; }

            set
            {
                _code = value;
                //set the Region name by currency code, to get the Name of the currency coin
                Region = this.SetRegionByCurrencyCode();
                //set the Region name two letter of the ISO Region name, to get the number format to represent the number
                //with nicer and matching format to the region, including with the Symbol of the coin
                CurrencyNumberFormat = this.SetLCIDByRegionName();
            }

        }

        /// <summary>
        /// set the change of the currency e.g : -0.235
        /// on this property we set also the matching image for the change
        /// </summary>
        public double Change
        {
            get { return _change; }

            set
            {
                _change = value;
                 ChangeRateStatusImageSource = this.SetImageByCurrencyChangeRate();
            }
        }
        
        #endregion

        #region Extra Properties
        /// <summary>
        /// set the image of the change, if change is -0.212, the image will be arrow to buttom 
        /// <see cref="CurrencyUtilities.SetImageByCurrencyChangeRate"/>
        /// </summary>
        public ImageSource ChangeRateStatusImageSource { get; private set; }

        /// <summary>
        ///set the region by the currency code, to get the english name of the coin
        /// <see cref="CurrencyUtilities.SetRegionByCurrencyCode"/>
        /// </summary>
        public RegionInfo Region { get; private set; }


        /// <summary>
        ///set the CurrencyNumberFormat, to get the numberformat that matching to the region of the coin
        ///this will be get us a "nicer" format of number
        /// <see cref="CurrencyContainer.Calculate"/>
        /// <see cref="CurrencyUtilities.SetLCIDByRegionName"/>
        /// </summary>
        public NumberFormatInfo CurrencyNumberFormat { get; private set; } 
        #endregion
      
    }
}
