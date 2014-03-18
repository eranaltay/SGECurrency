/* Copyright 2013 
 * Eran Altay eranaltay@gmail.com
 
   Unless required by applicable law or agreed to in writing, software
   distributed under the License is distributed on an "AS IS" BASIS,
   WITHOUT WARRANTIES OR CONDITIONS OF ANY KIND, either express or implied.
   See the License for the specific language governing permissions and
   limitations under the License.
*/

using System;
using System.Runtime.Serialization;

namespace SGECurrency.Exceptions
{
    #region Exception C-TORS
    /// <summary>
    /// Custom Exception
    /// </summary>
    [Serializable]
    public class CurrencyException : Exception
    {
        /// <summary>
        /// no params C-TOR
        /// </summary>
        public CurrencyException()
            : base()
        {

        }

        /// <summary>
        /// 
        /// </summary>
        public CurrencyException(string message, Exception innerException)
            : base(message, innerException)
        {
        }

        /// <summary>
        /// 
        /// </summary>
        public CurrencyException(string message)
            : base(message)
        {

        }

        /// <summary>
        /// 
        /// </summary>
        protected CurrencyException(SerializationInfo info, StreamingContext context)
            : base(info, context)
        {

        }
    } 
    #endregion
}
