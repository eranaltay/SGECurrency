/* Copyright 2013 
 * Eran Altay eranaltay@gmail.com
 
   Unless required by applicable law or agreed to in writing, software
   distributed under the License is distributed on an "AS IS" BASIS,
   WITHOUT WARRANTIES OR CONDITIONS OF ANY KIND, either express or implied.
   See the License for the specific language governing permissions and
   limitations under the License.
*/

using System.Collections.Generic;
using System.Windows;

namespace SGECurrency.DAL
{

    /// <summary>
    /// Interface, so we can implemet it for every use, i.e: another data fetching from X bank
    /// or currencies data
    /// </summary>
    public interface ICurrencyContainer<K, V>
        
    {
        /// <summary>
        /// The calculation method
        /// <param name="fromSide">from currency code</param>
        /// <param name="toSide">to currency code</param>
        /// <param name="expression">expresion to calculate</param>
        /// <returns>object, result</returns>
        /// <see cref="CurrencyContainer.Calculate"/>
        /// </summary>
        object Calculate(V fromSideObject, V toSideObject, string expression);

        /// <summary>
        /// Property  -Indexer
        /// </summary>
        V this[K key] { get; set; }

         /// <summary>
         /// property for generics Container
         /// </summary>
         IDictionary<K, V> Container { get; }
    }
}
