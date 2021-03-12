using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
using System.Windows.Shapes;
using System.ComponentModel;
using Problems;

namespace Euler_WPF
{
    /// <summary>
    /// Interaction logic for Page1.xaml
    /// </summary>
    public partial class ProblemSelectionPage : Page
    {

        BackgroundWorker ProblemLoadingWorker;

        public ProblemSelectionPage()
        {
            InitializeComponent();

            // Create ProblemLoadingWorker (backgroundworker)
            ProblemLoadingWorker = new BackgroundWorker
            {
                WorkerReportsProgress = true
            };
            ProblemLoadingWorker.DoWork += ProblemLoadingWorker_DoWork;
            ProblemLoadingWorker.RunWorkerCompleted += ProblemLoadingWorker_RunWorkerCompleted;
            ProblemLoadingWorker.ProgressChanged += ProblemLoadingWorker_ProgressChanged;

            ProblemData.LoadAtLeast(10);
            ProblemListBox_LoadAll();
        }

        



        #region Problem ListBox


        // Handles selection from the PLB
        private void ProblemListBox_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            ProblemDescriptionBorder.Child = ProblemData.Problems[ProblemListBox.SelectedIndex + 1].GetDescriptionSP();
        }


        // Handles scolling when the mouse is inside of the listbox
        private void ProblemListBox_PreviewMouseWheel(Object sender, MouseWheelEventArgs e)
        {
            ProblemListScrollViewer.ScrollToVerticalOffset(ProblemListScrollViewer.VerticalOffset - e.Delta / 3);
        }


        #region Problem loading


        // Handles the clicking of the "LoadMore" Button
        private void LoadMoreProblemsButton_Click(object sender, RoutedEventArgs e)
        {
            LoadMoreProblems(10);
        }


        // Handles loading new problems (starts up ProblemLoadingWorker + sets loading bar)
        private void LoadMoreProblems(int loadAm)
        {
            if (ProblemLoadingWorker.IsBusy != true)
            {
                // Generate loading bar
                LoadTenMoreButton.Content = GetNewLoadingBar(0);

                // Start the asynchronous operation.
                ProblemLoadingWorker.RunWorkerAsync(loadAm);
            }
        }


        // This gets executed by the ProblemLoadingWorker
        private void ProblemLoadingWorker_DoWork(object sender, DoWorkEventArgs e)
        {
            int loadAm = (int)(e.Argument);

            // For each problem
            foreach (int i in Enumerable.Range(0, loadAm))
            {
                // Load using ProblemData function
                ProblemData.LoadNext(1);
                // Report progress (updates loading bar)
                (sender as BackgroundWorker).ReportProgress(i * 100 / loadAm);
            }
        }


        // This function handles updating the UI when progress was made by the ProblemLoadingWorker
        private void ProblemLoadingWorker_ProgressChanged(object sender, ProgressChangedEventArgs e)
        {
            int loadPercent = (e.ProgressPercentage);
            Grid G = ((LoadTenMoreButton.Content as Border).Child as Grid);
            G.ColumnDefinitions[0].Width = new GridLength(loadPercent, GridUnitType.Star);
            G.ColumnDefinitions[1].Width = new GridLength(100 - loadPercent, GridUnitType.Star);

            ProblemListBox_LoadAll();
        }


        // This event handler deals with the results of the background operation.
        private void ProblemLoadingWorker_RunWorkerCompleted(object sender, RunWorkerCompletedEventArgs e)
        {
            // Remove loading bar
            LoadTenMoreButton.Content = new TextBlock()
            {
                Text = "Load 10 more",
                HorizontalAlignment = HorizontalAlignment.Center
            };
        }


        // Loads every non-loaded problem into the ProblemListBox
        private void ProblemListBox_LoadAll()
        {
            // For every non-loaded problem in ProblemData.Problems (pI = 0-based index)
            for (int pI = ProblemListBox.Items.Count; pI < ProblemData.Problems.Count; pI++)
            {
                // Data prep
                int pN = pI + 1;
                Problem P = ProblemData.Problems[pN];

                // ListBoxItem' content is a StackPanel
                StackPanel SP = new StackPanel();
                SP.Orientation = Orientation.Horizontal;
                SP.Children.Add(new TextBlock() { Text = $"{pN}: " });
                SP.Children.Add(new TextBlock() { Text = P.Title });

                // LBI
                ListBoxItem LBI = new ListBoxItem()
                {
                    Content = SP
                };

                // Add LBI to PLB
                ProblemListBox.Items.Add(LBI);
            }
            // Scroll PLB to bottom
            ProblemListScrollViewer.ScrollToBottom();
        }


        #endregion


        #endregion


        // Gets a new loading bar inside a Border
        private Border GetNewLoadingBar(int initialLoadPercent)
        {
            Border Bar = new Border()
            {
                BorderBrush = new SolidColorBrush(Colors.Black),
                BorderThickness = new Thickness(1),
            };

            Grid G = new Grid();
            G.ColumnDefinitions.Add(new ColumnDefinition()
            {
                Width = new GridLength(initialLoadPercent, GridUnitType.Star)
            });
            G.ColumnDefinitions.Add(new ColumnDefinition()
            {
                Width = new GridLength(100 - initialLoadPercent, GridUnitType.Star)
            });

            Rectangle R = new Rectangle()
            {
                Height = 15,
                Fill = new SolidColorBrush(Colors.Black)
            };

            G.Children.Add(R);

            Bar.Child = G;

            return Bar;
        }
    }
}
