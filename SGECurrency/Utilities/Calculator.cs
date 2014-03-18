/* Copyright 2013 
 * Eran Altay eranaltay@gmail.com
 
   Unless required by applicable law or agreed to in writing, software
   distributed under the License is distributed on an "AS IS" BASIS,
   WITHOUT WARRANTIES OR CONDITIONS OF ANY KIND, either express or implied.
   See the License for the specific language governing permissions and
   limitations under the License.
*/

using System;
using System.Text.RegularExpressions;
using SGECurrency.Exceptions;

namespace SGECurrency.Utilities
{
    /// <summary>
    /// This class will compute us the expression we want to calculate
    /// i.e will return us for 4+7 => 11
    /// </summary>
    static public class Calculator
    {
        #region Regex Operators

        private static readonly Regex BracketsRegex = new Regex(@"([a-z]*)\(([^\(\)]+)\)(\^|!?)", RegexOptions.Compiled);
        private static readonly Regex PowerRegex = new Regex(@"(-?\d+\.?\d*)\^(-?\d+\.?\d*)", RegexOptions.Compiled);
        private static readonly Regex MultiplyRegex = new Regex(@"(-?\d+\.?\d*)\*(-?\d+\.?\d*)", RegexOptions.Compiled);
        private static readonly Regex DivideRegex = new Regex(@"(-?\d+\.?\d*)/(-?\d+\.?\d*)", RegexOptions.Compiled);
        private static readonly Regex AddRegex = new Regex(@"(-?\d+\.?\d*)\+(-?\d+\.?\d*)", RegexOptions.Compiled);
        private static readonly Regex SubtractRegex = new Regex(@"(-?\d+\.?\d*)-(-?\d+\.?\d*)", RegexOptions.Compiled);
        #endregion

        #region Calculate Methods

        /// <summary>
        /// Calculate the Expression
        /// <returns>double</returns>
        /// </summary>
        public static double Calculate(string expression)
        {
            //remove empty chars
            double resultOfCalculation;
            expression = expression.Replace(" ", String.Empty).ToLower();

            //check for Brackets, so this will handle first, according to Math rules
            var m = BracketsRegex.Match(expression);
            while (m.Success)
            {
                expression = expression.Replace("(" + m.Groups[2].Value + ")", SetMatchingOperatorRegex(m.Groups[2].Value));
                m = BracketsRegex.Match(expression);
            }

            //trying to parse number after equation calc
            if (!Double.TryParse(SetMatchingOperatorRegex(expression), out resultOfCalculation))
                 throw new CurrencyException("Expression Invalid Format");

            //check for invalid result, and throw custom Exception
             if (CurrencyUtilities.CheckForInvalidResultNumber(resultOfCalculation))
                    throw new CurrencyException("Invalid Result Number");

            //return solved calculation of equation
            return resultOfCalculation;
        }

        /// <summary>
        /// compare the operator to do the calculation
        /// i.e: for the equation 
        /// If you combine several operators (+,-,*,/,^),
        /// it performs calculations in the following order: 
        ///Brackets (or parentheses) (look above for the brackets operation)
        ///Exponents.
        ///Multiplication and division.
        ///Addition and subtraction.
        /// <returns>string</returns>
        /// </summary>
        private static string SetMatchingOperatorRegex(string expression)
        {
            //regex of power, if '^' was found, start recursive call and calculate
            if (expression.IndexOf("^") != -1) expression = RecursiveSolveForMatchingRegex(PowerRegex, expression,
                x => Math.Pow(Convert.ToDouble(x.Groups[1].Value), Convert.ToDouble(x.Groups[2].Value)).ToString());

            //regex of divide, if '/' was found, start recursive call and calculate
            if (expression.IndexOf("/") != -1) expression = RecursiveSolveForMatchingRegex(DivideRegex, expression,
                x => (Convert.ToDouble(x.Groups[1].Value) / Convert.ToDouble(x.Groups[2].Value)).ToString());

            //regex of multiply, if '*' was found, start recursive call and calculate
            if (expression.IndexOf("*") != -1) expression = RecursiveSolveForMatchingRegex(MultiplyRegex, expression,
                x => (Convert.ToDouble(x.Groups[1].Value) * Convert.ToDouble(x.Groups[2].Value)).ToString());

            //regex of minus, if '-' was found, start recursive call and calculate
            if (expression.IndexOf("-") != -1) expression = RecursiveSolveForMatchingRegex(SubtractRegex, expression,
                x => (Convert.ToDouble(x.Groups[1].Value) - Convert.ToDouble(x.Groups[2].Value)).ToString());
            
            //regex of add, if '+' was found, start recursive call and calculate
            if (expression.IndexOf("+") != -1) expression = RecursiveSolveForMatchingRegex(AddRegex, expression,
                x => (Convert.ToDouble(x.Groups[1].Value) + Convert.ToDouble(x.Groups[2].Value)).ToString());

            return expression;
        }

        /// <summary>
        /// tail - recursive function to solve equation
        /// using generice Func<Match, string>
        /// </summary>
        private static string RecursiveSolveForMatchingRegex(Regex regex, string matchingExpression, Func<Match, string> func)
        {
            //match the ('^', '/', '+' , '-' , '(' , ')') regex of the equation
            MatchCollection collection = regex.Matches(matchingExpression);

            //count the collection of the matchingExpression, if there is no number to calculate, return the 
            //result
            if (collection.Count == 0) return matchingExpression;

            //get the equation from the matchcollection 
            for (int i = 0; i < collection.Count; i++)
                //i.e if the equation is '7+15+3', it will take '7+15', calculate it
                //and replace the '7+15' with '22', and eventualy '22+3'
                //and will continue the calculation until we get '25' which is '7+15+3'
                matchingExpression = matchingExpression.Replace(collection[i].Groups[0].Value, func(collection[i]));

            //recursive call with new operands
            //this call is tail-recursive, 
            //none of the recursive call do additional work after the recursive call is complete
            matchingExpression = RecursiveSolveForMatchingRegex(regex, matchingExpression, func);

            return matchingExpression;
        } 
        #endregion
    }
}
