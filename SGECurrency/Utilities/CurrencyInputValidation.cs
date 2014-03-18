/* Copyright 2013 
 * Eran Altay eranaltay@gmail.com
 
   Unless required by applicable law or agreed to in writing, software
   distributed under the License is distributed on an "AS IS" BASIS,
   WITHOUT WARRANTIES OR CONDITIONS OF ANY KIND, either express or implied.
   See the License for the specific language governing permissions and
   limitations under the License.
*/

using System;
using System.ComponentModel;
using System.Text.RegularExpressions;

namespace SGECurrency.Utilities
{

    /// <summary>
    /// Validator - this class will check us the Input Textbox for valid numbers and signs
    /// if validation will fail, we cannot procced doing calculation, until we have a valid input
    /// </summary>
    class CurrencyInputValidation : IDataErrorInfo
    {
        public string Input { get; set; }
        //the only signs that are valid on the input text box
        private readonly Regex _inputValidatorExpression = new Regex("^[0-9.+-/*()^ ]+$", RegexOptions.Compiled);

        /// <summary>
        /// check for input validation, check it every hit on key, if the key doesn't match the signs on the REGEX
        /// this validation will fail, and a animation warning will popup
        /// </summary>
        public string this[string columnName]
        {
            get
            {
                if (!String.IsNullOrWhiteSpace(Input))
                {
                    //if the expression below will return this value - validation will fail
                    //and we cannot procced do our operation
                    if (!_inputValidatorExpression.IsMatch(Input) || Input.StartsWith("-"))
                        return "Invalid Keys";
                }
                return null;
            }

        }

        /// <summary>
        /// 
        /// </summary>
        public string Error { get { throw new NotImplementedException(); } }
    }
}
