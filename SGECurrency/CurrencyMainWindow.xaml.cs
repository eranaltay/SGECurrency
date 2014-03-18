/* Copyright 2013 
 * Eran Altay, eranaltay@gmail.com
 
   Unless required by applicable law or agreed to in writing, software
   distributed under the License is distributed on an "AS IS" BASIS,
   WITHOUT WARRANTIES OR CONDITIONS OF ANY KIND, either express or implied.
   See the License for the specific language governing permissions and
   limitations under the License.
*/

using System;
using System.Collections;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Diagnostics;
using System.Linq;
using System.Reflection;
using System.Runtime.Remoting.Messaging;
using System.Threading;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Controls.Primitives;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Threading;
using SGECurrency.DAL;
using SGECurrency.Exceptions;
using SGECurrency.Managers;
using SGECurrency.Objects;
using SGECurrency.Utilities;
using log4net;
using log4net.Config;

namespace SGECurrency
{
    #region Delegates
    /// <summary>
    /// a generic delegate type which handle the asyncronious task of Fetching and getting XML/JSON/etc..
    /// <see cref="CurrencyMainWindow.GetBOIXMLAsynchronously"/>
    /// <returns>bool</returns>
    /// </summary>
    internal delegate T FetchAndParseDataAsynchronousDelegate<out T>();

    /// <summary>
    /// a delegate type, an event is attached to it, so we can throw GUI events 
    /// and notify the user about problems, and dialog boxes
    /// <see cref="XMLManager.MessagesToNotifyUserEvent"/>
    /// </summary>
    internal delegate void MessagesToNotifyUserDelegate(string message);

    /// <summary>
    /// a delegate- will invoke the
    /// <see cref="CurrencyMainWindow.LabelProgramStatusForUser"/>
    /// with an event from different parts of the program
    /// </summary>
    internal delegate void ShowProgramStatusDelegate(string message);

    /// <summary>
    /// a delegate - will update the label of the name of the Currecny.
    /// e.g: us dollars
    /// and will update the result on the input text box
    /// e.g: 7+5 will get us the result, 13 on  the text box
    /// </summary>
    internal delegate void UpdateResultOfCalculationDelegate(string toRegionInfo, string outputResult, string inputNumberToCalc);

    #endregion

    /// <summary>
    /// Main Window - Program starts here
    /// Initlize GUI
    /// Initlize Container
    /// Fetch And Parse Data From Server
    /// Listenres
    /// etc...
    /// </summary>
    public partial class CurrencyMainWindow : Window
    {
        #region Variables
        //for set the number of errors on input text box
        //is handles by 'CurrencyInputValidation'
        private int NoOfErrorsOnScreen { get; set; }
        //Container var
        private readonly ICurrencyContainer<string,CurrencyObject> _container;
        //event for invoking messages box fro user 
        internal event ShowProgramStatusDelegate ShowStatusEvent;
        //object of Log4Net, for debugging and Error Logs
        private readonly ILog _currencyLogger = LogManager.GetLogger(MethodBase.GetCurrentMethod().DeclaringType); 
        #endregion

        #region Startup Initializer
        /// <summary>
        /// Main window C-TOR
        /// </summary>
        public CurrencyMainWindow()
        {
            //configure 'Log4net.config'
            XmlConfigurator.Configure();
            //set info log
            _currencyLogger.Info("Initilize Program");
            InitializeComponent();
            //get the flags images for the combo boxes
            CurrencyUtilities.PopulateFlagImagesContainer();
            //instance of CurrrencyContatiner
            _container = CurrencyContainer.Instance;
            //register the event that will invoke the result of the of conversion
            CurrencyContainer.UpdateViewAfterCalculation += EndOfCalculation;
            //register the event that will invoke the status label of the program
            ShowStatusEvent += LabelProgramStatusForUser;
            //register the event that will invoke the messages box from the program to notify user
            XMLManager.MessagesToNotifyUserEvent += MessageBoxToNotifyUser;
            //get the XML by async task
            Title = "Loading...";
            GetBOIXMLAsynchronously();
            //set the Datacontext of the RatesGrid
            RatesDataGrid.DataContext = CurrencyUtilities.RatesGridObservableCollection;
            //set the inputValidator var to check for valid input from user
            var inputValidator = new CurrencyInputValidation();
            //attach the inputValidator var for the maingrid
            MainGrid.DataContext = inputValidator;
        }

        /// <summary>
        /// Initilze window on launch
        /// </summary>
        private void MainCurrencyWindowInitialized(object sender, EventArgs e)
        {
            //greeting message after startup
            ShowStatusEvent("Welcome To SGECurrency");
        }

        #endregion

