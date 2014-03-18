/* Copyright 2013 
 * Eran Altay eranaltay@gmail.com
 
   Unless required by applicable law or agreed to in writing, software
   distributed under the License is distributed on an "AS IS" BASIS,
   WITHOUT WARRANTIES OR CONDITIONS OF ANY KIND, either express or implied.
   See the License for the specific language governing permissions and
   limitations under the License.
*/

using System;
using System.Windows.Data;
using System.Windows.Media;

namespace SGECurrency.Utilities
{
    /// <summary>
    /// converter for the 'change' field on the DataGrid of the Rates Tab
    /// for negative number, will return red, otherwise return green
    /// </summary>
    class CurrencyChangeValueConverter : IValueConverter
    {
        /// <summary>
        /// set the text color according to the change value for the datagridview on the 'Rates' tab item, on the Change column
        /// so there will be an text effect that will describe the change value
        /// </summary>
        public object Convert(object value, Type targetType, object parameter, System.Globalization.CultureInfo culture)
        {
            if (value.ToString() == String.Empty)
            return value;

            var change = System.Convert.ToDouble(value);

            if (change > 0)
                return Brushes.ForestGreen;

            if(change < 0)
                return Brushes.Red;

            if (change == 0)
                return Brushes.DarkOrange;


            return Binding.DoNothing;
        }

        /// <summary>
        /// Convert Back Method
        ///  </summary>
        public object ConvertBack(object value, Type targetType, object parameter, System.Globalization.CultureInfo culture)
        {
           throw new NotImplementedException();
        }

    }
}
