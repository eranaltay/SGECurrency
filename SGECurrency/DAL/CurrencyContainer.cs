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
using System.ComponentModel;
using System.Globalization;
using System.Runtime.CompilerServices;
using System.Threading;
using System.Windows;
using SGECurrency.Annotations;
using SGECurrency.Objects;
using SGECurrency.Utilities;

namespace SGECurrency.DAL
{

    /// <summary>
    /// This Class has the Dictionary instance to hold all the Coins from the BOI.
    /// Implements the ICurrencyContainer
    /// <see cref="ICurrencyContainer{K,V}"/>
    /// </summary>
    public sealed class CurrencyContainer : ICurrencyContainer<string,CurrencyObject>
    {
        #region Var's
        //generic dictionary, to avoid the unboxing routin every accsess to the container
        private static IDictionary<string,CurrencyObject> _currencies;
        //instance of singleton pattern
        private static CurrencyContainer _instance;
        //event type to update result on some GUI component, like Name of coin in the label.
        internal static event UpdateResultOfCalculationDelegate UpdateViewAfterCalculation;

        #endregion

        #region C-TOR
        /// <summary>
        /// Singleton implementation - private C-TOR, and initilize it with a
        /// default value
        /// </summary>
        private CurrencyContainer()
        {
            //limited capacity, for better performance
            _currencies = new Dictionary<string, CurrencyObject>(30)
                {
                    {
                        //default value, always have to be on container
                        "ILS", new CurrencyObject
                            {
                                Name = "Shekel",
                                Unit = 1,
                                Code = "ILS",
                                Country = "Israel",
                                Rate = 1,
                                Change = 0
                            }

                    }
                };
        }
        
        #endregion

        /// <summary>
        /// Singleton implementation
        /// </summary>
        public static CurrencyContainer Instance
        {
            get
            {
                //Lazy initializer - for super performence, without using the Lock() object
                //The following implementation allows only a single thread to enter the critical area, 
                //which the lock block identifies, when no instance of Singleton has yet been created
                LazyInitializer.EnsureInitialized(ref _instance,
                    () => new CurrencyContainer());
                return _instance;
            }
        }

        /// <summary>
        /// Property - set and get the date from BOI XML file, Currencies date
        /// </summary>
        public DateTime CurrenciesDate
        {
            get; set;
        }


        #region Implemented Methods
        /// <summary>
        /// Calculate Method - will get the result of the conversion between two currencies
        /// <param name="fromSideObject"> from which currency we want to convert</param>
        /// <param name="toSideObject"> to which currency we want to convert</param>
        /// <param name="expression"> the expression we want to calculate</param>
        /// <returns>object, the result</returns>
        /// </summary>
        public object Calculate(CurrencyObject fromSideObject, CurrencyObject toSideObject, string expression)
        {
            //calculate the expression, and get double
            double resultOfCalculation = Calculator.Calculate(expression);

            //calculate result, using implicit operator on objects
            var convertedResult = resultOfCalculation / toSideObject * fromSideObject;

            //generic func<> delegate - set result in the apropiate format to show currency. e.g: $120.03 or  $120,323,54.000
            Func<NumberFormatInfo, double, string> convertToProperCurrencyView =
                (targetnumberFormat, calculatorResult) => String.Format(targetnumberFormat, "{0:C3}", calculatorResult);
 
            //throw event to update some GUI component, send the name of the coin, e.g: New Israel Shekel and the result of the calculation
            //if the calculation request was 5+8, will return 13.
            UpdateViewAfterCalculation(toSideObject.ToString(), inputNumberToCalc:resultOfCalculation.ToString(CultureInfo.InvariantCulture), outputResult:"");

            //return the converted request in proper currency view
            return convertToProperCurrencyView(toSideObject.CurrencyNumberFormat,convertedResult);
        }

        /// <summary>
        /// generic - GetEnumerator method
        /// </summary>
        public IEnumerator<CurrencyObject> GetEnumerator()
        {
            foreach (string currencyKey in _currencies.Keys)
            {
                yield return _currencies[currencyKey];
            }
        }

    
        /// <summary>
        /// Property - Indexer
        /// </summary>
        public CurrencyObject this[string key]
        {
            get { return _currencies[key]; }
            set { _currencies[key] = value; }
        }

        /// <summary>
        /// Property  - Implemnted Method- get the container
        /// </summary>
        public IDictionary<string, CurrencyObject> Container
        {
            get { return _currencies; }
        }
        #endregion

    }
}