        #region XML Fetch And Parse Business
        /// <summary>
        /// Async Task To get and fetch XML file from Bank Of Israel
        /// <see cref="XMLManager.PopulateContainer"/>
        /// </summary>
        public void GetBOIXMLAsynchronously()
        {
            //using clause - XMLManager inherits from WebClient which is Implemnting IDISPSABLE
            using (var fetchAndParseXML = new XMLManager())
            {
                //attach the XMLManger to the async delegate type
                //bool - returning type - for new XML or Same XML, so we don't update Data and GUI component unneccerily
                FetchAndParseDataAsynchronousDelegate<bool> asyncFetchAndParse = fetchAndParseXML.XMLProcess;
                //begin the async work, and register a callback func
                asyncFetchAndParse.BeginInvoke(AsynchronousTaskCallback, null);
            }
        }


        /// <summary>
        /// Async Task Callback method, update UI and finishing the Fetching And Parsing from
        /// BOI URL
        /// </summary>
        public void AsynchronousTaskCallback(IAsyncResult asyncResultCallback)
        {
            //get the result of the XMLManger
            var result = (AsyncResult) asyncResultCallback;
            //get boolean var
            var boiXMLDelegate = (FetchAndParseDataAsynchronousDelegate<bool>) result.AsyncDelegate;

            //check for the returning callback
            //if true, the XML's dates are equal and there is no need to get
            //new one, and initilize the GUI component, such the datagrid of the rates and the Comboboxes
            if (boiXMLDelegate.EndInvoke(asyncResultCallback))
            {
                ShowStatusEvent.Invoke("This Is The Latest Currencies Data");
                return;
            }

            //raising UI event in the middle of background thread - by Annonmyous method 
            //will reach here on the begining of the program or there is a newer XML then the current one.
            //avoid the "The calling thread cannot access this object because a different thread owns it" 
            //error, when trying to get to UI components trough another thread
            Dispatcher.Invoke(() =>
            {
                //attach the Currenecy Codes to the Combo boxes lists
                // e.g: Shekel-ILS
                // this will be match the names of the files on the .ico dir, so every selected item
                // will bring us the matching flag img
                //will also group the list to the combobox, and will sort it alphabeticly
                LeftComboBox.ItemsSource = CurrencyContainer.Instance.GetNamesOfCountriesAndCode().ToList();
                //attach to the other side of the currency combobox also
                RightComboBox.ItemsSource = LeftComboBox.ItemsSource;

                SetRatesGridAsObservable();

                //set the title with date of XML
                Title = "SGECurrency - Currencies Date: " + CurrencyContainer.Instance.CurrenciesDate.ToLongDateString();

                //set GUI componnet as enable           
                InputTextBox.IsEnabled = true;
                UpdateButton.IsEnabled = true;
                //attach local-db of components from previous launching
                InputTextBox.Text = Properties.Settings.Default.InputField;
                RightComboBox.SelectedIndex = Properties.Settings.Default.RightComboBox;
                LeftComboBox.SelectedIndex = Properties.Settings.Default.LeftComboBox;
                CalcButton.RaiseEvent(new RoutedEventArgs(ButtonBase.ClickEvent));
            });

        }

        /// <summary>
        /// Set The DataContext of the RatesDataGrid as observable collection, any new data will  
        /// fire this method and update Rates GUI columes. 
        /// </summary>
        private void SetRatesGridAsObservable()
        {
            CurrencyUtilities.RatesGridObservableCollection.Clear();
            //except deafult value - israel
            //set the ratedatagrid view of coins
            CurrencyContainer.Instance.GetAllCountriesWithExcept("Israel")
                .ToList()
                .ForEach(CurrencyUtilities.RatesGridObservableCollection.Add);
       }
        #endregion

        #region Componenet Listeners, Buttons, Combobox ,Keys
        /// <summary>
        /// listener to launch About Window
        /// <see cref="AboutAppWindow"/>
        /// </summary>
        private void OnClickAboutHyperLink(object sender, RoutedEventArgs e)
        {
            var aboutWindow = new AboutAppWindow();
            aboutWindow.ShowDialog();
        }


