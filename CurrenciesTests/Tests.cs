using System;
using System.IO;
using System.Linq;
using System.Xml.Linq;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using SGECurrency.Managers;
using SGECurrency.Objects;
using SGECurrency.Utilities;

namespace CurrenciesTests
{
    [TestClass]
    public class Tests
    {
        private string _xmlcontent;
        private XElement _bankOfIsraelXML;

        [TestMethod]
        public void XMLElementsTest()
        {
            var testXML = new XMLManager();
            testXML.FetchAndParseBOIXML(out _bankOfIsraelXML, out _xmlcontent);

            //act
            bool actual = _bankOfIsraelXML.HasElements;
            // assert
            Assert.AreEqual(true, actual);

            //actual list from BOI
            var listOfCurrencies = (from currencyElement in _bankOfIsraelXML.Descendants("CURRENCY")
                   select new CurrencyObject
                       {
                           Name = (string) currencyElement.Element("NAME"),
                           Code = (string) currencyElement.Element("CURRENCYCODE"),
                           Country = (string) currencyElement.Element("COUNTRY")
                       }).ToList();

            foreach (var currencyObject in listOfCurrencies)
            {
               Console.WriteLine(currencyObject.ToString());
            }

            Console.WriteLine(@"******************************************");


            //actual files number
           int numOfFiles =  new DirectoryInfo(@"C:\Users\EranAltay\Documents\Visual Studio 2012\Projects\SGECurrency\SGECurrency\bin\Debug\Flags")
                //get the files from the '~\CurrencyConverter\CurrencyConverter\bin\Debug\ico'
                .GetFiles(CurrencyUtilities.SupportedExtensionFlagsImages, SearchOption.TopDirectoryOnly).Count();
            

           //this will gurentee us the constancy between the flags images files and number of currencies from BOI
            //
           Assert.AreEqual(listOfCurrencies.Count , numOfFiles-1);
           Console.WriteLine(@"Number Of Flags Images Is Matching To Number Of Currencies");
        }



        [TestMethod]
        public void FormulaCalculationTest()
        {
            //arrange
            string equation = "4*5";

            double actual = Calculator.Calculate(equation);

            const double expected = 20;

            Assert.AreEqual(expected, actual);
            Console.WriteLine(equation + "= " + expected);

        }
    }
}