        /// <summary>
        /// Listener - every selection of one of the comboboxes will invok this func
        /// and will set the the right flag for the currenecy code.
        /// </summary>
        private void OnSelectedItemChangedFromComboBoxes(object leftOrRightComboBox, SelectionChangedEventArgs e)
        {
            //will hold the focused combobox.
            var chosenComboBox = leftOrRightComboBox as ComboBox;

            //will hold the selected key of the currenecy code e.g: Dollar-AUD
            Debug.Assert(chosenComboBox != null, "chosenComboBox != null");
            string selectedCode = chosenComboBox.SelectedItem.ToString();

            //get the img source, by the name of the file that holding the img file
            ImageSource imageFileObjectSource = CurrencyUtilities.FlagsImages[selectedCode]
                                                .FirstOrDefault();

            //will attach the img file to the Image component
            if (chosenComboBox.Equals(RightComboBox))
                RightFlagImage.Source = imageFileObjectSource;
            else
                LeftFlagImage.Source = imageFileObjectSource;

            //raise button event to calculate the exists input
            //if validation is falls, will not continue the invoke of the listener
            if (SwitchSideButton.IsEnabled) 
                CalcButton.RaiseEvent(new RoutedEventArgs(ButtonBase.ClickEvent));
        }

        /// <summary>
        /// listener - on mouse wheel move on flags images, change the index of combobox,
        ///  forward or backward
        /// </summary>
        private void OnMouseWheelChangeFlagImage(object sender, MouseWheelEventArgs e)
        {
            if (sender.Equals(RightFlagImage))
            {
                if (e.Delta < 0)
                    RightComboBox.SelectedIndex++;

                else if (e.Delta > 0)
                {
                    if (RightComboBox.SelectedIndex == 0)
                        return;
                    RightComboBox.SelectedIndex--;
                }
            }

            else
            {
                if (e.Delta < 0)
                    LeftComboBox.SelectedIndex++;

                else if (e.Delta > 0)
                {
                    if (LeftComboBox.SelectedIndex == 0)
                        return;
                    LeftComboBox.SelectedIndex--;
                }
            }
        }

        /// <summary>
        /// listener - if user deleted the input text box, the view will reset itself
        /// <see cref="ResetView"/>
        /// </summary>
        private void OnChangedInputTextBox(object sender, TextChangedEventArgs e)
        {
            //if the input is empty, will reset the view
            if (String.IsNullOrWhiteSpace(InputTextBox.Text))
                ResetView();
        }

        /// <summary>
        /// copy the result from the output text box
        /// </summary>
        private void OnClickOutputTextBoxContextMenu(object sender, RoutedEventArgs e)
        {
            //check for empty or nullness
            if (sender.Equals(CopyToClipboardMenuItem))
            {
                if (!String.IsNullOrWhiteSpace(OutputTextBox.Text))
                {
                    //save the text on clipboard
                    Clipboard.SetText(OutputTextBox.Text);
                }
            }
        }

        /// <summary>
        /// listener- catch 'Enter' Key event
        /// every 'Enter' key hit will raise the listener to get focus from 'Calc' button
        /// </summary>
        private void OnEnterKeyDown(object sender, KeyEventArgs e)
        {
            //hit on enter when window on focus will invoke the calc method
            if (e.Key == Key.Enter && CalcButton.IsEnabled)
                CalcButton.RaiseEvent(new RoutedEventArgs(ButtonBase.ClickEvent));
        }

        /// <summary>
        /// every hit on key of the keyboard will fire the focus on the input text box
        /// </summary>
        private void OnKeyDownMainWindow(object sender, TextCompositionEventArgs e)
        {
            //set the focus on Input Text when hit a key
            Keyboard.Focus(InputTextBox);
        }

        /// <summary>
        /// listener  - will inoke the checing for error on typing, no chars (A-Z, a-z)
        /// <see cref="CurrencyInputValidation.this"/>
        /// </summary>
        private void ValidationError(object sender, ValidationErrorEventArgs e)
        {
            if (e.Action == ValidationErrorEventAction.Added)
                NoOfErrorsOnScreen++;
            else
                NoOfErrorsOnScreen--;
        }

        /// <summary>
        /// Listener- check if calculation action can be execute
        /// </summary>
        private void ExchangeCalculationCanExecute(object sender, CanExecuteRoutedEventArgs e)
        {
            e.CanExecute = NoOfErrorsOnScreen == 0;
            e.Handled = true;
        }

        /// <summary>
        /// Listener- invoke click event on 'Update' button
        /// </summary>
        private void OnClickUpdateCurrenciesButton(object sender, RoutedEventArgs e)
        {
            InputTextBox.IsEnabled = false;
            //start to update
            GetBOIXMLAsynchronously();
            InputTextBox.IsEnabled = true;
        }

        /// <summary>
        /// Listener- invoke click event on 'Switch' button
        /// kind of swap
        /// </summary>
        private void OnClickSwitchSideButton(object sender, RoutedEventArgs e)
        {
            //set selected index of right side object
            var rightComboboxIndex = RightComboBox.SelectedIndex;
            //save the selected index of leftside
            RightComboBox.SelectedIndex = LeftComboBox.SelectedIndex;
            //and set the left side with the right side object
            LeftComboBox.SelectedIndex = rightComboboxIndex;
            //raise an event of calc operation
            CalcButton.RaiseEvent(new RoutedEventArgs(ButtonBase.ClickEvent));
        }
        
        private void OnWindowCloseEvent(object sender, System.ComponentModel.CancelEventArgs e)
        {
            Properties.Settings.Default.InputField = InputTextBox.Text;
            Properties.Settings.Default.RightComboBox = RightComboBox.SelectedIndex;
            Properties.Settings.Default.LeftComboBox = LeftComboBox.SelectedIndex;
            Properties.Settings.Default.Save();
        }
        #endregion

        #region Input Calculate Business
        /// <summary>
        /// Listener - hit on 'enter', 'calc' button, or change the currency rate
        /// will invoke this listener to calculate the amount.
        /// <see cref="DAL.CurrencyContainer.Calculate"/>
        /// </summary>
        private void OnCalcButtonClick(object sender, RoutedEventArgs e)
        {
            //if input empty don't continue invoke
            if (String.IsNullOrWhiteSpace(InputTextBox.Text)) return;

            //get the value from input text box
            var inputNumberToCalculate = InputTextBox.Text;

            try
            {
                // we get the key from the comboboxes, e.g:  from 'Dollar-AUD' to 'AUD' (this is the key 
                //for the object on the dictionary container).
                var fromSideComboBoxCode = LeftComboBox.SelectedItem.ToString().Split('-')[1];
                var toSideComboBoxCode = RightComboBox.SelectedItem.ToString().Split('-')[1];

                //get the currency objects, from the both sides
                var fromSideObject = CurrencyContainer.Instance[fromSideComboBoxCode];
                var toSideObject = CurrencyContainer.Instance[toSideComboBoxCode];

                //register calc method from Func delegate
                var calcMethod = new Func<CurrencyObject, CurrencyObject, string, object>(_container.Calculate);

                //call the Calculate Method
                OutputTextBox.Text =
                    calcMethod(fromSideObject, toSideObject, inputNumberToCalculate).ToString();
            }


            //catch of invalid input from user
            catch (CurrencyException invalidNumber)
            {
                _currencyLogger.Error(invalidNumber.Message, invalidNumber);
                ShowStatusEvent(invalidNumber.Message);
                EndOfCalculation(inputNumberAfterCalc:InputTextBox.Text);
            }

        }

        /// <summary>
        /// Event catch - When calculation ends, this method will update the textbox and labels
        /// <param name="convertedRegionInfo">string of RegionInfo - name of coin</param>
        /// <see cref="CurrencyObject.Region"/>
        /// <param name="inputNumberAfterCalc">number that the user entered to calc, after calculation
        /// showing the result on the input text box of the equation</param>
        /// </summary>
        private void EndOfCalculation(string convertedRegionInfo = "", string outputResult = "", string inputNumberAfterCalc = "")
        {
            CultureCoinName.Content = convertedRegionInfo;
            InputTextBox.Text = inputNumberAfterCalc;
            OutputTextBox.Text = outputResult;
            InputTextBox.SelectAll();
        }
        #endregion

        #region Notifications For User, ResetView, Updating Status of Program And MessageBox
        /// <summary>
        /// reset view after a new calc
        /// </summary>
        public void ResetView()
        {
            OutputTextBox.Text = String.Empty;
            CultureCoinName.Content = String.Empty;
        }


        /// <summary>
        /// Method- set the status label of the program, when error happend
        /// </summary>
        public void LabelProgramStatusForUser(string status)
        {
            //raise UI event
            //avoid the "The calling thread cannot access this object because a different thread owns it" 
            //error, when trying to get to UI components trough another thread
            Dispatcher.Invoke(() =>
            {
                ProgramStatus.Content = status;
                //Make the label visible, starting the storyboard.
                ProgramStatus.Visibility = Visibility.Visible;

                var dispatcherTimer = new DispatcherTimer {Interval = new TimeSpan(0, 0, 3)};
                //Set the timer interval to the length of the animation.
                dispatcherTimer.Tick += (EventHandler) delegate(object sender, EventArgs eventArgs)
                {
                    // The animation will be over now, collapse the label.
                    ProgramStatus.Visibility = Visibility.Collapsed;
                    // Get rid of the timer.
                    ((DispatcherTimer) sender).Stop();
                };
                dispatcherTimer.Start();
            });

        }

        /// <summary>
        /// method, will popup the user for custom message
        /// </summary>
        public void MessageBoxToNotifyUser(string message)
        {
            MessageBox.Show(message, "Notice", MessageBoxButton.OK, MessageBoxImage.Information,
               MessageBoxResult.None, MessageBoxOptions.None);
        }
        #endregion

    }
}
